import SwiftUI

struct AddAccountView: View {
    @EnvironmentObject var store: AccountStore
    @Environment(\.presentationMode) private var presentationMode

    @State private var username: String = ""
    @State private var server: String = ""
    @State private var password: String = ""
    @State private var errorMessage: String?

    var body: some View {
        VStack(spacing: 12) {
            Text("Add Account").font(.title2)

            TextField("Username", text: $username)
            TextField("Server", text: $server)
            SecureField("Password", text: $password)

            HStack {
                Spacer()
                Button("Cancel") {
                    presentationMode.wrappedValue.dismiss()
                }
                Button("Create") {
                    createTapped()
                }
                .keyboardShortcut(.defaultAction)
                .disabled(username.isEmpty || server.isEmpty)
            }
        }
        .padding()
        .frame(width: 420)
        .alert(item: Binding(get: {
            errorMessage.map { AlertWrapper(message: $0) }
        }, set: { _ in errorMessage = nil })) { wrapper in
            Alert(title: Text("Error"), message: Text(wrapper.message), dismissButton: .default(Text("OK")))
        }
    }

    private func createTapped() {
        let account = Account(username: username, server: server, password: password)
        do {
            try store.addAccount(account)
            presentationMode.wrappedValue.dismiss()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

private struct AlertWrapper: Identifiable {
    let id = UUID()
    let message: String
}
