import SwiftUI

struct AccountListView: View {
    @EnvironmentObject var store: AccountStore
    @State private var showingAdd = false
    @State private var selectedAccountID: UUID?

    var body: some View {
        HSplitView {
            VStack(alignment: .leading, spacing: 0) {
                HStack {
                    Text("Dragonboy Launcher").font(.largeTitle).bold()
                    Spacer()
                    Button(action: { showingAdd = true }) {
                        Image(systemName: "plus")
                    }
                    .buttonStyle(.borderedProminent)
                }
                .padding([.horizontal, .top])
                .padding(.bottom, 8)

                ScrollView {
                    LazyVStack(spacing: 12) {
                        ForEach(store.accounts) { account in
                            AccountRowView(
                                account: account,
                                isSelected: selectedAccountID == account.id,
                                onSelect: { selectedAccountID = account.id }
                            )
                            .environmentObject(store)
                            .padding(.horizontal)
                        }
                    }
                    .padding(.vertical)
                }
            }
            .frame(minWidth: 380, idealWidth: 420)

            Group {
                if let selectedID = selectedAccountID,
                   let account = store.accounts.first(where: { $0.id == selectedID }) {
                    AccountDetailView(account: account)
                        .environmentObject(store)
                        .id(selectedID)
                } else {
                    VStack(spacing: 12) {
                        Image(systemName: "person.crop.rectangle.stack")
                            .font(.system(size: 48))
                            .foregroundColor(.secondary.opacity(0.4))
                        Text("Select an account to view details")
                            .font(.title3)
                            .foregroundColor(.secondary)
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
            }
            .frame(minWidth: 400, idealWidth: 520)
        }
        .sheet(isPresented: $showingAdd) {
            AddAccountView().environmentObject(store)
        }
        .frame(minWidth: 900, minHeight: 520)
    }
}
