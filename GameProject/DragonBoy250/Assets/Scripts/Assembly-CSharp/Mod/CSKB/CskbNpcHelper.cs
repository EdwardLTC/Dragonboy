using System.Collections;
using UnityEngine;

namespace Mod.PickMob
{
	internal static class CskbNpcHelper
	{
		internal static bool IsMyCharInNpcPosition(Npc npc)
		{
			return npc != null && Char.myCharz().cx == npc.cx && Char.myCharz().cy == npc.ySd - npc.ySd % 24;
		}

		internal static bool ShouldOpenShopMenu(Npc npc)
		{
			return IsMyCharInNpcPosition(npc) && GameCanvas.panel is not null && !GameCanvas.panel.isShow;
		}

		internal static IEnumerator OpenShopMenu(Npc npc, short npcId, sbyte menuId)
		{
			Char.myCharz().arrItemShop = null;
			Char.myCharz().npcFocus = npc;
			Service.gI().openMenu(npcId);
			yield return new WaitForSecondsRealtime(1f);
			Service.gI().confirmMenu(npcId, menuId);
			GameCanvas.menu.doCloseMenu();
			Char.chatPopup = null;
		}
	}
}
