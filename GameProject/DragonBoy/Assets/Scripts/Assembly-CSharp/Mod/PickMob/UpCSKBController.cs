using System;
using Mod.Auto;
using Mod.ModHelper;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.Xmap;

namespace Mod.PickMob
{
	internal class UpCSKBController : ThreadActionUpdate<UpCSKBController>
	{

		internal override int Interval => 1000;

		const int ID_ICON_MD = 2758;
		const short ID_CAPSULE_MD = 379;
		const short ID_CAPSULE_KB = 380;

		static long notUsingMDTime;
		static int mapIdTrain;
		static int mapIdHome;
		static int zoneIdTrain;

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

			if (DateTime.Now.Ticks - notUsingMDTime > 3 * 60 * 10000000L)
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

			if (capsuleKB?.quantity == 99 && !XmapController.gI.IsActing)
			{
				XmapController.start(mapIdHome);
			}

			if (capsuleKB?.quantity == 99 && TileMap.mapID == mapIdHome)
			{
				Service.gI().getItem(1, Utils.getIndexItemBag(ID_CAPSULE_KB));
			}

			if (TileMap.mapID != mapIdTrain && !XmapController.gI.IsActing)
			{
				XmapController.start(mapIdTrain);
			}

			if (TileMap.mapID == mapIdTrain && TileMap.zoneID != zoneIdTrain)
			{
				Service.gI().requestChangeZone(zoneIdTrain, 0);
			}
		}

		static void Start()
		{
			Pk9rPickMob.SetAutoPickItems(true);
			Pk9rPickMob.SetAvoidSuperMonster(true);
			Pk9rPickMob.SetSlaughter(true);
			AutoGoback.mode = AutoGoback.GoBackMode.GoBackToFixedLocation;
			mapIdTrain = TileMap.mapID;
			zoneIdTrain = TileMap.zoneID;
			mapIdHome = XmapUtils.getIdMapHome(Char.myCharz().cgender);
			GameScr.info1.addInfo("[Up CSKB] start ", 0);
		}

		static void Stop()
		{
			GameScr.info1.addInfo("[Up CSKB] stop ", 0);
		}

		[HotkeyCommand('p')]
		internal void toggleActing()
		{
			toggle();
			if (IsActing)
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
