using System;
using System.Collections;
using System.Linq;
using Mod.Auto;
using Mod.Graphics;
using Mod.ModHelper;
using Mod.PickMob;
using Mod.Xmap;
using UnityEngine;

namespace Mod.CSKB
{
	internal class UpCSKBController : CoroutineMainThreadAction<UpCSKBController>
	{
		const long MD_IDLE_TIMEOUT_TICKS = 2 * 60 * TimeSpan.TicksPerSecond;
		static readonly int[] mapCanTrain =
		{
			92, 93, 94, 95, 96, 97, 98, 99, 100
		};

		static long notUsingMDTime;
		static int? mapIdTrain;

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			UpdateNotUsingMdTime();

			Item capsuleInBag = Utils.getItemInBag(CskbConstants.IdCapsuleKb);
			Item capsuleInBox = Utils.getItemInBox(CskbConstants.IdCapsuleKb);

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

			if (capsuleInBag?.quantity == CskbConstants.MaxCSKBInBag)
			{
				yield return HandleFullBag(capsuleInBag);
			}

			yield return ReturnToTrainMapIfNeeded(capsuleInBag);
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
			PickMobConfig.SetAutoPickItems(true);
			PickMobConfig.SetAvoidSuperMonster(true);
			PickMobConfig.SetSlaughter(true);
			AutoLogin.gI.Toggle(true);
			mapIdTrain = TileMap.mapID;

			Item capsuleInBag = Utils.getItemInBag(CskbConstants.IdCapsuleKb);
			Item capsuleInBox = Utils.getItemInBox(CskbConstants.IdCapsuleKb);

			if (capsuleInBox?.quantity == CskbConstants.MaxCSKBInBag && capsuleInBag?.quantity >= 970 && UpCSKB.actionOnFullBag == ActionOnFullBag.PutToBox)
			{
				Stop("Đã có " + CskbConstants.MaxCSKBInBag + " CSKB trong box và còn " + capsuleInBag.quantity + " CSKB trong túi");
				return;
			}

			if (!ItemTime.isExistItem(CskbConstants.IdIconMd))
			{
				bool isUseMDSuccess = Utils.useItem(CskbConstants.IdCapsuleMd);
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
			PickMobConfig.SetSlaughter(false);
			AutoLogin.gI.Toggle(false);
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
			base.OnStop();
		}

		static void UpdateNotUsingMdTime()
		{
			if (ItemTime.isExistItem(CskbConstants.IdIconMd))
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
			if (capsuleInBox?.quantity == CskbConstants.MaxCSKBInBag && capsuleInBag?.quantity >= 970 && UpCSKB.actionOnFullBag == ActionOnFullBag.PutToBox)
			{
				StopAndGoHome("Đã có " + CskbConstants.MaxCSKBInBag + " CSKB trong box và còn " + capsuleInBag.quantity + " CSKB trong túi");
				return false;
			}

			if (!Utils.useItem(CskbConstants.IdCapsuleMd))
			{
				StopAndGoHome("Không thể sử dụng MD");
				return false;
			}

			notUsingMDTime = 0L;
			return true;
		}

		static IEnumerator HandleFullBag(Item capsuleInBag)
		{
			ICskbFullBagAction action = CskbFullBagActionFactory.Create(UpCSKB.actionOnFullBag);
			yield return action.Execute(new CskbFullBagActionContext(capsuleInBag));
		}

		static IEnumerator ReturnToTrainMapIfNeeded(Item capsuleInBag)
		{
			if (mapIdTrain == null || XmapController.gI.IsActing || TileMap.mapID == mapIdTrain || capsuleInBag?.quantity == CskbConstants.MaxCSKBInBag)
			{
				yield break;
			}

			ClosePanels();
			yield return new WaitForSecondsRealtime(1f);
			XmapController.start(mapIdTrain.Value);
			yield return null;
		}

		static void ClosePanels()
		{
			GameCanvas.menu.doCloseMenu();
			Char.chatPopup = null;
			if (GameCanvas.panel != null)
			{
				GameCanvas.panel.hide();
			}
			if (GameCanvas.panel2 != null)
			{
				GameCanvas.panel2.hide();
			}
		}

		static void RegenHpWhenInHome()
		{
			Service.gI().pickItem(-1);
		}

		static void ReviveWhenDead()
		{
			Service.gI().returnTownFromDead();
		}

		static void StopAndGoHome(string message)
		{
			gI.Toggle(false);
			XmapController.start(XmapContext.MapLookup.GetHomeMapId(Char.myCharz().cgender));
			GameScr.info1.addInfo("[Up CSKB] " + message + ", đã tắt auto và về nhà", 0);
		}

		static void Stop(string message)
		{
			gI.Toggle(false);
			GameScr.info1.addInfo("[Up CSKB] " + message + ", đã tắt auto", 0);
		}
	}
}
