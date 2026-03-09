import Foundation
import Network

extension Notification.Name {
    static let socketClientConnected    = Notification.Name("socketClientConnected")

    static let socketClientDisconnected = Notification.Name("socketClientDisconnected")

    static let socketMessageReceived    = Notification.Name("socketMessageReceived")
}

private enum EIOPacketType: Int {
    case open    = 0
    case close   = 1
    case ping    = 2
    case pong    = 3
    case message = 4
    case upgrade = 5
    case noop    = 6
}

private enum SIOPacketType: Int {
    case connect      = 0
    case disconnect   = 1
    case event        = 2
    case ack          = 3
    case connectError = 4
    case binaryEvent  = 5
    case binaryAck    = 6
}

final class SocketServer {
    private(set) var port: UInt16 = 0

    let accountID: UUID
    let username: String
    let password: String

    private let pingInterval: Int = 25_000
    private let pingTimeout:  Int = 20_000

    /// Called (on the internal queue) when the last client disconnects
    /// after at least one client had connected.
    var onAllClientsDisconnected: ((UUID) -> Void)?

    private var listener: NWListener?
    private var connections: [NWConnection] = []
    private var pingTimers: [ObjectIdentifier: DispatchSourceTimer] = [:]
    private let queue: DispatchQueue

    /// Tracks whether at least one WebSocket client has successfully connected.
    private var hasConnectedOnce = false

    init(accountID: UUID, username: String, password: String) {
        self.accountID = accountID
        self.username  = username
        self.password  = password
        self.queue     = DispatchQueue(label: "socketserver.\(accountID.uuidString)", qos: .userInitiated)
    }

    @discardableResult
    func start() throws -> UInt16 {
        dispatchPrecondition(condition: .notOnQueue(.main))

        let params = NWParameters(tls: nil)
        params.allowLocalEndpointReuse = true

        let wsOptions = NWProtocolWebSocket.Options()
        wsOptions.autoReplyPing = true
        params.defaultProtocolStack.applicationProtocols.insert(wsOptions, at: 0)

        let nwListener = try NWListener(using: params, on: .any)
        self.listener = nwListener

        let semaphore = DispatchSemaphore(value: 0)
        var startError: Error?

        nwListener.stateUpdateHandler = { [weak self] state in
            switch state {
            case .ready:
                if let assignedPort = nwListener.port {
                    self?.port = assignedPort.rawValue
                }
                semaphore.signal()
            case .failed(let error):
                startError = error
                semaphore.signal()
            case .cancelled:
                semaphore.signal()
            default:
                break
            }
        }

        nwListener.newConnectionHandler = { [weak self] connection in
            self?.acceptConnection(connection)
        }

        nwListener.start(queue: queue)

        let timeout = semaphore.wait(timeout: .now() + 5)
        if timeout == .timedOut {
            nwListener.cancel()
            throw NSError(domain: "SocketServer", code: -1,
                          userInfo: [NSLocalizedDescriptionKey:
                                        "Timed out waiting for WebSocket listener"])
        }
        if let err = startError { throw err }

        print("[SocketServer] Listening on port \(port) for \(username) (Socket.IO EIO=4)")
        return port
    }
    
    func stop() {
        for conn in connections { conn.cancel() }
        connections.removeAll()
        for (_, timer) in pingTimers { timer.cancel() }
        pingTimers.removeAll()
        listener?.cancel()
        listener = nil
        print("[SocketServer] Stopped server on port \(port) for \(username)")
    }

    func emitEvent(_ event: String, data: Any, to connection: NWConnection) {
        guard let jsonData = try? JSONSerialization.data(withJSONObject: [event, data]),
              let jsonString = String(data: jsonData, encoding: .utf8) else { return }
        sendText("42\(jsonString)", to: connection)
    }

    func broadcastEvent(_ event: String, data: Any) {
        for conn in connections {
            emitEvent(event, data: data, to: conn)
        }
    }

    private func acceptConnection(_ connection: NWConnection) {
        connections.append(connection)

        connection.stateUpdateHandler = { [weak self] state in
            guard let self = self else { return }
            switch state {
            case .ready:
                print("[SocketServer:\(self.port)] WebSocket client connected")
                self.hasConnectedOnce = true
                self.performEngineIOHandshake(connection)
                NotificationCenter.default.post(
                    name: .socketClientConnected, object: nil,
                    userInfo: ["accountID": self.accountID, "port": self.port])
            case .failed, .cancelled:
                self.removeConnection(connection)
            default:
                break
            }
        }

        connection.start(queue: queue)
    }
    
    private func performEngineIOHandshake(_ connection: NWConnection) {
        let sid = generateSID()

        let openPayload: [String: Any] = [
            "sid": sid,
            "upgrades": [],
            "pingInterval": pingInterval,
            "pingTimeout": pingTimeout
        ]
        if let json = try? JSONSerialization.data(withJSONObject: openPayload),
           let jsonStr = String(data: json, encoding: .utf8) {
            sendText("\(EIOPacketType.open.rawValue)\(jsonStr)", to: connection)
        }

        let connectPayload: [String: Any] = ["sid": sid]
        if let json = try? JSONSerialization.data(withJSONObject: connectPayload),
           let jsonStr = String(data: json, encoding: .utf8) {
            sendText("4\(SIOPacketType.connect.rawValue)\(jsonStr)", to: connection)
        }

        receiveLoop(connection)
        startPingTimer(for: connection)
    }

