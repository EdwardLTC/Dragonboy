using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.ModHelper.Menu;
using Mod.R;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Random = System.Random;

namespace Mod.Xmap
{
	internal class Pk9rXmap
	{
		class XmapChatable : IChatable
		{
			public void onChatFromMe(string text, string to)
			{
				if (int.TryParse(text, out int timeout))
				{
					if (timeout < 10 || timeout > 300)
					{
						GameCanvas.startOKDlg(string.Format(Strings.inputNumberOutOfRange, 10, 300) + '!');
						return;
					}
				
					aStarTimeout = timeout;
					GameScr.info1.addInfo(Strings.xmapTimeout + ": " + aStarTimeout, 0);
				}
				else
				{
					GameCanvas.startOKDlg(Strings.invalidValue + '!');
					return;
				}
				onCancelChat();
			}

			public void onCancelChat()
			{
				ChatTextField.gI().ResetTF();
			}
		}

		internal static bool isUseCapsuleNormal = false;
		internal static bool isUseCapsuleVip = true;
		internal static bool isXmapAStar = false;
		static bool isChangingMap;
		static bool isMovingMyChar;
		internal static int aStarTimeout = 60;

		static Random random = new Random();

		[ChatCommand("xcsdb")]
		internal static void ToggleUseCapsuleVip()
		{
			isUseCapsuleVip = !isUseCapsuleVip;
			GameScr.info1.addInfo(Strings.xmapUseSpecialCapsule + ": " + Strings.OnOffStatus(isUseCapsuleVip), 0);
		}

		[ChatCommand("xcsb")]
		internal static void ToggleUseCapsuleNormal()
		{
			isUseCapsuleNormal = !isUseCapsuleNormal;
			GameScr.info1.addInfo(Strings.xmapUseNormalCapsule + ": " + Strings.OnOffStatus(isUseCapsuleNormal), 0);
		}

		[ChatCommand("xmp"), HotkeyCommand('x')]
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

		internal static void NextMap(MapNext mapNext)
		{
			switch (mapNext.type)
			{
			case TypeMapNext.AutoWaypoint:
				NextMapAutoWaypoint(mapNext);
				break;
			case TypeMapNext.NpcMenu:
				NextMapNpcMenu(mapNext);
				break;
			case TypeMapNext.NpcPanel:
				NextMapNpcPanel(mapNext);
				break;
			case TypeMapNext.Position:
				NextMapPosition(mapNext);
				break;
			case TypeMapNext.Capsule:
				NextMapCapsule(mapNext);
				break;
			}
		}

		internal static void NextMapAutoWaypoint(MapNext mapNext)
		{
			Waypoint waypoint = XmapUtils.findWaypoint(mapNext.to);
			ChangeMap(waypoint);
		}

		internal static void NextMapNpcMenu(MapNext mapNext)
		{
			int npcId = mapNext.info[0];
			if (npcId == 38)
			{
				bool flag = false;
				for (int i = 0; i < GameScr.vNpc.size(); i++)
				{
					Npc npc = (Npc)GameScr.vNpc.elementAt(i);
					if (npc.template.npcTemplateId == npcId)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					Waypoint waypoint;
					if (TileMap.mapID == 27 || TileMap.mapID == 29)
						waypoint = XmapUtils.findWaypoint(28);
					else
					{
						if (random.Next(27, 29) == 27)
						{
							waypoint = XmapUtils.findWaypoint(27);
						}
						else
						{
							waypoint = XmapUtils.findWaypoint(29);
						}

					}

					ChangeMap(waypoint);
					return;
				}
			}
			Service.gI().openMenu(npcId);
			for (int i = 1; i < mapNext.info.Length; i++)
			{
				int select = mapNext.info[i];
				Service.gI().confirmMenu((short)npcId, (sbyte)select);
			}
			Char.chatPopup = null;
		}

		internal static void NextMapNpcPanel(MapNext mapNext)
		{
			int idNpc = mapNext.info[0];
			int selectMenu = mapNext.info[1];
			int selectPanel = mapNext.info[2];
			Service.gI().openMenu(idNpc);
			Thread.Sleep(500);
			Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
			Thread.Sleep(500);
			Service.gI().requestMapSelect(selectPanel);
		}

		internal static void NextMapPosition(MapNext mapNext)
		{
			int xPos = mapNext.info[0];
			int yPos = mapNext.info[1];
			MoveMyChar(xPos, yPos);
			if (Utils.Distance(Char.myCharz().cx, Char.myCharz().cy, xPos, yPos) <= TileMap.size)
			{
				Service.gI().requestChangeMap();
				Service.gI().getMapOffline();
			}
		}

		internal static void NextMapCapsule(MapNext mapNext)
		{
			XmapUtils.mapCapsuleReturn = TileMap.mapID;
			int select = mapNext.info[0];
			Service.gI().requestMapSelect(select);
		}

		static void MoveMyChar(int x, int y)
		{ 
			Utils.TeleportMyChar(x, y);
		}

		static void ChangeMap(Waypoint waypoint)
		{
			Utils.ChangeMap(waypoint);
		}
	}
}
