# GameEvents — Mod Entry Point Documentation

`GameEvents.cs` is the central hub for the game mod. It contains **static event handler methods** that are called from various points in the original game code, allowing the mod to inject custom behavior without modifying the original game logic directly.

## How Hooking Works

The mod hooks into the original game in two ways:

1. **Direct calls** — The original game source files are modified to call `GameEvents.OnXxx()` at strategic points.
2. **IL Weaving (build-time)** — The `GameEventHook.cs` file (currently commented out as it is processed at build time) defines the mapping between original methods and their hook wrappers using `TryInstallHook<>()`.

---

## Game Events Reference

### Lifecycle Events

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnAwake()` | `ModScript.cs` | `Awake()` | Unity MonoBehaviour Awake — sets frame rate, background run, and time scale |
| `OnGameStart()` | `ModScript.cs` | `Start()` | Unity MonoBehaviour Start — initializes all mod subsystems (settings, auto-skill, auto-pean, char effects, commands, graphics, etc.) |
| `OnMainStart()` | `Main.cs` | `Start()` | Called when the Main game object starts — initializes UI images and default server list |
| `OnGamePause(bool)` | `Main.cs` | `OnApplicationPause(bool)` | Triggered when the app is paused/resumed — manages in-game account manager state |
| `OnGameClosing()` | `Main.cs` | `OnApplicationQuit()` | Called when the game is closing — saves teleport data, closes sessions, clears reporters |
| `OnFixedUpdateMain()` | `Main.cs` | `FixedUpdate()` | Fixed update loop — handles panel2 rendering and input when main panel is hidden |
| `OnUpdateMain()` | `Main.cs` | `Update()` | Main update loop — detects resolution changes and updates UI scaling |

### Resolution & Display

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnSetResolution()` | `MotherCanvas.cs` | (resolution setup) | Applies saved resolution settings (width, height, fullscreen) on non-mobile platforms |
| `OnCheckZoomLevel(int w, int h)` | `MotherCanvas.cs` | `checkZoomLevel(int, int)` | Overrides the zoom level calculation based on screen resolution |

### Language & Localization

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnLoadLanguage(sbyte)` | `mResources.cs` | `loadLanguague(sbyte)` | Loads custom mod strings when the game language changes |

### File I / RMS Storage

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnSaveRMSString(ref string, ref string)` | `Rms.cs` | `saveRMSString(string, string)` | Intercepts RMS string saves — prevents saving empty user Ao data |
| `OnRmsFileIo(Action)` | `Rms.cs` | Various RMS methods | Wraps RMS file I/O operations with cross-process data locking |
| `OnGetRMSPath(out string)` | `Rms.cs` | `GetiPhoneDocumentsPath()` | Overrides the RMS data directory path to use the mod's custom data folder |
| `OnClearAllRMS()` | `Rms.cs` | `clearAll()` | Custom RMS clear logic — preserves `.log` files and `isPlaySound` |

### Server & Connection

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnServerListScreenLoaded(ServerListScreen)` | `ServerListScreen.cs` | `show2()` | Called when the server list screen loads — initializes mod menu and teleport data |
| `OnSessionConnecting(ref string, ref int)` | `Session_ME.cs` | `connect(string, int)` | Intercepts server connection — allows custom server IP/port from account manager |
| `OnLogin(ref string, ref string, ref sbyte)` | `Service.cs` | `login(string, string, string, sbyte)` | Overrides login credentials — supports in-game account manager and external account manager |
| `OnServerListScreenInitCommand(ServerListScreen)` | `ServerListScreen.cs` | `switchToMe()` | Customizes the server list screen commands — adds account manager button |
| `OnLoadIP()` | `ServerListScreen.cs` | `loadIP()` | Loads server IP list from custom source |

### Chat & Communication

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnSendChat(string)` | `Service.cs` | `chat(string)` | Intercepts chat messages — allows chat commands (e.g. `/help`) |
| `OnChatPopupMultiLine(string)` | `ChatPopup.cs` | `addChatPopupMultiLine(string, int, Npc)` | Handles multi-line chat popups — processes BoMong messages |
| `OnAddBigMessage(string, Npc)` | `ChatPopup.cs` | `addBigMessage(string, int, Npc)` | Intercepts big NPC messages |
| `OnChatVip(string)` | `GameScr.cs` | `chatVip(string)` | Processes VIP chat messages for boss detection |
| `OnStartChatTextField(ChatTextField, IChatable)` | `ChatTextField.cs` | `startChat(IChatable, string)` | Called when chat text field starts |
| `OnUpdateChatTextField(ChatTextField)` | `ChatTextField.cs` | `update()` | Updates chat text field — prevents Enter key from being consumed |
| `OnPaintChatTextField(ChatTextField, mGraphics)` | `ChatTextField.cs` | `paint(mGraphics)` | Custom painting for chat text field |

