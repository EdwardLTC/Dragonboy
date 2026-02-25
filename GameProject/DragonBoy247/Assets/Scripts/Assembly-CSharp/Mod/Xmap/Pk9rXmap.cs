using System.Collections;
using System.Linq;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.ModHelper.Menu;
using Mod.R;
using UnityEngine;
using Random = System.Random;

namespace Mod.Xmap
{
	internal static class Pk9rXmap
	{
		const float ServiceCallDelaySeconds = 0.2f;
		internal static bool isUseCapsuleNormal;
		static bool isUseCapsuleVip = true;
		static bool isChangingMap;
		static bool isMovingMyChar;

		static readonly Random random = new Random();

		static void ToggleUseCapsuleVip()
		{
			isUseCapsuleVip = !isUseCapsuleVip;
			GameScr.info1.addInfo(Strings.xmapUseSpecialCapsule + ": " + Strings.OnOffStatus(isUseCapsuleVip), 0);
		}

		static void ToggleUseCapsuleNormal()
		{
			isUseCapsuleNormal = !isUseCapsuleNormal;
			GameScr.info1.addInfo(Strings.xmapUseNormalCapsule + ": " + Strings.OnOffStatus(isUseCapsuleNormal), 0);
		}

		[HotkeyCommand('x')]
		internal static void ShowXmapMenu()
		{
			if (XmapController.gI.IsActing)
			{
				XmapController.finishXmap();
				GameScr.info1.addInfo(Strings.xmapCanceled, 0);
				return;
			}

			XmapData.LoadGroupMaps();

			new MenuBuilder()
				.setChatPopup(string.Format(Strings.xmapChatPopup, TileMap.mapName, TileMap.mapID))
				.map(XmapData.groups, groupMap =>
				{
					string caption = groupMap.names[^1];
					if (groupMap.names.Length > mResources.language)
						caption = groupMap.names[mResources.language];
					return new MenuItem(caption, new MenuAction(() =>
					{
						XmapPanel.Show(groupMap.maps);
					}));
				})
				.addItem(Strings.settings, new MenuAction(ShowXmapSettings))
				.start();
		}

		static void ShowXmapSettings()
		{
			new MenuBuilder()
				.setChatPopup(string.Format(Strings.xmapChatPopup, TileMap.mapName, TileMap.mapID))
				.addItem(Strings.xmapUseNormalCapsule + ": " + Strings.OnOffStatus(isUseCapsuleNormal), new MenuAction(ToggleUseCapsuleNormal))
				.addItem(Strings.xmapUseSpecialCapsule + ": " + Strings.OnOffStatus(isUseCapsuleVip), new MenuAction(ToggleUseCapsuleVip))
				.start();
		}

		internal static void Info(string text)
		{
			if (XmapController.gI.IsActing)
			{
				if (LocalizedString.xmapCantGoHereKeywords.Any(lS => lS == text))
				{
					XmapController.finishXmap();
					GameScr.info1.addInfo(Strings.xmapCanceled, 0);
				}
				else if (text == LocalizedString.errorOccurred)
					MoveMyChar(XmapUtils.getX(2), XmapUtils.getY(2));
			}
		}

		internal static void FixBlackScreen()
		{
			Controller.gI().loadCurrMap(0);
			Service.gI().finishLoadMap();
			Char.isLoadingMap = false;
		}

		internal static bool CanUseCapsuleNormal()
		{
			return isUseCapsuleNormal && !Char.myCharz().IsCharDead() && XmapUtils.hasItemCapsuleNormal();
		}

		internal static bool CanUseCapsuleVip()
		{
			return isUseCapsuleVip && !Char.myCharz().IsCharDead() && XmapUtils.hasItemCapsuleVip();
		}

		internal static int GetMapIdFromPanelXmap(string mapName)
		{
			return int.Parse(mapName.Split(':')[0]);
		}

