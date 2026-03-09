import SwiftUI

struct AccountDetailView: View {
    @EnvironmentObject var store: AccountStore

    @State private var accountCopy: Account
    @State private var errorMessage: String?
    @State private var showSaved: Bool = false

    init(account: Account) {
        _accountCopy = State(initialValue: account)
    }

    /// Always read the latest from the store so live updates appear.
    private var liveAccount: Account {
        store.accounts.first(where: { $0.id == accountCopy.id }) ?? accountCopy
    }

    var body: some View {
        ScrollView {
            VStack(spacing: 16) {
                GroupBox(label: Label("Account Settings", systemImage: "person.crop.circle")) {
                    VStack(spacing: 8) {
                        TextField("Username", text: $accountCopy.username)
                        TextField("Server", text: $accountCopy.server)
                        SecureField("Password (leave blank to keep)", text: $accountCopy.password)

                        HStack {
                            if showSaved {
                                Text("Saved ✓").font(.caption).foregroundColor(.green)
                                    .transition(.opacity)
                            }
                            Spacer()
                            Button("Save") { saveTapped() }
                                .buttonStyle(.borderedProminent)
                        }
                    }
                    .padding(.top, 4)
                }

                if let info = liveAccount.characterInfo {
                    GroupBox(label: Label("Character", systemImage: "person.fill")) {
                        LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], alignment: .leading, spacing: 6) {
                            infoRow("Name", info.cName)
                            infoRow("Gender", info.genderString)
                            HStack(spacing: 4) {
                                Text("Status:").font(.caption).foregroundColor(.secondary)
                                Text(liveAccount.connectionStatus == .idle ? info.status : liveAccount.connectionStatus.displayText)
                                    .font(.caption).fontWeight(.medium)
                                    .foregroundColor(liveAccount.connectionStatus.displayColor)
                            }
                            infoRow("Position", "(\(info.cx), \(info.cy))")
                        }
                        .padding(.top, 4)
                    }

                    GroupBox(label: Label("Map", systemImage: "map")) {
                        LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], alignment: .leading, spacing: 6) {
                            infoRow("Map", info.mapName)
                            infoRow("Map ID", "\(info.mapID)")
                            infoRow("Zone", "\(info.zoneID)")
                        }
                        .padding(.top, 4)
                    }

                    GroupBox(label: Label("Stats", systemImage: "heart.fill")) {
                        VStack(spacing: 8) {
                            statBar(label: "HP", current: info.cHP, max: info.cHPFull, color: .red)
                            statBar(label: "MP", current: info.cMP, max: info.cMPFull, color: .blue)

                            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], alignment: .leading, spacing: 6) {
                                infoRow("Stamina", "\(info.cStamina)")
                                infoRow("Power", formatNumber(info.cPower))
                                infoRow("Tiềm Năng", formatNumber(info.cTiemNang))
                                infoRow("DAM", formatNumber(info.cDamFull))
                                infoRow("DEF", formatNumber(info.cDefull))
                                infoRow("Critical", "\(info.cCriticalFull)")
                            }
                        }
                        .padding(.top, 4)
                    }

                    GroupBox(label: Label("Pet", systemImage: "hare.fill")) {
                        VStack(spacing: 8) {
                            statBar(label: "Pet HP", current: info.cPetHP, max: info.cPetHPFull, color: .red)
                            statBar(label: "Pet MP", current: info.cPetMP, max: info.cPetMPFull, color: .blue)

                            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], alignment: .leading, spacing: 6) {
                                infoRow("Stamina", "\(info.cPetStamina)")
                                infoRow("Power", formatNumber(info.cPetPower))
                                infoRow("Tiềm Năng", formatNumber(info.cPetTiemNang))
                                infoRow("DAM", formatNumber(info.cPetDamFull))
                                infoRow("DEF", formatNumber(info.cPetDefull))
                                infoRow("Critical", "\(info.cPetCriticalFull)")
                            }
                        }
                        .padding(.top, 4)
                    }

                    GroupBox(label: Label("Currency", systemImage: "dollarsign.circle")) {
                        LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible()), GridItem(.flexible())], alignment: .leading, spacing: 6) {
                            infoRow("Vàng", formatNumber(info.xu))
                            infoRow("Ngọc xanh", formatNumber(Int64(info.luong)))
                            infoRow("Ngọc hồng", formatNumber(Int64(info.luongKhoa)))
                        }
                        .padding(.top, 4)
                    }

                    Text("Last updated: \(info.lastUpdated, formatter: timeFormatter)")
                        .font(.caption2)
                        .foregroundColor(.secondary)
                } else if liveAccount.connectionStatus.isConnected {
                    GroupBox {
                        VStack(spacing: 8) {
                            HStack {
                                ProgressView().controlSize(.small)
                                Text("Waiting for game data…")
                                    .foregroundColor(.secondary)
                            }
                            Text(liveAccount.connectionStatus.displayText)
                                .font(.caption).fontWeight(.medium)
                                .foregroundColor(liveAccount.connectionStatus.displayColor)
                        }
                        .frame(maxWidth: .infinity, alignment: .center)
                        .padding()
                    }
                } else {
                    GroupBox {
                        VStack(spacing: 8) {
                            Text("Launch the game to see live character info.")
                                .foregroundColor(.secondary)
                            if liveAccount.connectionStatus != .idle {
                                Text(liveAccount.connectionStatus.displayText)
                                    .font(.caption).fontWeight(.medium)
                                    .foregroundColor(liveAccount.connectionStatus.displayColor)
                            }
                        }
                        .frame(maxWidth: .infinity, alignment: .center)
                        .padding()
                    }
                }
            }
            .padding()
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .alert(item: Binding(get: {
            errorMessage.map { AlertWrapper(message: $0) }
        }, set: { _ in errorMessage = nil })) { wrapper in
            Alert(title: Text("Error"), message: Text(wrapper.message), dismissButton: .default(Text("OK")))
        }
    }

    private func saveTapped() {
        do {
            try store.updateAccount(accountCopy)
            withAnimation { showSaved = true }
            DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                withAnimation { showSaved = false }
            }
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    @ViewBuilder
    private func infoRow(_ label: String, _ value: String) -> some View {
        HStack(spacing: 4) {
            Text(label + ":").font(.caption).foregroundColor(.secondary)
            Text(value).font(.caption).fontWeight(.medium)
        }
    }

    @ViewBuilder
    private func statBar(label: String, current: Int64, max: Int64, color: Color) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            HStack {
                Text(label).font(.caption).foregroundColor(.secondary)
                Spacer()
                Text("\(formatNumber(current)) / \(formatNumber(max))")
                    .font(.caption).fontWeight(.medium)
            }
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    RoundedRectangle(cornerRadius: 3)
                        .fill(color.opacity(0.15))
                        .frame(height: 6)
                    RoundedRectangle(cornerRadius: 3)
                        .fill(color)
                        .frame(width: max > 0 ? geo.size.width * CGFloat(current) / CGFloat(max) : 0, height: 6)
                }
            }
            .frame(height: 6)
        }
    }

    private func formatNumber(_ value: Int64) -> String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .decimal
        formatter.groupingSeparator = ","
        return formatter.string(from: NSNumber(value: value)) ?? "\(value)"
    }

    private var timeFormatter: DateFormatter {
        let f = DateFormatter()
        f.dateFormat = "HH:mm:ss"
        return f
    }

    private struct AlertWrapper: Identifiable {
        let id = UUID()
        let message: String
    }
}
