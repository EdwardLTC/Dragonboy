using System;
using System.Collections;
using System.Collections.Generic;
using Mod.ModHelper;
using Mod.R;
using UnityEngine;

namespace Mod.Xmap
{
	internal class XmapController : CoroutineMainThreadAction<XmapController>
	{
		const float MaxStuckSeconds = 15f;
		int mapEnd;
		List<MapNext> way;
		int indexWay;
		bool isNextMapFailed;
		float lastProgressRealtime;
		int lastProgressMapId;
		int lastProgressStepIndex;
		protected override float Interval => 0.5f;
		
		protected override IEnumerator OnUpdate()
		{
			if (TileMap.mapID != lastProgressMapId || indexWay != lastProgressStepIndex)
			{
				MarkProgress();
			}
			else if (Time.realtimeSinceStartup - lastProgressRealtime >= MaxStuckSeconds && Utils.CanNextMap())
			{
				GameScr.info1.addInfo("[xmap] Stopped: no map progress in 15s!", 0);
				finishXmap();
				yield return null;
				yield break;
			}

			if (isNextMapFailed)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
				yield return null;
				yield break;
			}

			if (way == null || way.Count == 0)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
				yield return null;
				yield break;
			}
			
			if (TileMap.mapID == mapEnd && !Char.myCharz().IsCharDead())
			{
				GameScr.info1.addInfo(Strings.xmapDestinationReached + '!', 0);
				finishXmap();
				yield return null;
				yield break;
			}

			if (indexWay < 0 || indexWay >= way.Count)
			{
				isNextMapFailed = true;
				way = null;
				yield return null;
				yield break;
			}

			MapNext currentStep = way[indexWay];
			if (TileMap.mapID == currentStep.to)
			{
				indexWay++;
				MarkProgress();
				yield return null;
				yield break;
			}

			if (Char.myCharz().IsCharDead())
			{
				Service.gI().returnTownFromDead();
				isNextMapFailed = true;
				way = null;
				yield return null;
				yield break;
			}

			if (Utils.CanNextMap())
			{
				yield return Pk9rXmap.NextMap(currentStep);
			}
		}
		
		protected override void OnStart()
		{
			way = null;
			indexWay = 0;
			isNextMapFailed = false;
			MarkProgress();

			string mapName = TileMap.mapNames[mapEnd];
			GameScr.info1.addInfo(Strings.goTo + ": " + mapName, 0);
			try
			{
				way = XmapAlgorithm.FindWayBFS(TileMap.mapID, mapEnd);
			}
			catch (Exception ex)
			{
				GameScr.info1.addInfo("Load map err" + '!', 0);
				finishXmap();
			}

			if (way == null)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
			}
			base.OnStart();
		}

		protected override void OnStop()
		{
			way = null;
			indexWay = 0;
			isNextMapFailed = false;
			MarkProgress();
			base.OnStop();
		}

		void MarkProgress()
		{
			lastProgressRealtime = Time.realtimeSinceStartup;
			lastProgressMapId = TileMap.mapID;
			lastProgressStepIndex = indexWay;
		}

		internal static void start(int mapId)
		{
			if (gI == null)
			{
				return;
			}

			if (gI.IsActing)
			{
				finishXmap();
			}
			gI.mapEnd = mapId;
			gI.Toggle(true);
		}

		internal static void finishXmap()
		{
			if (gI == null)
			{
				return;
			}

			gI.Toggle(false);
		}
	}
}
