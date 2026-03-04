import Foundation
import Network

// MARK: - Notifications

extension Notification.Name {
    /// Posted when a game client connects to a socket server.
    /// userInfo: ["accountID": UUID, "port": UInt16]
    static let socketClientConnected    = Notification.Name("socketClientConnected")

    /// Posted when a game client disconnects.
    /// userInfo: ["accountID": UUID, "port": UInt16]
    static let socketClientDisconnected = Notification.Name("socketClientDisconnected")

    /// Posted when a Socket.IO event is received from the game.
    /// userInfo: ["accountID": UUID, "event": String, "args": [Any]]
    static let socketMessageReceived    = Notification.Name("socketMessageReceived")
}

// MARK: - Engine.IO / Socket.IO packet types

/// Engine.IO v4 packet types (first character of every wire message).
private enum EIOPacketType: Int {
    case open    = 0
    case close   = 1
    case ping    = 2
    case pong    = 3
    case message = 4
    case upgrade = 5
    case noop    = 6
}

/// Socket.IO v4 packet types (first character after the EIO "4" prefix).
private enum SIOPacketType: Int {
    case connect      = 0
    case disconnect   = 1
    case event        = 2
    case ack          = 3
    case connectError = 4
    case binaryEvent  = 5
    case binaryAck    = 6
}

// MARK: - SocketServer (Socket.IO / EIO=4 over WebSocket)

/// A per-account Socket.IO–compatible WebSocket server.
/// The game connects via `ws://127.0.0.1:<port>/socket.io/?EIO=4&transport=websocket`
/// and speaks the standard Engine.IO v4 + Socket.IO v4 wire protocol.
final class SocketServer {

    // MARK: Public properties

    /// The TCP port the server is listening on (available after `start()`).
    private(set) var port: UInt16 = 0

    /// Account metadata – sent to the game on demand.
    let accountID: UUID
    let username: String
    let password: String

    // MARK: Configurable timings (ms)

    private let pingInterval: Int = 25_000
    private let pingTimeout:  Int = 20_000

    // MARK: Private state

    private var listener: NWListener?
    private var connections: [NWConnection] = []
    private var pingTimers: [ObjectIdentifier: DispatchSourceTimer] = [:]
    private let queue: DispatchQueue

    // MARK: Init

    init(accountID: UUID, username: String, password: String) {
        self.accountID = accountID
        self.username  = username
        self.password  = password
        self.queue     = DispatchQueue(label: "socketserver.\(accountID.uuidString)",
                                       qos: .userInitiated)
    }

    // MARK: - Lifecycle

    /// Start the WebSocket listener on a random available port.
    /// Blocks until the port is known. Must NOT be called on the main thread.
    @discardableResult
    func start() throws -> UInt16 {
        dispatchPrecondition(condition: .notOnQueue(.main))

        // Build NWParameters with WebSocket on top of TCP.
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

    /// Gracefully shut down the server and all connections.
    func stop() {
        for conn in connections { conn.cancel() }
        connections.removeAll()
        for (_, timer) in pingTimers { timer.cancel() }
        pingTimers.removeAll()
        listener?.cancel()
        listener = nil
        print("[SocketServer] Stopped server on port \(port) for \(username)")
    }

    // MARK: - Public emit helpers

    /// Emit a Socket.IO event to a single connection.
    func emitEvent(_ event: String, data: Any, to connection: NWConnection) {
        guard let jsonData = try? JSONSerialization.data(withJSONObject: [event, data]),
              let jsonString = String(data: jsonData, encoding: .utf8) else { return }
        // 4 = EIO message, 2 = SIO event  →  "42[...]"
        sendText("42\(jsonString)", to: connection)
    }

    /// Broadcast a Socket.IO event to every connected client.
    func broadcastEvent(_ event: String, data: Any) {
        for conn in connections {
            emitEvent(event, data: data, to: conn)
        }
    }

    // MARK: - Connection handling

    private func acceptConnection(_ connection: NWConnection) {
        connections.append(connection)

        connection.stateUpdateHandler = { [weak self] state in
            guard let self = self else { return }
            switch state {
            case .ready:
                print("[SocketServer:\(self.port)] WebSocket client connected")
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

    // MARK: - Engine.IO handshake

    private func performEngineIOHandshake(_ connection: NWConnection) {
        let sid = generateSID()

        // 1) EIO OPEN – tells the client about session id & timings
        let openPayload: [String: Any] = [
            "sid": sid,
            "upgrades": [],                      // already on WS, nothing to upgrade to
            "pingInterval": pingInterval,
            "pingTimeout": pingTimeout
        ]
        if let json = try? JSONSerialization.data(withJSONObject: openPayload),
           let jsonStr = String(data: json, encoding: .utf8) {
            sendText("\(EIOPacketType.open.rawValue)\(jsonStr)", to: connection)
        }

        // 2) SIO CONNECT ack for the default namespace "/"
        let connectPayload: [String: Any] = ["sid": sid]
        if let json = try? JSONSerialization.data(withJSONObject: connectPayload),
           let jsonStr = String(data: json, encoding: .utf8) {
            sendText("4\(SIOPacketType.connect.rawValue)\(jsonStr)", to: connection)
        }

        // 3) Start the receive loop & periodic ping timer
        receiveLoop(connection)
        startPingTimer(for: connection)
    }

    // MARK: - WebSocket send / receive

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
                // Keep listening
                self.receiveLoop(connection)
            }
        }
    }

    // MARK: - Engine.IO packet dispatch

    private func handlePacket(_ packet: String, from connection: NWConnection) {
        guard let firstChar = packet.first else { return }

        // Try Engine.IO packet (starts with a digit 0-6)
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

        // Fallback: raw JSON message (game sends {"action":"updateInfo", ...})
        if firstChar == "{" || firstChar == "[" {
            handleRawJSON(packet, from: connection)
            return
        }

        print("[SocketServer:\(port)] Unknown packet: \(packet.prefix(80))")
    }

    // MARK: - Raw JSON handling (non–Socket.IO messages)

    /// The game may send plain JSON objects over WebSocket instead of
    /// using the Engine.IO / Socket.IO wire protocol.  We detect them
    /// here and route them through the same notification path.
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

        // Built-in handlers for raw JSON
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

    // MARK: - Socket.IO packet dispatch

    private func handleSIOPacket(_ payload: String, from connection: NWConnection) {
        guard let firstChar = payload.first,
              let sioType = SIOPacketType(rawValue: Int(String(firstChar)) ?? -1) else {
            return
        }

        switch sioType {
        case .connect:
            // Client confirming connect – reply with ack + sid
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

    // MARK: - Socket.IO event handling

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

        // Built-in handlers – extend as needed
        switch eventName {
        case "get_credentials":
            emitEvent("credentials",
                      data: ["username": username, "password": password],
                      to: connection)
        case "ping":
            emitEvent("pong", data: [:], to: connection)
        case "updateInfo":
            // Character info update – already forwarded via notification above
            break
        default:
            break
        }
    }

    // MARK: - Ping timer (server → client)

    private func startPingTimer(for connection: NWConnection) {
        let key = ObjectIdentifier(connection)
        let timer = DispatchSource.makeTimerSource(queue: queue)
        timer.schedule(deadline: .now() + .milliseconds(pingInterval),
                       repeating: .milliseconds(pingInterval))
        timer.setEventHandler { [weak self] in
            // EIO PING
            self?.sendText("\(EIOPacketType.ping.rawValue)", to: connection)
        }
        timer.resume()
        pingTimers[key] = timer
    }

    // MARK: - Helpers

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
    }
}
