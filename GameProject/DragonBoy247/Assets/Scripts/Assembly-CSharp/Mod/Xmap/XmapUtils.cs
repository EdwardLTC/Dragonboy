using System.Linq;
using JetBrains.Annotations;
using Mod.R;

namespace Mod.Xmap
{
	internal class XmapUtils
	{
		internal static readonly short ID_ITEM_CAPSULE_VIP = 194;
		internal static readonly short ID_ITEM_CAPSULE_NORMAL = 193;

		internal static readonly int ID_MAP_HOME_BASE = 21;
		internal static readonly int ID_MAP_LANG_BASE = 7;
		internal static readonly int ID_MAP_TTVT_BASE = 24;

		internal static int getX(sbyte type)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint.maxX < 60 && type == 0)
					return 15;
				if (waypoint.minX > TileMap.pxw - 60 && type == 2)
					return TileMap.pxw - 15;
			}
			return 0;
		}

		internal static int getY(sbyte type)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint.maxX < 60 && type == 0)
					return waypoint.maxY;
				if (waypoint.minX > TileMap.pxw - 60 && type == 2)
					return waypoint.maxY;
			}
			return 0;
		}

		[CanBeNull]
		internal static Waypoint findWaypoint(int idMap)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				string textPopup = Utils.getTextPopup(waypoint.popup);
				if (textPopup.Equals(TileMap.mapNames[idMap]))
				{
					return waypoint;
				}
			}
			return null;
		}

		internal static int getMapIdFromName(string mapName)
		{
			int offset = Char.myCharz().cgender;
			if (mapName.Equals(LocalizedString.goHome))
			{
				return ID_MAP_HOME_BASE + offset;
			}
			if (mapName.Equals(LocalizedString.spaceshipStation))
			{
				return ID_MAP_TTVT_BASE + offset;
			}
			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (mapName.Equals(TileMap.mapNames[i]))
				{
					return i;
				}
			}
			return -1;
		}

		internal static int getIdMapHome(int cgender)
		{
			return ID_MAP_HOME_BASE + cgender;
		}

		internal static int getIdMapLang(int cgender)
		{
			return ID_MAP_LANG_BASE * cgender;
		}

		internal static bool hasItemCapsuleVip()
		{
			Item[] items = Char.myCharz().arrItemBag;
			
			return items.FirstOrDefault(item => item != null && item.template.id == ID_ITEM_CAPSULE_VIP) != null;
		}

		internal static bool hasItemCapsuleNormal()
		{
			Item[] items = Char.myCharz().arrItemBag;
			return items.FirstOrDefault(item => item != null && item.template.id == ID_ITEM_CAPSULE_NORMAL && item.quantity > 10) != null;
		}
	}
}
