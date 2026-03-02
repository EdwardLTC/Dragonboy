import Foundation
import Darwin
import AppKit

enum ProcessManagerError: Error {
    case noSuchProcess(pid: Int)
    case permissionDenied(pid: Int)
    case terminationFailed(pid: Int)
}

final class ProcessManager {
    static let shared = ProcessManager()

    weak var store: AccountStore?

    private var monitors: [UUID: DispatchSourceProcess] = [:] 

    private var matchKey: String {
        if let bundle = Bundle(path: Launcher.defaultBundlePath), let bid = bundle.bundleIdentifier {
            return bid
        }
        return URL(fileURLWithPath: Launcher.defaultBundlePath).deletingPathExtension().lastPathComponent
    }

    static func processExists(_ pid: Int) -> Bool {
        errno = 0
        if kill(pid_t(pid), 0) == 0 { return true }
        return errno != ESRCH
    }

    static func terminate(pid: Int, timeout: TimeInterval = 2.0) throws {
        guard pid > 0 else { throw ProcessManagerError.noSuchProcess(pid: pid) }
        
        if !processExists(pid) { throw ProcessManagerError.noSuchProcess(pid: pid) }

        let termResult = kill(pid_t(pid), SIGTERM)
        if termResult == -1 {
            let e = errno
            if e == ESRCH { throw ProcessManagerError.noSuchProcess(pid: pid) }
            if e == EPERM { throw ProcessManagerError.permissionDenied(pid: pid) }
        }

        let deadline = Date().addingTimeInterval(timeout)
        while Date() < deadline {
            if !processExists(pid) { return }
            Thread.sleep(forTimeInterval: 0.12)
        }
        
        if processExists(pid) {
            let killResult = kill(pid_t(pid), SIGKILL)
            if killResult == -1 {
                let e = errno
                if e == ESRCH { return }
                if e == EPERM { throw ProcessManagerError.permissionDenied(pid: pid) }
                throw ProcessManagerError.terminationFailed(pid: pid)
            }

            let killDeadline = Date().addingTimeInterval(1.0)
            while Date() < killDeadline {
                if !processExists(pid) { return }
                Thread.sleep(forTimeInterval: 0.08)
            }

            if processExists(pid) {
                throw ProcessManagerError.terminationFailed(pid: pid)
            }
        }
    }

    func launch(account: Account) throws -> Int {
        var args: [String] = []
        args.append("--username")
        args.append(account.username)
        args.append("--server")
        args.append(account.server)
        args.append("--password")
        args.append(account.password)

        let pid = try Launcher.launch(bundlePath: Launcher.defaultBundlePath, args: args)
        
        DispatchQueue.main.async { [weak self] in
            self?.registerLaunched(pid: pid, for: account.id)
        }

        return pid
    }

    func registerLaunched(pid: Int, for accountID: UUID) {
        dispatchPrecondition(condition: .onQueue(.main))

        monitors[accountID]?.cancel()
        monitors.removeValue(forKey: accountID)

        store?.markLaunched(accountID: accountID, pid: pid)

        let source = DispatchSource.makeProcessSource(identifier: pid_t(pid), eventMask: .exit, queue: DispatchQueue.global())
        let cachedMatchKey = self.matchKey
        source.setEventHandler { [weak self] in
            guard let self = self else { return }

            var newPids: [Int] = []
            for attempt in 0..<3 {
                if attempt > 0 {
                    Thread.sleep(forTimeInterval: 1.0)
                }
                newPids = Launcher.findRunningPIDs(matching: cachedMatchKey)
                    .filter { $0 != pid }  // Exclude the exited pid
                if !newPids.isEmpty { break }
            }

            DispatchQueue.main.async {
                if let newPid = newPids.first {
                    self.registerLaunched(pid: newPid, for: accountID)
                } else {
                    self.store?.markStopped(accountID: accountID)
                    self.monitors[accountID]?.cancel()
                    self.monitors.removeValue(forKey: accountID)
                }
            }
        }
        source.resume()
        monitors[accountID] = source
    }

    func detectRunningInstances(accounts: [Account]) {
        let key = self.matchKey
        DispatchQueue.global().async { [weak self] in
            guard let self = self else { return }
            for account in accounts {
                let pids = Launcher.findRunningPIDs(matching: key)
                DispatchQueue.main.async {
                    if let store = self.store {
                        if let pid = pids.first {
                            if self.monitors[account.id] == nil {
                                self.registerLaunched(pid: pid, for: account.id)
                            } else {
                                store.markLaunched(accountID: account.id, pid: pid)
                            }
                        } else {
                            if self.monitors[account.id] == nil {
                                store.markStopped(accountID: account.id)
                            }
                        }
                    }
                }
            }
            DispatchQueue.main.async { self.store?.save() }
        }
    }

    func stop(account: Account) throws {
        guard let store = self.store else { return }

        var pidToKill: Int? = nil
        if Thread.isMainThread {
            if let index = store.accounts.firstIndex(where: { $0.id == account.id }) { pidToKill = store.accounts[index].pid }
        } else {
            DispatchQueue.main.sync { if let index = store.accounts.firstIndex(where: { $0.id == account.id }) { pidToKill = store.accounts[index].pid } }
        }

        guard let pid = pidToKill, pid > 0 else { throw ProcessManagerError.noSuchProcess(pid: pidToKill ?? -1) }

        let key = self.matchKey

        do {
            try ProcessManager.terminate(pid: pid, timeout: 2.0)
        } catch ProcessManagerError.permissionDenied {
            terminateViaNSRunningApplication(matchKey: key, timeout: 2.0)
        }

        let runningPids = Launcher.findRunningPIDs(matching: key)
        for runningPid in runningPids {
            if runningPid == pid { continue }
            do {
                try ProcessManager.terminate(pid: runningPid, timeout: 1.0)
            } catch {
                if case ProcessManagerError.permissionDenied = error {
                    terminateViaNSRunningApplication(matchKey: key, timeout: 1.0)
                }
            }
        }

        DispatchQueue.main.async {
            store.markStopped(accountID: account.id)
            self.monitors[account.id]?.cancel()
            self.monitors.removeValue(forKey: account.id)
        }
    }

    private func terminateViaNSRunningApplication(matchKey: String, timeout: TimeInterval) {
        var apps = NSRunningApplication.runningApplications(withBundleIdentifier: matchKey)

        if apps.isEmpty {
            apps = NSWorkspace.shared.runningApplications.filter { app in
                if let exec = app.executableURL?.lastPathComponent.lowercased() {
                    return exec.contains(matchKey.lowercased())
                }
                if let bundleID = app.bundleIdentifier {
                    return bundleID.lowercased().contains(matchKey.lowercased())
                }
                return false
            }
        }

        for app in apps {
            if app.isTerminated { continue }

            if app.terminate() {
                let deadline = Date().addingTimeInterval(timeout)
                while Date() < deadline && !app.isTerminated {
                    Thread.sleep(forTimeInterval: 0.08)
                }
            }

            if !app.isTerminated {
                _ = app.forceTerminate()
            }
        }
    }
}
