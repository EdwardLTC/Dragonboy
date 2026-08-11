import SwiftUI

struct AccountListView: View {
    @EnvironmentObject private var store: AccountStore
    @State private var showingAdd = false
    @State private var selectedAccountID: UUID?
    @State private var searchText = ""
    @State private var errorMessage: String?

    private var filteredAccounts: [Account] {
        let query = searchText.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !query.isEmpty else { return store.accounts }

        return store.accounts.filter {
            $0.username.localizedCaseInsensitiveContains(query) ||
            $0.server.localizedCaseInsensitiveContains(query) ||
            ($0.characterInfo?.cName.localizedCaseInsensitiveContains(query) ?? false)
        }
    }

    private var selectedAccount: Account? {
        guard let selectedAccountID else { return nil }
        return store.accounts.first { $0.id == selectedAccountID }
    }

    var body: some View {
        NavigationSplitView {
            List(selection: $selectedAccountID) {
                Section {
                    ForEach(filteredAccounts) { account in
                        AccountRowView(account: account) {
                            launchOrStop(account)
                        }
                        .tag(account.id)
                    }
                } header: {
                    Text("Accounts")
                }
            }
            .listStyle(.sidebar)
            .searchable(text: $searchText, prompt: "Search accounts")
            .navigationTitle("Dragonboy")
            .toolbar {
                ToolbarItem(placement: .primaryAction) {
                    Button("Add account", systemImage: "plus") {
                        showingAdd = true
                    }
                    .help("Add account")
                }
            }
        } detail: {
            if let account = selectedAccount {
                AccountDetailView(account: account, onLaunchOrStop: {
                    launchOrStop(account)
                }, onRemove: {
                    remove(account)
                })
                .id(account.id)
            } else {
                ContentUnavailableView(
                    "Select an account",
                    systemImage: "person.crop.circle.badge.questionmark",
                    description: Text("Choose an account from the sidebar, or add a new one to get started.")
                )
            }
        }
        .navigationSplitViewStyle(.balanced)
        .sheet(isPresented: $showingAdd) {
            AddAccountView()
                .environmentObject(store)
        }
        .alert("Could not complete action", isPresented: Binding(
            get: { errorMessage != nil },
            set: { if !$0 { errorMessage = nil } }
        )) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(errorMessage ?? "")
        }
        .onChange(of: store.accounts.map(\.id)) { _, accountIDs in
            if let selectedAccountID, !accountIDs.contains(selectedAccountID) {
                self.selectedAccountID = accountIDs.first
            }
        }
    }

    private func launchOrStop(_ account: Account) {
        guard !store.launching.contains(account.id) else { return }
        store.launching.insert(account.id)

        DispatchQueue.global(qos: .userInitiated).async {
            do {
                if account.connectionStatus.isConnected {
                    ProcessManager.shared.stop(account: account)
                } else {
                    try ProcessManager.shared.launch(account: account)
                }
            } catch {
                let message = (error as? LocalizedError)?.errorDescription ?? error.localizedDescription
                DispatchQueue.main.async {
                    errorMessage = message
                    store.launching.remove(account.id)
                }
            }
        }
    }

    private func remove(_ account: Account) {
        do {
            try store.removeAccount(account)
            if selectedAccountID == account.id {
                selectedAccountID = store.accounts.first?.id
            }
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
