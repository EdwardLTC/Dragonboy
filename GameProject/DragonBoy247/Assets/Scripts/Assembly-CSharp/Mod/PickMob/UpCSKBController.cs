using System;
using System.Collections;
using System.Linq;
using Mod.Auto;
using Mod.Graphics;
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
		const long MD_IDLE_TIMEOUT_TICKS = 2 * 60 * 10000000L;
		static readonly int[] mapCanTrain =
		{
			92, 93, 94, 95, 96, 97, 98, 99, 100
		};

		static long notUsingMDTime;
		static int? mapIdTrain;
		static int? zoneIdTrain;

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			AutoFusion();

			UpdateNotUsingMdTime();

			Item capsuleInBag = Utils.getItemInBag(ID_CAPSULE_KB);
			Item capsuleInBox = Utils.getItemInBox(ID_CAPSULE_KB);

			if (ShouldRecoverMd() && !TryRecoverMd(capsuleInBag, capsuleInBox))
			{
				yield break;
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

			if (capsuleInBag?.quantity == 99)
			{
				yield return HandleFullBag(capsuleInBag);
			}

			yield return ReturnToTrainMapIfNeeded(capsuleInBag);
			yield return ChangeToTrainZoneIfNeeded();
		}

		protected override void OnStart()
		{
			if (!mapCanTrain.Contains(TileMap.mapID))
			{
				Stop("Map hiện tại không thể train");
				return;
			}

			GraphicsReducer.Level = ReduceGraphicsLevel.Level2;
			HideGameUI.SetState(true);
			Pk9rPickMob.SetAttackMonsterBySendCommand(true);
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
				Stop("Đã có 99 CSKB trong box và còn " + capsuleInBag.quantity + " CSKB trong túi");
				return;
			}

			if (!ItemTime.isExistItem(ID_ICON_MD))
			{
				bool isUseMDSuccess = Utils.useItem(ID_CAPSULE_MD);
				if (!isUseMDSuccess)
				{
					Stop("Không thể sử dụng MD");
					return;
				}
			}

			notUsingMDTime = 0L;
			base.OnStart();
		}

		protected override void OnStop()
		{
			GraphicsReducer.Level = ReduceGraphicsLevel.Off;
			HideGameUI.SetState(false);
			Pk9rPickMob.SetAttackMonsterBySendCommand(false);
			Pk9rPickMob.SetSlaughter(false);
			AutoLogin.SetState(false);
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
			base.OnStop();
		}

		static void UpdateNotUsingMdTime()
		{
			if (ItemTime.isExistItem(ID_ICON_MD))
			{
				notUsingMDTime = 0L;
				return;
			}

			if (notUsingMDTime == 0L)
			{
				notUsingMDTime = DateTime.Now.Ticks;
			}
		}

		static bool ShouldRecoverMd()
		{
			return notUsingMDTime > 0 && DateTime.Now.Ticks - notUsingMDTime > MD_IDLE_TIMEOUT_TICKS;
		}

		static bool TryRecoverMd(Item capsuleInBag, Item capsuleInBox)
		{
			if (capsuleInBox?.quantity == 99 && capsuleInBag?.quantity >= 70 && UpCSKB.actionOnFullBag == ActionOnFullBag.PutToBox)
			{
				StopAndGoHome("Đã có 99 CSKB trong box và còn " + capsuleInBag.quantity + " CSKB trong túi");
				return false;
			}

			if (!Utils.useItem(ID_CAPSULE_MD))
			{
				StopAndGoHome("Không thể sử dụng MD");
				return false;
			}

			notUsingMDTime = 0L;
			return true;
		}

		static IEnumerator HandleFullBag(Item capsuleInBag)
		{
			if (UpCSKB.actionOnFullBag == ActionOnFullBag.PutToBox)
			{
				yield return PutFullBagIntoBox();
				yield break;
			}

			if (UpCSKB.actionOnFullBag == ActionOnFullBag.Deposit)
			{
				yield return DepositFullBag(capsuleInBag);
			}
		}

		static IEnumerator PutFullBagIntoBox()
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

		static IEnumerator DepositFullBag(Item capsuleInBag)
		{
			if (!XmapController.gI.IsActing && TileMap.mapID != MAP_MARKET_ID)
			{
				XmapController.start(MAP_MARKET_ID);
				yield return null;
			}

			if (TileMap.mapID != MAP_MARKET_ID || XmapController.gI.IsActing)
			{
				yield break;
			}

			Npc npc28 = Utils.findNpc(28);
			if (!IsMyCharInNpc28Position(npc28))
			{
				Utils.teleToNpc(28);
				yield return new WaitForSecondsRealtime(0.5f);
			}

			if (ShouldOpenDepositMenu(npc28))
			{
				yield return OpenDepositMenu(npc28);
			}

			if (TryGetDepositItem(out Item itemCSKBForDeposit))
			{
				yield return new WaitForSecondsRealtime(1f);
				Service.gI().kigui(0, itemCSKBForDeposit.itemId, 0, UpCSKB.moneyToDeposit, capsuleInBag.quantity);
			}
		}

		static bool ShouldOpenDepositMenu(Npc npc28)
		{
			return IsMyCharInNpc28Position(npc28) && GameCanvas.panel is not null && !GameCanvas.panel.isShow;
		}

		static bool IsMyCharInNpc28Position(Npc npc28)
		{
			return npc28 != null && Char.myCharz().cx == npc28.cx && Char.myCharz().cy == npc28.ySd - npc28.ySd % 24;
		}

		static IEnumerator OpenDepositMenu(Npc npc28)
		{
			Char.myCharz().arrItemShop = null;
			Char.myCharz().npcFocus = npc28;
			Service.gI().openMenu(28);
			yield return new WaitForSecondsRealtime(1f);
			Service.gI().confirmMenu(28, 1);
			yield return new WaitUntil(() =>
			{
				Char c = Char.myCharz();
				return c is { arrItemShop: { Length: > 4 } arr } && arr[4] != null;
			});
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

			itemCSKBForDeposit = itemShops[4].FirstOrDefault(i => i.template.id == ID_CAPSULE_KB && i.buyType == 0);
			return itemCSKBForDeposit is not null;
		}

		static IEnumerator ReturnToTrainMapIfNeeded(Item capsuleInBag)
		{
			if (mapIdTrain == null || XmapController.gI.IsActing || TileMap.mapID == mapIdTrain || capsuleInBag?.quantity == 99)
			{
				yield break;
			}

			ClosePanels();
			yield return new WaitForSecondsRealtime(1f);
			XmapController.start(mapIdTrain.Value);
			yield return null;
		}

		static void AutoFusion()
		{
			sbyte index = Utils.getIndexItemBag(921, 454);
			if (index == -1 || !Char.myCharz().havePet || Char.myCharz().isNhapThe || Char.myCharz().isFusion)
			{
				return;
			}

			Service.gI().useItem(0, 1, index, -1);
		}

		static void ClosePanels()
		{
			GameCanvas.panel.hideNow();
			GameCanvas.panel2?.hideNow();
			Char.chatPopup = null;
			GameCanvas.menu.doCloseMenu();
		}

		static IEnumerator ChangeToTrainZoneIfNeeded()
		{
			if (TileMap.mapID != mapIdTrain || zoneIdTrain == null || TileMap.zoneID == zoneIdTrain)
			{
				yield break;
			}

			yield return new WaitForSecondsRealtime(2f);
			Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
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

		static void Stop(string message)
		{
			gI.Toggle(false);
			GameScr.info1.addInfo("[Up CSKB] " + message + ", đã tắt auto", 0);
		}

		public override string ToString()
		{
			return "State: " + (gI.IsActing ? "Running" : "Stopped") + ", Map: " + mapIdTrain + ", ZoneIdTrain: " + zoneIdTrain;
		}
	}
}
