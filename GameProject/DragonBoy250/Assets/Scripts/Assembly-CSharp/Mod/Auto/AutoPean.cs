using System;
using System.Collections;
using System.Collections.Generic;
using Mod.CustomPanel;
using Mod.ModHelper;

namespace Mod.Auto
{
	internal enum BeanAction
	{
		Request,
		Donate,
		Harvest
	}

	internal class AutoPean : CoroutineMainThreadAction<AutoPean>
	{
		const string RMS_HP_PERCENT = "autopean_hp_percent";
		const string RMS_HP_VALUE = "autopean_hp_value";
		const string RMS_MP_PERCENT = "autopean_mp_percent";
		const string RMS_MP_VALUE = "autopean_mp_value";

		static readonly Dictionary<BeanAction, bool> _autoActions = new Dictionary<BeanAction, bool>
		{
			[BeanAction.Request] = false,
			[BeanAction.Donate] = false,
			[BeanAction.Harvest] = false
		};

		static AutoPean()
		{
			LoadSettings();
		}

		public static int HpPercent { get; private set; }
		public static long HpValue { get; private set; }
		public static int MpPercent { get; private set; }
		public static long MpValue { get; private set; }

		protected override float Interval => 0.25f;

		static void LoadSettings()
		{
			HpPercent = ModStorage.ReadInt(RMS_HP_PERCENT, 0);
			HpValue = ModStorage.ReadLong(RMS_HP_VALUE);
			MpPercent = ModStorage.ReadInt(RMS_MP_PERCENT, 0);
			MpValue = ModStorage.ReadLong(RMS_MP_VALUE);
		}

		public static void SetHpPercent(int percent)
		{
			if (percent < 0) percent = 0;
			if (percent > 100) percent = 100;
			HpPercent = percent;
			ModStorage.WriteInt(RMS_HP_PERCENT, HpPercent);
			if (HpPercent > 0)
				GameScr.info1.addInfo($"Tự ăn đậu khi HP < {HpPercent}%", 0);
			else
				GameScr.info1.addInfo("Đã tắt tự ăn đậu theo % HP", 0);
		}

		public static void SetHpValue(long value)
		{
			if (value < 0) value = 0;
			HpValue = value;
			ModStorage.WriteLong(RMS_HP_VALUE, HpValue);
			if (HpValue > 0)
				GameScr.info1.addInfo($"Tự ăn đậu khi HP < {HpValue:N0}", 0);
			else
				GameScr.info1.addInfo("Đã tắt tự ăn đậu theo số HP", 0);
		}

		public static void SetMpPercent(int percent)
		{
			if (percent < 0) percent = 0;
			if (percent > 100) percent = 100;
			MpPercent = percent;
			ModStorage.WriteInt(RMS_MP_PERCENT, MpPercent);
			if (MpPercent > 0)
				GameScr.info1.addInfo($"Tự ăn đậu khi MP < {MpPercent}%", 0);
			else
				GameScr.info1.addInfo("Đã tắt tự ăn đậu theo % MP", 0);
		}

		public static void SetMpValue(long value)
		{
			if (value < 0) value = 0;
			MpValue = value;
			ModStorage.WriteLong(RMS_MP_VALUE, MpValue);
			if (MpValue > 0)
				GameScr.info1.addInfo($"Tự ăn đậu khi MP < {MpValue:N0}", 0);
			else
				GameScr.info1.addInfo("Đã tắt tự ăn đậu theo số MP", 0);
		}

		protected override IEnumerator OnUpdate()
		{
			HandleSenzuBeans();
			CheckAutoUsePean();
			yield break;
		}

		internal static void ToggleOnAction(BeanAction action)
		{
			_autoActions[action] = !_autoActions[action];

			if (_autoActions[action])
			{
				HandleSenzuBeans();
			}
		}

		internal static bool IsActionEnabled(BeanAction action)
		{
			return _autoActions.TryGetValue(action, out bool enabled) && enabled;
		}

