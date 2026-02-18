using System;
using Mod.Auto;
using Mod.ModHelper;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.Xmap;
using UnityEngine;

namespace Mod.PickMob
{
	internal class UpCSKBController : ThreadActionUpdate<UpCSKBController>
	{

		internal override int Interval => 1000;

		const int ID_ICON_MD = 2758;
		const short ID_CAPSULE_MD = 379;
		const short ID_CAPSULE_KB = 380;

		static long notUsingMDTime;
		static long lastTimeGoBack;
		static int? mapIdTrain;
		static int? zoneIdTrain;

		protected override void update()
		{
			// if (!ItemTime.isExistItem(ID_ICON_MD))
			// {
			// 	if (notUsingMDTime == 0L)
			// 	{
			// 		notUsingMDTime = DateTime.Now.Ticks;
			// 	}
			// }
			// else
			// {
			// 	notUsingMDTime = 0L;
			// }
			//
			// if (notUsingMDTime > 0 && DateTime.Now.Ticks - notUsingMDTime > 2 * 60 * 10000000L)
			// {
			// 	bool isUseMDSuccess = Utils.useItem(ID_CAPSULE_MD);
			//
			// 	if (!isUseMDSuccess)
			// 	{
			// 		onStop();
			// 		return;
			// 	}
			//
			// 	notUsingMDTime = 0L;
			// }

			if (Char.myCharz().IsCharDead())
			{
				handleDeath();
			}

			if (Utils.IsMyCharHome())
			{
				regenHpWhenInHome();
			}

			Item capsuleKB = Utils.getItemInBag(ID_CAPSULE_KB);

			if (capsuleKB?.quantity == 99 && !XmapController.gI.IsActing && !Utils.IsMyCharHome() && Utils.CanNextMap())
			{
				XmapController.start(XmapUtils.getIdMapHome(Char.myCharz().cgender));
			}

			if (capsuleKB?.quantity == 99 && Utils.IsMyCharHome())
			{
				Service.gI().getItem(1, Utils.getIndexItemBag(ID_CAPSULE_KB));
			}

			if (mapIdTrain != null && !XmapController.gI.IsActing && TileMap.mapID != mapIdTrain && capsuleKB?.quantity != 99 && Utils.CanNextMap() && Char.myCharz().cHP > 1000)
			{
				XmapController.start(mapIdTrain.Value);
			}

			if (TileMap.mapID == mapIdTrain && zoneIdTrain != null && TileMap.zoneID != zoneIdTrain)
			{
				Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
			}
		}

		protected override void onStop()
		{
			Pk9rPickMob.SetSlaughter(false);
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
			base.onStop();
		}

		protected override void onStart()
		{
			Pk9rPickMob.SetAutoPickItems(true);
			Pk9rPickMob.SetAvoidSuperMonster(true);
			Pk9rPickMob.SetSlaughter(true);
			AutoLogin.SetState(true);
			mapIdTrain = TileMap.mapID;
			zoneIdTrain = TileMap.zoneID;
			GameScr.info1.addInfo("[Up CSKB]: Map Id Train=" + mapIdTrain + ", Zone Id Train=" + zoneIdTrain, 0);
			base.onStart();
		}

		static void handleDeath()
		{
			long now = mSystem.currentTimeMillis();
			long timeSinceDeath = now - lastTimeGoBack;
			if (timeSinceDeath > 4000)
			{
				lastTimeGoBack = now;
				return;
			}
			if (timeSinceDeath > 3000)
			{
				Service.gI().returnTownFromDead();
			}
		}

		static void regenHpWhenInHome()
		{
			Service.gI().pickItem(-1);
		}
	}
}
