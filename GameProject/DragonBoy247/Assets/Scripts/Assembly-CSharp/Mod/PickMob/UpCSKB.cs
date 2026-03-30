namespace Mod.PickMob
{
	internal enum ActionOnFullBag
	{
		PutToBox,
		Deposit
	}

	internal static class UpCSKB
	{
		public static ActionOnFullBag actionOnFullBag = ActionOnFullBag.Deposit;

		public static int moneyToDeposit = 40_000_000;

		static void SetActionOnFullBag(ActionOnFullBag action)
		{
			switch (action)
			{
			case ActionOnFullBag.PutToBox:
				GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động chuyển vào box", 0);
				break;
			case ActionOnFullBag.Deposit:
				GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động bán vào NPC", 0);
				break;
			}
		}

		static void SetMoneyToDeposit(int money)
		{
			GameScr.info1.addInfo($"[Up CSKB] Số tiền sẽ bán khi túi đầy: {money}", 0);
			moneyToDeposit = money;
		}
	}
}
