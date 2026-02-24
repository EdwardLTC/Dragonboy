using System.Collections;
using Mod.Auto;
using Mod.ModHelper;
using Mod.Xmap;
using UnityEngine;

namespace Mod.PickMob
{
	internal class UpCSKBController : CoroutineMainThreadAction<UpCSKBController>
	{

		const int ID_ICON_MD = 2758;
		const short ID_CAPSULE_MD = 379;
		const short ID_CAPSULE_KB = 380;

		static long notUsingMDTime;
		static long lastTimeGoBack;
		static int? mapIdTrain;
		static int? zoneIdTrain;
		
		static bool isChecked = false;

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
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
				yield return new WaitForSecondsRealtime(4f);
				Service.gI().returnTownFromDead();
			}

			if (Utils.IsMyCharHome())
			{
				regenHpWhenInHome();
			}

			Item capsuleKB = Utils.getItemInBag(ID_CAPSULE_KB);

			if (capsuleKB?.quantity == 99 && !XmapController.gI.IsActing && !Utils.IsMyCharHome() && Utils.CanNextMap())
			{
				XmapController.start(XmapUtils.getIdMapHome(Char.myCharz().cgender));
				yield return null;
			}

			if (capsuleKB?.quantity == 99 && Utils.IsMyCharHome())
			{
				Service.gI().getItem(1, Utils.getIndexItemBag(ID_CAPSULE_KB));
				yield return null;
			}

			if (mapIdTrain != null && !XmapController.gI.IsActing && TileMap.mapID != mapIdTrain && capsuleKB?.quantity != 99 && Utils.CanNextMap() && Char.myCharz().cHP > 1000)
			{
				if (!isChecked)
				{
					XmapController.start(mapIdTrain.Value);
					isChecked = true;
				}
				else
				{
					Debug.Log($"[Up CSKB] Đã kiểm tra đi lại về map train, không thực hiện xmap nữa. Map hiện tại: {TileMap.mapID}, Map train: {mapIdTrain}");
					Debug.Log($"State: isChecked={isChecked}, isActing={XmapController.gI.IsActing}, canNextMap={Utils.CanNextMap()}, charHP={Char.myCharz().cHP}");
				}
				yield return null;
			}

			if (TileMap.mapID == mapIdTrain && zoneIdTrain != null && TileMap.zoneID != zoneIdTrain)
			{
				Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
				yield return  null;
			}
		}

		protected override void OnStop()
		{
			Pk9rPickMob.SetSlaughter(false);
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
			base.OnStop();
		}

		protected override void OnStart()
		{
			Pk9rPickMob.SetAutoPickItems(true);
			Pk9rPickMob.SetAvoidSuperMonster(true);
			Pk9rPickMob.SetSlaughter(true);
			AutoLogin.SetState(true);
			mapIdTrain = TileMap.mapID;
			zoneIdTrain = TileMap.zoneID;
			GameScr.info1.addInfo("[Up CSKB]: Map Id Train=" + mapIdTrain + ", Zone Id Train=" + zoneIdTrain, 0);
			base.OnStart();
		}

		static void regenHpWhenInHome()
		{
			Service.gI().pickItem(-1);
		}
	}
}
