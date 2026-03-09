import Foundation
import SwiftUI

// MARK: - Connection Status

enum ConnectionStatus: Equatable {
    case idle
    case connected
    case disconnected
    case gameStatus(String)

    var isConnected: Bool {
        switch self {
        case .connected, .gameStatus: return true
        case .idle, .disconnected:    return false
        }
    }

    var displayText: String {
        switch self {
        case .idle:                  return ""
        case .connected:             return "Đã kết nối"
        case .disconnected:          return "Mất kết nối"
        case .gameStatus(let text):  return text
        }
    }

    var displayColor: Color {
        switch self {
        case .disconnected: return .red
        case .idle:         return .secondary
        default:            return .green
        }
    }
}

// MARK: - Account

struct Account: Identifiable, Codable, Hashable {
    var id: UUID = UUID()
    var username: String
    var password: String
    var server: String

    var createdAt: Date = Date()
    var updatedAt: Date = Date()

    /// Live character info from the game – persisted with account.
    var characterInfo: CharacterInfo?

    /// Socket connection status – runtime only, not persisted.
    var connectionStatus: ConnectionStatus = .idle

    private enum CodingKeys: String, CodingKey {
        case id, username, password, server, createdAt, updatedAt, characterInfo
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
