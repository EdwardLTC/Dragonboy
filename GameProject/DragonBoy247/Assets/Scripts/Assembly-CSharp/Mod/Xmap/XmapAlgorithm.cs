using System.Collections.Generic;

namespace Mod.Xmap
{
	internal static class XmapAlgorithm
	{
		internal static List<MapNext> FindWayBFS(int start, int end, List<MapNext>[] graph )
		{
			int length = graph.Length;

			if (start == end)
			{
				return new List<MapNext>();
			}
			
			bool[] visited = new bool[length];
			MapNext?[] prev = new MapNext?[length];

			Queue<int> queue = new Queue<int>();
			queue.Enqueue(start);
			visited[start] = true;

			while (queue.Count > 0)
			{
				int current = queue.Dequeue();

				if (current == end)
				{
					break;
				}

				foreach (MapNext next in graph[current])
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
				{
					return null;
				}

				MapNext step = prev[index].Value;
				path.Add(step);
				index = step.mapStart;
			}

			path.Reverse();
			return path;
		}
	}
}
