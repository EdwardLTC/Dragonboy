using System.Collections.Generic;

namespace Mod.Xmap
{
	internal static class XmapAlgorithm
	{
		internal static List<MapNext> FindWayDijkstra(int start, int end, List<MapNext>[] graph)
		{
			if (graph == null || start < 0 || end < 0 || start >= graph.Length || end >= graph.Length)
			{
				return null;
			}

			if (start == end)
			{
				return new List<MapNext>();
			}

			int length = graph.Length;
			MapNext[] prev = new MapNext[length];
			bool[] hasPrev = new bool[length];
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
				{
					if (!visited[i] && (cmap == -1 || dist[i] < dist[cmap]))
					{
						cmap = i;
					}
				}

				if (cmap == -1)
				{
					break;
				}

				if (dist[cmap] == int.MaxValue)
				{
					break;
				}

				List<MapNext> neighbors = graph[cmap];
				if (neighbors == null || neighbors.Count == 0)
				{
					visited[cmap] = true;
					continue;
				}

				int count = neighbors.Count;

				for (int i = 0; i < count; i++)
				{
					MapNext mapNext = neighbors[i];
					if (mapNext.to < 0 || mapNext.to >= length)
					{
						continue;
					}

					int cost = 1;
					if (mapNext.type == TypeMapNext.NpcMenu && mapNext.info != null && mapNext.info.Length > 0 && mapNext.info[0] == 38)
					{
						cost = 100;
					}

					int tentative = dist[cmap] + cost;
					if (tentative < dist[mapNext.to])
					{
						dist[mapNext.to] = tentative;
						prev[mapNext.to] = mapNext;
						hasPrev[mapNext.to] = true;
					}
				}

				visited[cmap] = true;
			}

			if (!hasPrev[end])
			{
				return null;
			}

			List<MapNext> way = new List<MapNext>();
			int index = end;
			while (index != start)
			{
				if (!hasPrev[index])
				{
					return null;
				}

				way.Add(prev[index]);
				index = prev[index].mapStart;
			}
			way.Reverse();

			return way;
		}
	}
}
