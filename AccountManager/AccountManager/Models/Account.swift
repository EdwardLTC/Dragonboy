import Foundation

struct Account: Identifiable, Codable, Hashable {
    var id: UUID = UUID()
    var username: String
    var password: String
    var server: String

    var isRunning: Bool = false
    var pid: Int?

    var createdAt: Date = Date()
    var updatedAt: Date = Date()

    /// Live character info from the game – persisted with account.
    var characterInfo: CharacterInfo?

    /// Socket connection status – runtime only, not persisted.
    var connectionStatus: String = ""

    private enum CodingKeys: String, CodingKey {
        case id, username, password, server, isRunning, pid, createdAt, updatedAt, characterInfo
    }

    // MARK: - Hashable (exclude characterInfo)

    static func == (lhs: Account, rhs: Account) -> Bool {
        lhs.id == rhs.id
    }

    func hash(into hasher: inout Hasher) {
        hasher.combine(id)
    }

    init(id: UUID = UUID(), username: String, server: String, password: String) {
        self.id = id
        self.username = username
        self.server = server
        self.password = password
    }

    var uniqueKey: String {
        return "\(username)@\(server)".lowercased()
    }
}
