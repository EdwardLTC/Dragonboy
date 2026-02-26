import SwiftUI

struct AccountRowView: View {
    @EnvironmentObject var store: AccountStore
    var account: Account

    @State private var isLaunching: Bool = false
    @State private var showDetail: Bool = false
    @State private var errorMessage: String?
    @State private var infoMessage: String?
    @State private var childErrorObserver: NSObjectProtocol?
    @State private var isRemoving: Bool = false
    @State private var showDeleteConfirm: Bool = false

    var body: some View {
        HStack(spacing: 12) {
            Circle()
                .fill(account.isRunning ? Color.green.opacity(0.85) : Color.gray.opacity(0.4))
                .frame(width: 44, height: 44)
                .overlay(Text(String(account.username.prefix(1)).uppercased()).font(.headline).foregroundColor(.white))

            VStack(alignment: .leading, spacing: 4) {
                Text(account.username).font(.headline)
                Text(account.server).font(.subheadline).foregroundColor(.secondary)
            }

            Spacer()

            if account.isRunning {
                Text("Running").font(.caption).padding(8).background(Color.green.opacity(0.13)).cornerRadius(8)
            } else {
                Text("Stopped").font(.caption).padding(8).background(Color.gray.opacity(0.06)).cornerRadius(8)
            }

            if isLaunching {
                ProgressView().progressViewStyle(.circular)
            } else {
                Button(action: {
                    launchTapped()
                }) {
                    Text(account.isRunning ? "Stop" : "Launch")
                }
                .buttonStyle(.borderedProminent)
            }

            // Remove account button
            if isRemoving {
                ProgressView().progressViewStyle(.circular).frame(width: 28, height: 28)
            } else {
                Button(action: {
                    showDeleteConfirm = true
                }) {
                    Image(systemName: "trash")
                }
                .buttonStyle(.plain)
                .help("Remove account")
            }

            Button(action: {
                showDetail = true
            }) {
                Image(systemName: "gearshape")
            }
            .buttonStyle(.plain)
            .sheet(isPresented: $showDetail) {
                AccountDetailView(account: account).environmentObject(store)
            }
        }
        .contentShape(Rectangle())
        .padding()
        .background(
            ZStack {
                RoundedRectangle(cornerRadius: 12, style: .continuous).fill(Color(NSColor.windowBackgroundColor))
                RoundedRectangle(cornerRadius: 12).stroke(Color.white.opacity(0.06), lineWidth: 1)
            }
        )
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .shadow(color: account.isRunning ? Color.green.opacity(0.18) : Color.clear, radius: 12)
                .allowsHitTesting(false)
        )
        .animation(.easeInOut, value: account.isRunning)
        .onAppear {
            childErrorObserver = NotificationCenter.default.addObserver(forName: .launcherChildOutput, object: nil, queue: .main) { note in
                guard let info = note.userInfo as? [String: Any], let type = info["type"] as? String, type == "stderr", let msg = info["message"] as? String else { return }
                if msg.contains("could not be found") || msg.contains("failed to connect") || msg.contains("getaddrinfo") || msg.contains("A server with the specified hostname could not be found") {
                    errorMessage = "Launched but game reported network errors: \(msg)"
                }
            }
        }
        .onDisappear {
            if let obs = childErrorObserver { NotificationCenter.default.removeObserver(obs); childErrorObserver = nil }
        }
        .alert(isPresented: $showDeleteConfirm) {
            Alert(title: Text("Remove account"), message: Text("Are you sure you want to remove account \(account.username) on \(account.server)?"), primaryButton: .destructive(Text("Remove"), action: {
                DispatchQueue.main.async {
                    isRemoving = true
                    errorMessage = nil
                }
        
                DispatchQueue.main.async {
                    do {
                        try store.removeAccount(account)
                        DispatchQueue.main.async {
                            isRemoving = false
                            infoMessage = "Removed account"
                        }
                    } catch {
                        DispatchQueue.main.async {
                            isRemoving = false
                            errorMessage = error.localizedDescription
                        }
                    }
                }
            }), secondaryButton: .cancel())
        }
    }

    private func launchTapped() {
        DispatchQueue.main.async {
            isLaunching = true
            errorMessage = nil
        }

        DispatchQueue.global(qos: .userInitiated).async {
            if account.isRunning {
                DispatchQueue.main.async {
                    isLaunching = false
                    errorMessage = "Stopping processes is not implemented in this prototype."
                }
                return
            }

            do {
                let pid = try store.launch(account: account)
                print("Launched \(account.username) pid=\(pid)")
                DispatchQueue.main.async {
                    isLaunching = false
                    infoMessage = "Launched (pid: \(pid))"
                    checkConnectivityForLaunchedApp(account: account)
                }
            } catch {
                let message: String
                if let le = error as? LocalizedError, let desc = le.errorDescription {
                    message = desc
                } else {
                    message = error.localizedDescription
                }
                print("Failed to launch app for account \(account.username): \(message)")
                DispatchQueue.main.async {
                    isLaunching = false
                    errorMessage = message
                }
            }
        }
    }

    /// After launching, probe the account.server to verify the game can reach
    /// the network. Retries a few times with a delay before reporting failure.
    private func checkConnectivityForLaunchedApp(account: Account, attempts: Int = 3, delay: TimeInterval = 3.0) {
        guard attempts > 0 else {
            DispatchQueue.main.async {
                errorMessage = "Launched but cannot connect to \(account.server)"
            }
            return
        }

        Launcher.checkConnectivity(to: account.server, timeout: 5.0) { success, reason in
            if success {
                DispatchQueue.main.async {
                    infoMessage = "Game connected to \(account.server)"
                }
            } else {
                if attempts > 1 {
                    DispatchQueue.global().asyncAfter(deadline: .now() + delay) {
                        self.checkConnectivityForLaunchedApp(
                            account: account,
                            attempts: attempts - 1,
                            delay: delay
                        )
                    }
                } else {
                    DispatchQueue.main.async {
                        errorMessage = "Launched but cannot connect to \(account.server): \(reason ?? "Unknown")"
                    }
                }
            }
        }
    }
}

private struct AlertWrapper: Identifiable {
    let id = UUID()
    let message: String
}
