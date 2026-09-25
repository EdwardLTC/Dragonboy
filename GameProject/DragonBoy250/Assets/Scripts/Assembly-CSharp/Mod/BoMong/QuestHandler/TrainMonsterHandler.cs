using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mod.PickMob;

namespace Mod.BoMong.QuestHandler
{
	public class TrainMonsterHandler : IQuestHandler
	{
		static readonly List<(string NameMob, int IdMap, int IdMob)> MobDatabase = new List<(string NameMob, int IdMap, int IdMob)>
		{
			("Mộc nhân", 14, 0),
			("Khủng long", 1, 1),
			("Lợn lòi", 8, 2),
			("Quỷ đất", 15, 3),
			("Khủng long mẹ", 2, 4),
			("Lợn lòi mẹ", 9, 5),
			("Quỷ đất mẹ", 16, 6),
			("Thằn lằn bay", 3, 7),
			("Phi long", 11, 8),
			("Quỷ bay", 17, 9),
			("Thằn lằn mẹ", 4, 10),
			("Phi long mẹ", 12, 11),
			("Quỷ bay mẹ", 18, 12),
			("Ốc mượn hồn", 29, 13),
			("Ốc sên", 33, 14),
			("Heo Xayda mẹ", 37, 15),
			("Heo rừng", 28, 16),
			("Heo da xanh", 32, 17),
			("Heo Xayda", 36, 18),
			("Heo rừng mẹ", 6, 19),
			("Heo xanh mẹ", 10, 20),
			("Alien", 19, 21),
			("Bulon", 30, 22),
			("Ukulele", 34, 23),
			("Quỷ mập", 38, 24),
			("Tambourine", 6, 25),
			("Drum", 10, 26),
			("Akkuman", 19, 27),
			("Không tặc", 29, 31),
			("Quỷ đầu to", 33, 32),
			("Quỷ địa ngục", 37, 33),
			("Nappa", 68, 39),
			("Soldier", 70, 40),
			("Appule", 71, 41),
			("Raspberry", 71, 42),
			("Thằn lằn xanh", 72, 43),
			("Quỷ đầu nhọn", 64, 44),
			("Quỷ đầu vàng", 63, 45),
			("Quỷ da tím", 66, 46),
			("Quỷ già", 67, 47),
			("Cá sấu", 73, 48),
			("Dơi da xanh", 67, 49),
			("Quỷ chim", 81, 50),
			("Lính đầu trọc", 74, 51),
			("Lính tai dài", 76, 52),
			("Lính vũ trụ", 77, 53),
			("Khỉ lông đen", 82, 54),
			("Khỉ giáp sắt", 83, 55),
			("Khỉ lông đỏ", 79, 56),
			("Khỉ lông vàng", 80, 57),
			("Xên con cấp 1", 92, 58),
			("Xên con cấp 2", 93, 59),
			("Xên con cấp 3", 94, 60),
			("Xên con cấp 4", 96, 61),
			("Xên con cấp 5", 97, 62),
			("Xên con cấp 6", 98, 63),
			("Xên con cấp 7", 99, 64),
			("Xên con cấp 8", 100, 65),
			("Tai tím", 106, 66),
			("Abo", 107, 67),
			("Kado", 109, 68),
			("Da xanh", 110, 69),
			("Khỉ lông xanh", 155, 78),
			("Taburine Đỏ", 155, 79),
			("Ếch mặt đỏ", 166, 86),
			("Jinai", 166, 87),
			("Máy đo sức mạnh", 42, 94)
		};

		public float intervalHandleQuest => 5f;

		public int MapIDForThisQuest(string questMessage)
		{
			int startIdx = questMessage.IndexOf("hạ", StringComparison.OrdinalIgnoreCase) + 2;
			int endIdx = questMessage.IndexOf("địa điểm", StringComparison.OrdinalIgnoreCase);

			if (startIdx < 2 || endIdx < startIdx)
			{
				return -1;
			}

			string nameMob = questMessage.Substring(startIdx, endIdx - startIdx).Trim();

			if (string.IsNullOrEmpty(nameMob))
			{
				return -1;
			}

			Dictionary<int, int> mapMobData = GetMapMobID(nameMob);

			if (mapMobData == null)
			{
				ChatPopup.addChatPopupMultiLine("Lỗi tìm map quái", 0, null);
				return -1;
			}

			return mapMobData.Keys.First();
		}

		public void OnQuestCompleted()
		{
			Pk9rPickMob.SetSlaughter(false);
		}

		public IEnumerator HandleQuest()
		{
			int? zoneId = FindLowestPlayerZoneId();

			if (zoneId.HasValue && zoneId.Value != TileMap.zoneID)
			{
				Service.gI().requestChangeZone(zoneId.Value, 0);
			}

			// Slaughter is handled by Pk9rPickMob, so we just yield return null here to keep the coroutine running.
			yield return null;
		}

		public void PreHandleQuest()
		{
			Pk9rPickMob.SetSlaughter(true);
		}

		[CanBeNull]
		static int? FindLowestPlayerZoneId()
		{
			GameScr gameScr = GameScr.gI();
			int[] zones = gameScr.zones;
			int[] numPlayer = gameScr.numPlayer;

			if (zones == null || numPlayer == null)
			{
				return null;
			}

			int count = System.Math.Min(zones.Length, numPlayer.Length);

			int? bestZoneId = null;
			int minPlayer = int.MaxValue;

			for (int i = 0; i < count; i++)
			{
				int zoneId = zones[i];
				int playerCount = numPlayer[i];

				if (playerCount < minPlayer)
				{
					minPlayer = playerCount;
					bestZoneId = zoneId;
				}
			}

			if (bestZoneId == TileMap.zoneID)
			{
				return null;
			}

			return bestZoneId;
		}

		[CanBeNull]
		static Dictionary<int, int> GetMapMobID(string mobName)
		{
			if (string.IsNullOrEmpty(mobName?.Trim()))
			{
				return null;
			}

			string searchName = mobName.ToLower().Trim();
			int bestMatchLength = -1;
			Dictionary<int, int> result = null;

			foreach ((string NameMob, int IdMap, int IdMob) mob in MobDatabase)
			{
				string dbName = mob.NameMob.ToLower().Trim();

				if (searchName.Contains(dbName) && dbName.Length > bestMatchLength)
				{
					bestMatchLength = dbName.Length;

					result = new Dictionary<int, int>
					{
						{
							mob.IdMap, mob.IdMob
						}
					};
				}
			}

			return result;
		}
	}
}
