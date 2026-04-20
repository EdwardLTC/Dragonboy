using Mod.R;

namespace Mod.Auto
{
	internal class AutoLogin
	{
		internal static bool isEnabled;
		static long lastTimeAttemptLogin;
		static long lastTimeUpdate;
		public static int server;
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
				if (mSystem.currentTimeMillis() - lastTimeUpdate <= 750) return;
				lastTimeUpdate = mSystem.currentTimeMillis();
				break;
			}
		}

		static void CheckForDisconnected()
		{
			if (!IsLoginSuccess() || !Session_ME.gI().isConnected())
			{
				lastTimeAttemptLogin = mSystem.currentTimeMillis();
				GameCanvas.serverScreen.switchToMe();
				Char.myChar = null;
				steps = 1;
			}
		}

		static bool IsLoginSuccess()
		{
			return GameCanvas.currentScreen is not ServerListScreen && GameCanvas.currentScreen is not LoginScr;
		}

		static void AttemptLogin()
		{
			if (GameCanvas.currentScreen is GameScr)
			{
				steps = 2;
				return;
			}
			GameCanvas.startOKDlg(string.Format(Strings.autoLoginReattemptLoginIn, 35 - (mSystem.currentTimeMillis() - lastTimeAttemptLogin) / 1000) + '!');
			if (mSystem.currentTimeMillis() - lastTimeAttemptLogin < 35000)
			{
				return;
			}
			lastTimeAttemptLogin = mSystem.currentTimeMillis();
			if (GameCanvas.currentScreen is LoginScr)
			{
				GameCanvas.loginScr.doLogin();
			}
			else if (GameCanvas.currentScreen is ServerListScreen)
			{
				if (ServerListScreen.ipSelect != server)
				{
					SwitchServer(server);
					return;
				}
				GameCanvas.serverScreen.perform(3, null);
			}
			else
			{
				GameCanvas.serverScreen.switchToMe();
			}
		}

		static void SwitchServer(int index)
		{
			try
			{
				ServerListScreen.ipSelect = index;
				GameCanvas.serverScreen.selectServer();
			}
			catch { }
		}

		internal static void SetState(bool state)
		{
			isEnabled = state;
		}
	}
}
