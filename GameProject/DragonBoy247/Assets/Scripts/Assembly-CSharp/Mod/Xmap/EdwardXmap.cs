using System.Linq;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.R;

namespace Mod.Xmap
{
	internal static class EdwardXmap
	{
		static readonly XmapMenuPresenter menuPresenter = new XmapMenuPresenter(XmapContext.Settings);

		[HotkeyCommand('x')]
		internal static void ShowXmapMenu()
		{
			menuPresenter.ShowMainMenu();
		}

		internal static void Info(string text)
		{
			if (!XmapController.gI.IsActing)
			{
				return;
			}

			if (LocalizedString.xmapCantGoHereKeywords.Any(keyword => keyword == text))
			{
				XmapController.finishXmap();
				GameScr.info1.addInfo(Strings.xmapCanceled, 0);
			}
			else if (text == LocalizedString.errorOccurred)
			{
				XmapContext.Navigation.TeleportCharacter(XmapContext.MapLookup.GetGateX(2), XmapContext.MapLookup.GetGateY(2));
			}
		}
	}

}
