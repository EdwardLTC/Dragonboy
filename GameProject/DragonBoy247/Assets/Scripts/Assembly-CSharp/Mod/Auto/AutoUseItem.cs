using System.Collections.Generic;
using System.Threading;

namespace Mod.Auto
{
	public class AutoUseItem : IActionListener, IChatable
	{

		static AutoUseItem _Instance;

		static readonly List<Item> listItemAuto = new List<Item>();

		static Item itemToAuto;

		static readonly string[] inputDelay = new string[2]
		{
			"Nhập delay", "giây"
		};

		static readonly string[] inputSellQuantity = new string[2]
		{
			"Nhập số lượng bán", "số lượng"
		};

		static readonly string[] inputBuyQuantity = new string[2]
		{
			"Nhập số lượng mua", "số lượng"
		};

		public void perform(int idAction, object p)
		{
			switch (idAction)
			{
			case 1:
				OpenTFAutoUseItem((Item)p);
				break;
			case 2:
				RemoveItemAuto((int)p);
				break;
			case 3:
				OpenTFAutoTradeItem((Item)p);
				break;
			}
		}

		public void onChatFromMe(string text, string to)
		{
			if (ChatTextField.gI().tfChat.getText() == null || ChatTextField.gI().tfChat.getText().Equals(string.Empty) || text.Equals(string.Empty))
			{
				ChatTextField.gI().isShow = false;
			}
			else if (ChatTextField.gI().strChat.Equals(inputDelay[0]))
			{
				try
				{
					int delay = int.Parse(ChatTextField.gI().tfChat.getText());
					itemToAuto.Delay = delay < 100 ? 100 : delay;
					GameScr.info1.addInfo("Auto " + itemToAuto.Name + ": " + itemToAuto.Delay + " ms", 0);
					listItemAuto.Add(itemToAuto);
				}
				catch
				{
					GameScr.info1.addInfo("Delay Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ResetChatTextField();
			}
			else if (ChatTextField.gI().strChat.Equals(inputBuyQuantity[0]))
			{
				try
				{
					int quantity = int.Parse(ChatTextField.gI().tfChat.getText());
					itemToAuto.Quantity = quantity;
					new Thread((ThreadStart)delegate
					{
						AutoBuy(itemToAuto);
					}).Start();
				}
				catch
				{
					GameScr.info1.addInfo("Số Lượng Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ResetChatTextField();
			}
			else
			{
				if (!ChatTextField.gI().strChat.Equals(inputSellQuantity[0]))
				{
					return;
				}
				try
				{
					int quantity2 = int.Parse(ChatTextField.gI().tfChat.getText());
					itemToAuto.Quantity = quantity2;
					new Thread((ThreadStart)delegate
					{
						AutoSell(itemToAuto);
					}).Start();
				}
				catch
				{
					GameScr.info1.addInfo("Số Lượng Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ResetChatTextField();
			}
		}

		public void onCancelChat()
		{
		}

		public static AutoUseItem getInstance()
		{
			if (_Instance == null)
			{
				_Instance = new AutoUseItem();
			}
			return _Instance;
		}

		public static void Update()
		{
			if (listItemAuto.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < listItemAuto.Count; i++)
			{
				Item item = listItemAuto[i];
				if (mSystem.currentTimeMillis() - item.LastTimeUse > item.Delay)
				{
					item.LastTimeUse = mSystem.currentTimeMillis();
					bool isUsed = Utils.useItem((short)item.Id);
					if (!isUsed)
					{
						GameScr.info1.addInfo("Không Thể Sử Dụng " + item.Name + "!", 0);
						listItemAuto.RemoveAt(i);
						i--;
						continue;
					}
					break;
				}
			}
		}

		static void ResetChatTextField()
		{
			ChatTextField.gI().strChat = "Chat";
			ChatTextField.gI().tfChat.name = "chat";
			ChatTextField.gI().isShow = false;
		}

		public static bool isAutoUse(int templateId)
		{
			for (int i = 0; i < listItemAuto.Count; i++)
			{
				if (listItemAuto[i].Id == templateId)
				{
					return true;
				}
			}
			return false;
		}

		static void RemoveItemAuto(int templateId)
		{
			for (int i = 0; i < listItemAuto.Count; i++)
			{
				if (listItemAuto[i].Id == templateId)
				{
					listItemAuto.RemoveAt(i);
					break;
				}
			}
		}

		static void OpenTFAutoUseItem(Item item)
		{
			itemToAuto = item;
			ChatTextField.gI().strChat = inputDelay[0];
			ChatTextField.gI().tfChat.name = inputDelay[1];
			GameCanvas.panel.isShow = false;
			ChatTextField.gI().startChat2(getInstance(), string.Empty);
		}

		static void OpenTFAutoTradeItem(Item item)
		{
			itemToAuto = item;
			GameCanvas.panel.isShow = false;
			if (item.IsSell)
			{
				ChatTextField.gI().strChat = inputSellQuantity[0];
				ChatTextField.gI().tfChat.name = inputSellQuantity[1];
			}
			else
			{
				ChatTextField.gI().strChat = inputBuyQuantity[0];
				ChatTextField.gI().tfChat.name = inputBuyQuantity[1];
			}
			ChatTextField.gI().startChat2(getInstance(), string.Empty);
		}

		static void AutoSell(Item item)
		{
			Thread.Sleep(100);
			short index = item.Index;
			while (item.Quantity > 0)
			{
				if (Char.myCharz().arrItemBag[index] == null || Char.myCharz().arrItemBag[index] != null && Char.myCharz().arrItemBag[index].template.id != (short)item.Id)
				{
					GameScr.info1.addInfo("Không Tìm Thấy Item!", 0);
					return;
				}
				Service.gI().saleItem(0, 1, (short)(index + 3));
				Thread.Sleep(100);
				Service.gI().saleItem(1, 1, index);
				Thread.Sleep(1000);
				item.Quantity--;
				if (Char.myCharz().xu > 1963100000)
				{
					GameScr.info1.addInfo("Xong!", 0);
					return;
				}
			}
			GameScr.info1.addInfo("Xong!", 0);
		}

		void AutoBuy(Item item)
		{
			while (item.Quantity > 0 && !GameScr.gI().isBagFull())
			{
				Service.gI().buyItem(!item.IsGold ? (sbyte)1 : (sbyte)0, item.Id, 0);
				item.Quantity--;
				Thread.Sleep(1000);
			}
			GameScr.info1.addInfo("Xong!", 0);
		}

		public class Item
		{

			public int Delay;
			public int Id;

			public short Index;

			public bool IsGold;

			public bool IsSell;

			public long LastTimeUse;

			public string Name;

			public int Quantity;

			public Item(int id, string name)
			{
				Id = id;
				Name = name;
			}

			public Item(int id, short isGold, bool index, bool isSell)
			{
				Id = id;
				IsGold = index;
				Index = isGold;
				IsSell = isSell;
			}
		}
	}
}
