using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mod.Graphics;
using UnityEngine;

namespace Mod
{
	internal static class GameHarmony
	{
		const string HarmonyId = "dragonboy247.mod.harmony";
		static readonly Type[] NoArguments = Array.Empty<Type>();
		static readonly BindingFlags BindingFlagsAll = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		static bool _isInitialized;
		static object _harmonyInstance;
		static MethodInfo _patchMethod;
		static ConstructorInfo _harmonyMethodCtor;

		internal static bool IsInstalled { get; private set; }

		internal static void Initialize()
		{
			if (_isInitialized)
				return;

			_isInitialized = true;

			try
			{
				if (!TryBindHarmony())
				{
					Debug.LogWarning("GameHarmony: Harmony was not found. Patches were not installed.");
					return;
				}

				ApplyPatches();
				IsInstalled = true;
				Debug.Log("GameHarmony: Harmony patches installed.");
			}
			catch (Exception ex)
			{
				IsInstalled = false;
				Debug.LogError($"GameHarmony: failed to install Harmony patches.\n{ex}");
			}
		}

		static bool TryBindHarmony()
		{
			Assembly harmonyAssembly = FindLoadedHarmonyAssembly() ?? LoadHarmonyAssemblyFromDisk();

			if (harmonyAssembly == null)
				return false;

			Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony");
			Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod");
			if (harmonyType == null || harmonyMethodType == null)
				return false;

			_harmonyMethodCtor = harmonyMethodType.GetConstructor(new[]
			{
				typeof(MethodInfo)
			});
			_patchMethod = harmonyType.GetMethod("Patch", new[]
			{
				typeof(MethodBase), harmonyMethodType, harmonyMethodType, harmonyMethodType, harmonyMethodType
			});
			if (_harmonyMethodCtor == null || _patchMethod == null)
				return false;

			_harmonyInstance = Activator.CreateInstance(harmonyType, HarmonyId);
			return _harmonyInstance != null;
		}

		static Assembly FindLoadedHarmonyAssembly()
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(assembly => assembly.GetType("HarmonyLib.Harmony") != null);
		}

		static Assembly LoadHarmonyAssemblyFromDisk()
		{
			string[] candidatePaths =
			{
				Path.Combine(Application.dataPath, "Plugins", "0Harmony.dll")
			};

			foreach (string candidatePath in candidatePaths)
			{
				if (!File.Exists(candidatePath))
					continue;

				try
				{
					Assembly assembly = Assembly.LoadFrom(candidatePath);
					if (assembly.GetType("HarmonyLib.Harmony") != null)
						return assembly;
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"GameHarmony: failed to load Harmony from {candidatePath}.\n{ex}");
				}
			}

