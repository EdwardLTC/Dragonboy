using Mod.ModHelper.Menu;
using Mod.R;

namespace Mod.Xmap
{
	internal sealed class XmapMenuPresenter
	{
		readonly XmapSettings settings;

		internal XmapMenuPresenter(XmapSettings settings)
		{
			this.settings = settings;
		}

		internal void ShowMainMenu()
		{
			if (XmapController.gI.IsActing)
			{
				XmapController.finishXmap();
				GameScr.info1.addInfo(Strings.xmapCanceled, 0);
				return;
			}

			XmapData.LoadGroupMaps();

			new MenuBuilder()
				.setChatPopup(string.Format(Strings.xmapChatPopup, TileMap.mapName, TileMap.mapID))
				.map(XmapData.groups, groupMap =>
				{
					return new MenuItem(groupMap.GetCaption(mResources.language), new MenuAction(() =>
					{
						XmapPanel.Show(groupMap.Maps);
					}));
				})
				.addItem(Strings.settings, new MenuAction(ShowSettings))
				.start();
		}

		void ShowSettings()
		{
			new MenuBuilder()
				.setChatPopup(string.Format(Strings.xmapChatPopup, TileMap.mapName, TileMap.mapID))
				.addItem(Strings.xmapUseNormalCapsule + ": " + Strings.OnOffStatus(settings.UseCapsuleNormal), new MenuAction(ToggleUseCapsuleNormal))
				.addItem(Strings.xmapUseSpecialCapsule + ": " + Strings.OnOffStatus(settings.UseCapsuleVip), new MenuAction(ToggleUseCapsuleVip))
				.start();
		}

		void ToggleUseCapsuleVip()
		{
			settings.UseCapsuleVip = !settings.UseCapsuleVip;
			GameScr.info1.addInfo(Strings.xmapUseSpecialCapsule + ": " + Strings.OnOffStatus(settings.UseCapsuleVip), 0);
		}

		void ToggleUseCapsuleNormal()
		{
			settings.UseCapsuleNormal = !settings.UseCapsuleNormal;
			GameScr.info1.addInfo(Strings.xmapUseNormalCapsule + ": " + Strings.OnOffStatus(settings.UseCapsuleNormal), 0);
		}
	}

}
