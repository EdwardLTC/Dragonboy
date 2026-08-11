import SwiftUI

struct AccountDetailView: View {
    @EnvironmentObject private var store: AccountStore

    let account: Account
    let onLaunchOrStop: () -> Void
    let onRemove: () -> Void

    @State private var accountCopy: Account
    @State private var errorMessage: String?
    @State private var showSaved = false
    @State private var showingDeleteConfirmation = false

    init(account: Account, onLaunchOrStop: @escaping () -> Void, onRemove: @escaping () -> Void) {
        self.account = account
        self.onLaunchOrStop = onLaunchOrStop
        self.onRemove = onRemove
        _accountCopy = State(initialValue: account)
    }

    private var liveAccount: Account {
        store.accounts.first(where: { $0.id == account.id }) ?? account
    }

    private var displayName: String {
        let characterName = liveAccount.characterInfo?.cName ?? ""
        return characterName.isEmpty ? liveAccount.username : characterName
    }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 24) {
                accountHeader

                if let info = liveAccount.characterInfo {
                    characterOverview(info)
                    vitals(info)
                    characterStats(info)
                } else {
                    waitingState
                }

                accountSettings
            }
            .padding(28)
            .frame(maxWidth: 760, alignment: .leading)
        }
        .navigationTitle(displayName)
        .toolbar {
            ToolbarItemGroup(placement: .primaryAction) {
                Button(liveAccount.connectionStatus.isConnected ? "Stop game" : "Launch game", systemImage: liveAccount.connectionStatus.isConnected ? "stop.fill" : "play.fill") {
                    onLaunchOrStop()
                }
                .buttonStyle(.borderedProminent)
                .font(.headline)
                .tint(liveAccount.connectionStatus.isConnected ? .red : .accentColor)
                .disabled(store.launching.contains(account.id))

                Button("Remove account", systemImage: "trash", role: .destructive) {
                    showingDeleteConfirmation = true
                }
                .help("Remove account")
            }
        }
        .confirmationDialog("Remove this account?", isPresented: $showingDeleteConfirmation, titleVisibility: .visible) {
            Button("Remove \(liveAccount.username)", role: .destructive, action: onRemove)
        } message: {
            Text("This removes the saved account from Account Manager. It does not affect the game account.")
        }
        .alert("Could not save account", isPresented: Binding(
            get: { errorMessage != nil },
            set: { if !$0 { errorMessage = nil } }
        )) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(errorMessage ?? "")
        }
    }

    private var accountHeader: some View {
        HStack(spacing: 16) {
            Circle()
                .fill(liveAccount.connectionStatus.isConnected ? Color.green.gradient : Color.gray.gradient)
                .frame(width: 64, height: 64)
                .overlay {
                    Text(displayName.prefix(1).uppercased())
                        .font(.title.weight(.bold))
                        .foregroundStyle(.white)
                }

            VStack(alignment: .leading, spacing: 5) {
                Text(displayName)
                    .font(.title.bold())
                Text("\(liveAccount.username)  ·  \(liveAccount.server)")
                    .foregroundStyle(.secondary)
                Label(statusText, systemImage: statusImage)
                    .font(.subheadline.weight(.medium))
                    .foregroundStyle(liveAccount.connectionStatus.displayColor)
            }

            Spacer()
        }
    }

    private var statusText: String {
        if store.launching.contains(account.id) { return "Starting game…" }
        if liveAccount.connectionStatus.displayText.isEmpty { return "Not running" }
        return liveAccount.connectionStatus.displayText
    }

    private var statusImage: String {
        if store.launching.contains(account.id) { return "arrow.triangle.2.circlepath" }
        return liveAccount.connectionStatus.isConnected ? "checkmark.circle.fill" : "circle"
    }

    private var waitingState: some View {
        ContentUnavailableView {
            Label("No live character data", systemImage: "person.crop.circle.badge.clock")
        } description: {
            Text(liveAccount.connectionStatus.isConnected
                 ? "Connected to the game. Waiting for character data."
                 : "Launch the game to see this character’s location, health, and stats.")
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 36)
        .background(.quaternary, in: RoundedRectangle(cornerRadius: 16, style: .continuous))
    }

    private func characterOverview(_ info: CharacterInfo) -> some View {
        VStack(alignment: .leading, spacing: 20) {
            Label("Location", systemImage: "map.fill")
                .font(.headline)

            LazyVGrid(columns: [GridItem(.adaptive(minimum: 130), spacing: 12)], spacing: 12) {
                metric("Map", value: info.mapName, icon: "map.fill")
                metric("Map ID", value: "\(info.mapID)", icon: "number")
                metric("Zone", value: "\(info.zoneID)", icon: "square.grid.2x2")
                metric("Position", value: "\(info.cx), \(info.cy)", icon: "location.fill")
            }

            Divider()

            Label("Currency", systemImage: "banknote.fill")
                .font(.headline)

            LazyVGrid(columns: [GridItem(.adaptive(minimum: 130), spacing: 12)], spacing: 12) {
                metric("Xu", value: formatNumber(info.xu), icon: "bitcoinsign.circle.fill")
                metric("Diamond", value: formatNumber(Int64(info.luong)), icon: "diamond.fill")
                metric("Ruby", value: formatNumber(Int64(info.luongKhoa)), icon: "circle.hexagongrid.fill")
            }
        }
    }

    private func vitals(_ info: CharacterInfo) -> some View {
        VStack(alignment: .leading, spacing: 14) {
            Label("Vitals", systemImage: "heart.fill")
                .font(.headline)
            statBar(label: "Health", current: info.cHP, max: info.cHPFull, color: .red)
            statBar(label: "Ki", current: info.cMP, max: info.cMPFull, color: .blue)
            if info.cPetHPFull > 0 || info.cPetMPFull > 0 {
                Divider()
                statBar(label: "Pet health", current: info.cPetHP, max: info.cPetHPFull, color: .orange)
                statBar(label: "Pet ki", current: info.cPetMP, max: info.cPetMPFull, color: .teal)
            }
        }
        .padding(18)
        .background(.quaternary, in: RoundedRectangle(cornerRadius: 16, style: .continuous))
    }

    private func characterStats(_ info: CharacterInfo) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Label("Stats", systemImage: "chart.bar.fill")
                .font(.headline)
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 120), spacing: 12)], spacing: 12) {
                metric("Power", value: formatNumber(info.cPower), icon: "bolt.fill")
                metric("Potential", value: formatNumber(info.cTiemNang), icon: "sparkles")
                metric("Damage", value: formatNumber(info.cDamFull), icon: "burst.fill")
                metric("Defense", value: formatNumber(info.cDefull), icon: "shield.fill")
                metric("Critical", value: "\(info.cCriticalFull)", icon: "scope")
                metric("Stamina", value: "\(info.cStamina)", icon: "figure.run")
            }
            Text("Updated \(info.lastUpdated, format: .dateTime.hour().minute().second())")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }

    private var accountSettings: some View {
        DisclosureGroup("Account settings") {
            VStack(spacing: 12) {
                TextField("Username", text: $accountCopy.username)
                TextField("Server", text: $accountCopy.server)
                SecureField("Password", text: $accountCopy.password)
                HStack {
                    if showSaved {
                        Label("Saved", systemImage: "checkmark.circle.fill")
                            .font(.caption)
                            .foregroundStyle(.green)
                    }
                    Spacer()
                    Button("Save changes", action: saveTapped)
                        .buttonStyle(.bordered)
                }
            }
            .padding(.top, 12)
        }
        .padding(18)
        .background(.quaternary, in: RoundedRectangle(cornerRadius: 16, style: .continuous))
    }

    private func metric(_ label: String, value: String, icon: String) -> some View {
        VStack(alignment: .leading, spacing: 5) {
            Label(label, systemImage: icon)
                .font(.caption)
                .foregroundStyle(.secondary)
            Text(value.isEmpty ? "—" : value)
                .font(.body.weight(.semibold))
                .lineLimit(1)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(12)
        .background(.quaternary, in: RoundedRectangle(cornerRadius: 12, style: .continuous))
    }

    private func statBar(label: String, current: Int64, max: Int64, color: Color) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                Text(label).font(.subheadline.weight(.medium))
                Spacer()
                Text("\(formatNumber(current)) / \(formatNumber(max))")
                    .font(.subheadline.monospacedDigit())
                    .foregroundStyle(.secondary)
            }
            ProgressView(value: max > 0 ? min(Double(current) / Double(max), 1) : 0)
                .tint(color)
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

    private func formatNumber(_ value: Int64) -> String {
        value.formatted(.number.grouping(.automatic))
    }
}