### Input & Controls

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnKeyPressed(int, bool)` | `GameCanvas.cs` | `keyPressedz(int)` | Intercepts key press events — supports key sync across instances |
| `OnKeyReleased(int, bool)` | `GameCanvas.cs` | `keyReleasedz(int)` | Intercepts key release events — supports key sync across instances |
| `OnGameScrPressHotkeysUnassigned()` | `GameScr.cs` | `updateKey()` (unassigned hotkeys) | Handles unassigned hotkey presses — routes to HotkeyCommandHandler |
| `OnGameScrPressHotkeys()` | `GameScr.cs` | `updateKey()` (assigned hotkeys) | Handles assigned hotkey presses |

### Game World & Map

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnUpdateGameScr()` | `GameScr.cs` | `update()` | Main game update — updates account info, pet info, zone info, runs all mod subsystems (Boss, ListCharsInMap, CharEffect, TeleportMenu, AutoTrainPet, AutoSellTrashItems, DelayedAction, AutoUseItem) |
| `OnInfoMapLoaded()` | `Controller.cs` | `loadInfoMap(Message)` | Called when map info is loaded — updates waypoint data |
| `OnOpenUIZone(GameScr, Message)` | `Service.cs` | `openUIZone()` | Parses zone UI data from server message |
| `OnStartOKDlg(string)` | `GameScr.cs` | (OK dialog) | Intercepts OK dialogs — suppresses "can't change zone" dialog |
| `OnRequestChangeMap()` | `GameScr.cs` | (change map request) | Handles map change requests |
| `OnGetMapOffline()` | `GameScr.cs` | (offline map) | Handles offline map loading |

### Character & Combat

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnUseSkill(Char)` | `Char.cs` | `setSkillPaint(SkillPaint, int)` | Triggered when a skill is used — adds char effect for the skill |
| `OnUpdateChar(Char)` | `Char.cs` | `update()` | Per-character update — tracks character effects |
| `OnCharRemoveHoldEff(Char)` | `Char.cs` | (hold effect removed) | Removes hold effect tracking when character's hold is cleared |
| `OnCharSetHoldChar(Char, Char)` | `Char.cs` | (hold char set) | Tracks when a character holds another character |
| `OnCharSetHoldMob(Char)` | `Char.cs` | (hold mob set) | Tracks when a character holds a mob |
| `OnAddInfoChar(Char, string)` | `Char.cs` | `addInfo(string)` | Intercepts character info messages — detects pet training messages |
| `OnAddInfoMe(string)` | `InfoMe.cs` | `addInfo(string, int)` | Processes info messages — handles Xmap info, grape auto-use, and BoMong messages |

### Mob Events

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnMobStartDie(Mob)` | `Mob.cs` | `startDie()` | Called when a mob starts dying |
| `OnUpdateMob(Mob)` | `Mob.cs` | `update()` | Per-mob update loop |

