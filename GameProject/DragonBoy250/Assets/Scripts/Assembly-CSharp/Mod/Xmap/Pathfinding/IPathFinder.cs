using System.Collections.Generic;

namespace Mod.Xmap
{
	internal interface IPathFinder
	{
		List<MapNext> FindWay(int startMapId, int endMapId, List<MapNext>[] graph);
	}

}
