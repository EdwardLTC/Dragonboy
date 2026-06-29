using System.Collections;
using Mod.R;
using UnityEngine;
using Random = System.Random;

namespace Mod.Xmap
{
	internal sealed class AutoWaypointStepHandler : IMapStepHandler
	{
		readonly MapLookupService mapLookup;
		readonly MapNavigationService navigation;

		internal AutoWaypointStepHandler(MapLookupService mapLookup, MapNavigationService navigation)
		{
			this.mapLookup = mapLookup;
			this.navigation = navigation;
		}

		public TypeMapNext StepType => TypeMapNext.AutoWaypoint;

		public IEnumerator Execute(MapNext step)
		{
			Waypoint waypoint = mapLookup.FindWaypoint(step.To);
			yield return navigation.ChangeMapViaWaypoint(waypoint);
		}
	}

	internal sealed class NpcMenuStepHandler : IMapStepHandler
	{
		static readonly Random random = new Random();
		readonly MapLookupService mapLookup;
		readonly MapNavigationService navigation;

		internal NpcMenuStepHandler(MapLookupService mapLookup, MapNavigationService navigation)
		{
			this.mapLookup = mapLookup;
			this.navigation = navigation;
		}

		public TypeMapNext StepType => TypeMapNext.NpcMenu;

		public IEnumerator Execute(MapNext step)
		{
			int npcId = step.Info[0];
			if (npcId == 38)
			{
				yield return EnsureFutureNpcAvailable(npcId);
			}

			yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
			Utils.TeleportToNPC(npcId);
			yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
			Service.gI().openMenu(npcId);
			for (int i = 1; i < step.Info.Length; i++)
			{
				int select = step.Info[i];
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
				Service.gI().confirmMenu((short)npcId, (sbyte)select);
			}

			Char.chatPopup = null;
		}

		IEnumerator EnsureFutureNpcAvailable(int npcId)
		{
			int retryCount = 0;
			while (true)
			{
				if (retryCount >= 30)
				{
					GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
					yield break;
				}

				if (FindNpc(npcId))
				{
					yield break;
				}

				Waypoint waypoint = ResolveFutureWaypoint();
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds * 2);
				yield return navigation.ChangeMapViaWaypoint(waypoint);
				retryCount++;
			}
		}

		static bool FindNpc(int npcId)
		{
			for (int i = 0; i < GameScr.vNpc.size(); i++)
			{
				Npc npc = (Npc)GameScr.vNpc.elementAt(i);
				if (npc.template.npcTemplateId == npcId)
				{
					return true;
				}
			}

			return false;
		}

		Waypoint ResolveFutureWaypoint()
		{
			if (TileMap.mapID == 27 || TileMap.mapID == 29)
			{
				return mapLookup.FindWaypoint(28);
			}

			return random.Next(27, 29) == 27
				? mapLookup.FindWaypoint(27)
				: mapLookup.FindWaypoint(29);
		}
	}

	internal sealed class NpcPanelStepHandler : IMapStepHandler
	{
		public TypeMapNext StepType => TypeMapNext.NpcPanel;

		public IEnumerator Execute(MapNext step)
		{
			int idNpc = step.Info[0];
			int selectMenu = step.Info[1];
			int selectPanel = step.Info[2];
			Service.gI().openMenu(idNpc);
			yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
			Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
			yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
			Service.gI().requestMapSelect(selectPanel);
			yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
		}
	}

	internal sealed class PositionStepHandler : IMapStepHandler
	{
		readonly MapNavigationService navigation;

		internal PositionStepHandler(MapNavigationService navigation)
		{
			this.navigation = navigation;
		}

		public TypeMapNext StepType => TypeMapNext.Position;

		public IEnumerator Execute(MapNext step)
		{
			int xPos = step.Info[0];
			int yPos = step.Info[1];
			navigation.TeleportCharacter(xPos, yPos);
			if (Utils.Distance(Char.myCharz().cx, Char.myCharz().cy, xPos, yPos) <= TileMap.size)
			{
				Service.gI().requestChangeMap();
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
				Service.gI().getMapOffline();
				yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
			}
		}
	}

	internal sealed class CapsuleStepHandler : IMapStepHandler
	{
		public TypeMapNext StepType => TypeMapNext.Capsule;

		public IEnumerator Execute(MapNext step)
		{
			int select = step.Info[0];
			Service.gI().requestMapSelect(select);
			yield return new WaitForSecondsRealtime(XmapTiming.ServiceCallDelaySeconds);
		}
	}

}
