# DragonBoy247 Agent Guide

A legacy 2D networked MMORPG client (ported from J2ME) running on **Unity 2022.3.62f3** with custom graphics rendering and TCP-based game protocol.

## Architecture Overview

### Core Entry Point & Singletons
- **Main.cs**: MonoBehaviour singleton. Initializes game state, manages platform detection (iOS/Android/PC), stores global static references (`Main.g`, `Main.midlet`, `Main.isPC`, etc.)
- **GameMidlet.cs**: Configuration hub - server IP, port, version tracking (intVERSION=247, VERSION="2.4.7"), initializes MotherCanvas and GameCanvas
- **GameScr.cs** (8009 lines): Main game screen/loop. Handles in-game rendering, position tracking (gW, gH for scaled dimensions), UI panels
- **Service.cs** (3275 lines): Game logic coordinator. Singleton via `Service.gI()`. Routes game commands to network, manages player actions
- **Controller.cs** (6787 lines): Implements `IMessageHandler`. Processes incoming network messages, manages game state transitions

### Networking Layer
**Custom TCP Protocol** using `sbyte[] (signed byte arrays)` - not standard JSON/Protobuf:

- **Session_ME** & **Session_ME2**: Dual-session architecture (main + secondary backup). Each:
  - Manages TcpClient connection and threading (`sendThread`, `collectorThread`)
  - Encrypts/decrypts with `readKey()`/`writeKey()` 
  - Dispatches messages to `IMessageHandler` (Controller)
  
- **Message class**: Command ID (sbyte) + binary payload via `myWriter`/`myReader`
  ```csharp
  Message msg = new Message((sbyte)18);  // command ID
  msg.writer().writeInt(playerId);       // custom binary serialization
  session.sendMessage(msg);
  ```

- **myReader/myWriter**: Big-endian binary serialization (not UTF-16). Writes/reads: sbyte, short, int, long, strings (length-prefixed), UTF-8 encoded
  - Used for both network messages AND local file/resource loading
  - Watch: `convertSbyteToByte()` for signed→unsigned casting quirks

### Rendering (Not Standard Unity)
- **mGraphics**: Custom rendering pipeline (bypasses standard Renderer)
- **IPaint interface**: Custom paint protocol (DrawString, DrawImage, FillRect, etc.)
- **mScreen, mFont, mLine**: Custom drawing primitives
- **Resources loaded as sbyte[]** for protocol compatibility

## Critical Developer Patterns

### Singleton Access Pattern
All major systems use the **`gI()` static factory** (mimics J2ME):
```csharp
Service.gI().sendMessage(...)     // NOT Service.Instance
Controller.gI().handleMessage(...) // Dual instances: me, me2
Session_ME.gI()                    // Static singleton
```
- **Never** instantiate these directly; always use gI()
- Main thread references stored in Main static fields

### Command/Message System
Game actions → Messages → TCP:
```csharp
// In Service.cs (~2900+ action methods like gotoPlayer, ...)
Message msg = new Message((sbyte)COMMAND_ID);
msg.writer().writeInt(param1);
msg.writer().writeString(param2);
session.sendMessage(msg);          // Queued, threaded send
msg.cleanup();                      // Critical: deallocate
```
- Message IDs are sbyte constants (0-127 mostly)
- Server echoes back OR sends `IMessageHandler.onRecieveMsg()` callbacks

### Template/Template-Based Data
Game data is **template-driven**:
- `ItemTemplate` defines item stats/appearance
- `SkillTemplate`, `MobTemplate`, `NpcTemplate`: Static definitions loaded at startup
- `ItemTemplates.gI()`, `SkillTemplate` singletons
- **Note**: Data is mutable! Modifying templates affects all instances

### Build System (Unity CLI)
```bash
./build.sh Android aab                           # APK/AAB
./build.sh StandaloneOSX                         # /Applications on Mac
./build.sh StandaloneWindows64                   # Windows .exe

# CLI args: -buildTarget, -androidFormat (apk|aab), -outputFolderPattern "{product}_{datetime}"
# Entry: Assets/Editor/Build/BuildPlayerCLI.cs → DragonBoy.Build.BuildPlayerCLI.Build()
```
- Supports template variable expansion: {product}, {platform}, {version}, {datetime}, {unity}
- All enabled scenes in EditorBuildSettings are included

### Mod System (Assets/Scripts/Assembly-CSharp/Mod/)
Organized extension points:
- `Mod.Graphics`: Custom rendering hooks
- `Mod.ModHelper`: Utilities
- `Mod.AccountManager`, `DeveloperFunctions`: Extension modules
- `GameEvents`, `GameEventHook`: Hook system for intercepting game actions

## Network Protocol Deep Dive

### Connection Flow
1. TcpClient.Connect(GameMidlet.IP, GameMidlet.PORT)
2. Send Message(-27) as handshake
3. Server responds with authentication/config
4. Session_ME.onRecieveMsg() parses responses

### Message Encryption
- `Session_ME.readKey()/writeKey()`: XOR-based obfuscation (not cryptography)
- Applied to every sbyte in session
- Key rotates per session

### Known Command IDs (Service.cs examples)
- 18: gotoPlayer(id) - teleport to player
- 110: serverData(action, id, data) - generic server action
- Message(-27): Handshake
- Mapping not centralized - scattered in Controller/Service handler code

## Debugging & Logging

- **Cout class**: Custom logging (check `Cout.LogError()`, `Cout.Log()`)
- **Res.outz()**: Game event logging
- **Build logs**: BuildArtifacts/_logs/build_{TARGET}_*.log after ./build.sh
- **No Debug Panel**: Game runs headless in production; use remote logging for mobile

## Common Pitfalls

1. **Global State**: Modifying `Main.g`, `GameScr.instance` directly affects all systems - track changes carefully
2. **Thread Safety**: Session networking runs on separate threads; use locks if modifying shared state
3. **Resource Leaks**: Message.cleanup() and myReader/myWriter.close() are critical; sbyte[] buffers aren't auto-collected
4. **Legacy Quirks**: 
   - `sbyte` used everywhere (not byte) - watch sign conversions
   - Strings are length-prefixed, not null-terminated
   - No exception handling in many hot paths
5. **Two Sessions**: Session_ME (primary), Session_ME2 (secondary) - both may be active; messages can confuse each other

## File Organization at a Glance

- **Assets/Scripts/Assembly-CSharp/**: 200+ monolithic game logic files (no subdirectories except Mod/)
- **Assets/Scripts/Assembly-CSharp/Mod/**: Extension framework (Graphics/, ModHelper/, DeveloperFunctions/)
- **Assets/Editor/Build/**: BuildPlayerCLI.cs - the ONLY editor script
- **Assets/Resources/**: Textures, shaders, data files (loaded as TextAssets for sbyte[] parsing)
- **ProjectSettings/**: Standard Unity config; note EditorBuildSettings defines what scenes to build

## Recommended Tools for Investigation

- Search for message handlers: `grep "onRecieve\|perform\|Handle"` - callback entry points
- Trace a command: Find sbyte constant → grep in Service.cs → find in Controller → check message parsing in myReader
- Network capture: Hook Session_ME.doSendMessage() to log all TCP frames
- Profile rendering: Search for mGraphics.drawString/drawImage calls in IPaint implementations

