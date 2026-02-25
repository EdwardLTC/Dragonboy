using System;
using System.IO;
using System.Linq;
using System.Threading;
using Mod.AccountManager;
using Mod.Auto;
using Mod.Auto.AutoChat;
using Mod.CharEffect;
using Mod.CustomPanel;
using Mod.Graphics;
using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.ModMenu;
using Mod.PickMob;
using Mod.R;
using Mod.TeleportMenu;
using Mod.Xmap;
using UnityEngine;
using CharacterInfo = Mod.AccountManager.CharacterInfo;

namespace Mod
{
	internal static class GameEvents
	{
		static float _previousWidth = Screen.width;
		static float _previousHeight = Screen.height;
		static bool isHaveSelectSkill_old;
		static long lastTimeGamePause;
		static long lastTimeRequestPetInfo;
		static long delayRequestPetInfo = 1000;
		static long lastTimeRequestZoneInfo;
		static long delayRequestZoneInfo = 100;
		static bool isFirstPause = true;
		static bool isOpenZoneUI;
		static GUIStyle style;
		static string nameCustomServer = "";
		static string currentHost = "";
		static ushort currentPort;

		internal static void OnAwake()
		{
			Application.targetFrameRate = 60;
			Application.runInBackground = true;
			Time.timeScale = 2f;
		}

		internal static void OnGameStart()
		{
			if (Utils.IsAndroidBuild())
			{
				Screen.sleepTimeout = SleepTimeout.NeverSleep;
				Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
				Screen.orientation = ScreenOrientation.AutoRotation;
				Screen.autorotateToLandscapeLeft = true;
				Screen.autorotateToLandscapeRight = true;
				Screen.autorotateToPortrait = false;
				Screen.autorotateToPortraitUpsideDown = false;
			}
			OnSetResolution();
			OnCheckZoomLevel(Screen.width, Screen.height);
			if (!Directory.Exists(Utils.dataPath))
			{
				Directory.CreateDirectory(Utils.dataPath);
			}
			CharEffectMain.Init();
			Setup.loadFile();
			ChatCommandHandler.loadDefault();
			HotkeyCommandHandler.loadDefault();
			GraphicsReducer.InitializeTileMap(true);
			SpaceshipSkip.isEnabled = true;
			InGameAccountManager.OnStart();
		}

		internal static void OnMainStart()
		{
			UIImage.OnStart();
			if (Rms.loadRMSInt("svselect") == -1)
			{
				ServerListScreen.linkDefault = Strings.DEFAULT_IP_SERVERS;
				ServerListScreen.getServerList(Strings.DEFAULT_IP_SERVERS);
			}
		}

		internal static void OnGamePause(bool paused)
		{
			if (mSystem.currentTimeMillis() - lastTimeGamePause > 1000 && !isFirstPause)
			{
				ModMenuMain.SaveData();
				if (!Utils.IsOpenedByExternalAccountManager)
				{
					InGameAccountManager.OnCloseAndPause();
				}
			}

			lastTimeGamePause = mSystem.currentTimeMillis();
			if (isFirstPause)
			{
				isFirstPause = false;
			}
		}

		internal static void OnGameClosing()
		{
			ModMenuMain.SaveData();
			Setup.clearStringTrash();
			TeleportMenuMain.SaveData();
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				InGameAccountManager.OnCloseAndPause();
			}
		}

		internal static void OnFixedUpdateMain()
		{
			if (GameCanvas.currentScreen != null)
			{
				if (!GameCanvas.panel.isShow && GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
				{
					GameCanvas.isFocusPanel2 = true;
					GameCanvas.panel2?.update();
					if (GameCanvas.panel2?.chatTField != null && GameCanvas.panel2.chatTField.isShow)
						GameCanvas.panel2?.chatTFUpdateKey();
					else
						GameCanvas.panel2?.updateKey();
				}

				if (!GameCanvas.panel.isShow && GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
				{
					int panel2DrawX = GameCanvas.panel2.X + GameCanvas.panel2.X - GameCanvas.panel2.cmx;
					if (!GameCanvas.isPointer(panel2DrawX, GameCanvas.panel2.Y, GameCanvas.panel2.W, GameCanvas.panel2.H) &&
					    GameCanvas.isPointerJustRelease && GameCanvas.panel2.isDoneCombine &&
					    !GameCanvas.panel2.pointerIsDowning)
					{
						GameCanvas.panel2.hide();
						GameCanvas.clearAllPointerEvent();
					}
				}
			}
		}

		internal static void OnUpdateMain()
		{
			if (!Main.started)
				return;
			if (!_previousWidth.Equals(Screen.width) || !_previousHeight.Equals(Screen.height))
			{
				_previousWidth = Screen.width;
				_previousHeight = Screen.height;
				ScaleGUI.initScaleGUI();
				GameCanvas.instance?.ResetSize();
				Utils.ResetTextField(ChatTextField.gI());
				GameScr.gamePad?.SetGamePadZone();
				GameScr.loadCamera(false, -1, -1);
				if (GameCanvas.panel2 != null)
				{
					GameCanvas.panel2.EmulateSetTypePanel(1);
				}
				ModMenuMain.UpdatePosition();
				InGameAccountManager.UpdateSizeAndPos();
			}
			AutoLogin.Update();
		}

		internal static void OnSaveRMSString(ref string filename, ref string data)
		{
			if (filename.StartsWith("userAo") && string.IsNullOrEmpty(data))
				filename = "";
		}

		internal static void OnLoadLanguage(sbyte newLanguage)
		{
			Strings.LoadLanguage(newLanguage);
			ModMenuMain.UpdateLanguage(newLanguage);
		}

		internal static void OnSetResolution()
		{
			if (Utils.IsAndroidBuild())
				return;
			if (Utils.sizeData != null)
			{
				int width = (int)Utils.sizeData["width"];
				int height = (int)Utils.sizeData["height"];
				bool fullScreen = (bool)Utils.sizeData["fullScreen"];
				if (Screen.width != width || Screen.height != height)
					Screen.SetResolution(width, height, fullScreen);
				new Thread(() =>
				{
					while (Screen.fullScreen != fullScreen)
					{
						Screen.fullScreen = fullScreen;
						Thread.Sleep(100);
					}
				}).Start();
			}
		}

		internal static void OnGameScrPressHotkeysUnassigned()
		{
			HotkeyCommandHandler.handleHotkey(GameCanvas.keyAsciiPress);
		}

		internal static void OnGameScrPressHotkeys()
		{

		}

		internal static bool OnSendChat(string text)
		{
			return ChatCommandHandler.handleChatText(text);
		}

		internal static void OnPaintChatTextField(ChatTextField instance, mGraphics g)
		{

		}

		internal static bool OnStartChatTextField(ChatTextField sender, IChatable parentScreen)
		{
			sender.parentScreen = parentScreen;
			if (sender.strChat.Replace(" ", "") != "Chat" || sender.tfChat.name != "chat")
				return false;
			//if (sender == ChatTextField.gI())
			//HistoryChat.gI.show();
			return false;
		}

		internal static bool OnGetRMSPath(out string result)
		{
			//result = $"{Application.persistentDataPath}\\{GameMidlet.IP}_{GameMidlet.PORT}_x{mGraphics.zoomLevel}\\";
			string subFolder = "TeaMobi";
			//string subFolder = $"TeaMobi{Path.DirectorySeparatorChar}Vietnam";

			//if (ServerListScreen.address[ServerListScreen.ipSelect] == "dragon.indonaga.com")
			//{
			//    switch (ServerListScreen.language[ServerListScreen.ipSelect])
			//    {
			//        case 1:
			//            subFolder = $"TeaMobi{Path.DirectorySeparatorChar}World";
			//            break;
			//        case 2:
			//            subFolder = $"TeaMobi{Path.DirectorySeparatorChar}Indonaga";
			//            break;
			//    }
			//}

			result = Utils.GetRootDataPath();
			// check ip server lậu, lưu rms riêng
			// ...
			result = Path.Combine(result, subFolder);
			if (!Directory.Exists(result))
				Directory.CreateDirectory(result);
			return true;
		}

		internal static bool OnTeleportUpdate(Teleport teleport)
		{
			if (SpaceshipSkip.isEnabled)
			{
				SpaceshipSkip.Update(teleport);
				return true;
			}

			return false;
		}

		internal static void OnUpdateChatTextField(ChatTextField sender)
		{
			if (!string.IsNullOrEmpty(sender.tfChat.getText()))
				GameCanvas.keyPressed[14] = false;
		}

		internal static bool OnClearAllRMS()
		{
			foreach (FileInfo file in new DirectoryInfo(Rms.GetiPhoneDocumentsPath() + "/").GetFiles()
				         .Where(f => f.Extension != ".log"))
				try
				{
					if (file.Name != "isPlaySound")
						file.Delete();
				}
				catch
				{
				}

			return true;
		}

		internal static void OnUpdateGameScr()
		{
			if (!Utils.IsOpenedByExternalAccountManager && GameCanvas.gameTick % (60 * Time.timeScale) == 0)
			{
				Account account = InGameAccountManager.SelectedAccount;
				if (account != null)
				{
					account.Gold = Char.myCharz().xu;
					account.Gem = Char.myCharz().luong;
					account.Ruby = Char.myCharz().luongKhoa;

					account.Info.Name = Char.myCharz().cName;
					account.Info.CharID = Char.myCharz().charID;
					account.Info.Gender = (sbyte)Char.myCharz().cgender;
					account.Info.EXP = Char.myCharz().cPower;
					account.Info.MaxHP = Char.myCharz().cHPFull;
					account.Info.MaxMP = Char.myCharz().cMPFull;
					account.Info.Icon = Char.myCharz().avatarz();
					if (Char.myCharz().havePet)
					{
						if (account.PetInfo == null)
							account.PetInfo = new CharacterInfo();
						account.PetInfo.Name = Char.myPetz().cName;
						account.PetInfo.CharID = Char.myPetz().charID;
						account.PetInfo.Gender = (sbyte)Utils.GetPetGender();
						account.PetInfo.EXP = Char.myPetz().cPower;
						account.PetInfo.MaxHP = Char.myPetz().cHPFull;
						account.PetInfo.MaxMP = Char.myPetz().cMPFull;
						account.PetInfo.Icon = Char.myPetz().avatarz();
					}
					else
					{
						account.PetInfo = null;
					}
				}
			}

			if (Char.myCharz().havePet && mSystem.currentTimeMillis() - lastTimeRequestPetInfo > delayRequestPetInfo)
			{
				delayRequestPetInfo = Res.random(750, 1000);
				lastTimeRequestPetInfo = mSystem.currentTimeMillis();
				Service.gI().petInfo();
			}

			if (mSystem.currentTimeMillis() - lastTimeRequestZoneInfo > delayRequestZoneInfo)
			{
				delayRequestZoneInfo = Res.random(200, 300);
				lastTimeRequestZoneInfo = mSystem.currentTimeMillis();
				Service.gI().openUIZone();
			}

			Char.myCharz().cspeed = Utils.myCharSpeed;
			Time.timeScale = Utils.timeScale;
			CharEffectMain.Update();
			TeleportMenuMain.Update();
			AutoGoback.Update();
			AutoTrainPet.Update();
			AutoSellTrashItems.Update();
			AutoLogin.OnGameScrUpdate();
			Pk9rPickMob.Update();
			AutoPean.Update();
			AutoSkill.Update();
		}

		internal static void OnLogin(ref string username, ref string pass, ref sbyte type)
		{
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				if (type == 1)
				{
					InGameAccountManager.ResetSelectedAccountIndex();
					Rms.DeleteStorage("acc");
					Rms.DeleteStorage("pass");
					return;
				}

				Account acc = InGameAccountManager.SelectedAccount;
				if (acc == null)
					return;
				type = (sbyte)acc.Type;
				username = acc.Username;
				if (acc.Type == AccountType.Registered)
				{
					pass = acc.Password;
				}
				else
				{
					pass = string.Empty;
					Rms.DeleteStorage("userAo" + acc.Server.index);
				}

				acc.LastTimeLogin = DateTime.Now;
			}
			else
			{
				username = Utils.username == "" ? username : Utils.username;
				if (username.StartsWith("User"))
				{
					pass = string.Empty;
					type = 1;
				}
				else
				{
					pass = Utils.password == "" ? pass : Utils.password;
				}
			}
		}

		internal static void OnServerListScreenLoaded(ServerListScreen serverListScreen)
		{
			ModMenuMain.Initialize();
			TeleportMenuMain.LoadData();
			AutoTrainPet.isFirstTimeCheckPet = true;
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				if (string.IsNullOrEmpty(nameCustomServer))
					return;
				serverListScreen.cmd[2 + serverListScreen.nCmdPlay].caption =
					mResources.server + ": [custom] " + nameCustomServer;
			}
		}

