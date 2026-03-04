namespace Mod.ModHelper
{
	internal class CharacterInfoMessage
	{
		public string action = "updateInfo";

		// Status
		public string status;

		// Character info
		public string cName;
		public int cgender;

		// Map info
		public string mapName;
		public int mapID;
		public int zoneID;

		// Position
		public int cx;
		public int cy;

		// Character stats
		public long cHP;
		public long cHPFull;
		public long cMP;
		public long cMPFull;
		public int cStamina;
		public long cPower;
		public long cTiemNang;

		// Base stats
		public int cHPGoc;
		public int cMPGoc;
		public int cDefGoc;
		public int cDamGoc;
		public int cCriticalGoc;

		// Full stats
		public long cDamFull;
		public long cDefull;
		public int cCriticalFull;

		// Pet stats
		public long cPetHP;
		public long cPetHPFull;
		public long cPetMP;
		public long cPetMPFull;
		public int cPetStamina;
		public long cPetPower;
		public long cPetTiemNang;
		public long cPetDamFull;
		public long cPetDefull;
		public int cPetCriticalFull;

		// Currency
		public long xu;
		public int luong;
		public int luongKhoa;

		internal static CharacterInfoMessage Create(Char myChar, Char myPet)
		{
			return new CharacterInfoMessage
			{
				status = Utils.status,
				cName = myChar.cName,
				cgender = myChar.cgender,
				mapName = TileMap.mapName,
				mapID = TileMap.mapID,
				zoneID = TileMap.zoneID,
				cx = myChar.cx,
				cy = myChar.cy,
				cHP = myChar.cHP,
				cHPFull = myChar.cHPFull,
				cMP = myChar.cMP,
				cMPFull = myChar.cMPFull,
				cStamina = myChar.cStamina,
				cPower = myChar.cPower,
				cTiemNang = myChar.cTiemNang,
				cHPGoc = myChar.cHPGoc,
				cMPGoc = myChar.cMPGoc,
				cDefGoc = myChar.cDefGoc,
				cDamGoc = myChar.cDamGoc,
				cCriticalGoc = myChar.cCriticalGoc,
				cDamFull = myChar.cDamFull,
				cDefull = myChar.cDefull,
				cCriticalFull = myChar.cCriticalFull,
				cPetHP = myPet.cHP,
				cPetHPFull = myPet.cHPFull,
				cPetMP = myPet.cMP,
				cPetMPFull = myPet.cMPFull,
				cPetStamina = myPet.cStamina,
				cPetPower = myPet.cPower,
				cPetTiemNang = myPet.cTiemNang,
				cPetDamFull = myPet.cDamFull,
				cPetDefull = myPet.cDefull,
				cPetCriticalFull = myPet.cCriticalFull,
				xu = myChar.xu,
				luong = myChar.luong,
				luongKhoa = myChar.luongKhoa
			};
		}
	}
}
