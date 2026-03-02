import Foundation
import Combine
import Darwin

final class AccountStore: ObservableObject {
    @Published private(set) var accounts: [Account] = []
    @Published var launching: Set<UUID> = []

    private let fileURL: URL

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
        
        ProcessManager.shared.store = self

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
        let snapshot: [Account]
        if Thread.isMainThread {
            snapshot = accounts
        } else {
            var s: [Account] = []
            DispatchQueue.main.sync { s = accounts }
            snapshot = s
        }

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

    // MARK: - Account state helpers
    func markLaunched(accountID: UUID, pid: Int) {
        DispatchQueue.main.async {
            if let idx = self.accounts.firstIndex(where: { $0.id == accountID }) {
                self.accounts[idx].isRunning = true
                self.accounts[idx].pid = pid
                self.launching.remove(accountID)
                self.save()
            }
        }
    }

    func markStopped(accountID: UUID) {
        DispatchQueue.main.async {
            if let idx = self.accounts.firstIndex(where: { $0.id == accountID }) {
                self.accounts[idx].isRunning = false
                self.accounts[idx].pid = nil
                self.save()
            }
            self.launching.remove(accountID)
        }
    }
}
