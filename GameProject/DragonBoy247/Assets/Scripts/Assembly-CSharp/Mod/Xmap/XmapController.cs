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
		bool isInitializing;
		float lastProgressRealtime;
		int lastProgressMapId;
		int lastProgressStepIndex;
		protected override float Interval => 0.5f;
		
		protected override IEnumerator OnUpdate()
		{
			if (isInitializing)
			{
				yield break;
			}
			
			bool isMapTransitioning = Char.isLoadingMap || Char.ischangingMap || GameCanvas.isLoading || Controller.isStopReadMessage;

			if (TileMap.mapID != lastProgressMapId || indexWay != lastProgressStepIndex)
			{
				MarkProgress();
			}
			else if (isMapTransitioning)
			{
				lastProgressRealtime = Time.realtimeSinceStartup;
			}
			else if (Time.realtimeSinceStartup - lastProgressRealtime >= MaxStuckSeconds)
			{
				GameScr.info1.addInfo("[xmap] Stopped: no map progress in 15s!", 0);
				finishXmap();
				yield break;
			}
			
			if (way == null || way.Count == 0 ||isNextMapFailed)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
				yield break;
			}
			
			if (TileMap.mapID == mapEnd && !Char.myCharz().IsCharDead())
			{
				GameScr.info1.addInfo(Strings.xmapDestinationReached + '!', 0);
				finishXmap();
				yield break;
			}

			if (indexWay < 0 || indexWay >= way.Count)
			{
				isNextMapFailed = true;
				way = null;
				yield break;
			}

			MapNext currentStep = way[indexWay];
			if (TileMap.mapID == currentStep.to)
			{
				indexWay++;
				MarkProgress();
				yield break;
			}

			if (Char.myCharz().IsCharDead())
			{
				Service.gI().returnTownFromDead();
				isNextMapFailed = true;
				way = null;
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
			isInitializing = true;
			
			string mapName = TileMap.mapNames[mapEnd];
			GameScr.info1.addInfo(Strings.goTo + ": " + mapName, 0);
			StartCoroutine(InitializeWay());
			MarkProgress();
			base.OnStart();
		}

		IEnumerator InitializeWay()
		{
			List<MapNext>[] graph = (List<MapNext>[])XmapData.links.Clone();

			yield return StartCoroutine(AddCapsuleLinkIfPossible(graph));
		
			way = XmapAlgorithm.FindWayBFS(TileMap.mapID, mapEnd, graph);
			
			if (way == null || way.Count == 0)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
			}
			isInitializing = false;
		}

		protected override void OnStop()
		{
			way = null;
			indexWay = 0;
			isNextMapFailed = false;
			isInitializing = false;
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

		static IEnumerator AddCapsuleLinkIfPossible(List<MapNext>[] graph)
		{
			if (!Pk9rXmap.CanUseCapsuleVip() && !Pk9rXmap.CanUseCapsuleNormal())
			{
				yield break;	
			}
			
			float deadlineUseCapsule = Time.realtimeSinceStartup + 2f;
			
			string[] oldMapNames = GameCanvas.panel.mapNames;
			
			while (GameCanvas.panel.type != Panel.TYPE_MAPTRANS)
			{
				if (Time.realtimeSinceStartup >= deadlineUseCapsule)
				{
					yield break;
				}
				if (Pk9rXmap.CanUseCapsuleVip())
				{
					Service.gI().useItem(0, 1, -1, XmapUtils.ID_ITEM_CAPSULE_VIP);
				}
				else if (Pk9rXmap.CanUseCapsuleNormal())
				{
					Service.gI().useItem(0, 1, -1, XmapUtils.ID_ITEM_CAPSULE_NORMAL);
				}
				else
				{
					yield break;
				}	
			}
			
			float deadlineLoadMapNames = Time.realtimeSinceStartup + 3f;
			while (GameCanvas.panel.mapNames == oldMapNames || GameCanvas.panel.mapNames == null || GameCanvas.panel.mapNames.Length == 0)
			{
				if (Time.realtimeSinceStartup >= deadlineLoadMapNames)
				{
					yield break;
				}
				yield return null;
			}

			int mapStart = TileMap.mapID;
			string[] mapNames = GameCanvas.panel.mapNames;
			
			int length = mapNames.Length;
			for (int select = 0; select < length; select++)
			{
				int to = XmapUtils.getMapIdFromName(mapNames[select]);
				Debug.Log("Capsule link: " + mapStart + " -> " + to + " (" + mapNames[select] + ")");
				if (to != -1)
				{
					graph[mapStart].Add(new MapNext(mapStart, to, TypeMapNext.Capsule, new[] { select }));
				}
			}
		}
	}
}
