# HƯỚNG DẪN DỰ ÁN VÀ KIẾN TRÚC CODE (AGENT.MD)

> **Dành cho các AI Agent / Nhà phát triển tham gia dự án Dragonboy (Bản Mod Cộng Đồng NRO Unity)**.  
> Đọc kỹ tài liệu này trước khi lập trình để nắm bắt kiến trúc, quy chuẩn code, các hàm tiện ích có sẵn và tránh các lỗi phổ biến.

---

## 1. TỔNG QUAN DỰ ÁN

* **Bản chất dự án**: Game Ngọc Rồng Online (Dragonboy) được port và phát triển trên nền tảng **Unity C#** (.NET Standard 2.1).
* **Cấu trúc thư mục chính**:
  * `/GameProject/DragonBoy250/`: Thư mục gốc của project Unity.
  * `/GameProject/DragonBoy250/Assets/Scripts/Assembly-CSharp/`: Toàn bộ mã nguồn C# của game.
  * `/GameProject/DragonBoy250/Assets/Scripts/Assembly-CSharp/Mod/`: **Nơi đặt tất cả các tính năng mod, auto, tool mở rộng**.
* **Lệnh kiểm tra build**:
  ```bash
  # Chạy tại thư mục /GameProject/DragonBoy250/
  dotnet build Assembly-CSharp.csproj
  ```

---

## 2. QUY TẮC BẮT BUỘC KHI THÊM / SỬA CODE

