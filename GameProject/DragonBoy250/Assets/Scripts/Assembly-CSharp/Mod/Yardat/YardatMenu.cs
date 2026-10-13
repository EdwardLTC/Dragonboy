using System;
using System.Collections.Generic;
using System.Linq;
using Mod.CustomPanel;
using Mod.Xmap;

namespace Mod.Yardat
{
	public static class YardatMenu
	{
		static readonly List<YardatPanelItem> panelItems = new List<YardatPanelItem>();

		public static void ShowMenu()
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
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Auto Yardat: {(AutoYardat.gI.IsActing ? "Bật" : "Tắt")}",
				Description = "Tự động train quái Yardat",
				OnClick = () => AutoYardat.gI.Toggle(!AutoYardat.gI.IsActing)
			});
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Đánh chuyển mục tiêu: {(YardatSettings.AutoChangeFocus ? "Bật" : "Tắt")}",
				Description = "Tự động chuyển sang mục tiêu Yardat khác",
				OnClick = () =>
				{
					YardatSettings.AutoChangeFocus = !YardatSettings.AutoChangeFocus;
					YardatSettings.Save();
				}
			});
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Tự chọn mọi quái Yardat: {(YardatSettings.AutoSelectAllYardat ? "Bật" : "Tắt")}",
				Description = "Tự động đánh mọi quái Yardat trong map",
				OnClick = () =>
				{
					YardatSettings.AutoSelectAllYardat = !YardatSettings.AutoSelectAllYardat;
					YardatSettings.Save();
				}
			});
			panelItems.Add(new YardatPanelItem
			{
				Name = "Cấu hình mục tiêu chỉ định",
				Description = "Mở panel chọn quái Yardat muốn đánh",
				OpensPanel = true,
				OnClick = YardatTargetPanel.Show
			});
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Auto Bông tai liên tục: {(YardatSettings.AutoPorata ? "Bật" : "Tắt")}",
				Description = "Tự động sử dụng Bông tai Porata",
				OnClick = () =>
				{
					YardatSettings.AutoPorata = !YardatSettings.AutoPorata;
					YardatSettings.Save();
				}
			});
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Auto Đệ về nhà: {(YardatSettings.PetGoHome ? "Bật" : "Tắt")}",
				Description = "Tự động đưa đệ về nhà",
				OnClick = () =>
				{
					YardatSettings.PetGoHome = !YardatSettings.PetGoHome;
					YardatSettings.Save();
				}
			});
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Tự di chuyển tới quái: {(YardatSettings.AutoMoveToTarget ? "Bật" : "Tắt")}",
				Description = "Tự động di chuyển tới mục tiêu",
				OnClick = () =>
				{
					YardatSettings.AutoMoveToTarget = !YardatSettings.AutoMoveToTarget;
					YardatSettings.Save();
				}
			});
			if (TileMap.mapID != 131 && TileMap.mapID != 132 && TileMap.mapID != 133)
			{
				panelItems.Add(new YardatPanelItem
				{
					Name = "Đến Hành Tinh Yardat (Map 131)",
					Description = "Di chuyển tới map 131",
					OnClick = () =>
					{
						XmapController.start(131);
					}
				});
			}
		}

		static void Paint(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(panel, g, panelItems, x => x.Name, x => x.Description);
		}

		static void PaintTabHeader(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Auto Yardat");
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

			YardatPanelItem item = panelItems[panel.selected];
			item.OnClick?.Invoke();
			if (!item.OpensPanel)
			{
				LoadItems();
				SetTab(panel);
			}
		}
	}

	internal class YardatPanelItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public Action OnClick { get; set; }
		public bool OpensPanel { get; set; }
	}

	internal static class YardatTargetPanel
	{
		static readonly List<YardatPanelItem> panelItems = new List<YardatPanelItem>();

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

			// 1. Tự chọn mọi quái Yardat (Bật / Tắt)
			panelItems.Add(new YardatPanelItem
			{
				Name = $"Tự chọn mọi quái Yardat: {(YardatSettings.AutoSelectAllYardat ? "Bật" : "Tắt")}",
				Description = "Tự động đánh mọi quái Yardat (charID <= 0) trong map",
				OnClick = () =>
				{
					YardatSettings.AutoSelectAllYardat = !YardatSettings.AutoSelectAllYardat;
					YardatSettings.Save();
				}
			});

			// 2. Thêm tất cả quái hiện tại
			panelItems.Add(new YardatPanelItem
			{
				Name = "Chọn tất cả quái trong map",
				Description = "Thêm toàn bộ quái Yardat trong map vào danh sách",
				OnClick = () =>
				{
					for (int i = 0; i < GameScr.vCharInMap.size(); i++)
					{
						Char c = (Char)GameScr.vCharInMap.elementAt(i);
						if (c != null && c.charID != Char.myCharz().charID && !c.isPet && !c.isMiniPet && c.charID <= 0 && !string.IsNullOrEmpty(c.cName))
						{
							YardatSettings.CustomTargetIds.Add(c.charID);
						}
					}
					YardatSettings.Save();
					GameScr.info1.addInfo($"Đã thêm {YardatSettings.CustomTargetIds.Count} quái vào danh sách!", 0);
				}
			});

			// 3. Xóa tất cả mục tiêu
			panelItems.Add(new YardatPanelItem
			{
				Name = "Bỏ chọn tất cả quái",
				Description = "Xóa toàn bộ mục tiêu chỉ định trong danh sách",
				OnClick = () =>
				{
					YardatSettings.CustomTargetIds.Clear();
					YardatSettings.Save();
					GameScr.info1.addInfo("Đã xóa toàn bộ mục tiêu chỉ định!", 0);
				}
			});

			// 4. Danh sách quái trong map hiện tại
			HashSet<int> displayedIds = new HashSet<int>();
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char c = (Char)GameScr.vCharInMap.elementAt(i);
				if (c != null && c.charID != Char.myCharz().charID && !c.isPet && !c.isMiniPet && c.charID <= 0 && !string.IsNullOrEmpty(c.cName))
				{
					int charID = c.charID;
					string name = c.cName;
					displayedIds.Add(charID);
					bool isAdded = YardatSettings.CustomTargetIds.Contains(charID);

					panelItems.Add(new YardatPanelItem
					{
						Name = $"{name} [{charID}]: {(isAdded ? "Bật" : "Tắt")}",
						Description = isAdded ? "Đang chọn đánh quái này (Nhấn để tắt)" : "Chưa chọn (Nhấn để bật)",
						OnClick = () =>
						{
							if (YardatSettings.CustomTargetIds.Contains(charID))
							{
								YardatSettings.CustomTargetIds.Remove(charID);
								GameScr.info1.addInfo($"Đã tắt {name} [{charID}]", 0);
							}
							else
							{
								YardatSettings.CustomTargetIds.Add(charID);
								GameScr.info1.addInfo($"Đã bật {name} [{charID}]", 0);
							}
							YardatSettings.Save();
						}
					});
				}
			}

			// 5. Các quái đã lưu từ trước nhưng hiện không ở trong map
			foreach (int savedId in YardatSettings.CustomTargetIds.ToList())
			{
				if (!displayedIds.Contains(savedId))
				{
					int id = savedId;
					panelItems.Add(new YardatPanelItem
					{
						Name = $"Quái đã lưu [{id}]: Bật",
						Description = "Quái đã lưu từ trước (không có trong map này, nhấn để xóa)",
						OnClick = () =>
						{
							YardatSettings.CustomTargetIds.Remove(id);
							YardatSettings.Save();
							GameScr.info1.addInfo($"Đã xóa mục tiêu đã lưu [{id}]", 0);
						}
					});
				}
			}
		}

		static void Paint(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(panel, g, panelItems, x => x.Name, x => x.Description);
		}

		static void PaintTabHeader(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Mục tiêu Yardat");
		}

		static void SetTab(Panel panel)
		{
			SetTabPanelTemplates.setTabListTemplate(panel, panelItems);
		}

		static void DoFire(Panel panel)
		{
			InfoDlg.hide();
			if (panel.selected >= 0 && panel.selected < panelItems.Count)
			{
				panelItems[panel.selected].OnClick?.Invoke();
				LoadItems();
				SetTab(panel);
			}
		}
	}
}
