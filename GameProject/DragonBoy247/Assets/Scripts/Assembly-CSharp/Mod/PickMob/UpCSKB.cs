using Mod.ModHelper.Menu;

namespace Mod.PickMob
{
	public enum ActionOnFullBag
	{
		PutToBox,
		Deposit
	}

	public class UpCSKB : IChatable
	{
		const string RMS_ACTION_ON_FULL_BAG = "upcskb_action_on_full_bag";
		const string RMS_MONEY_TO_DEPOSIT = "upcskb_price_for_deposit";

		public static ActionOnFullBag actionOnFullBag = ActionOnFullBag.Deposit;

		public static int moneyToDeposit = 40_000_000;

		static UpCSKB _Instance;

		static UpCSKB()
		{
			LoadRMS();
		}

		static UpCSKB getInstance => _Instance ??= new UpCSKB();

		public void onChatFromMe(string text, string to)
		{
			if (ChatTextField.gI().tfChat.getText() == null || ChatTextField.gI().tfChat.getText().Equals(string.Empty) || text.Equals(string.Empty))
			{
				ChatTextField.gI().isShow = false;
			}
			else if (ChatTextField.gI().strChat.Equals("Nhập giá kí gửi"))
			{
				try
				{
					int price = int.Parse(ChatTextField.gI().tfChat.getText());
					ApplyMoneyToDeposit(price, true, true);
				}
				catch
				{
					GameScr.info1.addInfo("Delay Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ResetChatTextField();
			}
		}

		public void onCancelChat()
		{
		}

		internal static void ShowMenu()
		{
			string actionOnFullBagText = actionOnFullBag == ActionOnFullBag.Deposit ? "Kí gửi" : "Chuyển vào rương";
			string cpDesc = "UP cskb by Edward\n";
			MenuBuilder menuBuilder = new MenuBuilder().setChatPopup(cpDesc);
			menuBuilder.addItem("Khi x99 cskb\n " + actionOnFullBagText, new MenuAction(() => SetActionOnFullBag(actionOnFullBag == ActionOnFullBag.Deposit ? ActionOnFullBag.PutToBox : ActionOnFullBag.Deposit)));
			if (actionOnFullBag == ActionOnFullBag.Deposit)
			{
				menuBuilder.addItem("Số tiền kí gửi khi x99 cskb\n " + moneyToDeposit, new MenuAction(OpenTFInputMoneyToDeposit));
			}
			menuBuilder.start();
		}

		static void SetActionOnFullBag(ActionOnFullBag action)
		{
			ApplyActionOnFullBag(action, true, true);
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
				ModStorage.WriteInt(RMS_ACTION_ON_FULL_BAG, (int)actionOnFullBag);
		}

		static void OpenTFInputMoneyToDeposit()
		{
			ChatTextField.gI().strChat = "Nhập giá kí gửi";
			ChatTextField.gI().tfChat.name = "input_price_to_deposit";
			GameCanvas.panel.isShow = false;
			ChatTextField.gI().startChat2(getInstance, string.Empty);
		}

		static void ApplyMoneyToDeposit(int money, bool saveRms, bool showInfo)
		{
			moneyToDeposit = money;
			if (showInfo)
			{
				GameScr.info1.addInfo($"[Up CSKB] Số tiền kí gửi khi túi đầy: {moneyToDeposit}", 0);
			}
			if (saveRms)
			{
				ModStorage.WriteInt(RMS_MONEY_TO_DEPOSIT, moneyToDeposit);
			}
		}

		static void LoadRMS()
		{
			int savedAction = ModStorage.ReadInt(RMS_ACTION_ON_FULL_BAG, (int)ActionOnFullBag.Deposit);
			if (savedAction == (int)ActionOnFullBag.PutToBox || savedAction == (int)ActionOnFullBag.Deposit)
			{
				ApplyActionOnFullBag((ActionOnFullBag)savedAction, false, false);
			}
			else
			{
				ApplyActionOnFullBag(actionOnFullBag, true, false);
			}

			int savedMoney = ModStorage.ReadInt(RMS_MONEY_TO_DEPOSIT, moneyToDeposit);
			if (savedMoney > 0)
			{
				ApplyMoneyToDeposit(savedMoney, false, false);
			}
			else
			{
				ApplyMoneyToDeposit(savedAction, true, false);
			}
		}

		static void ResetChatTextField()
		{
			ChatTextField.gI().strChat = "Chat";
			ChatTextField.gI().tfChat.name = "chat";
			ChatTextField.gI().isShow = false;
		}
	}
}
