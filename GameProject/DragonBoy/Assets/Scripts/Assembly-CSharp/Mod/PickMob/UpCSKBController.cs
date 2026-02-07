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

		internal override int Interval => 1500;

		const int ID_ICON_MD = 2758;
		const short ID_CAPSULE_MD = 379;
		const short ID_CAPSULE_KB = 380;

		static long notUsingMDTime;
		static int? mapIdTrain;
		static int mapIdHome;
		static int? zoneIdTrain;

		protected override void update()
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
					toggleActing();
					return;
				}

				notUsingMDTime = 0L;
			}

			Item capsuleKB = Utils.getItemInBag(ID_CAPSULE_KB);

			if (capsuleKB?.quantity == 99 && !XmapController.gI.IsActing && TileMap.mapID != mapIdHome)
			{
				XmapController.start(mapIdHome);
			}

			if (capsuleKB?.quantity == 99 && TileMap.mapID == mapIdHome)
			{
				Service.gI().getItem(1, Utils.getIndexItemBag(ID_CAPSULE_KB));
			}

			if (mapIdTrain != null && !XmapController.gI.IsActing && TileMap.mapID != mapIdTrain)
			{
				XmapController.start(mapIdTrain.Value);
			}

			if (TileMap.mapID == mapIdTrain && zoneIdTrain != null && TileMap.zoneID != zoneIdTrain)
			{
				Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
			}
		}

		static void Start()
		{
			Pk9rPickMob.SetAutoPickItems(true);
			Pk9rPickMob.SetAvoidSuperMonster(true);
			Pk9rPickMob.SetSlaughter(true);
			AutoLogin.SetState(true);
			AutoGoback.mode = AutoGoback.GoBackMode.GoBackToFixedLocation;
			mapIdTrain = TileMap.mapID;
			zoneIdTrain = TileMap.zoneID;
			mapIdHome = XmapUtils.getIdMapHome(Char.myCharz().cgender);
			GameScr.info1.addInfo("[Up CSKB]: Map Id Train=" + mapIdTrain + ", Zone Id Train=" + zoneIdTrain, 0);
			toggle(true);
		}

		static void Stop()
		{
			Pk9rPickMob.SetSlaughter(false);
			toggle(false);
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
		}

		internal void toggleActing()
		{
			if (IsActing)
			{
				Stop();
			}
			else
			{
				Start();
			}
		}

		internal static void SetState(bool isActing)
		{
			if (isActing)
			{
				Start();
			}
			else
			{
				Stop();
			}
		}
	}
}
