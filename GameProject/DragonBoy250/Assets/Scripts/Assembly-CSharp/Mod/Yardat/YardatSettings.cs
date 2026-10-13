using System.Collections.Generic;
using System.Linq;

namespace Mod.Yardat
{
	public static class YardatSettings
	{
		const string RMS_AUTO_CHANGE_FOCUS = "yardat_auto_change_focus";
		const string RMS_AUTO_PORATA = "yardat_auto_porata";
		const string RMS_PET_GO_HOME = "yardat_pet_go_home";
		const string RMS_AUTO_SELECT_ALL = "yardat_auto_select_all";
		const string RMS_AUTO_MOVE = "yardat_auto_move";
		const string RMS_TARGET_IDS = "yardat_target_ids";

		public static bool AutoChangeFocus { get; set; } = true;
		public static bool AutoPorata { get; set; }
		public static bool PetGoHome { get; set; } = true;
		public static bool AutoSelectAllYardat { get; set; } = true;
		public static bool AutoMoveToTarget { get; set; }
		public static HashSet<int> CustomTargetIds { get; private set; } = new HashSet<int>();

		public static void Load()
		{
			AutoChangeFocus = ModStorage.ReadBool(RMS_AUTO_CHANGE_FOCUS, true);
			AutoPorata = ModStorage.ReadBool(RMS_AUTO_PORATA);
			PetGoHome = ModStorage.ReadBool(RMS_PET_GO_HOME, true);
			AutoSelectAllYardat = ModStorage.ReadBool(RMS_AUTO_SELECT_ALL, true);
			AutoMoveToTarget = ModStorage.ReadBool(RMS_AUTO_MOVE);

			string idsStr = ModStorage.ReadString(RMS_TARGET_IDS);
			CustomTargetIds = new HashSet<int>();
			if (!string.IsNullOrEmpty(idsStr))
			{
				string[] parts = idsStr.Split(',');
				foreach (string part in parts)
				{
					if (int.TryParse(part.Trim(), out int id))
					{
						CustomTargetIds.Add(id);
					}
				}
			}
		}

		public static void Save()
		{
			ModStorage.WriteBool(RMS_AUTO_CHANGE_FOCUS, AutoChangeFocus);
			ModStorage.WriteBool(RMS_AUTO_PORATA, AutoPorata);
			ModStorage.WriteBool(RMS_PET_GO_HOME, PetGoHome);
			ModStorage.WriteBool(RMS_AUTO_SELECT_ALL, AutoSelectAllYardat);
			ModStorage.WriteBool(RMS_AUTO_MOVE, AutoMoveToTarget);

			string idsStr = string.Join(",", CustomTargetIds.Select(id => id.ToString()).ToArray());
			ModStorage.WriteString(RMS_TARGET_IDS, idsStr);
		}
	}
}
