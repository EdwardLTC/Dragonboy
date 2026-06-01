namespace Mod.ModHelper
{
	internal class CharacterInfoMessage
	{
		public string action = "updateInfo";
		public int cCriticalFull;
		public int cCriticalGoc;

		// Full stats
		public long cDamFull;
		public int cDamGoc;
		public int cDefGoc;
		public long cDefull;
		public int cgender;

		// Character stats
		public long cHP;
		public long cHPFull;

		// Base stats
		public int cHPGoc;
		public long cMP;
		public long cMPFull;
		public int cMPGoc;

		// Character info
		public string cName;
		public int cPetCriticalFull;
		public long cPetDamFull;
		public long cPetDefull;

		// Pet stats
		public long cPetHP;
		public long cPetHPFull;
		public long cPetMP;
		public long cPetMPFull;
		public long cPetPower;
		public int cPetStamina;
		public long cPetTiemNang;
		public long cPower;
		public int cStamina;
		public long cTiemNang;

		// Position
		public int cx;
		public int cy;
		public int luong;
		public int luongKhoa;
		public int mapID;

		// Map info
		public string mapName;

		// Status
		public string status;

		// Currency
		public long xu;
		public int zoneID;

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
