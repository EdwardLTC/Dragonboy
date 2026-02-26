import Foundation
import AppKit
import Network

extension Notification.Name {
    static let launcherChildOutput = Notification.Name("launcherChildOutput")
}
import Darwin

enum LauncherError: Error {
    case appNotFound(path: String)
    case executableNotFound(path: String)
    case launchFailed(Error)
}

extension LauncherError: LocalizedError {
    var errorDescription: String? {
        switch self {
        case .appNotFound(let path):
            return "Application bundle not found at path: \(path)"
        case .executableNotFound(let path):
            return "Could not find an executable inside the app bundle at: \(path)"
        case .launchFailed(let err):
            return "Failed to launch process: \(err.localizedDescription)"
        }
    }
}

final class Launcher {
    /// Default bundle path used when none provided
    static let defaultBundlePath = "/Applications/Dragonboy247_macos.app"

    static func isAppInstalled(at bundlePath: String) -> Bool {
        return FileManager.default.fileExists(atPath: bundlePath)
    }

    static func resolvedExecutableURL(in bundlePath: String) -> URL? {
        let bundleURL = URL(fileURLWithPath: bundlePath)
        guard let bundle = Bundle(url: bundleURL) else { return nil }
        if let execName = bundle.object(forInfoDictionaryKey: "CFBundleExecutable") as? String {
            let execURL = bundleURL.appendingPathComponent("Contents/MacOS/")
                .appendingPathComponent(execName)
            if FileManager.default.isExecutableFile(atPath: execURL.path) {
                return execURL
            }
        }

        let macOSDir = bundleURL.appendingPathComponent("Contents/MacOS")
        if let enumerator = FileManager.default.enumerator(at: macOSDir, includingPropertiesForKeys: [.isExecutableKey], options: [.skipsHiddenFiles]) {
            for case let fileURL as URL in enumerator {
                if FileManager.default.isExecutableFile(atPath: fileURL.path) {
                    return fileURL
                }
            }
        }

        return nil
    }

    /// Spawn the executable directly, set working directory to the executable's
    /// directory, merge environment, and capture stdout/stderr to post
    /// notifications (useful for debugging child output). Returns the PID.
    static func spawnExecutable(at execURL: URL, args: [String], env: [String: String]) throws -> Int {
        let proc = Process()
        proc.executableURL = execURL
        proc.arguments = args

        if !env.isEmpty {
            var merged = ProcessInfo.processInfo.environment
            for (k, v) in env { merged[k] = v }
            proc.environment = merged
        }

        // Ensure the process runs with its working directory inside the app bundle
        proc.currentDirectoryURL = execURL.deletingLastPathComponent()

        let outPipe = Pipe()
        let errPipe = Pipe()
        proc.standardOutput = outPipe
        proc.standardError = errPipe

        try proc.run()

        // Read stdout asynchronously and post notifications
        outPipe.fileHandleForReading.readabilityHandler = { fh in
            let data = fh.availableData
            if data.count > 0, let s = String(data: data, encoding: .utf8) {
                NotificationCenter.default.post(name: .launcherChildOutput, object: nil, userInfo: ["pid": Int(proc.processIdentifier), "type": "stdout", "message": s])
            }
        }

        errPipe.fileHandleForReading.readabilityHandler = { fh in
            let data = fh.availableData
            if data.count > 0, let s = String(data: data, encoding: .utf8) {
                NotificationCenter.default.post(name: .launcherChildOutput, object: nil, userInfo: ["pid": Int(proc.processIdentifier), "type": "stderr", "message": s])
            }
        }

        return Int(proc.processIdentifier)
    }


     /// Launches the executable directly, passing environment variables or arguments. Returns the PID of the launched process.
     static func launch(bundlePath: String, args: [String] = [], env: [String: String] = [:], forceNewInstance: Bool = false) throws -> Int {
        guard isAppInstalled(at: bundlePath) else { throw LauncherError.appNotFound(path: bundlePath) }

        let bundleURL = URL(fileURLWithPath: bundlePath)
        var matchKey = bundleURL.deletingPathExtension().lastPathComponent
        if let bundle = Bundle(url: bundleURL), let bid = bundle.bundleIdentifier {
            matchKey = bid
        }

        // Record currently running PIDs that match, so we can detect the newly launched one
        let beforePIDs = Set(findRunningPIDs(matching: matchKey))

        // If caller requested a forced new instance, prefer direct executable
        // launch (works for multiple instances) and fall back to `open -n`.
        if forceNewInstance {
            if let execURL = resolvedExecutableURL(in: bundlePath) {
                let proc = Process()
                proc.executableURL = execURL
                proc.arguments = args
                if !env.isEmpty {
                    var merged = ProcessInfo.processInfo.environment
                    for (k, v) in env { merged[k] = v }
                    proc.environment = merged
                }

                do {
                    try proc.run()
                    return Int(proc.processIdentifier)
                } catch {
                    // continue to open -n fallback below
                }
            }

            // Fallback to open -n when forcing new instance
            let openTask = Process()
            openTask.launchPath = "/usr/bin/open"
            var openArgs: [String] = ["-n", "-a", bundlePath]
            if !args.isEmpty {
                openArgs.append("--args")
                openArgs.append(contentsOf: args)
            }
            openTask.arguments = openArgs

            do {
                try openTask.run()
            } catch {
                throw LauncherError.launchFailed(error)
            }

            Thread.sleep(forTimeInterval: 0.35)
            let afterPIDs = Set(findRunningPIDs(matching: matchKey))
            let newPIDs = afterPIDs.subtracting(beforePIDs)
            if let pid = newPIDs.first { return pid }
            if let pid = afterPIDs.first { return pid }
            throw LauncherError.executableNotFound(path: bundlePath)
        }

        // Default behavior: use NSWorkspace to open the application with a
        // configuration. This launches/activates the app in the AppKit context
        // and allows passing arguments and environment via OpenConfiguration.
        let config = NSWorkspace.OpenConfiguration()
        if !args.isEmpty { config.arguments = args }
        if !env.isEmpty { config.environment = env }

        var launchedPID: Int? = nil
        var launchError: Error? = nil
        let sem = DispatchSemaphore(value: 0)
        NSWorkspace.shared.openApplication(at: bundleURL, configuration: config) { app, err in
            if let e = err {
                launchError = e
            } else if let running = app {
                launchedPID = Int(running.processIdentifier)
            }
            sem.signal()
        }

        // Wait briefly for NSWorkspace to respond
        let _ = sem.wait(timeout: .now() + 2.0)

        if let pid = launchedPID {
            return pid
        }

        // If NSWorkspace reported an error or didn't give us a PID, try to
        // spawn the executable directly so the app receives the args/env.
        if let execURL = resolvedExecutableURL(in: bundlePath) {
            // Before falling back to spawning a separate executable (which may
            // not integrate with the currently-running GUI instance), post a
            // Distributed Notification containing the args/env. This allows the
            // running app to receive the parameters if it subscribes to the
            // notification — useful when open-activated an existing instance.
            if !args.isEmpty || !env.isEmpty {
                let name = Notification.Name("com.dragonboy.launcher.launchArgs")
                DistributedNotificationCenter.default().post(name: name, object: nil, userInfo: ["args": args, "env": env, "bundlePath": bundlePath])
            }

            do {
                return try spawnExecutable(at: execURL, args: args, env: env)
            } catch {
            
            }
        }

        // As a last fallback, if the caller set forceNewInstance=true we already
        // attempted open -n earlier; here we simply report the NSWorkspace error
        if let e = launchError {
            throw LauncherError.launchFailed(e)
        }

        throw LauncherError.executableNotFound(path: bundlePath)
    }

