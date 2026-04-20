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
		const float CapsuleProbeTimeoutSeconds = 5f;
		const float CapsuleProbeRetrySeconds = 1f;
		float capsuleProbeDeadline;
		float capsuleProbeNextRetry;
		int indexWay;
		List<MapNext>[] initializeGraph;
		int initializeStartMapId;
		bool isInitializing;
		bool isNextMapFailed;
		bool isWaitingForCapsuleLinks;
		int lastProgressMapId;
		float lastProgressRealtime;
		int lastProgressStepIndex;
		int mapEnd;
		List<MapNext> way;

		protected override float Interval => 0.5f;

		protected override IEnumerator OnUpdate()
		{
			float now = Time.realtimeSinceStartup;
			int currentMapId = TileMap.mapID;

			if (isInitializing && !UpdateInitialization(now, currentMapId))
			{
				yield break;
			}

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
			initializeGraph = CloneGraph(XmapData.links);
			indexWay = 0;
			isNextMapFailed = false;
			isWaitingForCapsuleLinks = false;
			isInitializing = true;
			initializeStartMapId = TileMap.mapID;
			capsuleProbeDeadline = 0f;
			capsuleProbeNextRetry = 0f;

			string mapName = TileMap.mapNames[mapEnd];
			GameScr.info1.addInfo(Strings.goTo + ": " + mapName, 0);
			MarkProgress();
			base.OnStart();
		}

		protected override void OnStop()
		{
			way = null;
			initializeGraph = null;
			indexWay = 0;
			isNextMapFailed = false;
			isInitializing = false;
			isWaitingForCapsuleLinks = false;
			capsuleProbeDeadline = 0f;
			capsuleProbeNextRetry = 0f;
			MarkProgress();
			base.OnStop();
		}

		bool UpdateInitialization(float now, int currentMapId)
		{
			if (initializeStartMapId == mapEnd)
			{
				GameScr.info1.addInfo(Strings.xmapDestinationReached + '!', 0);
				finishXmap();
				isInitializing = false;
				return false;
			}

			if (!isWaitingForCapsuleLinks)
			{
				way = XmapAlgorithm.FindWayDijkstra(initializeStartMapId, mapEnd, initializeGraph);

				if (way == null)
				{
					GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
					finishXmap();
					isInitializing = false;
					return false;
				}

				if (way.Count > 5 && (Pk9rXmap.CanUseCapsuleVip() || Pk9rXmap.CanUseCapsuleNormal()))
				{
					isWaitingForCapsuleLinks = true;
					capsuleProbeDeadline = now + CapsuleProbeTimeoutSeconds;
					capsuleProbeNextRetry = 0f;
					GameCanvas.panel.mapNames = null;
					GameCanvas.panel.hideNow();
					GameCanvas.panel2.hideNow();
					Char.chatPopup = null;
					GameCanvas.menu.doCloseMenu();
					return false;
				}

				isInitializing = false;
				return true;
			}

			if (GameCanvas.panel is { isShow: true, mapNames: { Length: > 0 } })
			{
				string[] mapNames = GameCanvas.panel.mapNames;

				for (int select = 0; select < mapNames?.Length; select++)
				{
					int to = XmapUtils.getMapIdFromName(mapNames[select]);
					if (to != -1)
					{
						AddCapsuleLink(initializeGraph, currentMapId, to, select);
					}
				}

				way = XmapAlgorithm.FindWayDijkstra(initializeStartMapId, mapEnd, initializeGraph);
				isWaitingForCapsuleLinks = false;
				isInitializing = false;

				if (way == null)
				{
					GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
					finishXmap();
					return false;
				}

				return true;
			}

			if (now >= capsuleProbeNextRetry)
			{
				if (Pk9rXmap.CanUseCapsuleVip())
				{
					Service.gI().useItem(0, 1, -1, XmapUtils.ID_ITEM_CAPSULE_VIP);
				}
				else if (Pk9rXmap.CanUseCapsuleNormal())
				{
					Service.gI().useItem(0, 1, -1, XmapUtils.ID_ITEM_CAPSULE_NORMAL);
				}

				capsuleProbeNextRetry = now + CapsuleProbeRetrySeconds;
			}

			if (now < capsuleProbeDeadline)
			{
				return false;
			}

			isWaitingForCapsuleLinks = false;
			isInitializing = false;
			return true;
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

		static List<MapNext>[] CloneGraph(List<MapNext>[] source)
		{
			List<MapNext>[] clone = new List<MapNext>[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				clone[i] = source[i] != null ? new List<MapNext>(source[i]) : new List<MapNext>();
			}

			return clone;
		}

		static void AddCapsuleLink(List<MapNext>[] graph, int mapStart, int to, int select)
		{
			List<MapNext> links = graph[mapStart];
			for (int i = 0; i < links.Count; i++)
			{
				MapNext existing = links[i];
				if (existing.to == to && existing.type == TypeMapNext.Capsule && existing.info != null && existing.info.Length > 0 && existing.info[0] == select)
				{
					return;
				}
			}

			links.Add(new MapNext(mapStart, to, TypeMapNext.Capsule, new[]
			{
				select
			}));
		}
	}
}
