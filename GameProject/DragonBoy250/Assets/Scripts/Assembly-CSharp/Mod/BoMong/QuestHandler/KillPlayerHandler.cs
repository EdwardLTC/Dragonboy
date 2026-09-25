using System.Collections;
using JetBrains.Annotations;
using Mod.Auto;

namespace Mod.BoMong.QuestHandler
{
	public class KillPlayerHandler : IQuestHandler
	{
		public int MapIDForThisQuest(string questMessage)
		{
			return 14; // kakarot village map id
		}

		public IEnumerator HandleQuest()
		{
			int? zoneId = FindHighestPlayerZoneId();

			if (zoneId.HasValue && zoneId.Value != TileMap.zoneID)
			{
				Service.gI().requestChangeZone(zoneId.Value, 0);
			}

			yield return null;
		}

		public void OnQuestCompleted()
		{
			AutoKillAll.gI.Toggle(false);
		}

		public void PreHandleQuest()
		{
			AutoKillAll.gI.Toggle(true);
		}

		[CanBeNull]
		static int? FindHighestPlayerZoneId()
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
			int maxPlayer = int.MinValue;

			for (int i = 0; i < count; i++)
			{
				int zoneId = zones[i];
				int playerCount = numPlayer[i];

				if (playerCount > maxPlayer)
				{
					maxPlayer = playerCount;
					bestZoneId = zoneId;
				}
			}

			if (bestZoneId == TileMap.zoneID)
			{
				return null;
			}

			return bestZoneId;
		}
	}
}
