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