### 2.1. Đăng ký file vào `Assembly-CSharp.csproj`
* Dự án quản lý danh sách file biên dịch bằng `Assembly-CSharp.csproj`.
* **Mỗi file `.cs` mới tạo PHẢI được thêm vào `Assembly-CSharp.csproj`** theo định dạng:
  ```xml
  <Compile Include="Assets/Scripts/Assembly-CSharp/Mod/Path/To/YourFile.cs" />
  ```
  *(Luôn dùng dấu gạch chéo `/`, không dùng `\` để tránh lỗi cross-platform).*

### 2.2. Tạo file `.meta` của Unity
* Mọi file `.cs` và thư mục mới tạo **BẮT BUỘC** phải có file `.meta` tương ứng với GUID 32 ký tự hex duy nhất.
  ```yaml
  fileFormatVersion: 2
  guid: <32_hex_characters_lowercase>
  MonoImporter:
    externalObjects: {}
    serializedVersion: 2
    defaultReferences: []
    executionOrder: 0
    icon: {instanceID: 0}
    userData: 
    assetBundleName: 
    assetBundleVariant: 
  ```

### 2.3. Xung đột namespace `Math`
* Trong codebase có class `Math` riêng của game, do đó gọi `Math.Abs()` sẽ bị lỗi `CS0117`.
* **Quy chuẩn**: Luôn gọi rõ `System.Math.Abs(...)` hoặc hàm tiện ích `Res.abs(...)`.

---

## 3. CÁC PATTERN VÀ HỆ THỐNG CỐT LÕI CỦA MOD

### 3.1. Background Bot / Auto: `CoroutineMainThreadAction<T>`
Tất cả các tính năng chạy nền định kỳ (Auto Doanh trại, Auto Bò Mộng, Auto Yardat, Auto Goback, Auto Pean, Auto Attack,...) đều kế thừa class này:
```csharp
using Mod.ModHelper;

namespace Mod.YourFeature
{
    public class AutoYourFeature : CoroutineMainThreadAction<AutoYourFeature>
    {
        // Thời gian nghỉ giữa các lần lặp coroutine (giây)
        protected override float Interval => 0.2f;

        protected override void OnStart()
        {
            // Khởi tạo trạng thái khi bật tool
        }

        protected override void OnStop()
        {
            // Dọn dẹp trạng thái khi tắt tool
        }

        protected override IEnumerator OnUpdate()
        {
            if (Char.myCharz().IsCharDead())
            {
                yield break;
            }

            // Logic chạy định kỳ...
            yield break;
        }
    }
}
```
* **Cơ chế tự đăng ký**: `ModScript.Init()` tự động quét reflection toàn bộ các class kế thừa `CoroutineMainThreadAction<>` và tạo GameObject kèm `DontDestroyOnLoad`. **Không bao giờ cần gọi `AddComponent` bằng tay**.
* **Sử dụng**: `AutoYourFeature.gI.Toggle(true/false)` hoặc `AutoYourFeature.gI.IsActing`.

---

### 3.2. Menu tương tác trong game: `MenuBuilder`
Dùng `MenuBuilder` để tạo menu tương tác chuẩn phong cách NRO (hiển thị popup thoại NPC kèm các nút lựa chọn):
```csharp
using Mod.ModHelper.Menu;

public static class YourMenu
{
    public static void ShowMenu()
    {
        MenuBuilder menu = new MenuBuilder().setChatPopup("Tiêu đề hoặc mô tả chức năng\n[Trạng thái]");

        menu.addItem("Bật / Tắt chức năng", new MenuAction(() =>
        {
            AutoYourFeature.gI.Toggle(!AutoYourFeature.gI.IsActing);
            ShowMenu(); // Refresh lại menu
        }));

        menu.addItem("Cài đặt tùy chọn", new MenuAction(() =>
        {
            // Mở submenu
        }));

        menu.start();
    }
}
```

---

### 3.3. Menu dạng danh sách Bật/Tắt trong Panel: `CustomPanelMenu` (Kiểu AutoBeans / YardatTargetPanel)
Khi muốn hiển thị danh sách các mục bật/tắt (như các hành động Auto Đậu, hoặc danh sách các quái/mục tiêu), hiển thị trực tiếp trong **Panel UI** thay vì mở popup thoại NPC:
```csharp
using Mod.CustomPanel;

internal static class YourPanel
{
    static List<YourItem> items = new List<YourItem>();

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

    static void LoadItems() { ... }

    static void Paint(Panel panel, mGraphics g)
    {
        PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(panel, g, items, x => x.Name, x => x.Description);
    }

    static void PaintTabHeader(Panel panel, mGraphics g)
    {
        PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Tiêu đề Tab");
    }

    static void SetTab(Panel panel)
    {
        SetTabPanelTemplates.setTabListTemplate(panel, items);
    }

    static void DoFire(Panel panel)
    {
        InfoDlg.hide();
        if (panel.selected >= 0 && panel.selected < items.Count)
        {
            int prevCount = items.Count;
            items[panel.selected].OnClick?.Invoke();
            LoadItems();
            if (items.Count != prevCount)
                SetTab(panel);
        }
    }
}
```

---

### 3.4. Lưu trữ cài đặt: `ModStorage`
Lưu trữ cấu hình người dùng ra file dữ liệu cục bộ:
```csharp
// Đọc cấu hình (kèm giá trị mặc định)
bool isEnabled = ModStorage.ReadBool("key_name", defaultValue: false);
string str = ModStorage.ReadString("key_name", defaultValue: "default");
int num = ModStorage.ReadInt("key_name", defaultValue: 0);

// Ghi cấu hình
ModStorage.WriteBool("key_name", isEnabled);
ModStorage.WriteString("key_name", str);
ModStorage.WriteInt("key_name", num);
```
* Nạp cấu hình (`Settings.Load()`) tại `GameEvents.OnGameStart()` hoặc trong hàm `OnStart()` của tool.
* Các hàm `Read...` đã tự động validate `File.Exists(filePath)`, nếu file chưa tồn tại sẽ trả về `defaultValue` mà không quăng lỗi `FileNotFoundException`.

---

### 3.4. Menu tổng của Mod: `ModMenuMain`
Nằm tại `Assets/Scripts/Assembly-CSharp/Mod/ModMenu/ModMenuMain.cs`:
* **Thêm nút On/Off**: Thêm vào mảng `modMenuItemBools` (`ModMenuItemBooleanConfig`).
* **Thêm nút mở Menu tính năng**: Thêm vào mảng `modMenuItemFunctions` (`ModMenuItemFunctionConfig`).

---

### 3.5. Lệnh Chat và Phím tắt: `InGameCommand`
Nằm tại `Assets/Scripts/Assembly-CSharp/Mod/InGameCommand.cs`:
```csharp
[ChatCommand("lenhchat"), ChatCommand("lc")]
internal static void OpenMyTool()
{
    YourMenu.ShowMenu();
}

[HotkeyCommand('z')]
internal static void ShortcutAction()
{
    // Hành động khi nhấn phím
}
```
*(Hệ thống `ChatCommandHandler` tự quét reflection tất cả các method có attribute `[ChatCommand]`)*.

---

## 4. GAME DOMAIN KNOWLEDGE & CÁC API PHỔ BIẾN

### 4.1. Nhân vật người chơi (`Char.myCharz()`)
* `Char.myCharz()`: Thể hiện của nhân vật chính.
* Máu & Ki: `myChar.cHP`, `myChar.cHPFull`, `myChar.cMP`, `myChar.cMPFull`.
* Hành tinh: `myChar.cgender` (0: Trái Đất, 1: Namếc, 2: Xayda).
* Trạng thái sống/chết: `myChar.IsCharDead()`, `myChar.statusMe` (14: chết, 5: kiệt sức).
* Hợp thể: `myChar.isNhapThe` (true nếu đang hợp thể Porata/Lưỡng Long).
* Đang gồng/tái tạo năng lượng: `myChar.isCharge`.
* Đệ tử: `myChar.havePet`, `Char.myPetz()` (trạng thái: 0: tấn công, 1: bảo vệ, 2: đi theo, 3: về nhà).
  * Lệnh đệ tử: `Service.gI().petStatus(statusId);`

### 4.2. Quái thường (`Mob`) vs Nhân vật khác / Bot (`Char`)
* **Quái thường (`Mob`)**: Nằm trong `GameScr.vMob`. Target: `Char.myCharz().mobFocus`.
  * Tấn công quái:
    ```csharp
    MyVector vec = new MyVector();
    vec.addElement(mob);
    Service.gI().sendPlayerAttack(vec, new MyVector(), -1);
    ```
* **Bot / Clone / Chiến binh Yardat / Boss (`Char`)**: Nằm trong `GameScr.vCharInMap`. Target: `Char.myCharz().charFocus`.
  * **Đặc điểm quái Yardat**: Server gửi về dạng `Char` có `charID <= 0 && !c.isPet && !c.isMiniPet`.
  * **Đặc điểm đệ tử của mình**: Có `charID == -Char.myCharz().charID`.
  * Tấn công nhân vật / bot / boss:
    ```csharp
    MyVector vec = new MyVector();
    vec.addElement(targetChar);
    Service.gI().sendPlayerAttack(new MyVector(), vec, 2);
    ```

### 4.3. Kỹ năng (`Skill`)
* Skill đang chọn: `Char.myCharz().myskill`.
* Phím tắt skill: `GameScr.keySkill` (mảng các skill gán trên hotbar).
* Chọn skill: `GameScr.gI().doSelectSkill(skill, isShortcut: true);`
* Skill tấn công cơ bản / liên hoàn: `{ 0, 2, 4, 17 }` (0: Kame/Dấm, 2: Masenko/Demon, 4: Galick, 17: Liên hoàn / Dơi mun).
* `SkillPicker.GetSkillAttack()`: Tiện ích tự động chọn skill tấn công tốt nhất của nhân vật.

### 4.4. Túi đồ & Vật phẩm (`Item`)
* Danh sách túi đồ: `Char.myCharz().arrItemBag`.
* Tìm vị trí item trong túi: `sbyte index = Utils.getIndexItemBag(templateIds);`
* Dùng item từ túi: `Service.gI().useItem(0, 1, indexBag, -1);` (0 = use, 1 = bag).
* Bông tai Porata: Template ID `921` (Porata Cấp 2), `454` (Porata Cấp 1).
* Ngọc rồng: `item.template.type == Item.TYPE_NGOCRONG` (type 12, template ID 14 -> 20 cho 1 sao -> 7 sao).
  * **LƯU Ý**: Nâng cấp / Ghép ngọc rồng thực hiện qua luồng **Panel Combine UI** (`idAction == 6000`, `use_for_combine`), **KHÔNG dùng `useItem()`**.

### 4.5. Di chuyển & Xmap
* Map & Khu hiện tại: `TileMap.mapID`, `TileMap.zoneID`.
* Dịch chuyển tọa độ trong map: `Utils.TeleportMyChar(x, y);`
* Dịch chuyển tới NPC: `Utils.TeleportToNPC(npcId);`
* Chuyển map qua Xmap:
  * Gọi không chờ: `XmapController.start(targetMapId);`
  * Gọi trong Coroutine chờ đến nơi: `yield return XmapController.StartAndWait(targetMapId);`
* Hồi sinh về làng khi chết: `Service.gI().returnTownFromDead();`

---

## 5. CASE STUDIES: CÁC MODULE ĐÃ TRIỂN KHAI MẪU

1. **Auto Bò Mộng (`Assets/Scripts/Assembly-CSharp/Mod/BoMong/`)**:
   * Kiến trúc: Controller (`BoMongController`), Handler Factory (`QuestHandlerFactory`), Event Listener bắt tin nhắn popup game (`BoMongMessageEvent` lắng nghe `GameEvents.OnChatPopupMultiLine` và `OnAddInfoMe`).
   * Sử dụng Xmap tự động di chuyển đến Map 47 (Thần Vũ Trụ - NPC 17), chọn menu theo độ khó và tự động làm từng loại nhiệm vụ.

2. **Auto Doanh Trại (`Assets/Scripts/Assembly-CSharp/Mod/Barracks/`)**:
   * Map chuỗi Doanh Trại: `53 → 58 → 59 → 60 → 61 → 62 → 55 → 56 → 54 → 57`.
   * Tiêu diệt boss trước (`cTypePk == 5`), sau đó dọn lính và qua map tiếp theo.

3. **Auto Yardat (`Assets/Scripts/Assembly-CSharp/Mod/Yardat/`)**:
   * Phân tách thành 3 file: `AutoYardat.cs` (controller), `YardatSettings.cs` (lưu trữ RMS), `YardatMenu.cs` (giao diện `MenuBuilder` và `YardatTargetPanel` kiểu `CustomPanelMenu`).
   * Train vật phẩm Yardat tại map 131-133: Tự động đổi mục tiêu các bot `charID <= 0`, auto bông tai Porata xoay tua 11s/2s, tự động thu hồi đệ tử khi xuất hiện trong map để bảo vệ đệ.

---

## 6. CHECKLIST KHI HOÀN THÀNH TÍNH NĂNG MỚI
- [ ] Code kế thừa `CoroutineMainThreadAction` (nếu là bot nền) hoặc class quản lý tương ứng.
- [ ] Giao diện cấu hình qua `MenuBuilder`.
- [ ] Đăng ký lệnh chat vào `InGameCommand.cs` và menu mod trong `ModMenuMain.cs`.
- [ ] Nạp cấu hình `Settings.Load()` trong `GameEvents.OnGameStart()`.
- [ ] Tạo file `.meta` cho tất cả các file `.cs` mới.
- [ ] Thêm đường dẫn file `.cs` mới vào `Assembly-CSharp.csproj`.
- [ ] Chạy `dotnet build Assembly-CSharp.csproj` đảm bảo **0 Warning / 0 Error**.