		static void HandleSenzuBeans()
		{
			if (_autoActions[BeanAction.Request] && ClanUtils.CanAskForPeans())
				ClanUtils.RequestPeans();

			if (_autoActions[BeanAction.Donate] && ClanUtils.CanDonatePeans())
				ClanUtils.DonatePeans();

			if (_autoActions[BeanAction.Harvest])
				HarvestMagicTree();
		}

		static void CheckAutoUsePean()
		{
			if (TileMap.mapID == Char.myCharz().cgender + 21)
			{
				return;
			}

			// Không có điều kiện nào được cài đặt
			if (HpPercent <= 0 && HpValue <= 0 && MpPercent <= 0 && MpValue <= 0)
			{
				return;
			}

			Char myChar = Char.myCharz();
			if (myChar == null || myChar.isDie || myChar.cHP <= 0)
			{
				return;
			}

			// Hết đậu thần trong người
			if (GameScr.hpPotion <= 0)
			{
				return;
			}

			// Cooldown 15s ăn đậu
			if (mSystem.currentTimeMillis() - GameScr.gI().lastUsePotion < 15000)
			{
				return;
			}

			// Bị choáng / hóa đá / trói
			if (myChar.stone || myChar.blindEff || myChar.holdEffID > 0)
			{
				return;
			}

			bool needUse = false;

			// HP < x%
			if (HpPercent > 0 && myChar.cHPFull > 0 && myChar.cHP < myChar.cHPFull * HpPercent / 100)
			{
				needUse = true;
			}

			// HP < x số
			if (!needUse && HpValue > 0 && myChar.cHP < HpValue)
			{
				needUse = true;
			}

			// MP < x%
			if (!needUse && MpPercent > 0 && myChar.cMPFull > 0 && myChar.cMP < myChar.cMPFull * MpPercent / 100)
			{
				needUse = true;
			}

			// MP < x số
			if (!needUse && MpValue > 0 && myChar.cMP < MpValue)
			{
				needUse = true;
			}

			if (needUse)
			{
				GameScr.gI().doUseHP();
			}
		}

		static void HarvestMagicTree()
		{
			MagicTree magicTree = GameScr.gI().magicTree;

			if (!Utils.IsMyCharHome()
			    || magicTree.isUpdate
			    || magicTree.isPeasEffect
			    || magicTree.currPeas == 0)
				return;

			Service.gI().openMenu(4);
			Service.gI().confirmMenu(4, 0);
		}
	}

