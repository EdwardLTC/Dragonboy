using System;

namespace Mod.PickMob
{
	internal sealed class CskbZonePriorityTracker
	{
		readonly long stableTicks;
		long crowdedZoneStartTicks;
		int? emptyZoneCandidateId;
		long emptyZoneStartTicks;

		internal CskbZonePriorityTracker(long stableTicks)
		{
			this.stableTicks = stableTicks;
		}

		internal int ResolveTargetZone(int currentTargetZoneId)
		{
			long nowTicks = DateTime.Now.Ticks;
			bool isCurrentZoneCrowded = GetZonePlayerCount(TileMap.zoneID) > 1;
			UpdateCurrentZoneCrowdedTime(isCurrentZoneCrowded, nowTicks);
			UpdateEmptyZoneCandidate(FindEmptyZoneId(), nowTicks);

			if (!CanSwitchToEmptyZone(isCurrentZoneCrowded, nowTicks) || !emptyZoneCandidateId.HasValue)
			{
				return currentTargetZoneId;
			}

			int newTargetZoneId = emptyZoneCandidateId.Value;
			Reset();
			GameScr.info1.addInfo("[Up CSKB] đổi sang khu vắng " + newTargetZoneId, 0);
			return newTargetZoneId;
		}

		internal void Reset()
		{
			crowdedZoneStartTicks = 0L;
			emptyZoneStartTicks = 0L;
			emptyZoneCandidateId = null;
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

		void UpdateEmptyZoneCandidate(int? emptyZoneId, long nowTicks)
		{
			if (emptyZoneId == null)
			{
				emptyZoneCandidateId = null;
				emptyZoneStartTicks = 0L;
				return;
			}

			if (emptyZoneCandidateId != emptyZoneId)
			{
				emptyZoneCandidateId = emptyZoneId;
				emptyZoneStartTicks = nowTicks;
			}
		}

		bool CanSwitchToEmptyZone(bool isCurrentZoneCrowded, long nowTicks)
		{
			if (!isCurrentZoneCrowded || crowdedZoneStartTicks == 0L || nowTicks - crowdedZoneStartTicks < stableTicks)
			{
				return false;
			}

			return emptyZoneCandidateId != null && emptyZoneStartTicks != 0L && nowTicks - emptyZoneStartTicks >= stableTicks;
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
			for (int i = 0; i < count; i++)
			{
				int zoneId = zones[i];
				if (zoneId != TileMap.zoneID && numPlayer[i] == 0)
				{
					return zoneId;
				}
			}

			return null;
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
