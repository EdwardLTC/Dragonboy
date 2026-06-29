using Mod.ModHelper.Menu;

namespace Mod.PickMob
{
	public class UpCSKB : IChatable
	{
		const string RMS_ACTION_ON_FULL_BAG = "upcskb_action_on_full_bag";
		const string RMS_MONEY_TO_DEPOSIT = "upcskb_price_for_deposit";

		public static ActionOnFullBag actionOnFullBag = ActionOnFullBag.Sell;
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
				ChatTextField.gI().ResetTF();
			}
		}

		public void onCancelChat()
		{
		}

		internal static void ShowMenu()
		{
			string cpDesc = "UP cskb by Edward\n";
			MenuBuilder menuBuilder = new MenuBuilder().setChatPopup(cpDesc);
			menuBuilder.addItem("Khi x99 cskb\n " + ActionOnFullBagText(actionOnFullBag), new MenuAction(() => SetActionOnFullBag(NextActionOnFullBag(actionOnFullBag))));
			if (actionOnFullBag == ActionOnFullBag.Deposit)
			{
				menuBuilder.addItem("Số tiền kí gửi khi x99 cskb\n " + mSystem.numberTostring(moneyToDeposit), new MenuAction(OpenTFInputMoneyToDeposit));
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
			case ActionOnFullBag.Sell:
				if (showInfo)
					GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động bán vào cửa hàng", 0);
				break;
			}

			if (saveRms)
				ModStorage.WriteInt(RMS_ACTION_ON_FULL_BAG, (int)actionOnFullBag);
		}

		static void OpenTFInputMoneyToDeposit()
		{
			ChatTextField.gI().strChat = "Nhập giá kí gửi";
			ChatTextField.gI().tfChat.name = "Nhập giá kí gửi";
			ChatTextField.gI().tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
			GameCanvas.panel.isShow = false;
			ChatTextField.gI().startChat2(getInstance, string.Empty);
		}

		static void ApplyMoneyToDeposit(int money, bool saveRms, bool showInfo)
		{
			moneyToDeposit = money;
			if (showInfo)
			{
				GameScr.info1.addInfo($"[Up CSKB] Số tiền kí gửi khi túi đầy: {mSystem.numberTostring(moneyToDeposit)}", 0);
			}
			if (saveRms)
			{
				ModStorage.WriteInt(RMS_MONEY_TO_DEPOSIT, moneyToDeposit);
			}
		}

		static void LoadRMS()
		{
			int savedAction = ModStorage.ReadInt(RMS_ACTION_ON_FULL_BAG, (int)ActionOnFullBag.Deposit);
			if (savedAction == (int)ActionOnFullBag.PutToBox || savedAction == (int)ActionOnFullBag.Deposit || savedAction == (int)ActionOnFullBag.Sell)
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

		static string ActionOnFullBagText(ActionOnFullBag action)
		{
			return action switch
			{
				ActionOnFullBag.PutToBox => "Chuyển vào rương",
				ActionOnFullBag.Deposit => "Kí gửi",
				ActionOnFullBag.Sell => "Bán vào cửa hàng",
				_ => string.Empty
			};
		}

		static ActionOnFullBag NextActionOnFullBag(ActionOnFullBag action)
		{
			return action switch
			{
				ActionOnFullBag.PutToBox => ActionOnFullBag.Deposit,
				ActionOnFullBag.Deposit => ActionOnFullBag.Sell,
				ActionOnFullBag.Sell => ActionOnFullBag.PutToBox,
				_ => ActionOnFullBag.Deposit
			};
		}
	}
}