	internal class BeanPanelItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public Action OnClick { get; set; }
		public bool IsInput { get; set; }
	}

	internal class BeanChatHandler : IChatable
	{
		public static BeanChatHandler Instance { get; } = new BeanChatHandler();

		public void onChatFromMe(string text, string to)
		{
			ChatTextField.gI().isShow = false;
			ChatTextField.gI().ResetTF();

			if (string.IsNullOrEmpty(text))
				return;

			if (!long.TryParse(text.Trim(), out long value) || value < 0)
			{
				GameScr.info1.addInfo("Giá trị không hợp lệ!", 0);
				return;
			}

			switch (to)
			{
			case "hp_percent":
				AutoPean.SetHpPercent((int)value);
				break;
			case "hp_value":
				AutoPean.SetHpValue(value);
				break;
			case "mp_percent":
				AutoPean.SetMpPercent((int)value);
				break;
			case "mp_value":
				AutoPean.SetMpValue(value);
				break;
			}
		}

		public void onCancelChat()
		{
		}
	}

	internal static class BeanPanel
	{
		static readonly List<BeanPanelItem> panelItems = new List<BeanPanelItem>();

		internal static void Show()
		{
			LoadItems();
			CustomPanelMenu.Show(new CustomPanelMenuConfig
			{
				SetTabAction = SetTab,
				DoFireItemAction = DoFire,
				PaintTabHeaderAction = PaintTabHeader,
				PaintAction = Paint
			});
		}

		static void LoadItems()
		{
			panelItems.Clear();

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự động xin đậu: {(AutoPean.IsActionEnabled(BeanAction.Request) ? "Bật" : "Tắt")}",
				Description = "Tự động xin đậu khi có thể (Nhấn để Bật/Tắt)",
				OnClick = () => AutoPean.ToggleOnAction(BeanAction.Request)
			});

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự động cho đậu: {(AutoPean.IsActionEnabled(BeanAction.Donate) ? "Bật" : "Tắt")}",
				Description = "Tự động cho đậu khi có thể (Nhấn để Bật/Tắt)",
				OnClick = () => AutoPean.ToggleOnAction(BeanAction.Donate)
			});

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự động hái đậu thần: {(AutoPean.IsActionEnabled(BeanAction.Harvest) ? "Bật" : "Tắt")}",
				Description = "Tự động hái đậu thần khi có thể (Nhấn để Bật/Tắt)",
				OnClick = () => AutoPean.ToggleOnAction(BeanAction.Harvest)
			});

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự dùng khi HP < {AutoPean.HpPercent}%: {(AutoPean.HpPercent > 0 ? $"{AutoPean.HpPercent}%" : "Tắt")}",
				Description = "Nhấn để nhập % HP tối thiểu dùng đậu (0 để tắt)",
				IsInput = true,
				OnClick = () => OpenInput("Nhập % HP (0 để tắt):", "hp_percent", AutoPean.HpPercent.ToString())
			});

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự dùng khi HP < {AutoPean.HpValue:N0}: {(AutoPean.HpValue > 0 ? $"{AutoPean.HpValue:N0}" : "Tắt")}",
				Description = "Nhấn để nhập số HP tối thiểu dùng đậu (0 để tắt)",
				IsInput = true,
				OnClick = () => OpenInput("Nhập số HP (0 để tắt):", "hp_value", AutoPean.HpValue.ToString())
			});

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự dùng khi MP < {AutoPean.MpPercent}%: {(AutoPean.MpPercent > 0 ? $"{AutoPean.MpPercent}%" : "Tắt")}",
				Description = "Nhấn để nhập % MP/KI tối thiểu dùng đậu (0 để tắt)",
				IsInput = true,
				OnClick = () => OpenInput("Nhập % MP (0 để tắt):", "mp_percent", AutoPean.MpPercent.ToString())
			});

			panelItems.Add(new BeanPanelItem
			{
				Name = $"Tự dùng khi MP < {AutoPean.MpValue:N0}: {(AutoPean.MpValue > 0 ? $"{AutoPean.MpValue:N0}" : "Tắt")}",
				Description = "Nhấn để nhập số MP/KI tối thiểu dùng đậu (0 để tắt)",
				IsInput = true,
				OnClick = () => OpenInput("Nhập số MP (0 để tắt):", "mp_value", AutoPean.MpValue.ToString())
			});
		}

		static void OpenInput(string title, string to, string defaultValue)
		{
			GameCanvas.panel.isShow = false;
			ChatTextField.gI().strChat = title;
			ChatTextField.gI().tfChat.name = title;
			ChatTextField.gI().tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
			ChatTextField.gI().startChat2(BeanChatHandler.Instance, to);
			ChatTextField.gI().tfChat.setText(defaultValue == "0" ? string.Empty : defaultValue);
		}

		static void Paint(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(panel, g, panelItems, x => x.Name, x => x.Description);
		}

		static void PaintTabHeader(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Đậu thần");
		}

		static void SetTab(Panel panel)
		{
			SetTabPanelTemplates.setTabListTemplate(panel, panelItems);
		}

		static void DoFire(Panel panel)
		{
			InfoDlg.hide();
			if (panel.selected < 0 || panel.selected >= panelItems.Count)
				return;

			BeanPanelItem item = panelItems[panel.selected];
			item.OnClick?.Invoke();
			if (!item.IsInput)
			{
				LoadItems();
				SetTab(panel);
			}
		}
	}
}
