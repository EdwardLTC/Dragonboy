using System.Collections;
using System.Collections.Generic;
using Mod.ModHelper;
using Mod.R;
using UnityEngine;

namespace Mod.Xmap
{
	internal class XmapController : CoroutineMainThreadAction<XmapController>
	{
		readonly XmapProgressMonitor progressMonitor = new XmapProgressMonitor();
		readonly XmapRouteInitializer routeInitializer = new XmapRouteInitializer(XmapContext.PathFinder, XmapContext.Capsule, XmapContext.Graph, XmapContext.MapLookup);
		readonly MapStepExecutor stepExecutor = XmapContext.StepExecutor;

		bool forceNotUseCapsuleLinks;
		int indexWay;
		bool isNextMapFailed;
		int mapEnd;
		List<MapNext> way;

		protected override float Interval => 0.4f;

		protected override IEnumerator OnUpdate()
		{
			float now = Time.realtimeSinceStartup;
			int currentMapId = TileMap.mapID;

			if (routeInitializer.IsInitializing)
			{
				RouteInitializationResult initResult = routeInitializer.Update(now, currentMapId);
				if (!HandleRouteInitialization(initResult))
				{
					yield break;
				}
			}

			if (progressMonitor.HasTimedOut(now, currentMapId, indexWay))
			{
				GameScr.info1.addInfo("[xmap] Stopped: no map progress in 5s!", 0);
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
			if (currentMapId == currentStep.To)
			{
				indexWay++;
				progressMonitor.MarkProgress(currentMapId, indexWay);
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
				yield return stepExecutor.Execute(currentStep);
			}
		}

		bool HandleRouteInitialization(RouteInitializationResult initResult)
		{
			switch (initResult.Status)
			{
			case RouteInitializationStatus.Waiting:
				return false;
			case RouteInitializationStatus.DestinationReached:
				GameScr.info1.addInfo(Strings.xmapDestinationReached + '!', 0);
				finishXmap();
				return false;
			case RouteInitializationStatus.NoRouteFound:
				GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
				finishXmap();
				return false;
			default:
				way = initResult.Route;
				return true;
			}
		}

		protected override void OnStart()
		{
			way = null;
			indexWay = 0;
			isNextMapFailed = false;

			routeInitializer.Begin(TileMap.mapID, mapEnd, XmapData.links, forceNotUseCapsuleLinks);
			progressMonitor.Reset(TileMap.mapID, indexWay);

			string mapName = TileMap.mapNames[mapEnd];
			GameScr.info1.addInfo(Strings.goTo + ": " + mapName, 0);
			base.OnStart();
		}

		protected override void OnStop()
		{
			way = null;
			indexWay = 0;
			isNextMapFailed = false;
			routeInitializer.Reset();
			progressMonitor.Reset(TileMap.mapID, indexWay);
			forceNotUseCapsuleLinks = false;
			base.OnStop();
		}

		internal static void start(int mapId, bool forceNotUseCapsuleLinks = false)
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
			gI.forceNotUseCapsuleLinks = forceNotUseCapsuleLinks;
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
