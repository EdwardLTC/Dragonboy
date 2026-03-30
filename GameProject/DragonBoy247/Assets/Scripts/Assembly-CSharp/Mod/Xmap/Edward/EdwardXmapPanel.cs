using System.Collections.Generic;
using Mod.CustomPanel;

namespace Mod.Xmap.Edward
{
	public static class EdwardXmapPanel
	{
		static readonly List<int> currentMaps = new List<int>();

		internal static void Show(List<int> maps)
		{
			currentMaps.Clear();
			currentMaps.AddRange(maps);
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
			PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(
				panel,
				g,
				currentMaps,
				mapId => TileMap.mapNames[mapId],
				mapId => $"ID: {mapId}"
			);
		}

		static void PaintTabHeader(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Edward Xmap");
		}

		static void SetTab(Panel panel)
		{
			SetTabPanelTemplates.setTabListTemplate(panel, currentMaps);
		}

		static void DoFire(Panel panel)
		{
			InfoDlg.hide();
			panel.hide();
			EdwardXmapController.StartGoToMap(currentMaps[panel.selected]);
		}
	}
}
