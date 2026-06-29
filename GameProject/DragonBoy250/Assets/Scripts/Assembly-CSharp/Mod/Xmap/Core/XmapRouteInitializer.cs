using System.Collections.Generic;

namespace Mod.Xmap
{
	internal sealed class XmapRouteInitializer
	{
		const float CapsuleProbeTimeoutSeconds = 5f;
		const int MinStepsForCapsuleProbe = 5;
		readonly CapsuleService capsuleService;
		readonly MapGraphService graphService;
		readonly MapLookupService mapLookup;

		readonly IPathFinder pathFinder;

		float capsuleProbeDeadline;
		int endMapId;
		bool forceNotUseCapsuleLinks;
		List<MapNext>[] graph;
		int startMapId;

		internal XmapRouteInitializer(IPathFinder pathFinder, CapsuleService capsuleService, MapGraphService graphService, MapLookupService mapLookup)
		{
			this.pathFinder = pathFinder;
			this.capsuleService = capsuleService;
			this.graphService = graphService;
			this.mapLookup = mapLookup;
		}

		internal bool IsInitializing { get; private set; }
		internal bool IsWaitingForCapsuleLinks { get; private set; }
		internal List<MapNext> Route { get; private set; }

		internal void Begin(int _startMapId, int _endMapId, List<MapNext>[] sourceGraph, bool _forceNotUseCapsuleLinks)
		{
			startMapId = _startMapId;
			endMapId = _endMapId;
			forceNotUseCapsuleLinks = _forceNotUseCapsuleLinks;
			graph = graphService.CloneGraph(sourceGraph);
			Route = null;
			IsInitializing = true;
			IsWaitingForCapsuleLinks = false;
			capsuleProbeDeadline = 0f;
		}

		internal void Reset()
		{
			graph = null;
			Route = null;
			IsInitializing = false;
			IsWaitingForCapsuleLinks = false;
			capsuleProbeDeadline = 0f;
			forceNotUseCapsuleLinks = false;
		}

		internal RouteInitializationResult Update(float now, int currentMapId)
		{
			if (!IsInitializing)
			{
				return RouteInitializationResult.Completed(Route);
			}

			if (startMapId == endMapId)
			{
				IsInitializing = false;
				return RouteInitializationResult.DestinationReached();
			}

			if (!IsWaitingForCapsuleLinks)
			{
				return BeginCapsuleProbeOrComplete(now);
			}

			if (GameCanvas.panel is { isShow: true, mapNames: { Length: > 0 }, type: Panel.TYPE_MAPTRANS })
			{
				ApplyCapsuleLinks(currentMapId);
				Route = pathFinder.FindWay(startMapId, endMapId, graph);
				IsWaitingForCapsuleLinks = false;
				IsInitializing = false;

				if (Route == null)
				{
					return RouteInitializationResult.NoRouteFound();
				}

				return RouteInitializationResult.Completed(Route);
			}

			if (now < capsuleProbeDeadline)
			{
				return RouteInitializationResult.Waiting();
			}

			IsWaitingForCapsuleLinks = false;
			IsInitializing = false;
			return RouteInitializationResult.Completed(Route);
		}

		RouteInitializationResult BeginCapsuleProbeOrComplete(float now)
		{
			Route = pathFinder.FindWay(startMapId, endMapId, graph);
			if (Route == null)
			{
				IsInitializing = false;
				return RouteInitializationResult.NoRouteFound();
			}

			int indexOfNpcMenu38 = Route.FindIndex(mapNext => mapNext.IsNpcMenu(38));

			if (indexOfNpcMenu38 != -1 && indexOfNpcMenu38 < MinStepsForCapsuleProbe)
			{
				IsInitializing = false;
				return RouteInitializationResult.Completed(Route);
			}

			if (Route.Count > MinStepsForCapsuleProbe && capsuleService.CanUseAny() && !forceNotUseCapsuleLinks)
			{
				IsWaitingForCapsuleLinks = true;
				capsuleProbeDeadline = now + CapsuleProbeTimeoutSeconds;
				PrepareCapsuleProbe();
				capsuleService.UseProbeCapsule();
				return RouteInitializationResult.Waiting();
			}

			IsInitializing = false;
			return RouteInitializationResult.Completed(Route);
		}

		void ApplyCapsuleLinks(int currentMapId)
		{
			string[] mapNames = GameCanvas.panel.mapNames;
			for (int select = 0; select < mapNames?.Length; select++)
			{
				int destinationMapId = mapLookup.ResolveMapIdFromName(mapNames[select]);
				if (destinationMapId != -1)
				{
					graphService.AddCapsuleLink(graph, currentMapId, destinationMapId, select);
				}
			}
		}

		static void PrepareCapsuleProbe()
		{
			GameCanvas.panel.mapNames = null;
			Char.chatPopup = null;
			if (GameCanvas.panel != null)
			{
				GameCanvas.panel.hide();
			}

			if (GameCanvas.panel2 != null)
			{
				GameCanvas.panel2.hide();
			}

			GameCanvas.menu.doCloseMenu();
		}
	}

	internal enum RouteInitializationStatus
	{
		Waiting,
		Completed,
		NoRouteFound,
		DestinationReached
	}

	internal readonly struct RouteInitializationResult
	{
		internal RouteInitializationStatus Status { get; }
		internal List<MapNext> Route { get; }

		RouteInitializationResult(RouteInitializationStatus status, List<MapNext> route)
		{
			Status = status;
			Route = route;
		}

		internal static RouteInitializationResult Waiting()
		{
			return new RouteInitializationResult(RouteInitializationStatus.Waiting, null);
		}

		internal static RouteInitializationResult Completed(List<MapNext> route)
		{
			return new RouteInitializationResult(RouteInitializationStatus.Completed, route);
		}

		internal static RouteInitializationResult NoRouteFound()
		{
			return new RouteInitializationResult(RouteInitializationStatus.NoRouteFound, null);
		}

		internal static RouteInitializationResult DestinationReached()
		{
			return new RouteInitializationResult(RouteInitializationStatus.DestinationReached, null);
		}
	}
}
