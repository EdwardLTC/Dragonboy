using Mod.ModHelper.Menu;

namespace Mod.PickMob
{
	internal enum ActionOnFullBag
	{
		PutToBox,
		Deposit
	}

	internal static class UpCSKB
	{
		const string RMS_ACTION_ON_FULL_BAG = "upcskb_action_on_full_bag";
		const string RMS_MONEY_TO_DEPOSIT = "upcskb_price_for_deposit";

		public static ActionOnFullBag actionOnFullBag = ActionOnFullBag.Deposit;

		public static int moneyToDeposit = 40_000_000;
		
		static UpCSKB()
		{
			LoadRMS();
		}
		
		internal static void ShowMenu()
		{
			string actionOnFullBagText = actionOnFullBag == ActionOnFullBag.Deposit ? "Kí gửi" : "Chuyển vào rương";
			string cpDesc = "UP cskb by Edward\n";
			MenuBuilder menuBuilder = new MenuBuilder().setChatPopup(cpDesc);
			menuBuilder.addItem("Khi x99 cskb\n " + actionOnFullBagText, new MenuAction(() => SetActionOnFullBag(actionOnFullBag == ActionOnFullBag.Deposit ? ActionOnFullBag.PutToBox : ActionOnFullBag.Deposit)));
			menuBuilder.start();
		}
		
		static void SetActionOnFullBag(ActionOnFullBag action)
		{
			ApplyActionOnFullBag(action, true, true);
		}
		
		public static void SetMoneyToDeposit(int money)
		{
			ApplyMoneyToDeposit(money, true, true);
		}

		static void ApplyActionOnFullBag(ActionOnFullBag action, bool saveRms, bool showInfo)
		{
			actionOnFullBag = action;
			switch (action)
			{
			case ActionOnFullBag.PutToBox:
				if (showInfo)
					GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động chuyển vào rương", 0);
				break;
			case ActionOnFullBag.Deposit:
				if (showInfo)
					GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động kí gửi", 0);
				break;
			}

			if (saveRms)
				Rms.saveRMSInt(RMS_ACTION_ON_FULL_BAG, (int)actionOnFullBag);
		}

		static void ApplyMoneyToDeposit(int money, bool saveRms, bool showInfo)
		{
			moneyToDeposit = money;
			if (showInfo)
			{
				GameScr.info1.addInfo($"[Up CSKB] Số tiền sẽ bán khi túi đầy: {moneyToDeposit}", 0);
			}
			if (saveRms)
			{
				Rms.saveRMSInt(RMS_MONEY_TO_DEPOSIT, moneyToDeposit);
			}
		}
		
		static void LoadRMS()
		{
			int savedAction = Rms.loadRMSInt(RMS_ACTION_ON_FULL_BAG);
			if (savedAction == (int)ActionOnFullBag.PutToBox || savedAction == (int)ActionOnFullBag.Deposit)
			{
				ApplyActionOnFullBag((ActionOnFullBag)savedAction, false, false);
			}
			else
			{
				ApplyActionOnFullBag(actionOnFullBag, true, false);
			}

			int savedMoney = Rms.loadRMSInt(RMS_MONEY_TO_DEPOSIT);
			if (savedMoney > 0)
			{
				ApplyMoneyToDeposit(savedMoney, false, false);
			}
			else
			{
				ApplyMoneyToDeposit(moneyToDeposit, true, false);
			}
		}
	}
}
