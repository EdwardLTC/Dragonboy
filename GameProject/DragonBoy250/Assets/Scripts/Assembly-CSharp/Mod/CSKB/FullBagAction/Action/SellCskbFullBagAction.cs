using System.Collections;
using Mod.Xmap;
using UnityEngine;

namespace Mod.PickMob
{
	internal sealed class SellCskbFullBagAction : ICskbFullBagAction
	{
		public IEnumerator Execute(CskbFullBagActionContext context)
		{
			if (!XmapController.gI.IsActing && TileMap.mapID != CskbConstants.BunmaHomeMapId)
			{
				XmapController.start(CskbConstants.BunmaHomeMapId, true);
				yield return null;
			}

			if (TileMap.mapID != CskbConstants.BunmaHomeMapId || XmapController.gI.IsActing)
			{
				yield break;
			}

			Npc npc = Utils.findNpc(CskbConstants.SellNpcId);
			if (!CskbNpcHelper.IsMyCharInNpcPosition(npc))
			{
				Utils.teleToNpc(CskbConstants.SellNpcId);
				yield return new WaitForSecondsRealtime(0.5f);
			}

			if (CskbNpcHelper.ShouldOpenShopMenu(npc))
			{
				yield return CskbNpcHelper.OpenShopMenu(npc, CskbConstants.SellNpcId, 1);
			}

			if (TryGetCSKBIndexInBag(out sbyte index))
			{
				yield return new WaitForSecondsRealtime(0.5f);
				Service.gI().saleItem(0, 1, index);
				yield return new WaitForSecondsRealtime(0.5f);
				Service.gI().saleItem(1, 1, index);
				yield return new WaitForSecondsRealtime(0.5f);
			}
		}

		static bool TryGetCSKBIndexInBag(out sbyte index)
		{
			index = Utils.getIndexItemBag(CskbConstants.IdCapsuleKb);
			return index != -1;
		}
	}
}
