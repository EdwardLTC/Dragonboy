using System.Collections;
using UnityEngine;

namespace Mod.PickMob.Services
{
	public static class ZoneService
	{
		const float ZONE_CHANGE_TIMEOUT = 8f;
		const long NO_MOB_WAIT_MS = 1500;
		const long ZONE_CHANGE_COOLDOWN_MS = 2000;

		static long _noMobSinceMs;
		static long _lastZoneChangeMs;

		public static void ResetNoMobTimer()
		{
			_noMobSinceMs = 0;
		}

		public static bool ShouldChangeZone()
		{
			if (!PickMobConfig.IsAutoChangeZone || !MobPicker.AreAllTanSatMobsRespawning())
			{
				_noMobSinceMs = 0;
				return false;
			}

			long now = mSystem.currentTimeMillis();

			if (_noMobSinceMs == 0)
			{
				_noMobSinceMs = now;
			}

			return now - _noMobSinceMs >= NO_MOB_WAIT_MS && now - _lastZoneChangeMs >= ZONE_CHANGE_COOLDOWN_MS;
		}

		public static IEnumerator ChangeToLowestPlayerZone()
		{
			int? targetZone = FindLowestPlayerZoneId();
			if (!targetZone.HasValue)
			{
				yield break;
			}

			int startMapId = TileMap.mapID;
			_lastZoneChangeMs = mSystem.currentTimeMillis();
			_noMobSinceMs = 0;

			Service.gI().requestChangeZone(targetZone.Value, 0);

			float deadline = Time.realtimeSinceStartup + ZONE_CHANGE_TIMEOUT;
			while (Time.realtimeSinceStartup < deadline)
			{
				if (TileMap.zoneID == targetZone.Value || TileMap.mapID != startMapId)
				{
					yield break;
				}

				if (MobPicker.HasAttackableMob())
				{
					yield break;
				}

				yield return null;
			}
		}

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
	}
}
