using System.Collections;
using System.Linq;
using Mod.Xmap;
using UnityEngine;

namespace Mod.PickMob
{
	internal sealed class DepositCskbFullBagAction : ICskbFullBagAction
	{
		public IEnumerator Execute(CskbFullBagActionContext context)
		{
			if (!XmapController.gI.IsActing && TileMap.mapID != CskbConstants.MapMarketId)
			{
				XmapController.start(CskbConstants.MapMarketId);
				yield return null;
			}

			if (TileMap.mapID != CskbConstants.MapMarketId || XmapController.gI.IsActing)
			{
				yield break;
			}

			Npc npc = Utils.findNpc(CskbConstants.DepositNpcId);
			if (!CskbNpcHelper.IsMyCharInNpcPosition(npc))
			{
				Utils.teleToNpc(CskbConstants.DepositNpcId);
				yield return new WaitForSecondsRealtime(0.5f);
			}

			if (CskbNpcHelper.ShouldOpenShopMenu(npc))
			{
				yield return OpenDepositMenu(npc);
			}

			if (TryGetDepositItem(out Item itemCSKBForDeposit))
			{
				yield return new WaitForSecondsRealtime(1f);
				Service.gI().kigui(0, itemCSKBForDeposit.itemId, 0, UpCSKB.moneyToDeposit, context.CapsuleInBag.quantity);
			}
		}

		static IEnumerator OpenDepositMenu(Npc npc)
		{
			yield return CskbNpcHelper.OpenShopMenu(npc, CskbConstants.DepositNpcId, 1);
			yield return new WaitUntil(() =>
			{
				Char c = Char.myCharz();
				return c is { arrItemShop: { Length: > 4 } arr } && arr[4] != null;
			});
			GameCanvas.menu.doCloseMenu();
			Char.chatPopup = null;
		}

		static bool TryGetDepositItem(out Item itemCSKBForDeposit)
		{
			itemCSKBForDeposit = null;
			if (GameCanvas.panel is null || !GameCanvas.panel.isShow)
			{
				return false;
			}

			Item[][] itemShops = Char.myCharz().arrItemShop;
			if (itemShops == null || itemShops.Length <= 4 || itemShops[4] == null)
			{
				return false;
			}

			itemCSKBForDeposit = itemShops[4].FirstOrDefault(i => i.template.id == CskbConstants.IdCapsuleKb && i.buyType == 0);
			return itemCSKBForDeposit is not null;
		}
	}
}
