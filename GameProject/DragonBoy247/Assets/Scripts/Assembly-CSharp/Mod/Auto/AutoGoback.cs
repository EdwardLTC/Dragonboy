using System.Collections;
using Mod.ModHelper;
using Mod.Xmap;
using UnityEngine;

namespace Mod.Auto
{
	internal class AutoGoback : CoroutineMainThreadAction<AutoGoback>
	{
		static int? mapGoBackId;
		static int? zoneGobackId;
		static int? lastX;
		static int? lastY;

		public static bool IsGoingBack => mapGoBackId != null || zoneGobackId != null || lastX != null || lastY != null;

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().IsCharDead())
			{
				if (mapGoBackId == null || zoneGobackId == null)
				{
					mapGoBackId = TileMap.mapID;
					zoneGobackId = TileMap.zoneID;
					lastX = Char.myCharz().cx;
					lastY = Char.myCharz().cy;
				}
				yield return new WaitForSecondsRealtime(1f);
				ReviveWhenDead();
				yield break;
			}

			if (Utils.IsMyCharHome() && Char.myCharz().cHP < Char.myCharz().cHPFull)
			{
				yield return new WaitForSecondsRealtime(1f);
				RegenHpWhenInHome();
				yield break;
			}

			ReturnToTrainMapIfNeeded();
			ChangeToTrainZoneIfNeeded();
			GotoCoordinates();
		}

		static void ReviveWhenDead()
		{
			Service.gI().returnTownFromDead();
		}

		static void RegenHpWhenInHome()
		{
			Service.gI().pickItem(-1);
		}

		static void ReturnToTrainMapIfNeeded()
		{
			if (mapGoBackId == null || XmapController.gI.IsActing || TileMap.mapID == mapGoBackId)
			{
				return;
			}
			XmapController.start(mapGoBackId.Value);
		}

		static void ChangeToTrainZoneIfNeeded()
		{
			if (TileMap.mapID != mapGoBackId || zoneGobackId == null || TileMap.zoneID == zoneGobackId)
			{
				return;
			}
			Service.gI().requestChangeZone(zoneGobackId.Value, 0);
		}

		static void GotoCoordinates()
		{
			if (lastX == null || lastY == null || XmapController.gI.IsActing || TileMap.mapID != mapGoBackId || TileMap.zoneID != zoneGobackId)
			{
				return;
			}
			Utils.TeleportMyChar(lastX.Value, lastY.Value);
			ClearGoBackInfo();
		}

		static void ClearGoBackInfo()
		{
			mapGoBackId = null;
			zoneGobackId = null;
			lastX = null;
			lastY = null;
		}
	}
}
