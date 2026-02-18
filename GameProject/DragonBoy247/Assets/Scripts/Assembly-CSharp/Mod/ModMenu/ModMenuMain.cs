using System.Linq;
using Mod.AccountManager;
using Mod.Auto;
using Mod.CharEffect;
using Mod.CustomPanel;
using Mod.DeveloperFunctions;
using Mod.Graphics;
using Mod.PickMob;
using Mod.R;
using Mod.TeleportMenu;
using Mod.Xmap;
using UnityEngine;

namespace Mod.ModMenu
{
	internal static class ModMenuMain
	{
		class ModMenuMainActionListener : IActionListener
		{
			public void perform(int idAction, object p)
			{
				if (idAction == 1)
					ShowPanel();
			}
		}

		internal static Panel currentPanel
		{
			get => GameCanvas.panel2;
			set => GameCanvas.panel2 = value;
		}

		internal static ModMenuItemBoolean[] modMenuItemBools;
		internal static ModMenuItemValues[] modMenuItemValues;
		internal static ModMenuItemFunction[] modMenuItemFunctions;
		internal static ModMenuItemFunction[] modMenuItemDeveloperFunctions;

		internal static Texture2D imgMenu;

		static ModMenuMainActionListener actionListener = new ModMenuMainActionListener();
		static sbyte lastLanguage = -1;
		static GUIStyle style = new GUIStyle
		{
			font = Resources.Load<Font>($"FontSys/x{mGraphics.zoomLevel}/chelthm")
		};

		internal static Command cmdOpenModMenu;

		internal static void Initialize()
		{
			if (cmdOpenModMenu != null)
				return;
			cmdOpenModMenu = new Command("", actionListener, 1, null);
			cmdOpenModMenu.img = new Image();
			imgMenu = CustomGraphics.FlipTextureHorizontally(GameScr.imgMenu.texture);
			cmdOpenModMenu.img.texture = imgMenu;
			cmdOpenModMenu.img.w = cmdOpenModMenu.img.texture.width;
			cmdOpenModMenu.img.h = cmdOpenModMenu.img.texture.height;
			cmdOpenModMenu.isPlaySoundButton = false;
			cmdOpenModMenu.w = cmdOpenModMenu.img.w / mGraphics.zoomLevel;
			cmdOpenModMenu.h = cmdOpenModMenu.img.h / mGraphics.zoomLevel;
			UpdatePosition();
			LoadModMenuItems();
			LoadData();
		}

