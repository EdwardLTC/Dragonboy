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
		int indexWay;
		bool isInitializing;
		bool isNextMapFailed;
		int lastProgressMapId;
		float lastProgressRealtime;
		int lastProgressStepIndex;
		int mapEnd;
		List<MapNext> way;

		protected override float Interval => 0.5f;

		protected override IEnumerator OnUpdate()
		{
			if (isInitializing)
			{
				yield break;
			}

			int currentMapId = TileMap.mapID;
			float now = Time.realtimeSinceStartup;

			if (currentMapId != lastProgressMapId || indexWay != lastProgressStepIndex)
			{
				MarkProgress();
			}
			else if (GameCanvas.currentScreen is TransportScr)
			{
				lastProgressRealtime = now;
			}
			else if (now - lastProgressRealtime >= MaxStuckSeconds)
			{
				GameScr.info1.addInfo("[xmap] Stopped: no map progress in 15s!", 0);
				finishXmap();
				yield break;
			}

			if (way == null || way.Count == 0 || isNextMapFailed)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
				yield break;
			}

			if (currentMapId == mapEnd && !Char.myCharz().IsCharDead())
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
			if (currentMapId == currentStep.to)
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
			int startMapId = TileMap.mapID;
			List<MapNext>[] graph = (List<MapNext>[])XmapData.links.Clone();
			way = XmapAlgorithm.FindWayDijkstra(startMapId, mapEnd, XmapData.links);

			if (way == null || way.Count == 0)
			{
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
				isInitializing = false;
				yield break;
			}

			if (way.Count > 5)
			{
				yield return AddCapsuleLinkIfPossible(graph);
				way = XmapAlgorithm.FindWayDijkstra(startMapId, mapEnd, graph);
			}

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

			GameCanvas.panel.mapNames = null;

			float deadline = Time.realtimeSinceStartup + 5f;
			float retryDelay = 1f;
			float nextRetry = 0f;

			while (Time.realtimeSinceStartup < deadline)
			{
				if (GameCanvas.panel is { isShow: true, mapNames: { Length: > 0 } })
				{
					break;
				}

				if (Time.realtimeSinceStartup >= nextRetry)
				{
					if (Pk9rXmap.CanUseCapsuleVip())
					{
						Service.gI().useItem(0, 1, -1, XmapUtils.ID_ITEM_CAPSULE_VIP);
					}
					else if (Pk9rXmap.CanUseCapsuleNormal())
					{
						Service.gI().useItem(0, 1, -1, XmapUtils.ID_ITEM_CAPSULE_NORMAL);
					}

					nextRetry = Time.realtimeSinceStartup + retryDelay;
				}

				yield return null;
			}

			if (GameCanvas.panel is not { isShow: true, mapNames: { Length: > 0 } })
			{
				yield break;
			}

			int mapStart = TileMap.mapID;
			string[] mapNames = GameCanvas.panel.mapNames;

			for (int select = 0; select < mapNames?.Length; select++)
			{
				int to = XmapUtils.getMapIdFromName(mapNames[select]);
				if (to != -1)
				{
					graph[mapStart].Add(new MapNext(mapStart, to, TypeMapNext.Capsule, new[]
					{
						select
					}));
				}
			}
		}
	}
}
