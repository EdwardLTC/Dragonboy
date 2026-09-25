using Mod.BoMong.QuestHandler;

namespace Mod.BoMong
{
	internal static class BoMongSettings
	{
		const string RMS_DIFFICULTY = "bomong_difficulty";
		const string RMS_SKIP_MONSTER = "bomong_skip_monster";
		const string RMS_SKIP_GOLD = "bomong_skip_gold";
		const string RMS_SKIP_PLAYER = "bomong_skip_player";
		const string RMS_SKIP_LURCHER = "bomong_skip_lurcher";

		public static string Difficulty { get; private set; } = "siêu khó";

		public static bool SkipTrainMonster { get; set; }

		public static bool SkipTrainGold { get; set; }

		public static bool SkipKillPlayer { get; set; }

		public static bool SkipKillLurcher { get; set; } = true;

		public static void Load()
		{
			Difficulty = ModStorage.ReadString(RMS_DIFFICULTY, "siêu khó");
			if (string.IsNullOrEmpty(Difficulty))
			{
				Difficulty = "siêu khó";
			}

			SkipTrainMonster = ModStorage.ReadBool(RMS_SKIP_MONSTER);
			SkipTrainGold = ModStorage.ReadBool(RMS_SKIP_GOLD);
			SkipKillPlayer = ModStorage.ReadBool(RMS_SKIP_PLAYER);
			SkipKillLurcher = ModStorage.ReadBool(RMS_SKIP_LURCHER, true);
		}

		public static void Save()
		{
			ModStorage.WriteString(RMS_DIFFICULTY, Difficulty);
			ModStorage.WriteBool(RMS_SKIP_MONSTER, SkipTrainMonster);
			ModStorage.WriteBool(RMS_SKIP_GOLD, SkipTrainGold);
			ModStorage.WriteBool(RMS_SKIP_PLAYER, SkipKillPlayer);
			ModStorage.WriteBool(RMS_SKIP_LURCHER, SkipKillLurcher);
		}

		public static bool IsQuestSkipped(QuestType type)
		{
			return type switch
			{
				QuestType.TrainMonster => SkipTrainMonster,
				QuestType.TrainGold => SkipTrainGold,
				QuestType.KillPlayer => SkipKillPlayer,
				QuestType.KillLurcher => SkipKillLurcher,
				_ => false
			};
		}

		public static void CycleDifficulty()
		{
			Difficulty = Difficulty switch
			{
				"dễ" => "khó",
				"khó" => "siêu khó",
				"siêu khó" => "dễ",
				_ => "khó"
			};
			Save();
		}
	}
}
