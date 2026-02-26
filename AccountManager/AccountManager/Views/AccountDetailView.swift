import SwiftUI

struct AccountDetailView: View {
    @EnvironmentObject var store: AccountStore
    @Environment(\.presentationMode) private var presentationMode

    @State private var accountCopy: Account
    @State private var password: String = ""
    @State private var errorMessage: String?

    init(account: Account) {
        _accountCopy = State(initialValue: account)
    }

    var body: some View {
        VStack(spacing: 12) {
            Text("Account").font(.title2)

            TextField("Username", text: $accountCopy.username)
            TextField("Server", text: $accountCopy.server)
            SecureField("Password (leave blank to keep)", text: $accountCopy.password)

            HStack {
                Spacer()
                Button("Cancel") { presentationMode.wrappedValue.dismiss() }
                Button("Save") { saveTapped() }.keyboardShortcut(.defaultAction)
            }
        }
        .padding()
        .frame(width: 520)
        .alert(item: Binding(get: {
            errorMessage.map { AlertWrapper(message: $0) }
        }, set: { _ in errorMessage = nil })) { wrapper in
            Alert(title: Text("Error"), message: Text(wrapper.message), dismissButton: .default(Text("OK")))
        }
    }

    private func saveTapped() {
        do {
            try store.updateAccount(accountCopy)
            presentationMode.wrappedValue.dismiss()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // Small local wrapper to allow using .alert(item:) with a simple message
    private struct AlertWrapper: Identifiable {
        let id = UUID()
        let message: String
    }
}
