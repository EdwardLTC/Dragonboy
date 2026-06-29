using System.Linq;
using JetBrains.Annotations;
using Mod.R;

namespace Mod.Xmap
{
	internal sealed class MapLookupService
	{
		internal const short IdItemCapsuleVip = 194;
		internal const short IdItemCapsuleNormal = 193;
		public const int IdMapHomeBase = 21;
		public const int IdMapLangBase = 7;
		internal const int IdMapTTVTBase = 24;

		internal int GetGateX(sbyte type)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint.maxX < 60 && type == 0)
				{
					return 15;
				}

				if (waypoint.minX > TileMap.pxw - 60 && type == 2)
				{
					return TileMap.pxw - 15;
				}
			}

			return 0;
		}

		internal int GetGateY(sbyte type)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint.maxX < 60 && type == 0)
				{
					return waypoint.maxY;
				}

				if (waypoint.minX > TileMap.pxw - 60 && type == 2)
				{
					return waypoint.maxY;
				}
			}

			return 0;
		}

		[CanBeNull]
		internal Waypoint FindWaypoint(int mapId)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				string textPopup = Utils.getTextPopup(waypoint.popup);
				if (textPopup.Equals(TileMap.mapNames[mapId]))
				{
					return waypoint;
				}
			}

			return null;
		}

		internal int ResolveMapIdFromName(string mapName)
		{
			int offset = Char.myCharz().cgender;
			if (mapName.Contains(LocalizedString.goHome))
			{
				return IdMapHomeBase + offset;
			}

			if (mapName.Contains(LocalizedString.spaceshipStation))
			{
				return IdMapTTVTBase + offset;
			}

			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (mapName.Contains(TileMap.mapNames[i]))
				{
					return i;
				}
			}

			return -1;
		}

		internal int GetHomeMapId(int characterGender)
		{
			return IdMapHomeBase + characterGender;
		}

		internal int GetVillageMapId(int characterGender)
		{
			return IdMapLangBase * characterGender;
		}

		internal bool HasCapsuleVipInBag()
		{
			Item[] items = Char.myCharz().arrItemBag;
			return items.FirstOrDefault(item => item != null && item.template.id == IdItemCapsuleVip) != null;
		}

		internal bool HasCapsuleNormalInBag()
		{
			Item[] items = Char.myCharz().arrItemBag;
			return items.FirstOrDefault(item => item != null && item.template.id == IdItemCapsuleNormal && item.quantity > 10) != null;
		}
	}

}