    /// Attempt to find running PIDs for a bundle identifier or process name.
    static func findRunningPIDs(matching bundleIdentifierOrProcessName: String) -> [Int] {
        var pids: [Int] = []

        let apps = NSRunningApplication.runningApplications(withBundleIdentifier: bundleIdentifierOrProcessName)
        if !apps.isEmpty {
            for app in apps {
                pids.append(Int(app.processIdentifier))
            }
            return pids
        }

        let task = Process()
        let pipe = Pipe()
        task.launchPath = "/bin/ps"
        task.arguments = ["-ax", "-o", "pid=,comm="]
        task.standardOutput = pipe
        do {
            try task.run()
        } catch {
            return []
        }

        let data = pipe.fileHandleForReading.readDataToEndOfFile()
        guard let output = String(data: data, encoding: .utf8) else { return [] }

        for line in output.split(separator: "\n") {
            let trimmed = line.trimmingCharacters(in: .whitespaces)
            // format: "<pid> <path>"
            let comps = trimmed.split(separator: " ", maxSplits: 1).map(String.init)
            if comps.count >= 2 {
                let pidStr = comps[0]
                let cmd = comps[1]
                if cmd.lowercased().contains(bundleIdentifierOrProcessName.lowercased()) || cmd.lowercased().hasSuffix("/\(bundleIdentifierOrProcessName.lowercased())") {
                    if let pid = Int(pidStr) { pids.append(pid) }
                }
            }
        }

        return pids
    }

    static func checkConnectivity(to hostOrURL: String, timeout: TimeInterval = 5.0, completion: @escaping (Bool, String?) -> Void) {
        var candidate = hostOrURL.trimmingCharacters(in: .whitespacesAndNewlines)
        if candidate.isEmpty {
            completion(false, "No host provided")
            return
        }

        if URL(string: candidate)?.scheme == nil {
            candidate = "https://\(candidate)/"
        }

        guard let url = URL(string: candidate) else {
            completion(false, "Invalid URL")
            return
        }

        var req = URLRequest(url: url, timeoutInterval: timeout)
        req.httpMethod = "HEAD"

        let task = URLSession.shared.dataTask(with: req) { _, response, error in
            if let err = error {
                completion(false, err.localizedDescription)
                return
            }
            if let http = response as? HTTPURLResponse {
                if (200...399).contains(http.statusCode) {
                    completion(true, nil)
                } else {
                    completion(false, "HTTP status \(http.statusCode)")
                }
                return
            }
            // If we didn't get an HTTP response, still consider success if we got any response
            if response != nil {
                completion(true, nil)
            } else {
                completion(false, "No response")
            }
        }
        task.resume()
    }

    /// Synchronously check whether a host resolves via DNS. Returns true if
    /// getaddrinfo can resolve the host to at least one address.
    static func hostResolves(_ hostOrHostname: String) -> Bool {
        // Extract a hostname if a full URL was provided
        var hostname = hostOrHostname.trimmingCharacters(in: .whitespacesAndNewlines)
        if let u = URL(string: hostname), let h = u.host {
            hostname = h
        }
        guard !hostname.isEmpty else { return false }

        var hints = addrinfo(ai_flags: AI_ADDRCONFIG, ai_family: AF_UNSPEC, ai_socktype: SOCK_STREAM, ai_protocol: 0, ai_addrlen: 0, ai_canonname: nil, ai_addr: nil, ai_next: nil)
        var resPtr: UnsafeMutablePointer<addrinfo>? = nil
        let status = getaddrinfo(hostname, nil, &hints, &resPtr)
        if status == 0 {
            if resPtr != nil { freeaddrinfo(resPtr) }
            return true
        }
        return false
    }
}
