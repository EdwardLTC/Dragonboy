namespace Mod.Xmap
{
	internal static class XmapContext
	{
		internal static readonly XmapSettings Settings = new XmapSettings();
		internal static readonly MapLookupService MapLookup = new MapLookupService();
		internal static readonly MapGraphService Graph = new MapGraphService();
		internal static readonly MapNavigationService Navigation = new MapNavigationService();
		internal static readonly CapsuleService Capsule = new CapsuleService(MapLookup, Settings);
		internal static readonly IPathFinder PathFinder = new DijkstraPathFinder();
		internal static readonly MapStepExecutor StepExecutor = new MapStepExecutor(new IMapStepHandler[]
		{
			new AutoWaypointStepHandler(MapLookup, Navigation), new NpcMenuStepHandler(MapLookup, Navigation), new NpcPanelStepHandler(), new PositionStepHandler(Navigation), new CapsuleStepHandler()
		});
	}

}