### Rendering & Graphics

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnPaintGameScr(mGraphics)` | `GameScr.cs` | `paint(mGraphics)` | Custom game screen painting — draws mod menu, char effects, boss lines |
| `OnPaintGameCanvas(GameCanvas, mGraphics)` | `GameCanvas.cs` | `paint(mGraphics)` | Custom game canvas painting — renders panel2 and FPS counter |
| `OnPaintImageBar(mGraphics, bool, Char)` | `GameScr.cs` | (image bar painting) | Custom HP/MP bar text rendering |
| `OnCreateImage(string, out Image)` | `Image.cs` | `createImage(string)` | Overrides image loading — supports custom assets from mod directory |
| `OnMGraphicsDrawImage(Image, int, int, int)` | `mGraphics.cs` | `drawImage(Image, int, int, int)` | Intercepts image drawing — supports HideGameUI and GraphicsReducer |
| `AfterMGraphicsDrawImage(Image, int, int, int)` | `mGraphics.cs` | `drawImage(Image, int, int, int)` | Post-image drawing callback |
| `OnSetSkillBarPosition()` | `GameScr.cs` | `setSkillBarPosition()` | Overrides skill bar position layout |
| `OnGameScrPaintSelectedSkill(GameScr, mGraphics)` | `GameScr.cs` | `paintSelectedSkill(mGraphics)` | Custom selected skill painting — hides skill UI when HideGameUI is enabled |
| `AfterGameScrPaintSelectedSkill(GameScr, mGraphics)` | `GameScr.cs` | `paintSelectedSkill(mGraphics)` | Post-selected skill painting callback |
| `OnSkillPaint(Skill, int, int, mGraphics)` | `Skill.cs` | `paint(int, int, mGraphics)` | Custom skill icon painting — adds cooldown overlay |
| `OnPaintTouchControl(GameScr, mGraphics)` | `GameScr.cs` | `paintTouchControl(mGraphics)` | Custom touch control painting |
| `OnGameScrPaintGamePad(mGraphics)` | `GameScr.cs` | `paintGamePad(mGraphics)` | Custom game pad painting |
| `OnGamepadPaint(GamePad, mGraphics)` | `GamePad.cs` | `paint(mGraphics)` | Custom gamepad painting |

### Panel & UI

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnPaintPanel(Panel, mGraphics)` | `Panel.cs` | `paint(mGraphics)` | Custom panel painting — full custom inventory panel with mod features |
| `OnAfterPaintPanel(Panel, mGraphics)` | `Panel.cs` | `paint(mGraphics)` | Post-panel painting — adds item option highlights for trade, shop, box, combine, and inventory panels |
| `OnUpdatePanel(Panel)` | `Panel.cs` | `update()` | Custom panel update — handles custom panel menu actions |
| `OnPanelUpdateKeyInTabBar(Panel)` | `Panel.cs` | `updateKeyInTabBar()` | Custom tab bar key handling for inventory panels |
| `OnUpdateTouchPanel(Panel)` | `Panel.cs` | `updateKey()` (touch) | Custom touch panel update — handles scroll view for custom panels |
| `OnUpdateTouchGameScr(GameScr)` | `GameScr.cs` | `updateTouch()` | Custom touch input handling — supports custom panels, boss, list chars, mod menu, and touch controls |
| `OnPanelFireOption(Panel)` | `Panel.cs` | `doFireOption()` | Custom option panel fire — plays sound effects for each option |
| `OnSoundMnGetStrOption()` | `SoundMn.cs` | `getStrOption()` | Customizes the option panel strings |
| `OnPanelFireTool(Panel)` | `Panel.cs` | (tool panel fire) | Custom tool panel actions — zone change and logout |
| `OnGetSoundOption()` | `SoundMn.cs` | `getStrOption()` | Customizes the tool menu strings — adds register protect option |
| `OnPanelPaintToolInfo(mGraphics)` | `Panel.cs` | `paintToolInfo(mGraphics)` | Custom tool info panel — displays mod version, character name, and server info |

