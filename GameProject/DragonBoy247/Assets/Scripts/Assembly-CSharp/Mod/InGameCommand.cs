using JetBrains.Annotations;
using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;

namespace Mod
{
	[UsedImplicitly]
	internal static class InGameCommand
	{
		
		[HotkeyCommand('f')]
		internal static void usePorata()
		{
			sbyte index = Utils.getIndexItemBag(921, 454);
			if (index == -1 || !Char.myCharz().havePet)
			{
				GameScr.info1.addInfo("Yêu cầu bông tai và đệ tử", 0);
				return;
			}
			
			Service.gI().useItem(0, 1, index, -1);
			Service.gI().petStatus(3);	
			
		}
		
		[HotkeyCommand('j')]
		internal static void ChangeMapLeft()
		{
			if (Utils.IsMeInNRDMap() || Utils.waypointLeft == null)
				Utils.TeleportMyChar(60);
			else
				Utils.ChangeMap(Utils.waypointLeft);
		}

		[HotkeyCommand('k')]
		internal static void ChangeMapMiddle()
		{
			if (Utils.IsMeInNRDMap())
			{
				if (Char.myCharz().bag >= 0 && ClanImage.idImages.containsKey(Char.myCharz().bag.ToString()))
				{
					ClanImage clanImage = (ClanImage)ClanImage.idImages.get(Char.myCharz().bag.ToString());
					if (clanImage.idImage != null)
					{
						for (int i = 0; i < clanImage.idImage.Length; i++)
						{
							if (clanImage.idImage[i] == 2322)
							{
								for (int j = 0; j < GameScr.vNpc.size(); j++)
								{
									Npc npc = (Npc)GameScr.vNpc.elementAt(j);
									if (npc.template.npcTemplateId >= 30 && npc.template.npcTemplateId <= 36)
									{
										Char.myCharz().npcFocus = npc;
										Utils.TeleportMyChar(npc.cx, npc.cy - 3);
										return;
									}
								}
							}
						}
					}
				}
				for (int k = 0; k < GameScr.vItemMap.size(); k++)
				{
					ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(k);
					if (itemMap != null && itemMap.IsNRD())
					{
						Char.myCharz().itemFocus = itemMap;
						Utils.TeleportMyChar(itemMap.x, itemMap.y);
						return;
					}
				}
			}
			else if (Utils.waypointMiddle == null)
				Utils.TeleportMyChar(TileMap.pxw / 2);
			else
				Utils.ChangeMap(Utils.waypointMiddle);
		}

		[HotkeyCommand('l')]
		internal static void ChangeMapRight()
		{
			if (Utils.IsMeInNRDMap() || Utils.waypointRight == null)
				Utils.TeleportMyChar(TileMap.pxw - 60);
			else
				Utils.ChangeMap(Utils.waypointRight);
		}

		[HotkeyCommand('g')]
		internal static void sendGiaoDichToCharFocusing()
		{
			Char charFocus = Char.myCharz().charFocus;
			if (charFocus == null)
			{
				GameScr.info1.addInfo("Trỏ vào nhân vật để giao dịch", 0);
				return;
			}

			Service.gI().giaodich(0, charFocus.charID, -1, -1);
			GameScr.info1.addInfo("Đã gửi lời mời giao dịch đến " + charFocus.cName, 0);
		}

		[ChatCommand("k")]
		internal static void changeZone(int zone)
		{
			Service.gI().requestChangeZone(zone, -1);
		}

		[HotkeyCommand('m')]
		internal static void menuZone()
		{
			Utils.menuZone();
		}
		
		[HotkeyCommand('c')]
		internal static void useCapsule()
		{
			sbyte index = Utils.getIndexItemBag(193, 194);
			if (index == -1)
			{
				GameScr.info1.addInfo("Không tìm thấy capsule", 0);
				return;
			}

			Service.gI().useItem(0, 1, index, -1);
		}

		[HotkeyCommand('t')]
		internal static void UseTDLT()
		{
			Utils.useItem(521);
		}

		[HotkeyCommand('s')]
		internal static void FocusBoss()
		{ 
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char @char = (Char)GameScr.vCharInMap.elementAt(i);
				if (@char.cTypePk == 5)
				{
					Char.myCharz().charFocus = @char;
				}
			}
		}
	}
}
