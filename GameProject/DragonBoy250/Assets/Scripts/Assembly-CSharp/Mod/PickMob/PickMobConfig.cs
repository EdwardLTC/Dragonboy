using System.Collections.Generic;

namespace Mod.PickMob
{
	internal static class PickMobConfig
	{
		const int ID_ITEM_GEM = 77;
		const int ID_ITEM_GEM_LOCK = 861;
		static readonly sbyte[] IdSkillsBase =
		{
			2, 9, 17, 0, 4, 13, 19, 6 //21, 24, 25
		};
		static readonly short[] IdItemBlockBase =
		{
			225, 353, 354, 355, 356, 357, 358, 359, 360, 362, 1229
		};

		public static readonly List<int> IdMobsTanSat = new List<int>();
		public static readonly List<int> TypeMobsTanSat = new List<int>();
		public static readonly List<sbyte> IdSkillsTanSat = new List<sbyte>(IdSkillsBase);

		internal static int TimesAutoPickItemMax = 14;
		public static readonly List<short> IdItemPicks = new List<short>();
		public static readonly List<short> IdItemBlocks = new List<short>(IdItemBlockBase);
		public static readonly List<sbyte> TypeItemPicks = new List<sbyte>();
		public static readonly List<sbyte> TypeItemBlocks = new List<sbyte>();

		internal static bool IsTanSat { get; set; }
		internal static bool IsNeSieuQuai { get; set; } = true;
		internal static bool IsAutoPickItems { get; set; } = true;
		internal static bool IsItemMe { get; set; } = true;
		internal static bool IsLimitTimesPickItem { get; set; } = true;
		internal static bool IsAttackMonsterBySendCommand { get; set; }
		internal static bool IsSkipPickEventItems { get; set; } = true;
		internal static bool IsAutoChangeZone { get; set; } = true;
		internal static bool IsAutoFusion { get; set; } = true;

		internal static void SetAutoChangeZone(bool newState)
		{
			IsAutoChangeZone = newState;
		}

		internal static void SetAutoFusion(bool newState)
		{
			IsAutoFusion = newState;
		}

		internal static void Init()
		{
			PickMobController.gI.Toggle(true);
		}

		internal static void SetSlaughter(bool newState)
		{
			IsTanSat = newState;
		}

		internal static void SetAvoidSuperMonster(bool newState)
		{
			IsNeSieuQuai = newState;
		}

		internal static void SetAutoPickItems(bool newState)
		{
			IsAutoPickItems = newState;
		}

		internal static void SetAutoPickItemsFromOthers(bool newState)
		{
			IsItemMe = newState;
		}

		internal static void SetPickUpLimited(bool newState)
		{
			IsLimitTimesPickItem = newState;
		}
	}
}
