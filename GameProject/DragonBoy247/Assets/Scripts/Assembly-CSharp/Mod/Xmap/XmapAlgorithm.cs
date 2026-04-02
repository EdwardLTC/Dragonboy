using System.Collections.Generic;

namespace Mod.Xmap
{
	internal static class XmapAlgorithm
	{
		internal static List<MapNext> FindWayDijkstra(int start, int end, List<MapNext>[] graph )
		{
			int length = graph.Length;
			MapNext[] prev = new MapNext[length];
			bool[] visited = new bool[length];
			int[] dist = new int[length];
			
			for (int i = 0; i < length; i++)
			{
				dist[i] = int.MaxValue;
			}
			
			dist[start] = 0;

			for (int _ = 0; _ < length; _++)
			{
				int cmap = -1;
				for (int i = 0; i < length; i++)
					if (!visited[i] && (cmap == -1 || dist[i] < dist[cmap]))
						cmap = i;

				if (cmap == -1)
					break;

				List<MapNext> neighbors = graph[cmap];
				int count = neighbors.Count;
				
				for (int i = 0; i < count; i++)
				{
					MapNext mapNext = neighbors[i];
					int cost = 1;
					if (mapNext.type == TypeMapNext.NpcMenu && mapNext.info[0] == 38)
					{
						cost = 100;
					}

					int tentative = dist[cmap] + cost;
					if (tentative < dist[mapNext.to])
					{
						dist[mapNext.to] = tentative;
						prev[mapNext.to] = mapNext;
					}
				}

				visited[cmap] = true;
			}

			List<MapNext> way = new List<MapNext>();
			int index = end;
			while (index != start)
			{
				way.Add(prev[index]);
				index = prev[index].mapStart;
			}
			way.Reverse();

			if (way[0].mapStart == start)
			{
				return way;
			}
			return null;
		}
	}
}
