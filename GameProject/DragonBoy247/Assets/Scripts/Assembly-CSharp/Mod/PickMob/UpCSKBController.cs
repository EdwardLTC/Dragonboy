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
		const short MAP_MARKET_ID = 84;
		static readonly int[] mapCanTrain =
		{
			92, 93, 94, 95, 96, 97, 98, 99, 100
		};

		static long notUsingMDTime;
		static long lastTimeGoBack;
		static int? mapIdTrain;
		static int? zoneIdTrain;
		
		static bool isMenuDepositOpen;

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

			Item capsuleInBag = Utils.getItemInBag(ID_CAPSULE_KB);

			Item capsuleInBox = Utils.getItemInBox(ID_CAPSULE_KB);

			if (notUsingMDTime > 0 && DateTime.Now.Ticks - notUsingMDTime > 2 * 60 * 10000000L)
			{
				if (capsuleInBox?.quantity == 99 && capsuleInBag?.quantity >= 70)
				{
					StopAndGoHome("Đã có 99 CSKB trong box và còn " + capsuleInBag.quantity + " CSKB trong túi");
					yield break;
				}
			
				bool isUseMDSuccess = Utils.useItem(ID_CAPSULE_MD);
			
				if (!isUseMDSuccess)
				{
					StopAndGoHome("Không thể sử dụng MD");
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

			if (capsuleInBag?.quantity == 99 && UpCSKB.actionOnFullBag == ActionOnFullBag.PutToBox)
			{
				if (!XmapController.gI.IsActing && !Utils.IsMyCharHome() && Utils.CanNextMap())
				{
					XmapController.start(XmapUtils.getIdMapHome(Char.myCharz().cgender));
					yield return null;
				}

				if (Utils.IsMyCharHome())
				{
					PutCSKBIntoBox();
					yield return null;
				}
			}

			if (capsuleInBag?.quantity == 99 && UpCSKB.actionOnFullBag == ActionOnFullBag.Deposit)
			{
				if (!XmapController.gI.IsActing && TileMap.mapID != MAP_MARKET_ID)
				{
					XmapController.start(MAP_MARKET_ID);
					yield return null;
				}

				if (TileMap.mapID == MAP_MARKET_ID && !XmapController.gI.IsActing)
				{
					Npc npc28 = Utils.findNpc(28);
					
					if (Char.myCharz().cx != npc28.cx || Char.myCharz().cy != npc28.ySd - npc28.ySd % 24)
					{
						Utils.teleToNpc(28);
					}
					
					Item itemCSKBForDeposit = Char.myCharz().arrItemShop[4].FirstOrDefault(i => i.template.id == ID_CAPSULE_KB);

					if (Char.myCharz().cx == npc28.cx && Char.myCharz().cy == npc28.ySd - npc28.ySd % 24 && GameCanvas.panel is not null && !GameCanvas.panel.isShow)
					{
						yield return new WaitForSecondsRealtime(0.5f);
						Service.gI().openMenu(28);
						yield return new WaitForSecondsRealtime(0.5f);
						Service.gI().confirmMenu(28,1);
						Debug.Log("Đã mở menu gửi đồ");
					}

					if (GameCanvas.panel is not null && GameCanvas.panel.isShow && itemCSKBForDeposit is not null)
					{
						yield return new WaitForSecondsRealtime(1f);
						Service.gI().kigui(0, itemCSKBForDeposit.itemId, 0, UpCSKB.moneyToDeposit, capsuleInBag.quantity);
					}
				}
			}

			if (mapIdTrain != null && !XmapController.gI.IsActing && TileMap.mapID != mapIdTrain && capsuleInBag?.quantity != 99)
			{
				if (GameCanvas.panel is not null && GameCanvas.panel.isShow)
				{
					GameCanvas.panel.isShow = false;
				}
				if (GameCanvas.panel2 is not null && GameCanvas.panel2.isShow)
				{
					GameCanvas.panel2.isShow = false;
				}
				
				XmapController.start(mapIdTrain.Value);
				yield return null;
			}

			if (TileMap.mapID == mapIdTrain && TileMap.zoneID != zoneIdTrain && zoneIdTrain != null)
			{
				yield return new WaitForSecondsRealtime(2f);
				Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
			}
		}

		protected override void OnStop()
		{
			Pk9rPickMob.SetSlaughter(false);
			AutoLogin.SetState(false);
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
			Utils.status = "Đã kết nối";
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

			Item capsuleInBag = Utils.getItemInBag(ID_CAPSULE_KB);

			Item capsuleInBox = Utils.getItemInBox(ID_CAPSULE_KB);
			
			if (capsuleInBox?.quantity == 99 && capsuleInBag?.quantity >= 70)
			{
				gI.Toggle(false);
				GameScr.info1.addInfo("[Up CSKB] Đã có 99 CSKB trong box và còn " + capsuleInBag.quantity + " CSKB trong túi, đã tắt auto", 0);
				return;
			}
			
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
			Utils.status = "Up CSKB";
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

		static void PutCSKBIntoBox()
		{
			Service.gI().getItem(1, Utils.getIndexItemBag(ID_CAPSULE_KB));
		}

		static void StopAndGoHome(string message)
		{
			gI.Toggle(false);
			XmapController.start(XmapUtils.getIdMapHome(Char.myCharz().cgender));
			GameScr.info1.addInfo("[Up CSKB] " + message + ", đã tắt auto và về nhà", 0);
		}

		public override string ToString()
		{
			return "State: " + (gI.IsActing ? "Running" : "Stopped") + ", Map: " + mapIdTrain + ", ZoneIdTrain: " + zoneIdTrain;
		}
	}
}