		internal static void UpdatePosition()
		{
			if (cmdOpenModMenu == null)
				return;
			cmdOpenModMenu.x = GameCanvas.w - cmdOpenModMenu.w;
			//cmdOpenModMenu.y = GameCanvas.h / 2 - cmdOpenModMenu.h / 2;
			cmdOpenModMenu.y = (int)(mGraphics.getImageHeight(GameScr.imgChat) * 1.5f);
			if (currentPanel != null && currentPanel == GameCanvas.panel2 && currentPanel.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
			{
				currentPanel.cmdClose.x = GameCanvas.w - currentPanel.cmdClose.img.getWidth() - 1;
				currentPanel.cmdClose.y = 1;
			}
		}

		internal static void UpdateLanguage(sbyte newLanguage)
		{
			if (newLanguage == lastLanguage)
				return;
			lastLanguage = newLanguage;
			LoadModMenuItems();
		}

		static void LoadModMenuItems()
		{
			modMenuItemBools = new[]
			{
				// Main switches
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "VSync_Toggle",
					Title = "VSync",
					Description = Strings.vSyncDescription,
					GetValueFunc = () => QualitySettings.vSyncCount == 1,
					SetValueAction = value => QualitySettings.vSyncCount = value ? 1 : 0,
					RMSName = "enable_vsync"
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "AutoTrainCSKB_Toggle",
					Title = "Auto Train CSKB",
					Description = "Auto Train CSKB Description",
					GetValueFunc = () => UpCSKBController.gI.IsActing,
					SetValueAction = UpCSKBController.toggle
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "PickMob_AutoPickItem_Toggle",
					Title = Strings.autoPickItemTitle,
					Description = Strings.autoPickItemDescription,
					GetValueFunc = () => Pk9rPickMob.IsAutoPickItems,
					SetValueAction = Pk9rPickMob.SetAutoPickItems,
					RMSName = "pickmob_auto_pick",
					GetIsDisabled = () => UpCSKBController.gI.IsActing,
					GetDisabledReason = () => string.Format(Strings.functionShouldBeDisabled, Strings.autoTrainForNewbieTitle)
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "AutoSendAttack_Toggle",
					Title = Strings.autoAttack,
					Description = Strings.autoSendAttackDescription,
					GetValueFunc = () => AutoSendAttack.gI.IsActing,
					SetValueAction = AutoSendAttack.toggle,
					GetIsDisabled = () => Pk9rPickMob.IsTanSat,
					GetDisabledReason = () => string.Format(Strings.functionShouldBeDisabled, Strings.pickMobTitle) + '!'
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "AutoLogin_Toggle",
					Title = Strings.autoLoginTitle,
					Description = Strings.autoLoginDescription,
					GetValueFunc = () => AutoLogin.isEnabled,
					SetValueAction = AutoLogin.SetState
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "ShowTargetInfo_Toggle",
					Title = Strings.showTargetInfoTitle,
					Description = Strings.showTargetInfoDescription,
					GetValueFunc = () => CharEffectMain.isEnabled,
					SetValueAction = CharEffectMain.setState,
					RMSName = "show_target_info"
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "SkipSpaceship_Toggle",
					Title = Strings.skipSpaceshipTitle,
					Description = Strings.skipSpaceshipDescription,
					GetValueFunc = () => SpaceshipSkip.isEnabled,
					SetValueAction = value => SpaceshipSkip.isEnabled = value,
					RMSName = "skip_spaceship"
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "AutoAskForPeans_Toggle",
					Title = Strings.autoAskForPeansTitle,
					Description = Strings.autoAskForPeansDescription,
					GetValueFunc = () => AutoPean.isAutoRequest,
					SetValueAction = value => AutoPean.isAutoRequest = value,
					RMSName = "auto_ask_for_peans",
					GetIsDisabled = () => Char.myCharz().clan == null,
					GetDisabledReason = () => Strings.youAreNotInAClan + '!'
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "AutoDonatePeans_Toggle",
					Title = Strings.autoDonatePeansTitle,
					Description = Strings.autoDonatePeansDescription,
					GetValueFunc = () => AutoPean.isAutoDonate,
					SetValueAction = value => AutoPean.isAutoDonate = value,
					RMSName = "auto_donate_peans",
					GetIsDisabled = () => Char.myCharz().clan == null,
					GetDisabledReason = () => Strings.youAreNotInAClan + '!'
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "AutoHarvestPeans_Toggle",
					Title = Strings.autoHarvestPeansTitle,
					Description = Strings.autoHarvestPeansDescription,
					GetValueFunc = () => AutoPean.isAutoHarvest,
					SetValueAction = value => AutoPean.isAutoHarvest = value,
					RMSName = "auto_harvest_peans"
				}),
				// Auxiliary switches
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "Xmap_UseNormalCapsule_Toggle",
					Title = Strings.xmapUseNormalCapsule,
					Description = Strings.xmapUseNormalCapsuleDescription,
					GetValueFunc = () => Pk9rXmap.isUseCapsuleNormal,
					SetValueAction = value => Pk9rXmap.isUseCapsuleNormal = value,
					RMSName = "xmap_use_normal_capsule"
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "PickMob_AvoidSuperMob_Toggle",
					Title = Strings.pickMobAvoidSuperMobTitle,
					Description = Strings.avoidSuperMobDescription,
					GetValueFunc = () => Pk9rPickMob.IsNeSieuQuai,
					SetValueAction = Pk9rPickMob.SetAvoidSuperMonster,
					RMSName = "pickmob_avoid_super_mob"
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "PickMob_PickMyItemOnly_Toggle",
					Title = Strings.pickMobPickMyItemOnlyTitle,
					Description = Strings.pickMobPickMyItemOnlyDescription,
					GetValueFunc = () => Pk9rPickMob.IsItemMe,
					SetValueAction = Pk9rPickMob.SetAutoPickItemsFromOthers,
					RMSName = "pickmob_pick_my_item_only"
				}),
				new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "PickMob_LimitPickTimes_Toggle",
					Title = Strings.pickMobLimitPickTimesTitle,
					Description = Strings.pickMobLimitPickTimesDescription,
					GetValueFunc = () => Pk9rPickMob.IsLimitTimesPickItem,
					SetValueAction = Pk9rPickMob.SetPickUpLimited,
					RMSName = "pickmob_limit_pick_item_times"
				}),
			};
			modMenuItemValues = new ModMenuItemValues[]
			{
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_FPS",
					Title = "FPS",
					Description = Strings.setFPSDescription,
					GetValueFunc = () => Application.targetFrameRate,
					SetValueAction = value =>
					{
						if (value >= 5 && value <= Screen.currentResolution.refreshRateRatio.value)
							Application.targetFrameRate = (int)value;
					},
					MinValue = 5,
					MaxValue = Screen.currentResolution.refreshRateRatio.value,
					RMSName = "target_fps",
					GetIsDisabled = () => QualitySettings.vSyncCount == 1,
					GetDisabledReason = () => string.Format(Strings.functionShouldBeDisabled, "VSync"),
					TextFieldTitle = Strings.inputFPS,
					TextFieldHint = "FPS"
				}),
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_GameSpeed",
					Title = Strings.setGameSpeedTitle,
					Description = Strings.setGameSpeedDescription,
					IsFloatingPoint = true,
					GetValueFunc = () => Time.timeScale,
					SetValueAction = value =>
					{
						if (value >= .25d && value <= 20)
							Time.timeScale = (float)value;
					},
					MinValue = 0.25d,
					MaxValue = 20,
					RMSName = "game_speed",
					TextFieldTitle = Strings.inputGameSpeed,
					TextFieldHint = Strings.inputGameSpeedHint
				}),
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_GameDelay",
					Title = Strings.setGameDelayTitle,
					Description = Strings.setGameDelayDescription,
					IsFloatingPoint = true,
					GetValueFunc = () => System.Math.Round(Time.fixedDeltaTime * 100f, 3),
					SetValueAction = value =>
					{
						if (value >= 1 && value <= 5)
							Time.fixedDeltaTime = (float)value / 100f;
					},
					MinValue = 1,
					MaxValue = 5,
					RMSName = "game_delay",
					TextFieldTitle = Strings.inputGameDelay,
					TextFieldHint = Strings.inputGameDelayHint
				}),
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_ReduceGraphics",
					Title = Strings.setReduceGraphicsTitle,
					Values = Strings.setReduceGraphicsChoices,
					GetValueFunc = () => (int)GraphicsReducer.Level,
					SetValueAction = level => GraphicsReducer.Level = (ReduceGraphicsLevel)level,
					RMSName = "reduce_graphics"
				}),
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_GoBack",
					Title = "GoBack",
					Values = Strings.setGoBackChoices,
					GetValueFunc = () => (int)AutoGoback.mode,
					SetValueAction = value => AutoGoback.setState((int)value),
					GetIsDisabled = () => UpCSKBController.gI.IsActing,
					GetDisabledReason = () => string.Format(Strings.functionShouldBeDisabled, Strings.autoTrainForNewbieTitle)
				}),
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_AutoTrainPet",
					Title = Strings.setAutoTrainPetTitle,
					Values = Strings.setAutoTrainPetChoices,
					GetValueFunc = () => (int)AutoTrainPet.Mode,
					SetValueAction = value => AutoTrainPet.SetState((int)value),
					GetIsDisabled = () => !Char.myCharz().havePet || UpCSKBController.gI.IsActing,
					GetDisabledReason = () =>
					{
						if (!Char.myCharz().havePet)
							return Strings.youDontHaveDisciple + '!';
						return string.Empty;
					}
				}),
				new ModMenuItemValues(new ModMenuItemValuesConfig
				{
					ID = "Set_AutoAttackWhenDiscipleNeed",
					Title = Strings.setAutoAttackWhenDiscipleNeededTitle,
					Values = Strings.setAutoAttackWhenDiscipleNeededChoices,
					GetValueFunc = () => (int)AutoTrainPet.ModeAttackWhenNeeded,
					SetValueAction = value => AutoTrainPet.SetAttackState((int)value),
					RMSName = "auto_pet_mode",
					GetIsDisabled = () => AutoTrainPet.Mode <= AutoTrainPetMode.Disabled,
					GetDisabledReason = () => string.Format(Strings.functionShouldBeEnabled, Strings.setAutoTrainPetTitle)
				})
			};
			modMenuItemFunctions = new ModMenuItemFunction[]
			{
				new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "OpenXmapMenu",
					Title = Strings.openXmapMenuTitle,
					Description = Strings.openXmapMenuDescription,
					Action = Pk9rXmap.ShowXmapMenu
				}),
				new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "OpenPickMobMenu",
					Title = Strings.openPickMobMenuTitle,
					Description = Strings.openPickMobMenuDescription,
					Action = Pk9rPickMob.ShowMenu
				}),
				new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "OpenTeleportMenu",
					Title = Strings.openTeleportMenuTitle,
					Description = Strings.openTeleportMenuDescription,
					Action = TeleportMenuMain.ShowMenu
				}),
				new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "AddUserAoToAccountManager",
					Title = Strings.addUserAoToAccountManagerTitle,
					Description = Strings.addUserAoToAccountManagerDescription,
					Action = InGameAccountManager.AddUserAoToAccountManager,
					GetIsDisabled = () =>
					{
						if (Utils.IsOpenedByExternalAccountManager)
							return true;
						if (InGameAccountManager.SelectedAccount != null)
							return true;
						return string.IsNullOrEmpty(Rms.loadRMSString("userAo" + ServerListScreen.ipSelect));
					},
					GetDisabledReason = () =>
					{
						if (Utils.IsOpenedByExternalAccountManager)
							return Strings.openedByExternalAccountManager + '!';
						if (InGameAccountManager.SelectedAccount != null)
							return Strings.inGameAccountManagerUnregisteredAccountAlreadyAdded + '!';
						return Strings.accountAlreadyRegistered + '!';
					}
				})
				//new ModMenuItemFunction("Menu AutoItem", "Mở menu AutoItem (lệnh \"item\" hoặc bấm nút I)", AutoItem.ShowMenu),
				//new ModMenuItemFunction("Menu Custom Logo", "Mở menu logo tùy chỉnh", CustomLogo.ShowMenu),
				//new ModMenuItemFunction("Menu Custom Cursor", "Mở menu con trỏ tùy chỉnh", CustomCursor.ShowMenu),
			};
			modMenuItemDeveloperFunctions = new ModMenuItemFunction[]
			{
				new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "GetGameData",
					Title = "Extract data from the game",
					Description = "Extract information from the game (maps, monsters, NPCs, items, etc.) as JSON data",
					Action = GameData.ShowMenu
				})
			};
		}

		internal static void ShowPanel()
		{
			if (currentPanel == null)
				currentPanel = new Panel();
			CustomPanelMenu.Show(new CustomPanelMenuConfig
			{
				SetTabAction = SetTabModMenu,
				DoFireItemAction = DoFireModMenu,
				PaintAction = PaintModMenu
			}, currentPanel);
			currentPanel.cmdClose.x = GameCanvas.w - currentPanel.cmdClose.img.getWidth() - 1;
			currentPanel.cmdClose.y = 1;
		}

		internal static void Paint(mGraphics g)
		{
			if (Char.isLoadingMap)
				return;
			if (ChatTextField.gI().isShow)
				return;
			if (GameCanvas.menu.showMenu)
				return;
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
				return;
			cmdOpenModMenu?.paint(g);
			if (cmdOpenModMenu != null && (GameCanvas.isMouseFocus(cmdOpenModMenu.x, cmdOpenModMenu.y, cmdOpenModMenu.w, cmdOpenModMenu.h) || GameCanvas.isMouseFocus((int)(cmdOpenModMenu.x - cmdOpenModMenu.w * 1.5), cmdOpenModMenu.y, (int)(cmdOpenModMenu.w * 2.5), cmdOpenModMenu.h) && GameCanvas.isPointerDown))
				g.drawImage(ItemMap.imageFlare, cmdOpenModMenu.x + 4, cmdOpenModMenu.y + 15, mGraphics.VCENTER | mGraphics.HCENTER);
		}

		internal static void UpdateTouch()
		{
			if (cmdOpenModMenu == null)
				return;
			if (Char.isLoadingMap)
				return;
			if (ChatTextField.gI().isShow)
				return;
			if (GameCanvas.menu.showMenu)
				return;
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				return;
			}
			if (GameCanvas.isPointerHoldIn((int)(cmdOpenModMenu.x - cmdOpenModMenu.w * 1.5), cmdOpenModMenu.y, (int)(cmdOpenModMenu.w * 2.5), cmdOpenModMenu.h) && GameCanvas.isPointerClick)
			{
				GameCanvas.isPointerJustDown = false;
				GameScr.gI().isPointerDowning = false;
				cmdOpenModMenu.performAction();
				Char.myCharz().currentMovePoint = null;
				GameCanvas.clearAllPointerEvent();
			}
		}

		internal static void SetTabModMenu(Panel panel)
		{
			SetTabPanelTemplates.setTabListTemplate(panel, modMenuItemBools, modMenuItemValues, modMenuItemFunctions, modMenuItemDeveloperFunctions);
		}

		internal static void DoFireModMenu(Panel panel)
		{
			if (panel.currentTabIndex == 0)
				DoFireModMenuBool(panel);
			else if (panel.currentTabIndex == 1)
				DoFireModMenuValues(panel);
			else if (panel.currentTabIndex == 2)
				DoFireModMenuFunctions(panel);
			else if (panel.currentTabIndex == 3)
				DoFireModMenuDeveloperFunctions(panel);
			NotifySelectDisabledItem(panel);
		}

		static void DoFireModMenuFunctions(Panel panel)
		{
			if (modMenuItemFunctions[panel.selected].IsDisabled)
				return;
			panel.hide();
			modMenuItemFunctions[panel.selected].Action?.Invoke();
		}

		static void DoFireModMenuDeveloperFunctions(Panel panel)
		{
			if (panel.selected < 0)
				return;
			if (modMenuItemDeveloperFunctions[panel.selected].IsDisabled)
				return;
			panel.hide();
			modMenuItemDeveloperFunctions[panel.selected].Action?.Invoke();
		}

		static void DoFireModMenuBool(Panel panel)
		{
			if (panel.selected < 0)
				return;
			if (modMenuItemBools[panel.selected].IsDisabled)
				return;
			modMenuItemBools[panel.selected].SwitchSelection();
			GameScr.info1.addInfo(modMenuItemBools[panel.selected].Title + ": " + Strings.OnOffStatus(modMenuItemBools[panel.selected].Value), 0);
		}

		static void DoFireModMenuValues(Panel panel)
		{
			if (panel.selected < 0)
				return;
			int selected = panel.selected;
			if (modMenuItemValues[selected].IsDisabled)
				return;
			if (modMenuItemValues[selected].Values != null)
				modMenuItemValues[selected].SwitchSelection();
			else
				modMenuItemValues[selected].StartChat(currentPanel.chatTField = new ChatTextField());
		}

		static void NotifySelectDisabledItem(Panel panel)
		{
			int selected = panel.selected;
			if (selected == -1)
				return;
			if (panel.currentTabIndex == 0)
			{
				if (!modMenuItemBools[selected].IsDisabled)
					return;
				GameScr.info1.addInfo(modMenuItemBools[selected].DisabledReason, 0);
			}
			else if (panel.currentTabIndex == 1)
			{
				if (!modMenuItemValues[selected].IsDisabled)
					return;
				GameScr.info1.addInfo(modMenuItemValues[selected].DisabledReason, 0);
			}
			else if (panel.currentTabIndex == 2)
			{
				if (!modMenuItemFunctions[selected].IsDisabled)
					return;
				GameScr.info1.addInfo(modMenuItemFunctions[selected].DisabledReason, 0);
			}
			else if (panel.currentTabIndex == 3)
			{
				if (!modMenuItemDeveloperFunctions[selected].IsDisabled)
					return;
				GameScr.info1.addInfo(modMenuItemDeveloperFunctions[selected].DisabledReason, 0);
			}
		}

		internal static void PaintModMenu(Panel panel, mGraphics g)
		{
			g.setClip(panel.xScroll, panel.yScroll, panel.wScroll, panel.hScroll);
			g.translate(0, -panel.cmy);
			if (panel.currentTabIndex == 0)
				PaintModMenuBool(panel, g);
			else if (panel.currentTabIndex == 1)
				PaintModMenuValues(panel, g);
			else if (panel.currentTabIndex == 2)
				PaintModMenuFunctions(panel, g);
			else if (panel.currentTabIndex == 3)
				PaintModMenuDeveloperFunctions(panel, g);
		}

		static void PaintModMenuBool(Panel panel, mGraphics g)
		{
			if (modMenuItemBools == null || modMenuItemBools.Length != panel.currentListLength)
				return;
			int offset = System.Math.Max(panel.cmy / panel.ITEM_HEIGHT, 0);
			bool isReset = true;
			string descriptionTextInfo = string.Empty;
			int x = 0, y = 0;
			for (int i = offset; i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, panel.currentListLength); i++)
			{
				int xScroll = panel.xScroll;
				int yScroll = panel.yScroll + i * panel.ITEM_HEIGHT;
				int wScroll = panel.wScroll;
				int itemHeight = panel.ITEM_HEIGHT - 1;
				ModMenuItemBoolean modMenuItem = modMenuItemBools[i];
				if (!modMenuItem.IsDisabled)
					g.setColor(i != panel.selected ? 0xE7DFD2 : 0xF9FF4A);
				else
					g.setColor(i != panel.selected ? 0xb7afa2 : 0xd0d73b);
				g.fillRect(xScroll, yScroll, wScroll, itemHeight);
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + modMenuItem.Title, xScroll + 7, yScroll, 0);
				if (i == panel.selected && mFont.tahoma_7_blue.getWidth(modMenuItem.Description) > panel.wScroll - 20 && !panel.isClose)
				{
					isReset = false;
					descriptionTextInfo = modMenuItem.Description;
					x = xScroll + 7;
					y = yScroll + 11;
				}
				else
					mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(modMenuItem.Description, style, panel.wScroll - 5), xScroll + 7, yScroll + 11, 0);
				if (modMenuItem.Value)
					g.setColor(0x00b000);
				else
					g.setColor(0xe00000);
				g.fillRect(xScroll, yScroll, 2, itemHeight);
			}
			if (isReset)
				TextInfo.reset();
			else
			{
				TextInfo.paint(g, descriptionTextInfo, x, y, panel.wScroll - 5, 15, mFont.tahoma_7_blue);
				g.setClip(panel.xScroll, panel.yScroll, panel.wScroll, panel.hScroll);
				g.translate(0, -panel.cmy);
			}
			panel.paintScrollArrow(g);
		}

		static void PaintModMenuValues(Panel panel, mGraphics g)
		{
			if (modMenuItemValues == null || modMenuItemValues.Length != panel.currentListLength)
				return;
			int offset = System.Math.Max(panel.cmy / panel.ITEM_HEIGHT, 0);
			bool isReset = true;
			string descriptionTextInfo = string.Empty;
			int x = 0, y = 0;
			double currSelectedValue = 0;
			for (int i = offset; i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, panel.currentListLength); i++)
			{
				int num = panel.xScroll;
				int num2 = panel.yScroll + i * panel.ITEM_HEIGHT;
				int num3 = panel.wScroll;
				int num4 = panel.ITEM_HEIGHT - 1;
				ModMenuItemValues modMenuItem = modMenuItemValues[i];
				if (!modMenuItem.IsDisabled)
					g.setColor(i != panel.selected ? 0xE7DFD2 : 0xF9FF4A);
				else
					g.setColor(i != panel.selected ? 0xb7afa2 : 0xd0d73b);
				g.fillRect(num, num2, num3, num4);
				string str;
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + modMenuItem.Title, num + 5, num2, 0);
				int descWidth;
				if (modMenuItem.Values != null)
				{
					str = modMenuItem.getSelectedValue();
					descWidth = panel.wScroll - 5;
				}
				else
				{
					str = modMenuItem.Description;
					descWidth = panel.wScroll - 5 - mFont.tahoma_7_blue.getWidth(modMenuItem.SelectedValue.ToString());
					mFont.tahoma_7b_red.drawString(g, modMenuItem.SelectedValue.ToString(), num + num3 - 2, num2 + panel.ITEM_HEIGHT - 14, mFont.RIGHT);
				}
				if (i == panel.selected && mFont.tahoma_7_blue.getWidth(str) > descWidth && !panel.isClose)
				{
					isReset = false;
					descriptionTextInfo = str;
					currSelectedValue = modMenuItem.SelectedValue;
					x = num + 5;
					y = num2 + 11;
				}
				else
					mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(str, style, descWidth), num + 5, num2 + 11, 0);
			}
			if (isReset)
				TextInfo.reset();
			else
			{
				TextInfo.paint(g, descriptionTextInfo, x, y, panel.wScroll - 10 - mFont.tahoma_7b_red.getWidth(currSelectedValue.ToString()), 15, mFont.tahoma_7_blue);
				g.setClip(panel.xScroll, panel.yScroll, panel.wScroll, panel.hScroll);
				g.translate(0, -panel.cmy);
			}
			panel.paintScrollArrow(g);
		}

		static void PaintModMenuFunctions(Panel panel, mGraphics g)
		{
			if (modMenuItemFunctions == null || modMenuItemFunctions.Length != panel.currentListLength)
				return;
			int offset = System.Math.Max(panel.cmy / panel.ITEM_HEIGHT, 0);
			bool isReset = true;
			string descriptionTextInfo = string.Empty;
			int x = 0, y = 0;
			for (int i = offset; i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, panel.currentListLength); i++)
			{
				int num = panel.xScroll;
				int num2 = panel.yScroll + i * panel.ITEM_HEIGHT;
				int num3 = panel.wScroll;
				int num4 = panel.ITEM_HEIGHT - 1;
				ModMenuItemFunction modMenuItem = modMenuItemFunctions[i];
				if (!modMenuItem.IsDisabled)
					g.setColor(i != panel.selected ? 0xE7DFD2 : 0xF9FF4A);
				else
					g.setColor(i != panel.selected ? 0xb7afa2 : 0xd0d73b);
				g.fillRect(num, num2, num3, num4);
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + modMenuItem.Title, num + 5, num2, 0);
				if (i == panel.selected && mFont.tahoma_7_blue.getWidth(modMenuItem.Description) > panel.wScroll - 5 && !panel.isClose)
				{
					isReset = false;
					descriptionTextInfo = modMenuItem.Description;
					x = num + 5;
					y = num2 + 11;
				}
				else
					mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(modMenuItem.Description, style, panel.wScroll - 5), num + 5, num2 + 11, 0);
			}
			if (isReset)
				TextInfo.reset();
			else
			{
				TextInfo.paint(g, descriptionTextInfo, x, y, panel.wScroll - 5, 15, mFont.tahoma_7_blue);
				g.setClip(panel.xScroll, panel.yScroll, panel.wScroll, panel.hScroll);
				g.translate(0, -panel.cmy);
			}
			panel.paintScrollArrow(g);
		}

		static void PaintModMenuDeveloperFunctions(Panel panel, mGraphics g)
		{
			if (modMenuItemDeveloperFunctions == null || modMenuItemDeveloperFunctions.Length != panel.currentListLength)
				return;
			int offset = System.Math.Max(panel.cmy / panel.ITEM_HEIGHT, 0);
			bool isReset = true;
			string descriptionTextInfo = string.Empty;
			int x = 0, y = 0;
			for (int i = offset; i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, panel.currentListLength); i++)
			{
				int num = panel.xScroll;
				int num2 = panel.yScroll + i * panel.ITEM_HEIGHT;
				int num3 = panel.wScroll;
				int num4 = panel.ITEM_HEIGHT - 1;
				ModMenuItemFunction modMenuItem = modMenuItemDeveloperFunctions[i];
				if (!modMenuItem.IsDisabled)
					g.setColor(i != panel.selected ? 0xE7DFD2 : 0xF9FF4A);
				else
					g.setColor(i != panel.selected ? 0xb7afa2 : 0xd0d73b);
				g.fillRect(num, num2, num3, num4);
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + modMenuItem.Title, num + 5, num2, 0);
				if (i == panel.selected && mFont.tahoma_7_blue.getWidth(modMenuItem.Description) > panel.wScroll - 5 && !panel.isClose)
				{
					isReset = false;
					descriptionTextInfo = modMenuItem.Description;
					x = num + 5;
					y = num2 + 11;
				}
				else
					mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(modMenuItem.Description, style, panel.wScroll - 5), num + 5, num2 + 11, 0);
			}
			if (isReset)
				TextInfo.reset();
			else
			{
				TextInfo.paint(g, descriptionTextInfo, x, y, panel.wScroll - 5, 15, mFont.tahoma_7_blue);
				g.setClip(panel.xScroll, panel.yScroll, panel.wScroll, panel.hScroll);
				g.translate(0, -panel.cmy);
			}
			panel.paintScrollArrow(g);
		}

		internal static void SaveData()
		{
			foreach (ModMenuItemBoolean modMenuItem in modMenuItemBools)
				if (!string.IsNullOrEmpty(modMenuItem.RMSName))
					Utils.SaveData(modMenuItem.RMSName, modMenuItem.Value);
			foreach (ModMenuItemValues modMenuItem in modMenuItemValues)
			{
				if (string.IsNullOrEmpty(modMenuItem.RMSName))
					continue;
				if (modMenuItem.IsFloatingPoint)
					Utils.SaveData(modMenuItem.RMSName, modMenuItem.SelectedValue);
				else
					Utils.SaveData(modMenuItem.RMSName, (long)modMenuItem.SelectedValue);
			}
		}

		internal static void LoadData()
		{
			foreach (ModMenuItemBoolean modMenuItem in modMenuItemBools)
			{
				if (!string.IsNullOrEmpty(modMenuItem.RMSName) && Utils.TryLoadDataBool(modMenuItem.RMSName, out bool value))
					modMenuItem.Value = value;
			}
			foreach (ModMenuItemValues modMenuItem in modMenuItemValues)
			{
				if (string.IsNullOrEmpty(modMenuItem.RMSName))
					continue;
				if (modMenuItem.IsFloatingPoint && Utils.TryLoadDataDouble(modMenuItem.RMSName, out double data))
					modMenuItem.SelectedValue = data;
				else if (Utils.TryLoadDataLong(modMenuItem.RMSName, out long data2))
					modMenuItem.SelectedValue = data2;
			}
		}

		internal static ModMenuItem GetModMenuItem(string id)
		{
			return modMenuItemBools.Concat<ModMenuItem>(modMenuItemValues).Concat(modMenuItemValues).Concat(modMenuItemFunctions).Concat(modMenuItemDeveloperFunctions).FirstOrDefault(item => item.ID == id);
		}

		internal static T GetModMenuItem<T>(string id) where T : ModMenuItem
		{
			if (typeof(T) == typeof(ModMenuItemBoolean))
				return modMenuItemBools.FirstOrDefault(item => item.ID == id) as T;
			if (typeof(T) == typeof(ModMenuItemValues))
				return modMenuItemValues.FirstOrDefault(item => item.ID == id) as T;
			if (typeof(T) == typeof(ModMenuItemFunction))
				return modMenuItemFunctions.FirstOrDefault(item => item.ID == id) as T;
			if (typeof(T) == typeof(ModMenuItemFunction))
				return modMenuItemDeveloperFunctions.FirstOrDefault(item => item.ID == id) as T;
			return null;
		}
	}
}