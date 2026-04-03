using System.Collections;
using Mod.ModHelper;
using Mod.Xmap;
using UnityEngine;

namespace Mod.Auto
{
	public class AutoKillSelfAndPickGold : CoroutineMainThreadAction<AutoKillSelfAndPickGold>
	{
		int lastItemId = -1;

		float pickStartTime = -1f;
		protected override float Interval => 0.5f;

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().IsCharDead() || TileMap.mapID != XmapUtils.getIdMapLang(Char.myCharz().cgender) || AutoGoback.IsGoingBack)
			{
				yield break;
			}

			int itemMapId = -1;

			for (int i = 0; i < GameScr.vItemMap.size(); i++)
			{
				ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (itemMap.template.id == 190 && itemMap.playerId == Char.myCharz().charID)
				{
					itemMapId = itemMap.itemMapID;
					break;
				}
			}

			if (itemMapId != -1)
			{
				if (itemMapId != lastItemId)
				{
					lastItemId = itemMapId;
					pickStartTime = Time.realtimeSinceStartup;
				}

				if (Time.realtimeSinceStartup - pickStartTime < 5f)
				{
					Service.gI().pickItem(itemMapId);
				}
				else
				{
					AttackSelf();
				}
			}
			else
			{
				AttackSelf();
			}
		}

		void AttackSelf()
		{
			MyVector myVector = new MyVector();
			myVector.addElement(Char.myCharz());
			Service.gI().sendPlayerAttack(new MyVector(), myVector, -1);
		}

		protected override void OnStart()
		{
			if (TileMap.mapID != XmapUtils.getIdMapLang(Char.myCharz().cgender))
			{
				GameScr.info1.addInfo("Chỉ hoạt động ở map Làng", 0);
				Toggle(false);
				return;
			}
			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
			}
			AutoGoback.gI.Toggle(true);
			base.OnStart();
		}

		protected override void OnStop()
		{
			AutoGoback.gI.Toggle(false);
			GameScr.info1.addInfo("Đã tắt AutoKillSelfAndPickGold", 0);
			base.OnStop();
		}
	}
}