			return null;
		}

		static void Patch(Type originalType, string originalMethodName, Type[] parameterTypes, string prefix = null, string postfix = null)
		{
			MethodBase originalMethod = originalType.GetMethod(originalMethodName, BindingFlagsAll, null, parameterTypes, null);
			if (originalMethod == null)
			{
				Debug.LogWarning($"GameHarmony: skipped missing method {originalType.FullName}.{originalMethodName}");
				return;
			}

			object prefixMethod = CreateHarmonyMethod(prefix);
			object postfixMethod = CreateHarmonyMethod(postfix);
			_patchMethod.Invoke(_harmonyInstance, new[]
			{
				originalMethod, prefixMethod, postfixMethod, null, null
			});
		}

		static void PatchConstructor(Type originalType, Type[] parameterTypes, string prefix = null, string postfix = null)
		{
			ConstructorInfo originalConstructor = originalType.GetConstructor(BindingFlagsAll, null, parameterTypes, null);
			if (originalConstructor == null)
			{
				Debug.LogWarning($"GameHarmony: skipped missing constructor {originalType.FullName}({FormatParameterTypes(parameterTypes)})");
				return;
			}

			object prefixMethod = CreateHarmonyMethod(prefix);
			object postfixMethod = CreateHarmonyMethod(postfix);
			_patchMethod.Invoke(_harmonyInstance, new[]
			{
				originalConstructor, prefixMethod, postfixMethod, null, null
			});
		}

		static object CreateHarmonyMethod(string patchMethodName)
		{
			if (string.IsNullOrEmpty(patchMethodName))
				return null;

			MethodInfo patchMethod = typeof(GameHarmony).GetMethod(patchMethodName, BindingFlagsAll);
			if (patchMethod == null)
				throw new MissingMethodException(typeof(GameHarmony).FullName, patchMethodName);

			return _harmonyMethodCtor.Invoke(new object[]
			{
				patchMethod
			});
		}

		static string FormatParameterTypes(Type[] parameterTypes)
		{
			if (parameterTypes == null || parameterTypes.Length == 0)
				return string.Empty;

			return string.Join(", ", parameterTypes.Select(type => type?.Name ?? "null").ToArray());
		}

		static void ApplyPatches()
		{
			Patch(typeof(Main), "Start", NoArguments, postfix: nameof(Postfix_Main_Start));
			Patch(typeof(Main), "Update", NoArguments, postfix: nameof(Postfix_Main_Update));
			Patch(typeof(Main), "FixedUpdate", NoArguments, postfix: nameof(Postfix_Main_FixedUpdate));
			Patch(typeof(Main), "OnApplicationPause", new[]
			{
				typeof(bool)
			}, postfix: nameof(Postfix_Main_OnApplicationPause));
			Patch(typeof(Main), "OnApplicationQuit", NoArguments, nameof(Prefix_Main_OnApplicationQuit));

			Patch(typeof(MotherCanvas), "checkZoomLevel", new[]
			{
				typeof(int), typeof(int)
			}, nameof(Prefix_MotherCanvas_CheckZoomLevel));
			Patch(typeof(Image), "createImage", new[]
			{
				typeof(string)
			}, nameof(Prefix_Image_CreateImage));
			Patch(typeof(Rms), "saveRMSString", new[]
			{
				typeof(string), typeof(string)
			}, nameof(Prefix_Rms_SaveRMSString));
			Patch(typeof(Rms), "GetiPhoneDocumentsPath", NoArguments, nameof(Prefix_Rms_GetIPhoneDocumentsPath));
			Patch(typeof(Rms), "clearAll", NoArguments, nameof(Prefix_Rms_ClearAll));
			Patch(typeof(ServerListScreen), "initCommand", NoArguments, nameof(Prefix_ServerListScreen_InitCommand));
			Patch(typeof(ServerListScreen), "loadIP", NoArguments, nameof(Prefix_ServerListScreen_LoadIP));
			Patch(typeof(ServerListScreen), "switchToMe", NoArguments, postfix: nameof(Postfix_ServerListScreen_SwitchToMe));
			Patch(typeof(ServerListScreen), "show2", NoArguments, postfix: nameof(Postfix_ServerListScreen_Show2));
			Patch(typeof(Service), "gotoPlayer", new[]
			{
				typeof(int)
			}, nameof(Prefix_Service_GotoPlayer));
			Patch(typeof(Service), "login", new[]
			{
				typeof(string), typeof(string), typeof(string), typeof(sbyte)
			}, nameof(Prefix_Service_Login));
			Patch(typeof(Service), "requestChangeMap", NoArguments, nameof(Prefix_Service_RequestChangeMap));
			Patch(typeof(Service), "chat", new[]
			{
				typeof(string)
			}, nameof(Prefix_Service_Chat));
			Patch(typeof(Service), "getMapOffline", NoArguments, nameof(Prefix_Service_GetMapOffline));
			Patch(typeof(Session_ME), "connect", new[]
			{
				typeof(string), typeof(int)
			}, nameof(Prefix_SessionME_Connect));
			Patch(typeof(Controller), "loadInfoMap", new[]
			{
				typeof(Message)
			}, postfix: nameof(Postfix_Controller_LoadInfoMap));

			Patch(typeof(GameScr), "setSkillBarPosition", NoArguments, nameof(Prefix_GameScr_SetSkillBarPosition));
			Patch(typeof(GameScr), "updateKey", NoArguments, nameof(Prefix_GameScr_UpdateKey));
			Patch(typeof(GameScr), "update", NoArguments, nameof(Prefix_GameScr_Update));
			Patch(typeof(GameScr), "paint", new[]
			{
				typeof(mGraphics)
			}, postfix: nameof(Postfix_GameScr_Paint));
			Patch(typeof(GameScr), "paintTouchControl", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GameScr_PaintTouchControl));
			Patch(typeof(GameScr), "paintImageBar", new[]
			{
				typeof(mGraphics), typeof(bool), typeof(Char)
			}, postfix: nameof(Postfix_GameScr_PaintImageBar));
			Patch(typeof(GameScr), "paintSelectedSkill", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GameScr_PaintSelectedSkill), nameof(Postfix_GameScr_PaintSelectedSkill));
			Patch(typeof(GameScr), "openUIZone", new[]
			{
				typeof(Message)
			}, nameof(Prefix_GameScr_OpenUIZone));
			Patch(typeof(GameScr), "paintGamePad", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GameScr_PaintGamePad));
			Patch(typeof(GameScr), "chatVip", new[]
			{
				typeof(string)
			}, nameof(Prefix_GameScr_ChatVip));

			Patch(typeof(ChatTextField), "startChat", new[]
			{
				typeof(int), typeof(IChatable), typeof(string)
			}, nameof(Prefix_ChatTextField_StartChat));
			Patch(typeof(ChatTextField), "update", NoArguments, nameof(Prefix_ChatTextField_Update));
			Patch(typeof(ChatTextField), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_ChatTextField_Paint));

			// Patch(typeof(GameCanvas), "paintBGGameScr", new[]
			// {
			// 	typeof(mGraphics)
			// }, nameof(Prefix_GameCanvas_PaintBGGameScr));
			Patch(typeof(GameCanvas), "keyPressedz", new[]
			{
				typeof(int)
			}, nameof(Prefix_GameCanvas_KeyPressed));
			Patch(typeof(GameCanvas), "keyReleasedz", new[]
			{
				typeof(int)
			}, nameof(Prefix_GameCanvas_KeyReleased));
			Patch(typeof(GameCanvas), "paint", new[]
			{
				typeof(mGraphics)
			}, postfix: nameof(Postfix_GameCanvas_Paint));
			Patch(typeof(GameCanvas), "startOKDlg", new[]
			{
				typeof(string)
			}, nameof(Prefix_GameCanvas_StartOKDlg));

			Patch(typeof(Mob), "update", NoArguments, nameof(Prefix_Mob_Update));
			Patch(typeof(Mob), "startDie", NoArguments, nameof(Prefix_Mob_StartDie));
			Patch(typeof(Teleport), "update", NoArguments, nameof(Prefix_Teleport_Update));

			Patch(typeof(Char), "addInfo", new[]
			{
				typeof(string)
			}, nameof(Prefix_Char_AddInfo));
			Patch(typeof(Char), "update", NoArguments, nameof(Prefix_Char_Update));
			Patch(typeof(Char), "setSkillPaint", new[]
			{
				typeof(SkillPaint), typeof(int)
			}, nameof(Prefix_Char_SetSkillPaint));
			Patch(typeof(Char), "setHoldChar", new[]
			{
				typeof(Char)
			}, postfix: nameof(Postfix_Char_SetHoldChar));
			Patch(typeof(Char), "setHoldMob", new[]
			{
				typeof(Mob)
			}, postfix: nameof(Postfix_Char_SetHoldMob));
			Patch(typeof(Char), "removeHoleEff", NoArguments, postfix: nameof(Postfix_Char_RemoveHoldEff));

			Patch(typeof(GamePad), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GamePad_Paint));
			Patch(typeof(Skill), "paint", new[]
			{
				typeof(int), typeof(int), typeof(mGraphics)
			}, nameof(Prefix_Skill_Paint));
			Patch(typeof(InfoMe), "addInfo", new[]
			{
				typeof(string), typeof(int)
			}, postfix: nameof(Postfix_InfoMe_AddInfo));
			Patch(typeof(mGraphics), "drawImage", new[]
			{
				typeof(Image), typeof(int), typeof(int), typeof(int)
			}, nameof(Prefix_mGraphics_DrawImage), nameof(Postfix_mGraphics_DrawImage));

			ApplyGraphicsReducerPatches();

			Patch(typeof(mResources), "loadLanguague", new[]
			{
				typeof(sbyte)
			}, postfix: nameof(Postfix_mResources_LoadLanguage));
			Patch(typeof(Menu), "startAt", new[]
			{
				typeof(MyVector), typeof(int)
			}, nameof(Prefix_Menu_StartAt));

			Patch(typeof(Panel), "updateKey", NoArguments, nameof(Prefix_Panel_UpdateKey));
			Patch(typeof(Panel), "updateKeyInTabBar", NoArguments, nameof(Prefix_Panel_UpdateKeyInTabBar));
			Patch(typeof(Panel), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_Panel_Paint), nameof(Postfix_Panel_Paint));
			Patch(typeof(Panel), "paintToolInfo", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_Panel_PaintToolInfo));
			Patch(typeof(Panel), "update", NoArguments, nameof(Prefix_Panel_Update));
			Patch(typeof(Panel), "doFireTool", NoArguments, nameof(Prefix_Panel_DoFireTool));
			Patch(typeof(Panel), "doFireOption", NoArguments, nameof(Prefix_Panel_DoFireOption));

			Patch(typeof(ItemMap), "setPoint", new[]
			{
				typeof(int), typeof(int)
			}, nameof(Prefix_ItemMap_SetPoint));
			Patch(typeof(ChatPopup), "addBigMessage", new[]
			{
				typeof(string), typeof(int), typeof(Npc)
			}, nameof(Prefix_ChatPopup_AddBigMessage));
			Patch(typeof(ChatPopup), "addChatPopupMultiLine", new[]
			{
				typeof(string), typeof(int), typeof(Npc)
			}, nameof(Prefix_ChatPopup_AddChatPopupMultiLine));
		}

		static void ApplyGraphicsReducerPatches()
		{
			Patch(typeof(ServerEffect), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_ServerEffect_Paint));
			Patch(typeof(Npc), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Npc_Paint));
			Patch(typeof(TileMap), "paintTilemap", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_TileMap_PaintTilemap));
			Patch(typeof(TileMap), "paintOutTilemap", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_TileMap_PaintOutTilemap));
			Patch(typeof(Mob), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Mob_Paint));
			Patch(typeof(MagicTree), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_MagicTree_Paint));
			Patch(typeof(ItemMap), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_ItemMap_Paint));
			Patch(typeof(InfoMe), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_InfoMe_Paint));
			Patch(typeof(GameScr), "paintBgItem", new[]
			{
				typeof(mGraphics), typeof(int)
			}, nameof(Prefix_GraphicsReducer_GameScr_PaintBgItem));
			Patch(typeof(GameScr), "paintEffect", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_GameScr_PaintEffect));
			Patch(typeof(Effect), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Effect_Paint));
			Patch(typeof(Char), "paintCharBody", new[]
			{
				typeof(mGraphics), typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool)
			}, nameof(Prefix_GraphicsReducer_Char_PaintCharBody));
			Patch(typeof(Char), "paint_map_line", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintMapLine));
			Patch(typeof(Char), "paintEffect", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintEffect));
			Patch(typeof(Char), "paintEff_Pet", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintEffPet));
			Patch(typeof(Char), "paintEff_Lvup_front", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintEffLvupFront));
			Patch(typeof(Char), "paintEff_Lvup_behind", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintEffLvupBehind));
			Patch(typeof(Char), "paintEffFront", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintEffFront));
			Patch(typeof(Char), "paintEffBehind", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintEffBehind));
			Patch(typeof(Char), "paintAuraFront", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintAuraFront));
			Patch(typeof(Char), "paintAuraBehind", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintAuraBehind));
			Patch(typeof(Char), "paintSuperEffFront", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintSuperEffFront));
			Patch(typeof(Char), "paintSuperEffBehind", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintSuperEffBehind));
			Patch(typeof(Char), "paintMount2", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintMount2));
			Patch(typeof(Char), "paintMount1", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_PaintMount1));
			Patch(typeof(Char), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_Char_Paint));
			Patch(typeof(Char), "updateSuperEff", NoArguments, nameof(Prefix_GraphicsReducer_Char_UpdateSuperEff));
			Patch(typeof(BgItem), "paint", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_BgItem_Paint));
			Patch(typeof(BackgroudEffect), "addEffect", new[]
			{
				typeof(int)
			}, nameof(Prefix_GraphicsReducer_BackgroundEffect_AddEffect));
			Patch(typeof(BackgroudEffect), "paintFog", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_BackgroundEffect_PaintFog));
			Patch(typeof(BackgroudEffect), "paintCloud2", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_BackgroundEffect_PaintCloud2));
			Patch(typeof(BackgroudEffect), "updateFog", NoArguments, nameof(Prefix_GraphicsReducer_BackgroundEffect_UpdateFog));
			Patch(typeof(BackgroudEffect), "updateCloud2", NoArguments, nameof(Prefix_GraphicsReducer_BackgroundEffect_UpdateCloud2));
			Patch(typeof(BackgroudEffect), "initCloud", NoArguments, nameof(Prefix_GraphicsReducer_BackgroundEffect_InitCloud));
			Patch(typeof(GameCanvas), "paintBGGameScr", new[]
			{
				typeof(mGraphics)
			}, nameof(Prefix_GraphicsReducer_GameCanvas_PaintBGGameScr));
		}

		static void Postfix_Main_Start()
		{
			GameEvents.OnMainStart();
		}
		static void Postfix_Main_Update()
		{
			GameEvents.OnUpdateMain();
		}
		static void Postfix_Main_FixedUpdate()
		{
			GameEvents.OnFixedUpdateMain();
		}
		static void Postfix_Main_OnApplicationPause(bool __0)
		{
			GameEvents.OnGamePause(__0);
		}
		static void Prefix_Main_OnApplicationQuit()
		{
			GameEvents.OnGameClosing();
		}

		static bool Prefix_MotherCanvas_CheckZoomLevel(int __0, int __1)
		{
			return !GameEvents.OnCheckZoomLevel(__0, __1);
		}

		static bool Prefix_Image_CreateImage(string __0, ref Image __result)
		{
			if (!GameEvents.OnCreateImage(__0, out Image image))
				return true;

			__result = image;
			return false;
		}

		static void Prefix_Rms_SaveRMSString(ref string __0, ref string __1)
		{
			GameEvents.OnSaveRMSString(ref __0, ref __1);
		}

		static bool Prefix_Rms_GetIPhoneDocumentsPath(ref string __result)
		{
			if (!GameEvents.OnGetRMSPath(out string result))
				return true;

			__result = result;
			return false;
		}

		static bool Prefix_Rms_ClearAll()
		{
			return !GameEvents.OnClearAllRMS();
		}
		static bool Prefix_ServerListScreen_InitCommand(ServerListScreen __instance)
		{
			return !GameEvents.OnServerListScreenInitCommand(__instance);
		}
		static void Prefix_ServerListScreen_LoadIP()
		{
			GameEvents.OnLoadIP();
		}
		static void Postfix_ServerListScreen_SwitchToMe(ServerListScreen __instance)
		{
			GameEvents.OnServerListScreenLoaded(__instance);
		}
		static void Postfix_ServerListScreen_Show2()
		{
			GameEvents.OnScreenDownloadDataShow();
		}

		static bool Prefix_Service_GotoPlayer(int __0)
		{
			return !GameEvents.OnGotoPlayer(__0);
		}
		static void Prefix_Service_Login(ref string __0, ref string __1, string __2, ref sbyte __3)
		{
			GameEvents.OnLogin(ref __0, ref __1, ref __3);
		}
		static bool Prefix_Service_RequestChangeMap()
		{
			return !GameEvents.OnRequestChangeMap();
		}
		static bool Prefix_Service_Chat(string __0)
		{
			return !GameEvents.OnSendChat(__0);
		}
		static bool Prefix_Service_GetMapOffline()
		{
			return !GameEvents.OnGetMapOffline();
		}

		static void Prefix_SessionME_Connect(ref string __0, ref int __1)
		{
			GameEvents.OnSessionConnecting(ref __0, ref __1);
		}
		static void Postfix_Controller_LoadInfoMap()
		{
			GameEvents.OnInfoMapLoaded();
		}

		static bool Prefix_GameScr_SetSkillBarPosition()
		{
			return !GameEvents.OnSetSkillBarPosition();
		}

		static bool Prefix_GameScr_UpdateKey(GameScr __instance)
		{
			if (!Controller.isStopReadMessage && !Char.myCharz().isTeleport && !Char.myCharz().isPaintNewSkill && !InfoDlg.isLock)
			{
				if (GameCanvas.isTouch && !ChatTextField.gI().isShow && !GameCanvas.menu.showMenu && !__instance.isNotPaintTouchControl())
				{
					if (GameEvents.OnUpdateTouchGameScr(__instance))
						return false;
				}

				if ((!ChatTextField.gI().isShow || GameCanvas.keyAsciiPress == 0) &&
				    !__instance.isLockKey &&
				    !GameCanvas.menu.showMenu &&
				    !__instance.isOpenUI() &&
				    !Char.isLockKey &&
				    Char.myCharz().skillPaint == null &&
				    GameCanvas.keyAsciiPress != 0 &&
				    __instance.mobCapcha == null &&
				    TField.isQwerty)
				{
					GameEvents.OnGameScrPressHotkeys();
					if (!IsAssignedSkillHotkeyPressed() && GameCanvas.keyAsciiPress != 114 && GameCanvas.keyAsciiPress != 47)
						GameEvents.OnGameScrPressHotkeysUnassigned();
				}
			}

			return true;
		}

		static bool IsAssignedSkillHotkeyPressed()
		{
			return GameCanvas.keyPressed[0] || GameCanvas.keyPressed[1] || GameCanvas.keyPressed[2] || GameCanvas.keyPressed[3] ||
			       GameCanvas.keyPressed[4] || GameCanvas.keyPressed[5] || GameCanvas.keyPressed[6] || GameCanvas.keyPressed[7] ||
			       GameCanvas.keyPressed[8] || GameCanvas.keyPressed[9];
		}

		static void Prefix_GameScr_Update()
		{
			GameEvents.OnUpdateGameScr();
		}
		static void Postfix_GameScr_Paint(mGraphics __0)
		{
			GameEvents.OnPaintGameScr(__0);
		}
		static bool Prefix_GameScr_PaintTouchControl(GameScr __instance, mGraphics __0)
		{
			return !GameEvents.OnPaintTouchControl(__instance, __0);
		}
		static void Postfix_GameScr_PaintImageBar(mGraphics __0, bool __1, Char __2)
		{
			GameEvents.OnPaintImageBar(__0, __1, __2);
		}

		static void Prefix_GameScr_PaintSelectedSkill(GameScr __instance, mGraphics __0)
		{
			if (!Char.myCharz().IsCharDead())
				GameEvents.OnGameScrPaintSelectedSkill(__instance, __0);
		}

		static void Postfix_GameScr_PaintSelectedSkill(GameScr __instance, mGraphics __0)
		{
			if (Char.myCharz().IsCharDead())
				return;

			if (__instance.mobCapcha == null &&
			    (GameCanvas.currentDialog != null ||
			     ChatPopup.currChatPopup != null ||
			     GameCanvas.menu.showMenu ||
			     __instance.isPaintPopup() ||
			     GameCanvas.panel.isShow ||
			     Char.myCharz().taskMaint.taskId == 0 ||
			     ChatTextField.gI().isShow ||
			     GameCanvas.currentScreen == MoneyCharge.instance))
			{
				return;
			}

			GameEvents.AfterGameScrPaintSelectedSkill(__instance, __0);
		}

		static bool Prefix_GameScr_OpenUIZone(GameScr __instance, Message __0)
		{
			return !GameEvents.OnOpenUIZone(__instance, __0);
		}
		static bool Prefix_GameScr_PaintGamePad(mGraphics __0)
		{
			return !GameEvents.OnGameScrPaintGamePad(__0);
		}
		static void Prefix_GameScr_ChatVip(string __0)
		{
			GameEvents.OnChatVip(__0);
		}

		static bool Prefix_ChatTextField_StartChat(ChatTextField __instance, int __0, IChatable __1, string __2)
		{
			return !GameEvents.OnStartChatTextField(__instance, __1);
		}

		static void Prefix_ChatTextField_Update(ChatTextField __instance)
		{
			if (!__instance.isShow)
				GameEvents.OnUpdateChatTextField(__instance);
		}

		static void Prefix_ChatTextField_Paint(ChatTextField __instance, mGraphics __0)
		{
			GameEvents.OnPaintChatTextField(__instance, __0);
		}

		// static bool Prefix_GameCanvas_PaintBGGameScr(mGraphics __0) => !GameEvents.OnPaintBgGameScr(__0);
		static bool Prefix_GameCanvas_KeyPressed(int __0)
		{
			return !GameEvents.OnKeyPressed(__0, false);
		}
		static bool Prefix_GameCanvas_KeyReleased(int __0)
		{
			return !GameEvents.OnKeyReleased(__0, false);
		}
		static void Postfix_GameCanvas_Paint(GameCanvas __instance, mGraphics __0)
		{
			GameEvents.OnPaintGameCanvas(__instance, __0);
		}
		static bool Prefix_GameCanvas_StartOKDlg(string __0)
		{
			return !GameEvents.OnStartOKDlg(__0);
		}

		static void Prefix_Mob_Update(Mob __instance)
		{
			GameEvents.OnUpdateMob(__instance);
		}
		static void Prefix_Mob_StartDie(Mob __instance)
		{
			GameEvents.OnMobStartDie(__instance);
		}
		static void Prefix_Teleport_Update(Teleport __instance)
		{
			GameEvents.OnTeleportUpdate(__instance);
		}

		static void Prefix_Char_AddInfo(Char __instance, string __0)
		{
			GameEvents.OnAddInfoChar(__instance, __0);
		}
		static void Prefix_Char_Update(Char __instance)
		{
			GameEvents.OnUpdateChar(__instance);
		}
		static bool Prefix_Char_SetSkillPaint(Char __instance)
		{
			return !GameEvents.OnUseSkill(__instance);
		}
		static void Postfix_Char_SetHoldChar(Char __instance, Char __0)
		{
			GameEvents.OnCharSetHoldChar(__instance, __0);
		}
		static void Postfix_Char_SetHoldMob(Char __instance)
		{
			GameEvents.OnCharSetHoldMob(__instance);
		}
		static void Postfix_Char_RemoveHoldEff(Char __instance)
		{
			GameEvents.OnCharRemoveHoldEff(__instance);
		}

		static bool Prefix_GamePad_Paint(GamePad __instance, mGraphics __0)
		{
			return !GameEvents.OnGamepadPaint(__instance, __0);
		}
		static bool Prefix_Skill_Paint(Skill __instance, int __0, int __1, mGraphics __2)
		{
			return !GameEvents.OnSkillPaint(__instance, __0, __1, __2);
		}
		static void Postfix_InfoMe_AddInfo(string __0)
		{
			GameEvents.OnAddInfoMe(__0);
		}

		static bool Prefix_mGraphics_DrawImage(Image __0, int __1, int __2, int __3)
		{
			return !GameEvents.OnMGraphicsDrawImage(__0, __1, __2, __3);
		}
		static void Postfix_mGraphics_DrawImage(Image __0, int __1, int __2, int __3)
		{
			GameEvents.AfterMGraphicsDrawImage(__0, __1, __2, __3);
		}
		static void Postfix_mResources_LoadLanguage(sbyte __0)
		{
			GameEvents.OnLoadLanguage(__0);
		}
		static bool Prefix_Menu_StartAt(MyVector __0)
		{
			return !GameEvents.OnMenuStartAt(__0);
		}

		static void Prefix_Panel_UpdateKey(Panel __instance)
		{
			if ((__instance.tabIcon == null || !__instance.tabIcon.isShow) && !__instance.isClose && __instance.isShow && !__instance.cmdClose.isPointerPressInside())
				GameEvents.OnUpdateTouchPanel(__instance);
		}

		static bool Prefix_Panel_UpdateKeyInTabBar(Panel __instance)
		{
			return !GameEvents.OnPanelUpdateKeyInTabBar(__instance);
		}
		static bool Prefix_Panel_Paint(Panel __instance, mGraphics __0)
		{
			return !GameEvents.OnPaintPanel(__instance, __0);
		}

		static void Postfix_Panel_Paint(Panel __instance, mGraphics __0)
		{
			if (GameCanvas.panel.combineSuccess == -1)
				GameEvents.OnAfterPaintPanel(__instance, __0);
		}

		static bool Prefix_Panel_PaintToolInfo(mGraphics __0)
		{
			return !GameEvents.OnPanelPaintToolInfo(__0);
		}
		static bool Prefix_Panel_Update(Panel __instance)
		{
			return !GameEvents.OnUpdatePanel(__instance);
		}
		static bool Prefix_Panel_DoFireTool(Panel __instance)
		{
			return !GameEvents.OnPanelFireTool(__instance);
		}
		static bool Prefix_Panel_DoFireOption(Panel __instance)
		{
			return !GameEvents.OnPanelFireOption(__instance);
		}

		static void Prefix_ItemMap_SetPoint(int __0, int __1)
		{
			GameEvents.OnSetPointItemMap(__0, __1);
		}
		static bool Prefix_ChatPopup_AddBigMessage(string __0, int __1, Npc __2)
		{
			return !GameEvents.OnAddBigMessage(__0, __2);
		}
		static bool Prefix_ChatPopup_AddChatPopupMultiLine(string __0)
		{
			return !GameEvents.OnChatPopupMultiLine(__0);
		}

		static bool Prefix_GraphicsReducer_ServerEffect_Paint(ServerEffect __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnServerEffectPaint();
		}

		static bool Prefix_GraphicsReducer_Npc_Paint(Npc __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnNpcPaint(__instance, __0);
		}

		static bool Prefix_GraphicsReducer_TileMap_PaintTilemap(mGraphics __0)
		{
			return !GraphicsReducer.OnTileMapPaintTile(__0);
		}

		static bool Prefix_GraphicsReducer_TileMap_PaintOutTilemap(mGraphics __0)
		{
			return !GraphicsReducer.OnTileMapPaintOutTile();
		}

		static bool Prefix_GraphicsReducer_Mob_Paint(Mob __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnMobPaint(__instance, __0);
		}

		static bool Prefix_GraphicsReducer_MagicTree_Paint(MagicTree __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnMagicTreePaint(__instance, __0);
		}

		static bool Prefix_GraphicsReducer_ItemMap_Paint(ItemMap __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnItemMapPaint(__instance, __0);
		}

		static bool Prefix_GraphicsReducer_InfoMe_Paint(InfoMe __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnInfoMePaint(__instance, __0);
		}

		static bool Prefix_GraphicsReducer_GameScr_PaintBgItem(GameScr __instance, mGraphics __0, int __1)
		{
			return !GraphicsReducer.OnGameScrPaintBgItem();
		}

		static bool Prefix_GraphicsReducer_GameScr_PaintEffect(GameScr __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnGameScrPaintEffect();
		}

		static bool Prefix_GraphicsReducer_Effect_Paint(Effect __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnEffectPaint();
		}

		static bool Prefix_GraphicsReducer_Char_PaintCharBody(Char __instance, mGraphics __0, int __1, int __2, int __3, int __4, bool __5)
		{
			return !GraphicsReducer.OnCharPaintCharBody(__instance, __0, __1, __2, __3, __5);
		}

		static bool Prefix_GraphicsReducer_Char_PaintMapLine(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintMapLine();
		}

		static bool Prefix_GraphicsReducer_Char_PaintEffect(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintEffect();
		}

		static bool Prefix_GraphicsReducer_Char_PaintEffPet(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintEff_Pet();
		}

		static bool Prefix_GraphicsReducer_Char_PaintEffLvupFront(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintEff_LvUp_Front();
		}

		static bool Prefix_GraphicsReducer_Char_PaintEffLvupBehind(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintEff_LvUp_Behind();
		}

		static bool Prefix_GraphicsReducer_Char_PaintEffFront(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintEffFront();
		}

		static bool Prefix_GraphicsReducer_Char_PaintEffBehind(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintEffBehind();
		}

		static bool Prefix_GraphicsReducer_Char_PaintAuraFront(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintAuraFront();
		}

		static bool Prefix_GraphicsReducer_Char_PaintAuraBehind(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintAuraBehind();
		}

		static bool Prefix_GraphicsReducer_Char_PaintSuperEffFront(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintSuperEffFront();
		}

		static bool Prefix_GraphicsReducer_Char_PaintSuperEffBehind(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintSuperEffBehind();
		}

		static bool Prefix_GraphicsReducer_Char_PaintMount2(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintMount2();
		}

		static bool Prefix_GraphicsReducer_Char_PaintMount1(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaintMount1(__instance, __0);
		}

		static bool Prefix_GraphicsReducer_Char_Paint(Char __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnCharPaint();
		}

		static bool Prefix_GraphicsReducer_Char_UpdateSuperEff(Char __instance)
		{
			return !GraphicsReducer.OnCharUpdateSuperEff();
		}

		static bool Prefix_GraphicsReducer_BgItem_Paint(BgItem __instance, mGraphics __0)
		{
			return !GraphicsReducer.OnBgItemPaint();
		}

		static bool Prefix_GraphicsReducer_BackgroundEffect_AddEffect(int __0)
		{
			return !GraphicsReducer.OnBackgroundEffectAddEffect();
		}

		static bool Prefix_GraphicsReducer_BackgroundEffect_PaintFog(mGraphics __0)
		{
			return !GraphicsReducer.OnBackgroundEffectPaintFog();
		}

		static bool Prefix_GraphicsReducer_BackgroundEffect_PaintCloud2(mGraphics __0)
		{
			return !GraphicsReducer.OnBackgroundEffectPaintCloud2();
		}

		static bool Prefix_GraphicsReducer_BackgroundEffect_UpdateFog()
		{
			return !GraphicsReducer.OnBackgroundEffectUpdateFog();
		}

		static bool Prefix_GraphicsReducer_BackgroundEffect_UpdateCloud2()
		{
			return !GraphicsReducer.OnBackgroundEffectUpdateCloud2();
		}

		static bool Prefix_GraphicsReducer_BackgroundEffect_InitCloud()
		{
			return !GraphicsReducer.OnBackgroundEffectInitCloud();
		}

		static bool Prefix_GraphicsReducer_GameCanvas_PaintBGGameScr(mGraphics __0)
		{
			return !GraphicsReducer.OnPaintBgGameScr(__0);
		}
	}
}
