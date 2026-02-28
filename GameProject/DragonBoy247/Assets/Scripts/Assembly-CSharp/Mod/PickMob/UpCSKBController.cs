using System;
using System.Collections;
using System.Linq;
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
		static readonly int[] mapCanTrain = {92, 93, 94, 95, 96, 97, 98, 99, 100};

		static long notUsingMDTime;
		static long lastTimeGoBack;
		static int? mapIdTrain;
		static int? zoneIdTrain;

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			if (!ItemTime.isExistItem(ID_ICON_MD))
			{
				if (notUsingMDTime == 0L)
				{
					notUsingMDTime = DateTime.Now.Ticks;
				}
			}
			else
			{
				notUsingMDTime = 0L;
			}
			
			if (notUsingMDTime > 0 && DateTime.Now.Ticks - notUsingMDTime > 2 * 60 * 10000000L)
			{
				bool isUseMDSuccess = Utils.useItem(ID_CAPSULE_MD);
			
				if (!isUseMDSuccess)
				{
					gI.Toggle(false);
					GameScr.info1.addInfo("[Up CSKB] Không thể sử dụng MD, đã tắt auto", 0);
					yield break;
				}
			
				notUsingMDTime = 0L;
			}

			if (Char.myCharz().IsCharDead())
			{
				yield return new WaitForSecondsRealtime(1f);
				ReviveWhenDead();
			}

			if (Utils.IsMyCharHome() && Char.myCharz().cHP < 1000)
			{
				RegenHpWhenInHome();
				yield return new WaitForSecondsRealtime(1f);
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

			if (mapIdTrain != null && !XmapController.gI.IsActing && TileMap.mapID != mapIdTrain && capsuleKB?.quantity != 99)
			{
				XmapController.start(mapIdTrain.Value);
				yield return null;
			}

			if (TileMap.mapID == mapIdTrain && TileMap.zoneID != zoneIdTrain && zoneIdTrain != null)
			{
				Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
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
			if (!mapCanTrain.Contains(TileMap.mapID))
			{
				gI.Toggle(false);
				GameScr.info1.addInfo("[Up CSKB] Map hiện tại không thể train, đã tắt auto", 0);
				return;
			}
			Pk9rPickMob.SetAutoPickItems(true);
			Pk9rPickMob.SetAvoidSuperMonster(true);
			Pk9rPickMob.SetSlaughter(true);
			AutoLogin.SetState(true);
			mapIdTrain = TileMap.mapID;
			zoneIdTrain = TileMap.zoneID;
			if (!ItemTime.isExistItem(ID_ICON_MD))
			{
				bool isUseMDSuccess = Utils.useItem(ID_CAPSULE_MD);
				if (!isUseMDSuccess)
				{
					gI.Toggle(false);
					GameScr.info1.addInfo("[Up CSKB] Không thể sử dụng MD, đã tắt auto", 0);
					return;
				}
			}
			base.OnStart();
		}

		static void RegenHpWhenInHome()
		{
			Service.gI().pickItem(-1);
		}
		
		static void ReviveWhenDead()
		{
			Service.gI().returnTownFromDead();
		}

		public override string ToString()
		{
			return "State: " + (gI.IsActing ? "Running" : "Stopped") + ", Map: " + mapIdTrain + ", ZoneIdTrain: " + zoneIdTrain;
		}
	}
}
