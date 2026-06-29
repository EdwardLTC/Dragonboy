using System.Collections.Generic;

namespace Mod.Xmap
{
	internal sealed class MapGraphService
	{
		internal List<MapNext>[] CloneGraph(List<MapNext>[] source)
		{
			List<MapNext>[] clone = new List<MapNext>[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				clone[i] = source[i] != null ? new List<MapNext>(source[i]) : new List<MapNext>();
			}

			return clone;
		}

		internal void AddCapsuleLink(List<MapNext>[] graph, int mapStart, int destinationMapId, int select)
		{
			List<MapNext> links = graph[mapStart];
			for (int i = 0; i < links.Count; i++)
			{
				MapNext existing = links[i];
				if (existing.IsCapsuleSelection(select) && existing.To == destinationMapId)
				{
					return;
				}
			}

			links.Add(new MapNext(mapStart, destinationMapId, TypeMapNext.Capsule, new[]
			{
				select
			}));
		}
	}

}
