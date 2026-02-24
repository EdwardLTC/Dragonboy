using System.Collections.Generic;

namespace Mod.Xmap
{
	internal class XmapAlgorithm
	{
		internal static List<MapNext> findWay(int mapStart, int mapEnd)
		{
			int length = XmapData.links.Length;
			MapNext[] prev = new MapNext[length];
			bool[] visited = new bool[length];
			int[] dist = new int[length];
			for (int i = 0; i < length; i++)
				dist[i] = int.MaxValue;
			dist[mapStart] = 0;

			for (int _ = 0; _ < length; _++)
			{
				int cmap = -1;
				for (int i = 0; i < length; i++)
					if (!visited[i] && (cmap == -1 || dist[i] < dist[cmap]))
						cmap = i;

				if (cmap == -1)
					break;

				List<MapNext> neighbors = XmapData.links[cmap];
				int count = neighbors.Count;
				for (int i = 0; i < count; i++)
				{
					MapNext mapNext = neighbors[i];
					int cost = 1;
					if (mapNext.type == TypeMapNext.NpcMenu && mapNext.info[0] == 38)
						cost = 100;

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
			int index = mapEnd;
			while (index != mapStart)
			{
				way.Add(prev[index]);
				index = prev[index].mapStart;
			}
			way.Reverse();

			if (way[0].mapStart == mapStart)
			{
				return way;
			}
			return null;
		}
		
		internal static List<MapNext> FindWayBFS(int start, int end)
		{
			int length = XmapData.links.Length;

			if (start == end)
				return new List<MapNext>();

			bool[] visited = new bool[length];
			MapNext?[] prev = new MapNext?[length];

			Queue<int> queue = new Queue<int>();
			queue.Enqueue(start);
			visited[start] = true;

			while (queue.Count > 0)
			{
				int current = queue.Dequeue();

				if (current == end)
					break;

				foreach (MapNext next in XmapData.links[current])
				{
					if (!visited[next.to])
					{
						visited[next.to] = true;
						prev[next.to] = next;
						queue.Enqueue(next.to);
					}
				}
			}

			List<MapNext> path = new List<MapNext>();
			int index = end;

			while (index != start)
			{
				if (prev[index] == null)
					return null;

				MapNext step = prev[index].Value;
				path.Add(step);
				index = step.mapStart;
			}

			path.Reverse();
			return path;
		}
	}
}
