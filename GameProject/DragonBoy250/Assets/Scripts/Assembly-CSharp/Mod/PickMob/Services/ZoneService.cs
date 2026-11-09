using System.Collections;
using System.Collections.Generic;
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
			int minPlayer = int.MaxValue;

			// First pass: find the minimum player count
			for (int i = 0; i < count; i++)
			{
				int playerCount = numPlayer[i];
				if (playerCount < minPlayer)
				{
					minPlayer = playerCount;
				}
			}

			// Second pass: collect all zones tied for the lowest player count
			List<int> tiedZones = new List<int>();
			for (int i = 0; i < count; i++)
			{
				if (numPlayer[i] == minPlayer)
				{
					tiedZones.Add(zones[i]);
				}
			}

			if (tiedZones.Count == 0)
			{
				return null;
			}

			// Filter out the current zone if there are other options
			List<int> candidates = new List<int>();
			foreach (int zoneId in tiedZones)
			{
				if (zoneId != TileMap.zoneID)
				{
					candidates.Add(zoneId);
				}
			}

			// If all tied zones are the current zone, return null
			if (candidates.Count == 0)
			{
				return null;
			}

			// Randomly pick from the candidates
			int randomIndex = Random.Range(0, candidates.Count);
			return candidates[randomIndex];
		}
	}
}
