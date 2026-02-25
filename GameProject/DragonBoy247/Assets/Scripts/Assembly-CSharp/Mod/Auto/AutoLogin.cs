using Mod.R;
using Mod.Xmap;
using UnityEngine;

namespace Mod.Auto
{
	internal class AutoLogin
	{
		internal static bool isEnabled;
		internal static bool IsRunning => isEnabled && steps > 0;
		static long lastTimeAttemptLogin;
		static long lastTimeUpdate;
		static int lastMapID;
		static int lastZoneID;
		static int lastX;
		static int lastY;
		static int steps;

		internal static void Update()
		{
			if (!isEnabled)
				return;
			switch (steps)
			{
			case 0:
				CheckForDisconnected();
				break;
			case 1:
				if (mSystem.currentTimeMillis() - lastTimeUpdate <= 750) return;
				lastTimeUpdate = mSystem.currentTimeMillis();
				AttemptLogin();
				break;
			case 2:
				// if (mSystem.currentTimeMillis() - lastTimeUpdate <= 750) return;
				lastTimeUpdate = mSystem.currentTimeMillis();
				break;
			}
		}

		static void CheckForDisconnected()
		{
			if ((GameCanvas.currentScreen is not GameScr && !Char.isLoadingMap && !Char.ischangingMap) || !Session_ME.gI().isConnected())
			{
				lastTimeAttemptLogin = mSystem.currentTimeMillis();
				GameCanvas.serverScreen.switchToMe();
				GameCanvas.startOKDlg(string.Format(Strings.autoLoginReattemptLoginIn, 30) + '!');
				steps = 1;
			}
		}

		static void AttemptLogin()
		{
			if (GameCanvas.currentScreen is GameScr)
			{
				steps = 2;
				return;
			}
			if (mSystem.currentTimeMillis() - lastTimeAttemptLogin < 35000)
			{
				return;
			}
			lastTimeAttemptLogin = mSystem.currentTimeMillis();
			if (GameCanvas.currentScreen is ServerListScreen)
			{
				Debug.Log("Currently in ServerListScreen, switching to it again to reset state...");
				GameCanvas.serverScreen.switchToMe();
				GameCanvas.serverScreen.perform(3, null);
				GameCanvas.serverScreen.switchToMe();
			}
			else
			{
				isEnabled = false;
				Debug.Log("Not in ServerListScreen, cannot attempt login. Disabling AutoLogin.");
			}
		}

		static void GotoLastMapAndZone()
		{
			if (TileMap.mapID != lastMapID)
			{
				if (!XmapController.gI.IsActing)
					XmapController.start(lastMapID);
			}
			else if (TileMap.zoneID != lastZoneID)
				Service.gI().requestChangeZone(lastZoneID, 0);
			else if (Utils.Distance(Char.myCharz().cx, Char.myCharz().cy, lastX, lastY) > 15)
				Utils.TeleportMyChar(lastX, lastY);
			else
			{
				Char.chatPopup = null;
				ChatPopup.currChatPopup = null;
				steps = 0;
			}
		}

		internal static void OnGameScrUpdate()
		{
			if (!isEnabled)
				return;
			if (steps == 0 && GameCanvas.gameTick % (60f * Time.timeScale) == 0f)
			{
				lastMapID = TileMap.mapID;
				lastZoneID = TileMap.zoneID;
				lastX = Char.myCharz().cx;
				lastY = Utils.GetYGround(Char.myCharz().cx);
			}
		}

		internal static void SetState(bool state)
		{
			isEnabled = state;
		}
	}
}
