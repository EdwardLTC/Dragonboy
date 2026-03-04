import Foundation

/// Live character data received from the game client every ~1 second.
struct CharacterInfo: Codable, Equatable {
    var lastUpdated: Date = Date()

    // Status
    var status: String = ""

    // Character info
    var cName: String = ""
    var cgender: Int = 0

    // Map info
    var mapName: String = ""
    var mapID: Int = 0
    var zoneID: Int = 0

    // Position
    var cx: Int = 0
    var cy: Int = 0

    // Character stats
    var cHP: Int64 = 0
    var cHPFull: Int64 = 0
    var cMP: Int64 = 0
    var cMPFull: Int64 = 0
    var cStamina: Int = 0
    var cPower: Int64 = 0
    var cTiemNang: Int64 = 0

    // Base stats
    var cHPGoc: Int = 0
    var cMPGoc: Int = 0
    var cDefGoc: Int = 0
    var cDamGoc: Int = 0
    var cCriticalGoc: Int = 0

    // Full stats
    var cDamFull: Int64 = 0
    var cDefull: Int64 = 0
    var cCriticalFull: Int = 0

    // Pet stats
    var cPetHP: Int64 = 0
    var cPetHPFull: Int64 = 0
    var cPetMP: Int64 = 0
    var cPetMPFull: Int64 = 0
    var cPetStamina: Int = 0
    var cPetPower: Int64 = 0
    var cPetTiemNang: Int64 = 0
    var cPetDamFull: Int64 = 0
    var cPetDefull: Int64 = 0
    var cPetCriticalFull: Int = 0

    // Currency
    var xu: Int64 = 0
    var luong: Int = 0
    var luongKhoa: Int = 0

    // MARK: - Computed helpers

    var genderString: String {
        switch cgender {
        case 0: return "Trái Đất"
        case 1: return "Namek"
        case 2: return "Xayda"
        default: return "Unknown"
        }
    }

    var hpPercent: Double {
        cHPFull > 0 ? Double(cHP) / Double(cHPFull) * 100 : 0
    }

    var mpPercent: Double {
        cMPFull > 0 ? Double(cMP) / Double(cMPFull) * 100 : 0
    }

    var petHPPercent: Double {
        cPetHPFull > 0 ? Double(cPetHP) / Double(cPetHPFull) * 100 : 0
    }

    var petMPPercent: Double {
        cPetMPFull > 0 ? Double(cPetMP) / Double(cPetMPFull) * 100 : 0
    }

    // MARK: - Parse from JSON dictionary

    static func from(_ dict: [String: Any]) -> CharacterInfo {
        var info = CharacterInfo()
        info.lastUpdated = Date()

        info.status         = dict["status"] as? String ?? ""
        info.cName          = dict["cName"] as? String ?? ""
        info.cgender        = (dict["cgender"] as? NSNumber)?.intValue ?? 0

        info.mapName        = dict["mapName"] as? String ?? ""
        info.mapID          = (dict["mapID"] as? NSNumber)?.intValue ?? 0
        info.zoneID         = (dict["zoneID"] as? NSNumber)?.intValue ?? 0

        info.cx             = (dict["cx"] as? NSNumber)?.intValue ?? 0
        info.cy             = (dict["cy"] as? NSNumber)?.intValue ?? 0

        info.cHP            = (dict["cHP"] as? NSNumber)?.int64Value ?? 0
        info.cHPFull        = (dict["cHPFull"] as? NSNumber)?.int64Value ?? 0
        info.cMP            = (dict["cMP"] as? NSNumber)?.int64Value ?? 0
        info.cMPFull        = (dict["cMPFull"] as? NSNumber)?.int64Value ?? 0
        info.cStamina       = (dict["cStamina"] as? NSNumber)?.intValue ?? 0
        info.cPower         = (dict["cPower"] as? NSNumber)?.int64Value ?? 0
        info.cTiemNang      = (dict["cTiemNang"] as? NSNumber)?.int64Value ?? 0

        info.cHPGoc         = (dict["cHPGoc"] as? NSNumber)?.intValue ?? 0
        info.cMPGoc         = (dict["cMPGoc"] as? NSNumber)?.intValue ?? 0
        info.cDefGoc        = (dict["cDefGoc"] as? NSNumber)?.intValue ?? 0
        info.cDamGoc        = (dict["cDamGoc"] as? NSNumber)?.intValue ?? 0
        info.cCriticalGoc   = (dict["cCriticalGoc"] as? NSNumber)?.intValue ?? 0

        info.cDamFull       = (dict["cDamFull"] as? NSNumber)?.int64Value ?? 0
        info.cDefull        = (dict["cDefull"] as? NSNumber)?.int64Value ?? 0
        info.cCriticalFull  = (dict["cCriticalFull"] as? NSNumber)?.intValue ?? 0

        info.cPetHP         = (dict["cPetHP"] as? NSNumber)?.int64Value ?? 0
        info.cPetHPFull     = (dict["cPetHPFull"] as? NSNumber)?.int64Value ?? 0
        info.cPetMP         = (dict["cPetMP"] as? NSNumber)?.int64Value ?? 0
        info.cPetMPFull     = (dict["cPetMPFull"] as? NSNumber)?.int64Value ?? 0
        info.cPetStamina    = (dict["cPetStamina"] as? NSNumber)?.intValue ?? 0
        info.cPetPower      = (dict["cPetPower"] as? NSNumber)?.int64Value ?? 0
        info.cPetTiemNang   = (dict["cPetTiemNang"] as? NSNumber)?.int64Value ?? 0
        info.cPetDamFull    = (dict["cPetDamFull"] as? NSNumber)?.int64Value ?? 0
        info.cPetDefull     = (dict["cPetDefull"] as? NSNumber)?.int64Value ?? 0
        info.cPetCriticalFull = (dict["cPetCriticalFull"] as? NSNumber)?.intValue ?? 0

        info.xu             = (dict["xu"] as? NSNumber)?.int64Value ?? 0
        info.luong          = (dict["luong"] as? NSNumber)?.intValue ?? 0
        info.luongKhoa      = (dict["luongKhoa"] as? NSNumber)?.intValue ?? 0

        return info
    }
}
