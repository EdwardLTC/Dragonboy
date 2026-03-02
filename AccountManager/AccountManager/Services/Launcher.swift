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
    static func spawnExecutable(at execURL: URL, args: [String]) throws -> Int {
        let proc = Process()
        proc.executableURL = execURL
        proc.arguments = args
        proc.currentDirectoryURL = execURL.deletingLastPathComponent()

        let outPipe = Pipe()
        let errPipe = Pipe()
        proc.standardOutput = outPipe
        proc.standardError = errPipe

        try proc.run()

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
    static func launch(bundlePath: String, args: [String] = []) throws -> Int {
        
        guard isAppInstalled(at: bundlePath) else {
            throw LauncherError.appNotFound(path: bundlePath)
        }
        
        let openURL = URL(fileURLWithPath: "/usr/bin/open")
        
        guard FileManager.default.isExecutableFile(atPath: openURL.path) else {
            throw LauncherError.executableNotFound(path: openURL.path)
        }
        
        var launchArgs = ["-n", bundlePath]
        if !args.isEmpty {
            launchArgs.append("--args")
            launchArgs.append(contentsOf: args)
        }
        
        do {
            let pid = try spawnExecutable(
                at: openURL,
                args: launchArgs
            )
            
            if pid <= 0 {
                throw LauncherError.launchFailed(NSError(domain: "Launcher", code: -1, userInfo: [
                    NSLocalizedDescriptionKey: "Invalid PID returned from spawn"
                ]))
            }
            
            return pid
            
        } catch {
            throw LauncherError.launchFailed(error)
        }
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
}
