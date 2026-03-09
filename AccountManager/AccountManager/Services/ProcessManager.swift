import Foundation
import AppKit

final class ProcessManager {
    static let shared = ProcessManager()

    weak var store: AccountStore?

    private var socketServers: [UUID: SocketServer] = [:]

    func launch(account: Account) throws {
        let server = SocketServer(accountID: account.id,
                                  username: account.username,
                                  password: account.password)

        server.onAllClientsDisconnected = { [weak self] accountID in
            self?.handleSocketDisconnected(accountID: accountID)
        }

        let port = try server.start()

        var args: [String] = []
        args.append("-port")
        args.append(String(port))
        args.append("-username")
        args.append(account.username)
        args.append("-password")
        args.append(account.password)

        do {
            try Launcher.launch(bundlePath: Launcher.defaultBundlePath, args: args)
        } catch {
            server.stop()
            throw error
        }

        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }
            self.socketServers[account.id] = server
        }
    }

    func stop(account: Account) {
        let accountID = account.id

        if let server = socketServers[accountID] {
            server.broadcastEvent("stop", data: ["action":"stop"])
            print("[ProcessManager] Sent 'stop' command to game for \(accountID)")
        } else {
            print("[ProcessManager] No socket server for \(accountID) – marking stopped")
            DispatchQueue.main.async { [weak self] in
                self?.store?.markStopped(accountID: accountID)
            }
        }
    }

    private func handleSocketDisconnected(accountID: UUID) {
        print("[ProcessManager] Socket disconnected for \(accountID) – cleaning up")

        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }
            self.socketServers[accountID]?.stop()
            self.socketServers.removeValue(forKey: accountID)
            self.store?.markStopped(accountID: accountID)
        }
    }
    
    func socketServer(for accountID: UUID) -> SocketServer? {
        return socketServers[accountID]
    }
}