		internal static IEnumerator NextMap(MapNext mapNext)
		{
			switch (mapNext.type)
			{
			case TypeMapNext.AutoWaypoint:
				yield return NextMapAutoWaypoint(mapNext);
				break;
			case TypeMapNext.NpcMenu:
				yield return NextMapNpcMenu(mapNext);
				break;
			case TypeMapNext.NpcPanel:
				yield return NextMapNpcPanel(mapNext);
				break;
			case TypeMapNext.Position:
				yield return NextMapPosition(mapNext);
				break;
			case TypeMapNext.Capsule:
				yield return NextMapCapsule(mapNext);
				break;
			}
		}

		static IEnumerator NextMapAutoWaypoint(MapNext mapNext)
		{
			Waypoint waypoint = XmapUtils.findWaypoint(mapNext.to);
			yield return ChangeMap(waypoint);
		}

		static IEnumerator NextMapNpcMenu(MapNext mapNext)
		{
			int npcId = mapNext.info[0];
			if (npcId == 38)
			{
				int retryCount = 0;
				while (true)
				{
					if (retryCount >= 30)
					{
						GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
						yield break;
					}
					bool foundNpc = false;
					for (int i = 0; i < GameScr.vNpc.size(); i++)
					{
						Npc npc = (Npc)GameScr.vNpc.elementAt(i);
						if (npc.template.npcTemplateId == npcId)
						{
							foundNpc = true;
							break;
						}
					}

					if (foundNpc)
					{
						break;
					}

					Waypoint waypoint;
					if (TileMap.mapID == 27 || TileMap.mapID == 29)
					{
						waypoint = XmapUtils.findWaypoint(28);
					}
					else
					{
						waypoint = random.Next(27, 29) == 27 ? XmapUtils.findWaypoint(27) : XmapUtils.findWaypoint(29);
					}
					yield return ChangeMap(waypoint);
					yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
					retryCount++;
				}
			}

			Utils.TeleportToNPC(npcId);
			yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
			Service.gI().openMenu(npcId);
			if (mapNext.info.Length > 1)
			{
				yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
			}
			for (int i = 1; i < mapNext.info.Length; i++)
			{
				int select = mapNext.info[i];
				Service.gI().confirmMenu((short)npcId, (sbyte)select);
				if (i < mapNext.info.Length - 1)
				{
					yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
				}
			}
			Char.chatPopup = null;
		}

		static IEnumerator NextMapNpcPanel(MapNext mapNext)
		{
			int idNpc = mapNext.info[0];
			int selectMenu = mapNext.info[1];
			int selectPanel = mapNext.info[2];
			Service.gI().openMenu(idNpc);
			yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
			Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
			yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
			Service.gI().requestMapSelect(selectPanel);
		}

		static IEnumerator NextMapPosition(MapNext mapNext)
		{
			int xPos = mapNext.info[0];
			int yPos = mapNext.info[1];
			MoveMyChar(xPos, yPos);
			if (Utils.Distance(Char.myCharz().cx, Char.myCharz().cy, xPos, yPos) <= TileMap.size)
			{
				Service.gI().requestChangeMap();
				yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
				Service.gI().getMapOffline();
			}
		}

		static IEnumerator NextMapCapsule(MapNext mapNext)
		{
			XmapUtils.mapCapsuleReturn = TileMap.mapID;
			int select = mapNext.info[0];
			Service.gI().requestMapSelect(select);
			yield break;
		}

		static void MoveMyChar(int x, int y)
		{
			Utils.TeleportMyChar(x, y);
		}

		static IEnumerator ChangeMap(Waypoint waypoint)
		{
			if (waypoint != null)
			{
				Utils.TeleportMyChar(waypoint.GetX(), waypoint.GetY());
				yield return new WaitForSecondsRealtime(ServiceCallDelaySeconds);
				Utils.requestChangeMap(waypoint);
			}
		}
	}
}
