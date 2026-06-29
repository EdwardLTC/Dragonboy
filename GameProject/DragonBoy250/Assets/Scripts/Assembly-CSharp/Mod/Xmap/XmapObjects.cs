using System;
using System.Collections.Generic;

namespace Mod.Xmap
{
	internal sealed class MapNext
	{
		internal MapNext(int mapStart, int to, TypeMapNext type, int[] info)
		{
			MapStart = mapStart;
			To = to;
			Type = type;
			Info = info ?? Array.Empty<int>();
		}
		internal int MapStart { get; }
		internal int To { get; }
		internal TypeMapNext Type { get; }
		internal int[] Info { get; }

		internal bool IsNpcMenu(int npcId)
		{
			return Type == TypeMapNext.NpcMenu && Info.Length > 0 && Info[0] == npcId;
		}

		internal bool IsCapsuleSelection(int select)
		{
			return Type == TypeMapNext.Capsule && Info.Length > 0 && Info[0] == select;
		}
	}

	internal sealed class GroupMap
	{
		internal GroupMap(string[] nameGroup, List<int> maps)
		{
			Names = nameGroup;
			Maps = maps;
		}

		string[] Names { get; }

		internal List<int> Maps { get; }

		internal string GetCaption(int languageIndex)
		{
			if (Names == null || Names.Length == 0)
			{
				return string.Empty;
			}

			if (languageIndex >= 0 && languageIndex < Names.Length)
			{
				return Names[languageIndex];
			}

			return Names[^1];
		}

		internal void RemoveHomeMaps(int cgender)
		{
			switch (cgender)
			{
			case 0:
				Maps.Remove(22);
				Maps.Remove(23);
				break;
			case 1:
				Maps.Remove(21);
				Maps.Remove(23);
				break;
			default:
				Maps.Remove(21);
				Maps.Remove(22);
				break;
			}
		}
	}

	internal enum TypeMapNext
	{
		AutoWaypoint,
		NpcMenu,
		NpcPanel,
		Position,
		Capsule
	}
}
