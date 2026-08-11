import SwiftUI

struct AccountRowView: View {
    @EnvironmentObject private var store: AccountStore
    let account: Account
    let onLaunchOrStop: () -> Void

    private var liveAccount: Account {
        store.accounts.first(where: { $0.id == account.id }) ?? account
    }

    private var displayName: String {
        let characterName = liveAccount.characterInfo?.cName ?? ""
        return characterName.isEmpty ? liveAccount.username : characterName
    }

    var body: some View {
        HStack(spacing: 10) {
            Circle()
                .fill(liveAccount.connectionStatus.isConnected ? .green : .secondary.opacity(0.35))
                .frame(width: 32, height: 32)
                .overlay {
                    Text(displayName.prefix(1).uppercased())
                        .font(.caption.weight(.bold))
                        .foregroundStyle(.white)
                }

            VStack(alignment: .leading, spacing: 2) {
                Text(displayName)
                    .font(.body.weight(.medium))
                    .lineLimit(1)

                Text(liveAccount.characterInfo?.mapName ?? liveAccount.server)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            }

            Spacer(minLength: 8)

            if store.launching.contains(account.id) {
                ProgressView()
                    .controlSize(.small)
            } else {
                Button(action: onLaunchOrStop) {
                    Image(systemName: liveAccount.connectionStatus.isConnected ? "stop.fill" : "play.fill")
                        .font(.body.weight(.bold))
                        .frame(width: 32, height: 32)
                }
                .buttonStyle(.borderless)
                .foregroundStyle(liveAccount.connectionStatus.isConnected ? Color.red : Color.accentColor)
                .help(liveAccount.connectionStatus.isConnected ? "Stop game" : "Launch game")
            }
        }
        .padding(.vertical, 4)
        .accessibilityElement(children: .combine)
        .accessibilityLabel("\(displayName), \(liveAccount.connectionStatus.displayText.isEmpty ? "offline" : liveAccount.connectionStatus.displayText)")
    }
}