    private func sendText(_ text: String, to connection: NWConnection) {
        let metadata = NWProtocolWebSocket.Metadata(opcode: .text)
        let context  = NWConnection.ContentContext(
            identifier: "socketio",
            metadata: [metadata]
        )
        guard let data = text.data(using: .utf8) else { return }
        connection.send(content: data, contentContext: context,
                        isComplete: true,
                        completion: .contentProcessed { error in
            if let error = error {
                print("[SocketServer:\(self.port)] WS send error: \(error)")
            }
        })
    }

    private func receiveLoop(_ connection: NWConnection) {
        connection.receiveMessage { [weak self] data, context, isComplete, error in
            guard let self = self else { return }

            if let data = data, !data.isEmpty,
               let text = String(data: data, encoding: .utf8) {
                self.handlePacket(text, from: connection)
            }

            if let error = error {
                print("[SocketServer:\(self.port)] WS receive error: \(error)")
                self.removeConnection(connection)
            } else {
                self.receiveLoop(connection)
            }
        }
    }
    
    private func handlePacket(_ packet: String, from connection: NWConnection) {
        guard let firstChar = packet.first else { return }

        if let eioType = EIOPacketType(rawValue: Int(String(firstChar)) ?? -1) {
            switch eioType {
            case .ping:
                let body = String(packet.dropFirst())
                sendText("\(EIOPacketType.pong.rawValue)\(body)", to: connection)

            case .pong:
                break

            case .message:
                let sioPayload = String(packet.dropFirst())
                handleSIOPacket(sioPayload, from: connection)

            case .close:
                removeConnection(connection)

            default:
                break
            }
            return
        }

        if firstChar == "{" || firstChar == "[" {
            handleRawJSON(packet, from: connection)
            return
        }

        print("[SocketServer:\(port)] Unknown packet: \(packet.prefix(80))")
    }
    
    private func handleRawJSON(_ text: String, from connection: NWConnection) {
        guard let data = text.data(using: .utf8),
              let dict = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            print("[SocketServer:\(port)] Failed to parse raw JSON: \(text.prefix(80))")
            return
        }

        let action = dict["action"] as? String ?? "unknown"

        print("[SocketServer:\(port)] Raw JSON action '\(action)'")

        NotificationCenter.default.post(
            name: .socketMessageReceived, object: nil,
            userInfo: ["accountID": accountID,
                       "event": action,
                       "args": [dict]])
                       
        switch action {
        case "get_credentials":
            let payload: [String: Any] = ["action": "credentials",
                                           "username": username,
                                           "password": password]
            if let jsonData = try? JSONSerialization.data(withJSONObject: payload),
               let jsonStr = String(data: jsonData, encoding: .utf8) {
                sendText(jsonStr, to: connection)
            }
        default:
            break
        }
    }

    private func handleSIOPacket(_ payload: String, from connection: NWConnection) {
        guard let firstChar = payload.first,
              let sioType = SIOPacketType(rawValue: Int(String(firstChar)) ?? -1) else {
            return
        }

        switch sioType {
        case .connect:
            let sid = generateSID()
            let ack: [String: Any] = ["sid": sid]
            if let json = try? JSONSerialization.data(withJSONObject: ack),
               let jsonStr = String(data: json, encoding: .utf8) {
                sendText("4\(SIOPacketType.connect.rawValue)\(jsonStr)", to: connection)
            }

        case .event:
            let jsonPart = String(payload.dropFirst())
            handleEvent(jsonPart, from: connection)

        case .disconnect:
            removeConnection(connection)

        default:
            break
        }
    }

    private func handleEvent(_ json: String, from connection: NWConnection) {
        guard let data = json.data(using: .utf8),
              let array = try? JSONSerialization.jsonObject(with: data) as? [Any],
              let eventName = array.first as? String else { return }

        let args = Array(array.dropFirst())

        print("[SocketServer:\(port)] Event '\(eventName)' args: \(args)")

        NotificationCenter.default.post(
            name: .socketMessageReceived, object: nil,
            userInfo: ["accountID": accountID,
                       "event": eventName,
                       "args": args])

        switch eventName {
        case "ping":
            emitEvent("pong", data: [:], to: connection)
        default:
            break
        }
    }

    private func startPingTimer(for connection: NWConnection) {
        let key = ObjectIdentifier(connection)
        let timer = DispatchSource.makeTimerSource(queue: queue)
        timer.schedule(deadline: .now() + .milliseconds(pingInterval),
                       repeating: .milliseconds(pingInterval))
        timer.setEventHandler { [weak self] in
            self?.sendText("\(EIOPacketType.ping.rawValue)", to: connection)
        }
        timer.resume()
        pingTimers[key] = timer
    }

    private func generateSID() -> String {
        UUID().uuidString
            .replacingOccurrences(of: "-", with: "")
            .prefix(20)
            .lowercased()
    }

    private func removeConnection(_ connection: NWConnection) {
        connection.cancel()
        connections.removeAll { $0 === connection }

        let key = ObjectIdentifier(connection)
        pingTimers[key]?.cancel()
        pingTimers.removeValue(forKey: key)

        print("[SocketServer:\(port)] Client disconnected")
        NotificationCenter.default.post(
            name: .socketClientDisconnected, object: nil,
            userInfo: ["accountID": accountID, "port": port])

        if connections.isEmpty && hasConnectedOnce {
            print("[SocketServer:\(port)] All clients disconnected – triggering process cleanup")
            onAllClientsDisconnected?(accountID)
        }
    }
}
