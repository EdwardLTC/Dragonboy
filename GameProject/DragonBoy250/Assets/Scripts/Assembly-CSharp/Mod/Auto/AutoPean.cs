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

		static readonly Dictionary<BeanAction, bool> _autoActions = new Dictionary<BeanAction, bool>
		{
			[BeanAction.Request] = false,
			[BeanAction.Donate] = false,
			[BeanAction.Harvest] = false
		};

		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			HandleSenzuBeans();
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

		static void HandleSenzuBeans()
		{
			if (_autoActions[BeanAction.Request] && ClanUtils.CanAskForPeans())
				ClanUtils.RequestPeans();

			if (_autoActions[BeanAction.Donate] && ClanUtils.CanDonatePeans())
				ClanUtils.DonatePeans();

			if (_autoActions[BeanAction.Harvest])
				HarvestMagicTree();
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

		public static Dictionary<int, (BeanAction action, string name, string description)> GetActions()
		{
			return new Dictionary<int, (BeanAction action, string name, string description)>
			{
				[0] = (BeanAction.Request, $"Tự động xin đậu: {(_autoActions[BeanAction.Request] ? "Bật" : "Tắt")}", "Tự động xin đậu khi có thể"),
				[1] = (BeanAction.Donate, $"Tự động cho đậu: {(_autoActions[BeanAction.Donate] ? "Bật" : "Tắt")}", "Tự động cho đậu khi có thể"),
				[2] = (BeanAction.Harvest, $"Tự động hái đậu thần: {(_autoActions[BeanAction.Harvest] ? "Bật" : "Tắt")}", "Tự động hái đậu thần khi có thể")
			};
		}
	}

	internal static class BeanPanel
	{
		static Dictionary<int, (BeanAction action, string name, string description)> actions = new Dictionary<int, (BeanAction action, string name, string description)>();

		internal static void Show()
		{
			actions.Clear();
			actions = AutoPean.GetActions();
			CustomPanelMenu.Show(new CustomPanelMenuConfig
			{
				SetTabAction = SetTab,
				DoFireItemAction = DoFire,
				PaintTabHeaderAction = PaintTabHeader,
				PaintAction = Paint
			});
		}

		static void Paint(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(panel, g, actions, x => x.Value.name, x => x.Value.description);
		}

		static void PaintTabHeader(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Đậu thần");
		}

		static void SetTab(Panel panel)
		{
			SetTabPanelTemplates.setTabListTemplate(panel, actions);
		}

		static void DoFire(Panel panel)
		{
			InfoDlg.hide();
			AutoPean.ToggleOnAction(actions[panel.selected].action);
			actions = AutoPean.GetActions();
		}
	}
}
