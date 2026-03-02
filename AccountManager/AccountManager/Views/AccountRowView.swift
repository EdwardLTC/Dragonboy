import SwiftUI

struct AccountRowView: View {
    @EnvironmentObject var store: AccountStore
    var account: Account
    private var liveAccount: Account {
        if let idx = store.accounts.firstIndex(where: { $0.id == account.id }) {
            return store.accounts[idx]
        }
        return account
    }
    
    @State private var showDetail: Bool = false
    @State private var errorMessage: String?
    @State private var infoMessage: String?
    @State private var childErrorObserver: NSObjectProtocol?
    @State private var isRemoving: Bool = false
    @State private var showDeleteConfirm: Bool = false

    var body: some View {
        HStack(spacing: 12) {
            Circle()
                .fill(liveAccount.isRunning ? Color.green.opacity(0.85) : Color.gray.opacity(0.4))
                .frame(width: 44, height: 44)
                .overlay(Text(String(liveAccount.username.prefix(1)).uppercased()).font(.headline).foregroundColor(.white))

            VStack(alignment: .leading, spacing: 4) {
                Text(liveAccount.username).font(.headline)
                Text(liveAccount.server).font(.subheadline).foregroundColor(.secondary)
            }

            Spacer()

            if store.launching.contains(account.id) {
                ProgressView().progressViewStyle(.circular)
            } else {
                Button(action: {
                    launchTapped()
                }) {
                    Text(liveAccount.isRunning ? "Stop" : "Launch")
                }
                .buttonStyle(.borderedProminent)
                .tint(liveAccount.isRunning ? Color.red : Color.accentColor)
                .disabled(store.launching.contains(account.id))
            }

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
                AccountDetailView(account: liveAccount).environmentObject(store)
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
        DispatchQueue.global(qos: .userInitiated).async {
            DispatchQueue.main.async { store.launching.insert(account.id) }
            do {
                if account.isRunning {
                    try ProcessManager.shared.stop(account: account)
                    DispatchQueue.main.async {
                        infoMessage = "Stopped"
                    }
                } else {
                    let pid = try ProcessManager.shared.launch(account: account)
                    DispatchQueue.main.async {
                        infoMessage = "Launched (pid: \(pid))"
                    }
                }
            } catch {
                let message: String
                if let le = error as? LocalizedError, let desc = le.errorDescription {
                    message = desc
                } else {
                    message = error.localizedDescription
                }
                DispatchQueue.main.async {
                    errorMessage = message
                    store.launching.remove(account.id)
                }
            }
        }
    }
}

private struct AlertWrapper: Identifiable {
    let id = UUID()
    let message: String
}
