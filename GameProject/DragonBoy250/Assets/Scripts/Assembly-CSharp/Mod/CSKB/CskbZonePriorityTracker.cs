using System;
using System.Collections.Generic;

namespace Mod.PickMob
{
	internal sealed class CskbZonePriorityTracker
	{
		static readonly Random Random = new Random();
		readonly long stableTicks;
		long crowdedZoneStartTicks;

		internal CskbZonePriorityTracker(long stableTicks)
		{
			this.stableTicks = stableTicks;
		}

		internal int ResolveTargetZone(int currentTargetZoneId)
		{
			long nowTicks = DateTime.Now.Ticks;

			bool isCurrentZoneCrowded = GetZonePlayerCount(TileMap.zoneID) > 1;

			UpdateCurrentZoneCrowdedTime(isCurrentZoneCrowded, nowTicks);

			if (!CanSwitchToEmptyZone(isCurrentZoneCrowded, nowTicks))
			{
				return currentTargetZoneId;
			}

			int? emptyZoneId = FindEmptyZoneId();

			if (!emptyZoneId.HasValue)
			{
				return currentTargetZoneId;
			}

			Reset();

			GameScr.info1.addInfo("[Up CSKB] đổi sang khu vắng " + emptyZoneId.Value, 0);

			return emptyZoneId.Value;
		}

		internal void Reset()
		{
			crowdedZoneStartTicks = 0L;
		}

		void UpdateCurrentZoneCrowdedTime(bool isCurrentZoneCrowded, long nowTicks)
		{
			if (!isCurrentZoneCrowded)
			{
				crowdedZoneStartTicks = 0L;
				return;
			}

			if (crowdedZoneStartTicks == 0L)
			{
				crowdedZoneStartTicks = nowTicks;
			}
		}

		bool CanSwitchToEmptyZone(bool isCurrentZoneCrowded, long nowTicks)
		{
			return isCurrentZoneCrowded && crowdedZoneStartTicks != 0L && nowTicks - crowdedZoneStartTicks >= stableTicks;
		}

		static int? FindEmptyZoneId()
		{
			GameScr gameScr = GameScr.gI();
			int[] zones = gameScr.zones;
			int[] numPlayer = gameScr.numPlayer;
			if (zones == null || numPlayer == null)
			{
				return null;
			}

			int count = System.Math.Min(zones.Length, numPlayer.Length);
			List<int> emptyZones = new List<int>();
			for (int i = 0; i < count; i++)
			{
				int zoneId = zones[i];
				if (zoneId != TileMap.zoneID && numPlayer[i] == 0)
				{
					emptyZones.Add(zoneId);
				}
			}

			if (emptyZones.Count == 0)
			{
				return null;
			}

			return emptyZones[Random.Next(emptyZones.Count)];
		}

		static int GetZonePlayerCount(int zoneId)
		{
			GameScr gameScr = GameScr.gI();
			int[] zones = gameScr.zones;
			int[] numPlayer = gameScr.numPlayer;
			if (numPlayer == null)
			{
				return 0;
			}

			if (zones != null)
			{
				int count = System.Math.Min(zones.Length, numPlayer.Length);
				for (int i = 0; i < count; i++)
				{
					if (zones[i] == zoneId)
					{
						return numPlayer[i];
					}
				}
			}

			if (zoneId >= 0 && zoneId < numPlayer.Length)
			{
				return numPlayer[zoneId];
			}

			return 0;
		}
	}
}
