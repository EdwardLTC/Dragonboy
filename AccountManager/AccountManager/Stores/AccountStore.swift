import Foundation
import Combine

final class AccountStore: ObservableObject {
    @Published private(set) var accounts: [Account] = []

    private let fileURL: URL
    private var monitors: [UUID: DispatchSourceProcess] = [:]

    init() {
        let fm = FileManager.default
        if let appSupport = try? fm.url(for: .applicationSupportDirectory, in: .userDomainMask, appropriateFor: nil, create: true) {
            let dir = appSupport.appendingPathComponent(Bundle.main.bundleIdentifier ?? "AccountManager")
            try? fm.createDirectory(at: dir, withIntermediateDirectories: true)
            self.fileURL = dir.appendingPathComponent("accounts.json")
        } else {
            let dir = fm.urls(for: .documentDirectory, in: .userDomainMask).first!
            self.fileURL = dir.appendingPathComponent("accounts.json")
        }
        // Load persisted accounts on initialization
        load()
    }

    // MARK: - Persistence
    func load() {
        let fm = FileManager.default
        var loadedAccounts: [Account] = []

        if fm.fileExists(atPath: fileURL.path) {
            do {
                let data = try Data(contentsOf: fileURL)
                let decoder = JSONDecoder()
                decoder.dateDecodingStrategy = .iso8601
                loadedAccounts = try decoder.decode([Account].self, from: data)
            } catch {
                print("Failed to load accounts: \(error)")
            }
        }

        if Thread.isMainThread {
            self.accounts = loadedAccounts
        } else {
            DispatchQueue.main.sync {
                self.accounts = loadedAccounts
            }
        }

    }

    func save() {
        // Capture a snapshot of accounts on the main thread to avoid races
        let snapshot: [Account]
        if Thread.isMainThread {
            snapshot = accounts
        } else {
            var s: [Account] = []
            DispatchQueue.main.sync { s = accounts }
            snapshot = s
        }

        // Write the snapshot to disk asynchronously
        DispatchQueue.global(qos: .utility).async {
            do {
                let encoder = JSONEncoder()
                encoder.outputFormatting = [.prettyPrinted]
                encoder.dateEncodingStrategy = .iso8601
                let data = try encoder.encode(snapshot)
                try data.write(to: self.fileURL, options: [.atomic])
            } catch {
                print("Failed to save accounts: \(error)")
            }
        }
    }

    // MARK: - CRUD
    func addAccount(_ account: Account) throws {
        var exists = false
        if Thread.isMainThread {
            exists = accounts.contains(where: { $0.uniqueKey == account.uniqueKey })
        } else {
            DispatchQueue.main.sync { exists = accounts.contains(where: { $0.uniqueKey == account.uniqueKey }) }
        }

        if exists {
            throw NSError(domain: "AccountStore", code: 1, userInfo: [NSLocalizedDescriptionKey: "An account with this username and server already exists."])
        }

        let performAdd = {
            self.accounts.append(account)
            self.save()
        }

        if Thread.isMainThread {
            performAdd()
        } else {
            DispatchQueue.main.sync { performAdd() }
        }
    }

    func updateAccount(_ account: Account) throws {
        let work = {
            guard let idx = self.accounts.firstIndex(where: { $0.id == account.id }) else { return }
            var updated = account
            updated.updatedAt = Date()
            self.accounts[idx] = updated
            self.save()
        }

        if Thread.isMainThread {
            work()
        } else {
            DispatchQueue.main.sync { work() }
        }
    }

    func removeAccount(_ account: Account) throws {
        let work = {
            self.accounts.removeAll { $0.id == account.id }
            self.save()
        }

        if Thread.isMainThread {
            work()
        } else {
            DispatchQueue.main.sync { work() }
        }
    }

    @discardableResult
    func launch(account: Account) throws -> Int {
        var env: [String: String] = [:]
        env["DB_USERNAME"] = account.username
        env["DB_SERVER"] = account.server

        var args: [String] = []
        args.append("--username")
        args.append(account.username)
        args.append("--server")
        args.append(account.server)
        args.append("--password")
        args.append(account.password)

        let pid = try Launcher.launch(bundlePath: Launcher.defaultBundlePath, args: args, env: env)
        registerLaunched(pid: pid, for: account.id)
        return pid
    }

    func registerLaunched(pid: Int, for accountID: UUID) {
        DispatchQueue.main.async {
            if let idx = self.accounts.firstIndex(where: { $0.id == accountID }) {
                self.accounts[idx].isRunning = true
                self.accounts[idx].pid = pid
                self.save()
            }
        }

        let source = DispatchSource.makeProcessSource(identifier: pid_t(pid), eventMask: .exit, queue: DispatchQueue.global())
        source.setEventHandler { [weak self] in
            DispatchQueue.main.async {
                guard let self = self else { return }
                if let idx = self.accounts.firstIndex(where: { $0.id == accountID }) {
                    self.accounts[idx].isRunning = false
                    self.accounts[idx].pid = nil
                    self.save()
                }
                self.monitors[accountID]?.cancel()
                self.monitors.removeValue(forKey: accountID)
            }
        }
        source.resume()
        if Thread.isMainThread {
            monitors[accountID] = source
        } else {
            DispatchQueue.main.async { self.monitors[accountID] = source }
        }
    }

    func detectRunningInstances() {
        // Capture accounts snapshot on main, then perform detection in background
        let accountsSnapshot: [Account] = Thread.isMainThread ? self.accounts : {
            var s: [Account] = []
            DispatchQueue.main.sync { s = self.accounts }
            return s
        }()

        DispatchQueue.global().async {
            for account in accountsSnapshot {
                var matchKey: String = ""
                if let bundle = Bundle(path: Launcher.defaultBundlePath), let bid = bundle.bundleIdentifier {
                    matchKey = bid
                } else if let name = URL(fileURLWithPath: Launcher.defaultBundlePath).deletingPathExtension().lastPathComponent.split(separator: ".").first {
                    matchKey = String(name)
                }

                let pids = Launcher.findRunningPIDs(matching: matchKey)
                DispatchQueue.main.async {
                    if let idx = self.accounts.firstIndex(where: { $0.id == account.id }) {
                        if let pid = pids.first {
                            self.accounts[idx].isRunning = true
                            self.accounts[idx].pid = pid
                        } else {
                            self.accounts[idx].isRunning = false
                            self.accounts[idx].pid = nil
                        }
                    }
                }
            }
            self.save()
        }
    }
}