### Teleport

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnTeleportUpdate(Teleport)` | `Teleport.cs` | `update()` | Intercepts teleport update — supports SpaceshipSkip feature |

### Item & Menu

| GameEvents Method | Original File | Original Method | Description |
|---|---|---|---|
| `OnSetPointItemMap(int, int)` | `ItemMap.cs` | `setPoint(int, int)` | Intercepts item map point setting |
| `OnMenuStartAt(MyVector)` | `Menu.cs` | `startAt(MyVector, int)` | Intercepts menu start |
| `OnGotoPlayer(int, bool)` | `Service.cs` | `gotoPlayer(int)` | Intercepts goto player — (currently disabled) |

---

## Original Files Modified for Hooking

The following original game source files have been modified to include `GameEvents.OnXxx()` calls:

| File | Events Hooked |
|---|---|
| `Main.cs` | `OnMainStart`, `OnFixedUpdateMain`, `OnUpdateMain`, `OnGameClosing`, `OnGamePause` |
| `MotherCanvas.cs` | `OnCheckZoomLevel` |
| `mResources.cs` | `OnLoadLanguage` |
| `mGraphics.cs` | `OnMGraphicsDrawImage`, `AfterMGraphicsDrawImage` |
| `Rms.cs` | `OnSaveRMSString`, `OnRmsFileIo`, `OnGetRMSPath`, `OnClearAllRMS` |
| `Image.cs` | `OnCreateImage` |
| `ServerListScreen.cs` | `OnServerListScreenLoaded`, `OnSessionConnecting`, `OnServerListScreenInitCommand`, `OnLoadIP` |
| `Session_ME.cs` | `OnSessionConnecting` |
| `Service.cs` | `OnLogin`, `OnSendChat`, `OnGotoPlayer`, `OnOpenUIZone` |
| `GameCanvas.cs` | `OnKeyPressed`, `OnKeyReleased`, `OnPaintGameCanvas` |
| `GameScr.cs` | `OnUpdateGameScr`, `OnPaintGameScr`, `OnUpdateTouchGameScr`, `OnSetSkillBarPosition`, `OnGameScrPaintSelectedSkill`, `AfterGameScrPaintSelectedSkill`, `OnPaintImageBar`, `OnStartOKDlg`, `OnRequestChangeMap`, `OnGetMapOffline`, `OnGameScrPressHotkeysUnassigned`, `OnGameScrPressHotkeys`, `OnPaintTouchControl`, `OnGameScrPaintGamePad` |
| `ChatTextField.cs` | `OnStartChatTextField`, `OnUpdateChatTextField`, `OnPaintChatTextField` |
| `ChatPopup.cs` | `OnChatPopupMultiLine`, `OnAddBigMessage` |
| `Controller.cs` | `OnInfoMapLoaded` |
| `Char.cs` | `OnUseSkill`, `OnUpdateChar`, `OnCharRemoveHoldEff`, `OnCharSetHoldChar`, `OnCharSetHoldMob`, `OnAddInfoChar` |
| `InfoMe.cs` | `OnAddInfoMe` |
| `Mob.cs` | `OnMobStartDie`, `OnUpdateMob` |
| `Panel.cs` | `OnPaintPanel`, `OnAfterPaintPanel`, `OnUpdatePanel`, `OnPanelUpdateKeyInTabBar`, `OnUpdateTouchPanel`, `OnPanelFireOption`, `OnPanelFireTool`, `OnPanelPaintToolInfo` |
| `SoundMn.cs` | `OnSoundMnGetStrOption`, `OnGetSoundOption` |
| `Teleport.cs` | `OnTeleportUpdate` |
| `ItemMap.cs` | `OnSetPointItemMap` |
| `Menu.cs` | `OnMenuStartAt` |
| `Skill.cs` | `OnSkillPaint` |
| `GamePad.cs` | `OnGamepadPaint` |
| `ModScript.cs` | `OnAwake`, `OnGameStart` |

## Mod Subsystems Initialized by GameEvents

`OnGameStart()` initializes the following mod subsystems:

- **YardatSettings** — Yardat (auto-pathing) settings
- **AutoPean** — Auto-peanut collection
- **CharEffectMain** — Character visual effects
- **ChatCommandHandler** — Chat-based commands
- **HotkeyCommandHandler** — Keyboard hotkey commands
- **GraphicsReducer** — Tile map graphics optimization
- **PickMobConfig** — Mob picking configuration
- **SpaceshipSkip** — Teleport skip feature
- **InGameAccountManager** — In-game account management
- **AutoSkill** — Auto-skill system
- **UIReportersManager** — Boss and ListCharsInMap reporters
- **GameLauncherClient** — External launcher integration

`OnUpdateGameScr()` runs these mod subsystems every frame:

- **Boss** — Boss tracking and painting
- **ListCharsInMap** — Character listing in map
- **CharEffectMain** — Character effect updates
- **TeleportMenuMain** — Teleport menu updates
- **AutoTrainPet** — Auto pet training
- **AutoSellTrashItems** — Auto-sell trash items
- **DelayedAction** — Delayed action queue
- **AutoUseItem** — Auto-use items
