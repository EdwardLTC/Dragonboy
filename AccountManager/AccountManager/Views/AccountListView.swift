import SwiftUI

struct AccountListView: View {
    @EnvironmentObject var store: AccountStore
    @State private var showingAdd = false

    var body: some View {
        NavigationView {
            VStack(alignment: .leading) {
                HStack {
                    Text("Dragonboy Launcher").font(.largeTitle).bold()
                    Spacer()
                    Button(action: { showingAdd = true }) {
                        Image(systemName: "plus")
                    }
                    .buttonStyle(.borderedProminent)
                }
                .padding([.horizontal, .top])

                ScrollView {
                    LazyVStack(spacing: 12) {
                        ForEach(store.accounts) { account in
                            AccountRowView(account: account).environmentObject(store).padding(.horizontal)
                        }
                    }
                    .padding(.vertical)
                }
            }
        }
        .sheet(isPresented: $showingAdd) {
            AddAccountView().environmentObject(store)
        }
        .frame(minWidth: 700, minHeight: 480)
    }
}
