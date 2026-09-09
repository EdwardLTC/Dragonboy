using System.Collections;
using Mod.ModHelper;
using Mod.R;

namespace Mod.Auto
{
	internal sealed class AutoLogin : CoroutineMainThreadAction<AutoLogin>
	{

		const long ReloginCooldownMs = 35_000;

		static long _lastLoginAttemptTime;
		static int _targetServerIndex;
		static Step _currentStep;

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			switch (_currentStep)
			{
			case Step.CheckConnection:
				CheckConnectionStatus();
				break;

			case Step.AttemptLogin:
				AttemptLogin();
				break;
			}

			yield break;
		}

		static void CheckConnectionStatus()
		{
			bool disconnected = !IsLoggedIn() || !Session_ME.gI().isConnected();
			if (!disconnected)
			{
				return;
			}

			GameCanvas.serverScreen.switchToMe();
			Char.myChar = null;
			_currentStep = Step.AttemptLogin;
		}

		static void AttemptLogin()
		{
			if (GameCanvas.currentScreen is GameScr)
			{
				_currentStep = Step.CheckConnection;
				return;
			}

			long elapsedSinceLastAttempt = mSystem.currentTimeMillis() - _lastLoginAttemptTime;
			long remainingCooldownSeconds = (ReloginCooldownMs - elapsedSinceLastAttempt) / 1000;

			GameCanvas.startOKDlg(
				string.Format(Strings.autoLoginReattemptLoginIn, remainingCooldownSeconds) + '!');

			bool stillOnCooldown = elapsedSinceLastAttempt < ReloginCooldownMs;
			if (stillOnCooldown)
			{
				return;
			}

			_lastLoginAttemptTime = mSystem.currentTimeMillis();

			if (GameCanvas.currentScreen is LoginScr)
			{
				GameCanvas.loginScr.doLogin();
			}
			else if (GameCanvas.currentScreen is ServerListScreen)
			{
				TryLoginFromServerList();
			}
			else
			{
				GameCanvas.serverScreen.switchToMe();
			}
		}

		static void TryLoginFromServerList()
		{
			bool wrongServerSelected = ServerListScreen.ipSelect != _targetServerIndex;
			if (wrongServerSelected)
			{
				SwitchServer(_targetServerIndex);
				return;
			}

			GameCanvas.serverScreen.perform(3, null);
		}

		static void SwitchServer(int index)
		{
			try
			{
				ServerListScreen.ipSelect = index;
				GameCanvas.serverScreen.selectServer();
			}
			catch
			{
				// Ignore any exceptions that may occur during server switching
			}
		}

		static bool IsLoggedIn()
		{
			return GameCanvas.currentScreen is not ServerListScreen && GameCanvas.currentScreen is not LoginScr;
		}

		public static void SetServer(int index)
		{
			_targetServerIndex = index;
		}

		enum Step
		{
			CheckConnection,
			AttemptLogin
		}
	}
}