		internal static void OnSessionConnecting(ref string host, ref int port)
		{
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				nameCustomServer = "";
				if (InGameAccountManager.SelectedAccount == null)
					return;
				Server server = InGameAccountManager.SelectedServer;
				if (server == null)
					return;
				InGameAccountManager.SelectedServer = null;
				if (server.IsCustomIP())
				{
					host = currentHost = server.hostnameOrIPAddress;
					port = currentPort = server.port;
					nameCustomServer = server.name;
				}
				else
				{
					host = currentHost = ServerListScreen.address[server.index];
					port = currentPort = (ushort)ServerListScreen.port[server.index];
					ServerListScreen.ipSelect = server.index;
				}
			}
			else
			{
				if (Utils.server != null)
				{
					host = (string)Utils.server["ip"];
					port = (int)Utils.server["port"];
				}
			}
		}

		internal static void OnScreenDownloadDataShow()
		{
		}

		internal static bool OnCheckZoomLevel(int w, int h)
		{
			if (Utils.IsAndroidBuild())
			{
				if (w * h >= 2073600)
					mGraphics.zoomLevel = 4;
				else if (w * h >= 691200)
					mGraphics.zoomLevel = 3;
				else if (w * h > 153600)
					mGraphics.zoomLevel = 2;
				else
					mGraphics.zoomLevel = 1;
			}
			else
			{
				mGraphics.zoomLevel = 2;
				if (w * h < 480000)
					mGraphics.zoomLevel = 1;
			}

			return true;
		}

		internal static bool OnKeyPressed(int keyCode, bool isFromSync)
		{
			if (Utils.channelSyncKey != -1 && !isFromSync)
			{
				//SocketClient.gI.sendMessage(new
				//{
				//    action = "syncKeyPressed",
				//    keyCode,
				//    Utils.channelSyncKey
				//});
			}

			return false;
		}

		internal static bool OnKeyReleased(int keyCode, bool isFromSync)
		{
			if (Utils.channelSyncKey != -1 && !isFromSync)
			{
				//SocketClient.gI.sendMessage(new
				//{
				//    action = "syncKeyReleased",
				//    keyCode,
				//    Utils.channelSyncKey
				//});
			}

			return false;
		}

		internal static bool OnChatPopupMultiLine(string chat)
		{
			return false;
		}

		internal static bool OnAddBigMessage(string chat, Npc npc)
		{
			return true;
		}

		internal static void OnInfoMapLoaded()
		{
			Utils.UpdateWaypointChangeMap();
			GameScr.gI().pts = null;
		}

		internal static void OnPaintGameScr(mGraphics g)
		{
			try
			{
				ModMenuMain.Paint(g);
				CharEffectMain.Paint(g);
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				throw;
			}

		}

		internal static bool OnUseSkill(Char ch)
		{
			if (ch.me)
			{
				CharEffectMain.AddEffectCreatedByMe(ch.myskill);
			}
			return false;
		}

		internal static void OnAddInfoMe(string str)
		{
			Pk9rXmap.Info(str);
		}

		internal static bool OnUpdateTouchGameScr(GameScr instance)
		{
			// If panel2 (mod menu/custom panel on the right) is handling touch, block gameplay touch behind it.
			if (!GameCanvas.panel.isShow && GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				int panel2DrawX = GameCanvas.panel2.X + GameCanvas.panel2.X - GameCanvas.panel2.cmx;
				if (GameCanvas.isPointerJustRelease &&
				    !GameCanvas.isPointer(panel2DrawX, GameCanvas.panel2.Y, GameCanvas.panel2.W, GameCanvas.panel2.H) &&
				    GameCanvas.panel2.isDoneCombine && !GameCanvas.panel2.pointerIsDowning)
				{
					GameCanvas.panel2.hide();
					GameCanvas.clearAllPointerEvent();
					return true;
				}

				if (GameCanvas.panel2.pointerIsDowning ||
				    GameCanvas.isPointer(panel2DrawX, GameCanvas.panel2.Y, GameCanvas.panel2.W,
					    GameCanvas.panel2.H))
				{
					instance.isPointerDowning = false;
					if (Char.myCharz() != null)
						Char.myCharz().currentMovePoint = null;
					return true;
				}
			}

			ModMenuMain.UpdateTouch();
			if (GameCanvas.isTouchControl)
			{
				if (!TileMap.isOfflineMap())
					return false;
				if (GameCanvas.isMouseFocus(GameScr.xC, GameScr.yC, 34, 34))
				{
					mScreen.keyMouse = 15;
				}
				else if (GameCanvas.isMouseFocus(GameScr.xHP, GameScr.yHP, 40, 40))
				{
					if (Char.myCharz().statusMe != 14)
						mScreen.keyMouse = 10;
				}
				else if (GameCanvas.isMouseFocus(GameScr.xF, GameScr.yF, 40, 40))
				{
					if (Char.myCharz().statusMe != 14)
						mScreen.keyMouse = 5;
				}
				else if (instance.cmdMenu != null && GameCanvas.isMouseFocus(instance.cmdMenu.x, instance.cmdMenu.y,
					         instance.cmdMenu.w / 2, instance.cmdMenu.h))
				{
					mScreen.keyMouse = 1;
				}
				else
				{
					mScreen.keyMouse = -1;
				}

				if (GameCanvas.isPointerHoldIn(GameScr.xC, GameScr.yC, 34, 34))
				{
					mScreen.keyTouch = 15;
					GameCanvas.isPointerJustDown = false;
					instance.isPointerDowning = false;
					if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
					{
						ChatTextField.gI().startChat(instance, string.Empty);
						SoundMn.gI().buttonClick();
						Char.myCharz().currentMovePoint = null;
						GameCanvas.clearAllPointerEvent();
					}

					return true;
				}
			}

			if (instance.mobCapcha != null)
			{
				;
			}
			else if (GameScr.isHaveSelectSkill)
			{
				if (Char.myCharz().IsCharDead())
					return false;
				if (!instance.isCharging())
				{
					Skill[] skills = Main.isPC ? GameScr.keySkill : GameScr.onScreenSkill;
					int xSMax = int.MinValue;
					int xSMin = int.MaxValue;
					int ySMax = int.MinValue;
					int ySMin = int.MaxValue;
					for (int i = skills.Length - 1; i >= 0; i--)
						if (skills[i] != null)
						{
							xSMax = Math.max(GameScr.xS[i], xSMax);
							xSMin = Math.min(GameScr.xS[i], xSMin);
							ySMax = Math.max(GameScr.yS[i], ySMax);
							ySMin = Math.min(GameScr.yS[i], ySMin);
						}

					if (GameCanvas.isPointerHoldIn(GameScr.xSkill - 5, ySMin - 5, xSMax - xSMin + GameScr.wSkill,
						    ySMax - ySMin + GameScr.wSkill))
					{
						for (int i = 0; i < GameScr.onScreenSkill.Length; i++)
							if (GameCanvas.isPointerHoldIn(GameScr.xSkill + GameScr.xS[i], GameScr.yS[i],
								    GameScr.wSkill, GameScr.wSkill))
							{
								GameCanvas.isPointerJustDown = false;
								instance.isPointerDowning = false;
								instance.keyTouchSkill = i;
								if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
								{
									GameCanvas.isPointerClick = GameCanvas.isPointerJustDown =
										GameCanvas.isPointerJustRelease = false;
									instance.selectedIndexSkill = i;
									if (GameScr.indexSelect < 0)
										GameScr.indexSelect = 0;
									if (!Main.isPC)
									{
										if (instance.selectedIndexSkill > GameScr.onScreenSkill.Length - 1)
											instance.selectedIndexSkill = GameScr.onScreenSkill.Length - 1;
									}
									else if (instance.selectedIndexSkill > GameScr.keySkill.Length - 1)
									{
										instance.selectedIndexSkill = GameScr.keySkill.Length - 1;
									}

									Skill skill = Main.isPC
										? GameScr.keySkill[instance.selectedIndexSkill]
										: GameScr.onScreenSkill[instance.selectedIndexSkill];
									if (skill != null)
										instance.doSelectSkill(skill, true);
									break;
								}
							}

						return true;
					}
				}
			}
			return false;
		}

		internal static void OnUpdateTouchPanel(Panel instance)
		{
			if (instance.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
				instance.updateKeyScrollView();
		}

		internal static void OnSetPointItemMap(int xEnd, int yEnd)
		{

		}

		internal static bool OnMenuStartAt(MyVector menuItems)
		{

			return false;
		}

		internal static void OnAddInfoChar(Char c, string info)
		{
			if (LocalizedString.saoMayLuoiThe.ContainsReversed(info.ToLower()) &&
			    AutoTrainPet.Mode > AutoTrainPetMode.Disabled && c.charID == -Char.myCharz().charID)
				AutoTrainPet.saoMayLuoiThe = true;
		}

		internal static bool OnPaintBgGameScr(mGraphics g)
		{
			return false;
		}

		internal static void OnMobStartDie(Mob mob)
		{
			Pk9rPickMob.MobStartDie(mob);
		}

		internal static void OnUpdateMob(Mob mob)
		{
			Pk9rPickMob.UpdateCountDieMob(mob);
		}

		internal static bool OnCreateImage(string filename, out Image image)
		{
			string streamingAssetsPath = Application.streamingAssetsPath;
			if (Utils.IsAndroidBuild())
				streamingAssetsPath = Path.Combine(Utils.PersistentDataPath, "StreamingAssets");
			string customAssetsPath = Path.Combine(streamingAssetsPath, "CustomAssets");
			image = new Image();
			Texture2D texture2D;
			if (!Utils.IsEditor() && !Directory.Exists(customAssetsPath))
				Directory.CreateDirectory(customAssetsPath);
			string filePath = Path.Combine(customAssetsPath, filename.Replace('/', Path.DirectorySeparatorChar) + ".png");
			if (File.Exists(filePath))
			{
				texture2D = new Texture2D(1, 1);
				texture2D.LoadImage(File.ReadAllBytes(filePath));
			}
			else
			{
				texture2D = Resources.Load<Texture2D>(filename);
			}

			if (texture2D == null)
				throw new NullReferenceException(nameof(texture2D));
			image.texture = texture2D;
			image.w = image.texture.width;
			image.h = image.texture.height;
			image.texture.anisoLevel = 0;
			image.texture.filterMode = FilterMode.Point;
			image.texture.mipMapBias = 0f;
			image.texture.wrapMode = TextureWrapMode.Clamp;
			return true;
		}

		internal static void OnChatVip(string chatVip)
		{
			
		}

		internal static bool OnUpdateScrollMousePanel(Panel panel, ref int pXYScrollMouse)
		{
			return false;
		}

		internal static void OnPanelHide(Panel instance)
		{
		}

		internal static void OnUpdateKeyPanel(Panel instance)
		{
		}

		internal static void OnUpdateChar(Char ch)
		{
			CharEffectMain.UpdateChar(ch);
		}

		internal static void OnCharRemoveHoldEff(Char ch)
		{
			CharEffectMain.RemoveHold(ch);
		}

		internal static void OnCharSetHoldChar(Char ch, Char r)
		{
			CharEffectMain.AddCharHoldChar(ch, r);
		}

		internal static void OnCharSetHoldMob(Char ch)
		{
			CharEffectMain.AddCharHoldMob(ch);
		}

		internal static bool OnPaintTouchControl(GameScr instance, mGraphics g)
		{
			if (instance.isNotPaintTouchControl())
				return false;
			GameScr.resetTranslate(g);
			if (mScreen.keyTouch == 15 || Utils.IsPC() && mScreen.keyMouse == 15)
				g.drawImage(Utils.IsPC() ? GameScr.imgChatsPC2 : GameScr.imgChat2, GameScr.xC + 17,
					GameScr.yC + 17 + mGraphics.addYWhenOpenKeyBoard, mGraphics.HCENTER | mGraphics.VCENTER);
			else
				g.drawImage(Utils.IsPC() ? GameScr.imgChatPC : GameScr.imgChat, GameScr.xC + 17,
					GameScr.yC + 17 + mGraphics.addYWhenOpenKeyBoard, mGraphics.HCENTER | mGraphics.VCENTER);
			return true;
		}

		internal static bool OnGameScrPaintGamePad(mGraphics g)
		{
			GameScr.isHaveSelectSkill = isHaveSelectSkill_old;
			if (GameScr.isAnalog != 0 && Char.myCharz().statusMe != 14)
			{
				g.drawImage(mScreen.keyTouch == 5 ? GameScr.imgFire1 : GameScr.imgFire0, GameScr.xF + 20,
					GameScr.yF + 20, mGraphics.HCENTER | mGraphics.VCENTER);
				GameScr.gamePad.paint(g);
				g.drawImage(mScreen.keyTouch != 13 ? GameScr.imgFocus : GameScr.imgFocus2, GameScr.xTG + 20,
					GameScr.yTG + 20, mGraphics.HCENTER | mGraphics.VCENTER);
			}

			return true;
		}

		internal static bool OnPanelFireOption(Panel panel)
		{
			if (panel.selected >= 0)
				switch (panel.selected)
				{
				case 0:
					SoundMn.gI().AuraToolOption();
					break;
				case 1:
					SoundMn.gI().AuraToolOption2();
					break;
				case 2:
					SoundMn.gI().chatVipToolOption();
					break;
				case 3:
					SoundMn.gI().soundToolOption();
					break;
				case 4:
					SoundMn.gI().analogToolOption();
					break;
				case 5:
					SoundMn.gI().CaseSizeScr();
					break;
				case 6:
					GameCanvas.startYesNoDlg(mResources.changeSizeScreen,
						new Command(mResources.YES, panel, 170391, null),
						new Command(mResources.NO, panel, 4005, null));
					break;
				}

			return true;
		}

		internal static bool OnSoundMnGetStrOption()
		{
			Panel.strCauhinh = new[]
			{
				mResources.aura_off?.Trim() + ": " + Strings.OnOffStatus(Char.isPaintAura), mResources.aura_off_2?.Trim() + ": " + Strings.OnOffStatus(Char.isPaintAura2), mResources.serverchat_off?.Trim() + ": " + Strings.OnOffStatus(GameScr.isPaintChatVip), mResources.turnOffSound?.Trim() + ": " + Strings.OnOffStatus(GameCanvas.isPlaySound), mResources.analog?.Trim() + ": " + Strings.OnOffStatus(GameScr.isAnalog != 0), (GameCanvas.lowGraphic ? mResources.cauhinhcao : mResources.cauhinhthap)?.Trim(), mGraphics.zoomLevel <= 1 ? mResources.x2Screen : mResources.x1Screen
			};
			return true;
		}

		internal static bool OnSetSkillBarPosition()
		{
			Skill[] skills = /*GameCanvas.isTouch ? GameScr.onScreenSkill : */GameScr.keySkill;
			GameScr.xS = new int[skills.Length];
			GameScr.yS = new int[skills.Length];
			if (GameCanvas.isTouchControlSmallScreen && GameScr.isUseTouch)
			{
				GameScr.padSkill = 5;
			}
			else
			{
				GameScr.wSkill = 30;
				if (GameCanvas.w <= 320)
					GameScr.ySkill = GameScr.gH - GameScr.wSkill - 6;
				else
					GameScr.wSkill = 40;
			}

			GameScr.xSkill = 17;
			GameScr.ySkill = GameCanvas.h - 40;
			if (GameScr.gamePad.isSmallGamePad && GameScr.isAnalog == 1)
			{
				GameScr.xHP = Math.min(skills.Length, 5) * GameScr.wSkill;
				GameScr.yHP = GameScr.ySkill;
			}
			else
			{
				GameScr.xHP = GameCanvas.w - 45;
				GameScr.yHP = GameCanvas.h - 45;
			}

			//GameScr.setTouchBtn();
			if (GameScr.isAnalog != 0)
			{
				GameScr.xTG = GameScr.xF = GameCanvas.w - 45;
				if (GameScr.gamePad.isLargeGamePad)
				{
					//GameScr.xSkill = GameScr.gamePad.wZone + 20;
					int skillsCountNotNull = skills.Length;
					for (int i = skills.Length - 1; i >= 0; i--)
						if (skills[i] == null)
							skillsCountNotNull--;
						else
							break;
					GameScr.wSkill = 35;
					GameScr.xSkill = Math.max(GameScr.gamePad.wZone + 20,
						GameCanvas.hw - skillsCountNotNull * GameScr.wSkill / 2);
					GameScr.xHP = GameScr.xF - 45;
				}
				else if (GameScr.gamePad.isMediumGamePad)
				{
					GameScr.xHP = GameScr.xF - 45;
				}

				GameScr.yF = GameCanvas.h - 45;
				GameScr.yTG = GameScr.yF - 45;
			}

			if (GameCanvas.isTouchControlSmallScreen && GameScr.isUseTouch ||
			    !GameScr.gamePad.isLargeGamePad && GameScr.isAnalog == 1)
			{
				for (int i = 0; i < GameScr.xS.Length; i++)
				{
					GameScr.xS[i] = i * GameScr.wSkill;
					GameScr.yS[i] = GameScr.ySkill;
					if (GameScr.xS.Length > 5 && i >= GameScr.xS.Length / 2)
					{
						GameScr.xS[i] = (i - GameScr.xS.Length / 2) * GameScr.wSkill;
						GameScr.yS[i] = GameScr.ySkill - 32;
					}
				}
			}
			else
			{
				int lastJ = 0;
				for (int i = 0; i < GameScr.xS.Length; i++)
				{
					GameScr.xS[i] = i * GameScr.wSkill;
					GameScr.yS[i] = GameScr.ySkill;
					if (lastJ == 0 && GameScr.xSkill + i * GameScr.wSkill > GameScr.xHP - 30)
						lastJ = i;
					if (GameScr.xS.Length > 5 && lastJ > 0 && i >= lastJ)
					{
						GameScr.xS[i] = (i - lastJ) * GameScr.wSkill;
						GameScr.yS[i] = GameScr.ySkill - 32;
					}
				}
			}

			return true;
		}

		internal static bool OnGamepadPaint(GamePad instance, mGraphics g)
		{
			if (GameScr.isAnalog != 0)
			{
				return true;
			}

			return false;
		}

		internal static void OnGameScrPaintSelectedSkill(GameScr instance, mGraphics g)
		{
			if (!GameScr.isHaveSelectSkill)
				return;
			isHaveSelectSkill_old = GameScr.isHaveSelectSkill;
			GameScr.isHaveSelectSkill = false;
			if (HideGameUI.isEnabled)
				return;
			Skill[] array;
			if (Main.isPC)
				array = GameScr.keySkill;
			else if (GameCanvas.isTouch)
				array = GameScr.onScreenSkill;
			else
				array = GameScr.keySkill;
			if (!GameCanvas.isTouch)
			{
				g.setColor(0xAA2C11);
				g.fillRect(GameScr.xSkill + GameScr.xHP + 2, GameScr.yHP - 10 + 6, 20, 10);
				mFont.tahoma_7_white.drawString(g, "*", GameScr.xSkill + GameScr.xHP + 12, GameScr.yHP - 8 + 6,
					mFont.CENTER);
			}

			int num = instance.nSkill;
			if (Main.isPC || !GameCanvas.isTouch)
				num = array.Length;
			string[] array2 = TField.isQwerty
				? new string[10]
				{
					"1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
				}
				: new string[5]
				{
					"7", "8", "9", "10", "11"
				};
			bool hasSkillsInTopRow = false;
			bool isStartHasSkill = false;
			for (int i = num - 1; i >= 0; i--)
			{
				if (array[i] != null)
					isStartHasSkill = true;
				if (isStartHasSkill)
					if (GameScr.yS[i] == GameScr.ySkill - 32)
					{
						hasSkillsInTopRow = true;
						break;
					}
			}

			isStartHasSkill = false;

			for (int i = num - 1; i >= 0; i--)
			{
				Skill skill = array[i];
				if (skill != null)
				{
					isStartHasSkill = true;
					if (skill != Char.myCharz().myskill)
						g.drawImage(GameScr.imgSkill, GameScr.xSkill + GameScr.xS[i] - 1, GameScr.yS[i] - 1, 0);
					else
						g.drawImage(GameScr.imgSkill2, GameScr.xSkill + GameScr.xS[i] - 1, GameScr.yS[i] - 1, 0);
				}
				else
				{
					if (isStartHasSkill)
						g.drawImage(GameScr.imgSkill, GameScr.xSkill + GameScr.xS[i] - 1, GameScr.yS[i] - 1, 0);
					continue;
				}

				if (Utils.IsPC())
				{
					int num2 = 27;
					if (hasSkillsInTopRow)
					{
						if (GameScr.yS[i] == GameScr.ySkill - 32)
							num2 = -13;
					}
					else
					{
						num2 = -13;
					}

					mFont.tahoma_7b_white.drawString(g, array2[i], GameScr.xSkill + GameScr.xS[i] + 14,
						GameScr.yS[i] + num2 + 1, mFont.CENTER, mFont.tahoma_7b_dark);
				}

				skill.paint(GameScr.xSkill + GameScr.xS[i] + 13, GameScr.yS[i] + 13, g);
				if (i == instance.selectedIndexSkill && !instance.isPaintUI() && GameCanvas.gameTick % 10 > 5 ||
				    i == instance.keyTouchSkill)
					g.drawImage(ItemMap.imageFlare, GameScr.xSkill + GameScr.xS[i] + 13, GameScr.yS[i] + 14, 3);
			}
		}

		internal static void AfterGameScrPaintSelectedSkill(GameScr instance, mGraphics g)
		{

		}

		internal static bool OnPanelPaintToolInfo(mGraphics g)
		{
			mFont.tahoma_7b_white.drawString(g, Strings.communityMod, 60, 4, mFont.LEFT, mFont.tahoma_7b_dark);
			mFont.tahoma_7_yellow.drawString(g, Strings.gameVersion + ": v" + GameMidlet.VERSION, 60, 16, mFont.LEFT,
				mFont.tahoma_7_grey);
			mFont.tahoma_7_yellow.drawString(g, mResources.character + ": " + Char.myCharz().cName, 60, 27, mFont.LEFT,
				mFont.tahoma_7_grey);
			Account account = InGameAccountManager.SelectedAccount;
			string serverName = account.Server.IsCustomIP()
				? account.Server.name
				: ServerListScreen.nameServer[account.Server.index];
			mFont.tahoma_7_yellow.drawString(g,
				mResources.account + " " + mResources.account_server.ToLower() + " " + serverName, 60, 39, mFont.LEFT,
				mFont.tahoma_7_grey);
			return true;
		}

		internal static bool OnSkillPaint(Skill skill, int x, int y, mGraphics g)
		{
			if (!HideGameUI.isEnabled)
				SmallImage.drawSmallImage(g, skill.template.iconId, x, y, 0, StaticObj.VCENTER_HCENTER);
			long coolingDown = mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill;
			if (coolingDown < skill.coolDown)
			{
				float opacity = .6f;
				int realX = x - 11;
				int realY = y - 11;
				Color color = new Color(0, 0, 0, opacity);
				Color color2 = new Color(0, 0, 0, opacity / 2);
				g.setColor(color2);
				g.fillRect(realX, realY, 22, 22);
				float coolDownRatio = 1 - coolingDown / (float)skill.coolDown;
				CustomGraphics.drawCooldownRect(x, y, 22, 22, coolDownRatio, color);
				string cooldownStr = $"{(skill.coolDown - coolingDown) / 1000f:#.0}".Replace(',', '.');
				if (cooldownStr.Length > 4)
					cooldownStr = cooldownStr.Substring(0, cooldownStr.IndexOf('.'));
				mFont.tahoma_7_yellow.drawString(g, cooldownStr, x + 1, y - 12 + mFont.tahoma_7.getHeight() / 2,
					mFont.CENTER);
			}
			else
			{
				skill.paintCanNotUseSkill = false;
			}

			return true;
		}

		internal static bool OnGotoPlayer(int id, bool isAutoUseYardrat = true)
		{
			if (isAutoUseYardrat)
			{
				new Thread(delegate()
				{
					int previousDisguiseId = -1;
					if (Char.myCharz().arrItemBody[5] == null || Char.myCharz().arrItemBody[5] != null &&
					    (Char.myCharz().arrItemBody[5].template.id < 592 ||
					     Char.myCharz().arrItemBody[5].template.id > 594))
					{
						if (Char.myCharz().arrItemBody[5] != null)
							previousDisguiseId = Char.myCharz().arrItemBody[5].template.id;
						for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
						{
							Item item = Char.myCharz().arrItemBag[i];
							if (item != null && item.template.id >= 592 && item.template.id <= 594)
							{
								do
								{
									Service.gI().getItem(4, (sbyte)i);
									Thread.Sleep(250);
								} while (Char.myCharz().arrItemBody[5].template.id < 592 ||
								         Char.myCharz().arrItemBody[5].template.id > 594);

								break;
							}
						}
					}

					Service.gI().gotoPlayer(id);
					if (previousDisguiseId != -1)
					{
						Thread.Sleep(500);
						for (int j = 0; j < Char.myCharz().arrItemBag.Length; j++)
						{
							Item item = Char.myCharz().arrItemBag[j];
							if (item != null && item.template.id == previousDisguiseId)
							{
								do
								{
									Service.gI().getItem(4, (sbyte)j);
									Thread.Sleep(250);
								} while (Char.myCharz().arrItemBody[5].template.id != previousDisguiseId);

								break;
							}
						}
					}
				}).Start();
				return true;
			}

			return false;
		}

		internal static bool OnPaintPanel(Panel panel, mGraphics g)
		{
			if (panel.type != CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
				return false;
			g.translate(-g.getTranslateX(), -g.getTranslateY());
			g.translate(-panel.cmx, 0);
			g.translate(panel.X, panel.Y);
			GameCanvas.paintz.paintFrameSimple(panel.X, panel.Y, panel.W, panel.H, g);
			g.setClip(panel.X + 1, panel.Y, panel.W - 2, panel.yScroll - 2);
			g.setColor(0x987B55);
			g.fillRect(panel.X, panel.Y, panel.W - 2, 50);
			//panel.paintCharInfo(g, Char.myCharz());
			CustomPanelMenu.PaintTopInfo(panel, g);
			panel.paintBottomMoneyInfo(g);
			if (!CustomPanelMenu.PaintTabHeader(panel, g))
				panel.paintTab(g);
			CustomPanelMenu.Paint(panel, g);
			GameScr.resetTranslate(g);
			panel.paintDetail(g);
			if (panel.cmx == panel.cmtoX)
				panel.cmdClose.paint(g);
			if (panel.tabIcon != null && panel.tabIcon.isShow)
				panel.tabIcon.paint(g);
			return true;
		}

		internal static void OnPaintGameCanvas(GameCanvas instance, mGraphics g)
		{
			if (style == null)
			{
				style = new GUIStyle(GUI.skin.label)
				{
					fontStyle = FontStyle.Bold,
					fontSize = (int)(8.5 * mGraphics.zoomLevel)
				};
				style.normal.textColor = style.hover.textColor = Color.yellow;
			}

			if (!GameCanvas.panel.isShow)
			{
				if (GameCanvas.panel2 != null)
				{
					g.translate(-g.getTranslateX(), -g.getTranslateY());
					g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
					if (GameCanvas.panel2.isShow)
					{
						GameCanvas.panel2.paint(g);
					}
					if (GameCanvas.panel2.chatTField != null && GameCanvas.panel2.chatTField.isShow)
					{
						GameCanvas.panel2.chatTField.paint(g);
					}
				}
			}

			g.setColor(new Color(0.2f, 0.2f, 0.2f, 0.6f));
			double fps = System.Math.Round(1f / Time.smoothDeltaTime * Time.timeScale, 1);
			string fpsStr = fps.ToString("F1").Replace(',', '.');
			g.fillRect(0, 0, mFont.tahoma_7b_red.getWidth(fpsStr) + 2, 12);
			mFont.tahoma_7b_red.drawString(g, fpsStr, 2, 0, 0);
		}

			internal static bool OnUpdatePanel(Panel instance)
			{
				if (instance == GameCanvas.panel)
				{
					Panel panel = ModMenuMain.currentPanel;
					int instanceDrawX = instance.X + instance.X - instance.cmx;
					if (panel != null && panel.isShow && GameCanvas.isPointerJustRelease &&
					    !GameCanvas.isPointer(instanceDrawX, instance.Y, instance.W, instance.H) &&
					    !GameCanvas.isPointer(panel.X + panel.X - panel.cmx, panel.Y, panel.W, panel.H) &&
					    !instance.pointerIsDowning)
					{
						instance.hide();
						return false;
					}
				}

			if (instance.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
				if ((instance.chatTField == null || !instance.chatTField.isShow) && !instance.isKiguiXu &&
				    !instance.isKiguiLuong && (instance.tabIcon == null || !instance.tabIcon.isShow) &&
				    instance.waitToPerform > 0)
					if (instance.waitToPerform - 1 == 0)
					{
						instance.waitToPerform--;
						instance.lastSelect[instance.currentTabIndex] = instance.selected;
						CustomPanelMenu.DoFire(instance);
					}

			return false;
		}

		internal static bool OnPanelUpdateKeyInTabBar(Panel instance)
		{
			if (instance.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
			{
				if (instance.scroll != null && instance.scroll.pointerIsDowning || instance.pointerIsDowning)
					return true;
				int num = instance.currentTabIndex;
				if (instance.isTabInven() && instance.isnewInventory)
				{
					if (instance.selected == -1)
					{
						if (GameCanvas.keyPressed[6])
						{
							instance.currentTabIndex++;
							if (instance.currentTabIndex >= instance.currentTabName.Length)
							{
								if (GameCanvas.panel2 != null)
								{
									instance.currentTabIndex = instance.currentTabName.Length - 1;
									GameCanvas.isFocusPanel2 = true;
								}
								else
								{
									instance.currentTabIndex = 0;
								}
							}

							instance.selected = instance.lastSelect[instance.currentTabIndex];
							instance.lastTabIndex[instance.type] = instance.currentTabIndex;
						}

						if (GameCanvas.keyPressed[4])
						{
							instance.currentTabIndex--;
							if (instance.currentTabIndex < 0)
								instance.currentTabIndex = instance.currentTabName.Length - 1;
							if (GameCanvas.isFocusPanel2)
								GameCanvas.isFocusPanel2 = false;
							instance.selected = instance.lastSelect[instance.currentTabIndex];
							instance.lastTabIndex[instance.type] = instance.currentTabIndex;
						}
					}
					// sellectInventory is now synced with selected in updateKeyScrollView
					if (instance.sellectInventory < 0)
						instance.sellectInventory = 0;
					if (instance.sellectInventory >= instance.nTableItem)
						instance.sellectInventory = instance.nTableItem - 1;
				}
				else if (!instance.IsTabOption())
				{
					if (GameCanvas.keyPressed[!Main.isPC ? 6 : 24])
					{
						if (instance.isTabInven())
						{
							if (instance.selected >= 0)
							{
								instance.updateKeyInvenTab();
							}
							else
							{
								instance.currentTabIndex++;
								if (instance.currentTabIndex >= instance.currentTabName.Length)
								{
									if (GameCanvas.panel2 != null)
									{
										instance.currentTabIndex = instance.currentTabName.Length - 1;
										GameCanvas.isFocusPanel2 = true;
									}
									else
									{
										instance.currentTabIndex = 0;
									}
								}

								instance.selected = instance.lastSelect[instance.currentTabIndex];
								instance.lastTabIndex[instance.type] = instance.currentTabIndex;
							}
						}
						else
						{
							instance.currentTabIndex++;
							if (instance.currentTabIndex >= instance.currentTabName.Length)
							{
								if (GameCanvas.panel2 != null)
								{
									instance.currentTabIndex = instance.currentTabName.Length - 1;
									GameCanvas.isFocusPanel2 = true;
								}
								else
								{
									instance.currentTabIndex = 0;
								}
							}

							instance.selected = instance.lastSelect[instance.currentTabIndex];
							instance.lastTabIndex[instance.type] = instance.currentTabIndex;
						}
					}

					if (GameCanvas.keyPressed[!Main.isPC ? 4 : 23])
					{
						instance.currentTabIndex--;
						if (instance.currentTabIndex < 0)
							instance.currentTabIndex = instance.currentTabName.Length - 1;
						if (GameCanvas.isFocusPanel2)
							GameCanvas.isFocusPanel2 = false;
						instance.selected = instance.lastSelect[instance.currentTabIndex];
						instance.lastTabIndex[instance.type] = instance.currentTabIndex;
					}
				}

				instance.keyTouchTab = -1;
				for (int i = 0; i < instance.currentTabName.Length; i++)
				{
					if (!GameCanvas.isPointer(instance.startTabPos + i * instance.TAB_W, 52, instance.TAB_W - 1, 25))
						continue;
					instance.keyTouchTab = i;
					if (GameCanvas.isPointerJustRelease)
					{
						instance.currentTabIndex = i;
						instance.lastTabIndex[instance.type] = i;
						GameCanvas.isPointerJustRelease = false;
						instance.selected = instance.lastSelect[instance.currentTabIndex];
						if (num == instance.currentTabIndex && instance.cmRun == 0)
						{
							instance.cmtoY = 0;
							instance.selected = GameCanvas.isTouch ? -1 : 0;
						}

						break;
					}
				}

				if (num == instance.currentTabIndex)
					return true;
				instance.size_tab = 0;
				SoundMn.gI().panelClick();
				CustomPanelMenu.SetTab(instance);
				instance.selected = instance.lastSelect[instance.currentTabIndex];

				return true;
			}

			return false;
		}

		internal static void OnPaintImageBar(mGraphics g, bool isLeft, Char c)
		{
			if (!isLeft)
				return;
			if (c != Char.myCharz())
				return;
			int xHP = 85;
			int xMP = xHP;
			int yHP = 4;
			int yMP = 19;
			string cHP = Utils.FormatWithSIPrefix(Char.myCharz().cHP);
			string cMP = Utils.FormatWithSIPrefix(Char.myCharz().cMP);
			g.setColor(new Color(0.2f, 0.2f, 0.2f, 0.6f));
			if (mGraphics.zoomLevel > 1)
			{
				style.fontSize = (int)(8.5 * mGraphics.zoomLevel);
				g.fillRect(xHP, yHP + 1, Utils.getWidth(style, cHP) + 1, Utils.getHeight(style, cHP) - 2);
				g.drawString(cHP, xHP, yHP, style);
				style.fontSize = 5 * mGraphics.zoomLevel;
				g.fillRect(xMP - 1, yMP + 1, Utils.getWidth(style, cMP) + 1, Utils.getHeight(style, cMP) - 2);
				g.drawString(cMP, xMP, yMP, style);
			}
			else
			{
				g.fillRect(xHP - 1, yHP + 1, mFont.tahoma_7b_yellow.getWidth(cHP),
					mFont.tahoma_7b_yellow.getHeight() - 2);
				mFont.tahoma_7b_yellow.drawString(g, cHP, xHP, yHP, mFont.LEFT);
				g.fillRect(xMP - 1, yMP + 1, mFont.tahoma_7_yellow.getWidth(cMP),
					mFont.tahoma_7_yellow.getHeight() - 2);
				mFont.tahoma_7_yellow.drawString(g, cMP, xMP, yMP, mFont.LEFT);
			}
		}

		internal static void OnLoadIP()
		{
			ServerListScreen.getServerList(Strings.DEFAULT_IP_SERVERS);
		}

		internal static void OnAfterPaintPanel(Panel panel, mGraphics g)
		{

			sbyte GetColor_Item_Upgrade(int lv)
			{
				if (lv < 8)
					return 0;
				if (lv == 9)
					return 4;
				if (lv == 10)
					return 1;
				if (lv == 11)
					return 5;
				if (lv == 12)
					return 3;
				if (lv == 13)
					return 2;
				return 6;
			}

			int GetColor_ItemBg(int id)
			{
				switch (id)
				{
				case 4:
					return 1269146;
				case 1:
					return 2786816;
				case 5:
					return 13279744;
				case 3:
					return 12537346;
				case 2:
					return 7078041;
				case 6:
					return 11599872;
				default:
					return -1;
				}
			}

			if (GameCanvas.panel.combineSuccess != -1)
				return;
			g.translate(-(panel.cmx - panel.cmtoX), -panel.cmy);
			if (panel.type == 13) //trade
			{
				bool? isMe = null;
				if (panel.currentTabIndex == 0 && panel != GameCanvas.panel)
					isMe = false;
				if (panel.currentTabIndex == 2)
					isMe = false;
				if (panel.currentTabIndex == 1)
					isMe = true;
				if (isMe != null)
				{
					MyVector myVector = isMe.Value ? panel.vMyGD : panel.vFriendGD;
					if (myVector.size() <= 0)
						return;
					int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT, 0);
					for (int i = offset;
					     i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, myVector.size());
					     i++)
					{
						Item item = (Item)myVector.elementAt(i);
						if (item == null)
							continue;
						int y = panel.yScroll + i * panel.ITEM_HEIGHT;
						if (item.itemOption != null)
						{
							ItemOption itemOption = item.GetBestItemOption();
							if (itemOption == null)
								goto Label;
							int param = itemOption.param;
							int id = itemOption.optionTemplate.id;
							if (param > 7 || id >= 127 && id <= 135)
								param = 7;
							if (id == 107)
							{
								if (param > 1)
									param = (int)System.Math.Ceiling((double)param / 2);
								else if (param == 1)
									goto Label;
							}

							if (param <= 0)
								goto Label;
							g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
							for (int j = 0; j < item.itemOption.Length; j++)
								if (item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
								{
									byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
									if (GetColor_ItemBg(id_) != -1)
										g.setColor(GetColor_ItemBg(id_));
								}

							g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
							CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 11, 34, panel.ITEM_HEIGHT - 1, item);
							SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2, panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
						}

						Label: ;
						CustomGraphics.PaintItemOptions(g, panel, item, y);
					}
				}
			}
			else if (panel.type == 1 || panel.type == 17) //shop
			{
				if (panel.type == 1 && panel.currentTabIndex == panel.currentTabName.Length - 1 &&
				    GameCanvas.panel2 == null && panel.typeShop != 2)
					return;
				if (panel.typeShop == 2 && panel == GameCanvas.panel)
					if (Char.myCharz().arrItemShop[panel.currentTabIndex].Length == 0 && panel.type != 17)
						return;
				Item[] array = Char.myCharz().arrItemShop[panel.currentTabIndex];
				if (panel.typeShop == 2 && (panel.currentTabIndex == 4 || panel.type == 17))
				{
					array = Char.myCharz().arrItemShop[4];
					if (array.Length == 0)
						return;
				}

				for (int i = 0; i < array.Length; i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					Item item = array[i];
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 24, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 12, y + 11, 24, panel.ITEM_HEIGHT - 1,
							item);
						SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 24 / 2,
							panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					if (panel.type == Panel.TYPE_KIGUI)
					{
						CustomGraphics.PaintItemOptions(g, panel, item, y + mFont.tahoma_7b_blue.getHeight() + 2);
					}
					else if (panel.type == Panel.TYPE_SHOP)
					{
						if (!string.IsNullOrEmpty(item.nameNguoiKyGui))
						{
							if (GameCanvas.gameTick % 120 > 60 && (Utils.HasStarOption(item, out _, out _) ||
							                                       Utils.HasActivateOption(item)))
							{
								int w = mFont.tahoma_7b_green.getWidth(item.nameNguoiKyGui) + 5;
								g.setColor(i != panel.selected ? 0xE7DFD2 : 0xF9FF4A);
								g.fillRect(panel.X + Panel.WIDTH_PANEL - 2 - w,
									y + mFont.tahoma_7b_blue.getHeight() + 2, w, mFont.tahoma_7b_green.getHeight());
								CustomGraphics.PaintItemOptions(g, panel, item,
									y + mFont.tahoma_7b_blue.getHeight() + 2);
							}
						}
						else
						{
							CustomGraphics.PaintItemOptions(g, panel, item, y + mFont.tahoma_7b_blue.getHeight() + 2);
						}
					}
					else
					{
						CustomGraphics.PaintItemOptions(g, panel, item, y);
					}
				}
			}
			else if (panel.type == 21 && panel.currentTabIndex == 0) //pet inventory
			{
				Item[] arrItemBody = Char.myPetz().arrItemBody;
				for (int i = 0; i < arrItemBody.Length; i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					Item item = arrItemBody[i];
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 14, 34, panel.ITEM_HEIGHT - 1, item);
						SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2, panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
			else if (panel.type == 2 && panel.currentTabIndex == 0) //box
			{
				Item[] arrItemBox = Char.myCharz().arrItemBox;
				int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT - 1, 0);
				for (int i = offset;
				     i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, arrItemBox.Length);
				     i++)
				{
					int y = panel.yScroll + (i + 1) * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					if (i == 0)
						continue;
					Item item = arrItemBox[i];
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 11, 34, panel.ITEM_HEIGHT - 1,
							item);
						SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2,
							y + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
			else if (panel.type == 12 && panel.currentTabIndex == 0) //combine
			{
				if (panel.vItemCombine.size() == 0)
					return;
				int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT, 0);
				for (int i = offset;
				     i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, panel.vItemCombine.size() + 1);
				     i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					if (i == panel.vItemCombine.size())
						continue;
					Item item = (Item)panel.vItemCombine.elementAt(i);
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 11, 34, panel.ITEM_HEIGHT - 1, item);
						SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2, panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
			else if (panel.type == 21 && panel.currentTabIndex == 2 ||
			         panel.type == 0 && panel.currentTabIndex == 1 ||
			         panel.type == 2 && panel.currentTabIndex == 1 ||
			         panel.type == 7 ||
			         panel.type == 12 && panel.currentTabIndex == 1 ||
			         panel.type == 13 && panel.currentTabIndex == 0 && panel == GameCanvas.panel ||
			         panel.type == 1 && panel.currentTabIndex == panel.currentTabName.Length - 1 &&
			         GameCanvas.panel2 == null && panel.typeShop != 2) //my inventory
			{
				Item[] arrItemBody = Char.myCharz().arrItemBody;
				Item[] arrItemBag = Char.myCharz().arrItemBag;
				int totalItems = arrItemBody.Length + arrItemBag.Length;
				int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT, 0);
				for (int i = offset;
				     i < Mathf.Clamp(offset + (panel.hScroll - 21) / panel.ITEM_HEIGHT + 2, 0, totalItems);
				     i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					bool isBodyItem = i < arrItemBody.Length;
					int bagIndex = i - arrItemBody.Length;
					Item item = isBodyItem ? arrItemBody[i] : bagIndex < arrItemBag.Length ? arrItemBag[bagIndex] : null;
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						if (i == panel.selected)
							g.setColor(0x919600);
						else if (isBodyItem)
							g.setColor(0x987B55);
						else
							g.setColor(0xB49F84);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g,
							panel.xScroll + 17 + (panel == GameCanvas.panel2 ? 2 : 0), y + 11, 34,
							panel.ITEM_HEIGHT - 1, item);
						SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2,
							y + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
		}

		internal static bool OnServerListScreenInitCommand(ServerListScreen screen)
		{
			screen.nCmdPlay = 0;
			string text = Rms.loadRMSString("acc");
			sbyte[] userAo = Rms.loadRMS("userAo" + ServerListScreen.ipSelect);
			if (text == null)
			{
				if (userAo != null)
					screen.nCmdPlay = 1;
			}
			else if (text.Equals(string.Empty))
			{
				if (userAo != null)
					screen.nCmdPlay = 1;
			}
			else
			{
				screen.nCmdPlay = 1;
			}

			screen.cmd = new Command[4 + screen.nCmdPlay];
			int num = GameCanvas.hh - 15 * screen.cmd.Length + 28;
			for (int i = 0; i < screen.cmd.Length; i++)
			{
				switch (i)
				{
				case 0:
					screen.cmd[0] = new Command(string.Empty, screen, 3, null);
					if (string.IsNullOrEmpty(text))
					{
						screen.cmd[0].caption = mResources.playNew + "";
						if (Rms.loadRMS("userAo" + ServerListScreen.ipSelect) != null)
							screen.cmd[0].caption = mResources.choitiep + "";
					}
					else if (!Utils.IsOpenedByExternalAccountManager)
					{
						Account acc = InGameAccountManager.SelectedAccount;
						if (acc == null)
							screen.cmd[0].caption = mResources.playAcc + ": " + new string('*', text.Length);
						else
							screen.cmd[0].caption = mResources.playAcc + ": " + acc.Info.Name;
					}
					else
					{
						screen.cmd[0].caption = mResources.playAcc + ": " + new string('*', text.Length);
					}

					if (screen.cmd[0].caption.Length > 23)
					{
						screen.cmd[0].caption = screen.cmd[0].caption.Substring(0, 23);
						screen.cmd[0].caption += "...";
					}

					break;
				case 1:
					if (screen.nCmdPlay == 1)
					{
						screen.cmd[1] = new Command(string.Empty, screen, 10100, null);
						screen.cmd[1].caption = mResources.playNew;
					}
					else
					{
						if (!Utils.IsOpenedByExternalAccountManager)
						{
							screen.cmd[1] = new Command(Strings.accounts, new InGameAccountManager.ActionListener(), 7, null);
						}
						else
						{
							screen.cmd[1] = new Command(mResources.change_account, screen, 7, null);
						}
					}

					break;
				case 2:
					if (screen.nCmdPlay == 1)
					{
						if (!Utils.IsOpenedByExternalAccountManager)
							screen.cmd[2] = new Command(Strings.accounts, new InGameAccountManager.ActionListener(), 7, null);
						else
							screen.cmd[2] = new Command(mResources.change_account, screen, 7, null);
					}
					else
					{
						screen.cmd[2] = new Command(string.Empty, screen, 17, null);
					}

					break;
				case 3:
					if (screen.nCmdPlay == 1)
						screen.cmd[3] = new Command(string.Empty, screen, 17, null);
					else
						screen.cmd[3] = new Command(mResources.option, screen, 8, null);
					break;
				case 4:
					screen.cmd[4] = new Command(mResources.option, screen, 8, null);
					break;
				}

				screen.cmd[i].y = num;
				screen.cmd[i].setType();
				screen.cmd[i].x = (GameCanvas.w - screen.cmd[i].w) / 2;
				num += 30;
			}

			return true;
		}

		internal static bool OnPanelFireTool(Panel panel)
		{
			if (panel.selected < 0)
				return false;
			if (SoundMn.IsDelAcc && panel.selected == Panel.strTool.Length - 1)
				return false;
			if (!Char.myCharz().havePet)
				switch (panel.selected)
				{
				case 4:
					if (GameScr.gI().pts != null)
						Utils.menuZone();
					isOpenZoneUI = true;
					return true;
				case 8:
					GameCanvas.timeBreakLoading = mSystem.currentTimeMillis() + 30000;
					ServerListScreen.countDieConnect = 0;
					GameCanvas.instance.resetToLoginScr = false;
					GameCanvas.instance.doResetToLoginScr(GameCanvas.serverScreen);
					return true;
				}
			else
				switch (panel.selected)
				{
				case 5:
					if (GameScr.gI().pts != null)
						Utils.menuZone();
					isOpenZoneUI = true;
					return true;
				case 9:
					GameCanvas.timeBreakLoading = mSystem.currentTimeMillis() + 30000;
					ServerListScreen.countDieConnect = 0;
					GameCanvas.instance.resetToLoginScr = false;
					GameCanvas.instance.doResetToLoginScr(GameCanvas.serverScreen);
					return true;
				}

			return false;
		}

		internal static bool OnGetSoundOption()
		{
			bool canRegister = GameCanvas.loginScr.isLogin2 || !Utils.IsOpenedByExternalAccountManager &&
				InGameAccountManager.SelectedAccount != null &&
				InGameAccountManager.SelectedAccount.Type ==
				AccountType.Unregistered;
			if (canRegister && Char.myCharz().taskMaint != null && Char.myCharz().taskMaint.taskId >= 2)
			{
				Panel.strTool = new[]
				{
					mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout, mResources.REGISTOPROTECT
				};
				if (Char.myCharz().havePet)
					Panel.strTool = new[]
					{
						mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.pet, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout, mResources.REGISTOPROTECT
					};
			}
			else
			{
				Panel.strTool = new[]
				{
					mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout
				};
				if (Char.myCharz().havePet)
					Panel.strTool = new[]
					{
						mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.pet, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout
					};
			}

			if (SoundMn.IsDelAcc)
			{
				string[] array = new string[Panel.strTool.Length + 1];
				for (int i = 0; i < Panel.strTool.Length; i++) array[i] = Panel.strTool[i];
				array[Panel.strTool.Length] = mResources.delacc;
				Panel.strTool = array;
			}

			return true;
		}

		internal static bool OnOpenUIZone(GameScr instance, Message message)
		{
			InfoDlg.hide();
			try
			{
				instance.zones = new int[message.reader().readByte()];
				instance.pts = new int[instance.zones.Length];
				instance.numPlayer = new int[instance.zones.Length];
				instance.maxPlayer = new int[instance.zones.Length];
				instance.rank1 = new int[instance.zones.Length];
				instance.rankName1 = new string[instance.zones.Length];
				instance.rank2 = new int[instance.zones.Length];
				instance.rankName2 = new string[instance.zones.Length];
				for (int i = 0; i < instance.zones.Length; i++)
				{
					instance.zones[i] = message.reader().readByte();
					instance.pts[i] = message.reader().readByte();
					instance.numPlayer[i] = message.reader().readByte();
					instance.maxPlayer[i] = message.reader().readByte();
					if (message.reader().readByte() == 1)
					{
						instance.rankName1[i] = message.reader().readUTF();
						instance.rank1[i] = message.reader().readInt();
						instance.rankName2[i] = message.reader().readUTF();
						instance.rank2[i] = message.reader().readInt();
					}
				}
			}
			catch (Exception ex)
			{
				Cout.LogError("Loi ham OPEN UIZONE " + ex);
			}

			return true;
		}

		internal static bool OnStartOKDlg(string info)
		{
			if (info == LocalizedString.cantChangeZoneInThisMap)
			{
				if (isOpenZoneUI)
				{
					isOpenZoneUI = false;
					return false;
				}

				return true;
			}

			return false;
		}

		internal static bool OnRequestChangeMap()
		{
			isOpenZoneUI = false;
			return false;
		}

		internal static bool OnGetMapOffline()
		{
			isOpenZoneUI = false;
			return false;
		}

		internal static bool OnMGraphicsDrawImage(Image image, int x, int y, int anchor)
		{
			if (HideGameUI.isEnabled && !HideGameUI.ShouldDrawImage(image))
				return true;
			if (GraphicsReducer.IsEnabled && !GraphicsReducer.ShouldDrawImage(image))
				return true;
			return false;
		}

		internal static void AfterMGraphicsDrawImage(Image image, int x, int y, int anchor)
		{
		}
	}
}
