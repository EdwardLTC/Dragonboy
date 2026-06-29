using System.Collections;
using UnityEngine;

namespace Mod.Xmap
{
	internal sealed class MapNavigationService
	{
		internal void TeleportCharacter(int x, int y)
		{
			Utils.TeleportMyChar(x, y);
		}

		internal IEnumerator ChangeMapViaWaypoint(Waypoint waypoint)
		{
			if (waypoint == null)
			{
				yield break;
			}

			if (waypoint == Utils.waypointMiddle)
			{
				TeleportCharacter(waypoint.GetX(), waypoint.GetY());
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
				Utils.requestChangeMap(waypoint);
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
				yield break;
			}

			TeleportCharacter(waypoint.GetXInsideMap(), waypoint.GetY());

			if (Char.myCharz().cx == waypoint.GetXInsideMap() && Char.myCharz().cy == waypoint.GetY())
			{
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds * 2);
				WalkToGate(waypoint);
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
			}
		}

		static void WalkToGate(Waypoint waypoint)
		{
			Char me = Char.myCharz();
			if (me.currentMovePoint == null)
			{
				int offset = me.cx < waypoint.GetX() ? -15 : 15;
				me.currentMovePoint = new MovePoint(waypoint.GetX() - offset, waypoint.GetY());
			}
		}
	}

}
