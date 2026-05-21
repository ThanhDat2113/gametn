# Phân Tích Toàn Diện Cấu Trúc Dự Án Game

Tài liệu này cung cấp một cái nhìn tổng quan chi tiết về cấu trúc, luồng hoạt động và các thành phần cốt lõi của dự án, bao gồm các hệ thống từ di chuyển trên bản đồ, chiến đấu, hội thoại, đến quản lý vật phẩm và đội hình.

## 1. Tổng Quan Cấu Trúc Thư Mục

Cấu trúc thư mục chính chứa mã nguồn của trò chơi nằm tại `Assets/Scripts`. Các hệ thống con được tổ chức vào các thư mục riêng biệt:

-   `Assets/Scripts/Combat`: Chứa toàn bộ logic liên quan đến hệ thống chiến đấu.
    -   `Actions`: Định nghĩa các hành động (kỹ năng, vật phẩm) mà nhân vật có thể thực hiện.
    -   `Character`: Logic cho các đơn vị chiến đấu (cả người chơi và kẻ thù).
    -   `Formation`: Quản lý việc sắp xếp đội hình trước trận đấu.
    -   `Manager`: Trung tâm điều phối của trận đấu (`CombatManager`).
-   `Assets/Scripts/Dialogue`: Hệ thống hiển thị và quản lý hội thoại.
-   `Assets/Scripts/Inventory`: Quản lý vật phẩm và kho đồ của người chơi.
-   `Assets/Scripts/MapGamePlay`: Logic cho việc di chuyển, tương tác trên bản đồ thế giới.
-   `Assets/Scripts/ScriptableObject`: Nơi định nghĩa các `ScriptableObject` dùng chung cho toàn dự án (dữ liệu nhân vật, vật phẩm, kỹ năng, kẻ thù).

---

## 2. Luồng Hoạt Động Chính: Từ Bản Đồ Đến Chiến Đấu

Đây là luồng hoạt động cơ bản nhất của trò chơi, kết nối hai cảnh (Scene) chính: **Map Scene** và **Combat Scene**.

**Sơ đồ luồng:**

`[Player di chuyển trên Map Scene]` -> `[Va chạm với MapLocationTrigger]` -> `[Lấy dữ liệu đội hình hiện tại]` -> `[Lưu dữ liệu vào FormationDataStorage]` -> `[Chuyển sang Combat Scene]` -> `[CombatManager đọc dữ liệu và bắt đầu trận đấu]`

**Chi tiết các bước:**

1.  **Di Chuyển trên Bản Đồ (`Map Scene`):**
    -   Người chơi điều khiển nhân vật chính di chuyển trong một môi trường 3D.
    -   `MAPPlayerController.cs` xử lý input và di chuyển nhân vật bằng `CharacterController`. Camera theo dõi nhân vật nhưng có góc nhìn cố định, tạo hiệu ứng 2.5D.
    -   Các nhân vật và kẻ thù được hiển thị dưới dạng sprite 2D (billboard) trong không gian 3D.

2.  **Kích Hoạt Chiến Đấu:**
    -   Trên bản đồ có các đối tượng `MapLocationTrigger` (ví dụ: một nhóm kẻ thù).
    -   Khi người chơi va chạm với trigger này, hàm `OnTriggerEnter` trong `MapLocationTrigger.cs` sẽ được gọi.
    -   Trigger này sẽ gọi đến `CombatManager.Instance.StartCombat()`.

3.  **Lưu Trữ và Truyền Dữ Liệu Đội Hình:**
    -   Trước khi chuyển cảnh, thông tin về đội hình hiện tại của người chơi (vị trí các nhân vật) cần được lưu lại.
    -   `FormationManager.cs` chịu trách nhiệm quản lý và cung cấp dữ liệu đội hình này.
    -   Dữ liệu đội hình (`FormationData`) được lưu vào một lớp `static` là `FormationDataStorage.cs`. Lớp `static` này tồn tại xuyên suốt giữa các cảnh, đóng vai trò là cầu nối truyền dữ liệu.
    -   **File quan trọng:** `d:\unity\gametn\Assets\Scripts\Combat\Formation\FormationDataStorage.cs`
        ```csharp
        // Lớp static để giữ dữ liệu đội hình khi chuyển cảnh
        public static class FormationDataStorage
        {
            public static FormationData PendingFormation { get; set; }
        }
        ```

4.  **Chuyển Cảnh và Bắt Đầu Trận Đấu (`Combat Scene`):**
    -   Sau khi lưu dữ liệu, game chuyển sang `Combat Scene`.
    -   `CombatManager.cs` trong `Combat Scene` sẽ được khởi tạo.
    -   Trong phương thức `Start()` hoặc `Awake()`, `CombatManager` sẽ kiểm tra `FormationDataStorage.PendingFormation` để lấy dữ liệu đội hình mà người chơi đã thiết lập.
    -   `CombatManager` sử dụng dữ liệu này cùng với dữ liệu nhóm kẻ thù (`EnemyGroupData`) để khởi tạo các đơn vị chiến đấu (`CombatUnit`) cho cả hai phe.
    -   **File quan trọng:** `d:\unity\gametn\Assets\Scripts\Combat\CombatManager.cs`
        ```csharp
        // Hàm này được gọi từ Map Scene để bắt đầu quá trình
        public void StartCombat(FormationData formation, EnemyGroupData enemyGroup)
        {
            FormationDataStorage.PendingFormation = formation;
            // Logic chuyển scene...
        }

        // Khi Combat Scene tải xong, CombatManager sẽ đọc dữ liệu và thiết lập trận đấu
        private void Start()
        {
            if (FormationDataStorage.PendingFormation != null)
            {
                // Dùng dữ liệu từ FormationDataStorage để tạo đội hình người chơi
                SpawnPlayerUnits(FormationDataStorage.PendingFormation);
                // Tạo đội hình kẻ thù
                SpawnEnemyUnits(...);
            }
        }
        ```

---

## 3. Phân Tích Chi Tiết Các Hệ Thống

### a. Hệ Thống Chiến Đấu (`Combat System`)

Đây là hệ thống phức tạp nhất, được điều khiển bởi `CombatManager` và một máy trạng thái (`State Machine`).

-   **`CombatManager.cs`**:
    -   Là một **Singleton**, đảm bảo chỉ có một thực thể duy nhất quản lý trận đấu.
    -   Chịu trách nhiệm khởi tạo trận đấu, quản lý các lượt, kiểm tra điều kiện thắng/thua, và kết thúc trận đấu.
    -   Sử dụng một `CombatStateMachine` để quản lý các giai đoạn của trận đấu (Bắt đầu, Lên kế hoạch, Thực thi, Kết thúc).

-   **`CombatUnit.cs`**:
    -   Lớp cơ sở cho tất cả các nhân vật và kẻ thù trong trận đấu.
    -   Lưu trữ các chỉ số (HP, MP, Tốc độ), trạng thái, và các kỹ năng có thể sử dụng.

-   **`ActionSlotUI.cs` và `Skill/Item System`**:
    -   `ActionSlotUI` (`d:\unity\gametn\Assets\Scripts\Combat\ActionSlotUI.cs`) là thành phần UI cho phép người chơi chọn hành động (kỹ năng, vật phẩm) cho nhân vật.
    -   Dữ liệu về kỹ năng (`SkillData`) và vật phẩm (`ItemData`) được định nghĩa bằng `ScriptableObject`, giúp dễ dàng tạo và chỉnh sửa mà không cần thay đổi mã nguồn.

### b. Hệ Thống Đội Hình (`Formation System`)

-   **`FormationManager.cs`**:
    -   Quản lý giao diện người dùng (UI) cho phép người chơi kéo-thả các nhân vật vào một lưới (grid) để sắp xếp vị trí chiến đấu.
    -   Cung cấp dữ liệu `FormationData` cho `CombatManager` khi trận đấu bắt đầu.
    -   **File quan trọng:** `d:\unity\gametn\Assets\Scripts\Combat\Formation\FormationManager.cs`
        ```csharp
        // Xây dựng cấu trúc dữ liệu đội hình từ UI
        public FormationData BuildGrid() {
            //... logic lấy vị trí nhân vật từ các ô trên lưới
        }
        ```

### c. Hệ Thống Vật Phẩm và Kho Đồ (`Inventory System`)

-   **`InventoryManager.cs`**:
    -   Là một **Singleton**, quản lý toàn bộ vật phẩm của người chơi.
    -   Cung cấp các hàm để thêm (`AddItem`), xóa (`RemoveItem`), và kiểm tra số lượng vật phẩm.
    -   Hỗ trợ lưu và tải dữ liệu kho đồ ra file bằng `BinaryFormatter`.
    -   **File quan trọng:** `d:\unity\gametn\Assets\Scripts\Inventory\InventoryManager.cs`
        ```csharp
        // Lưu kho đồ vào file
        private void SaveToFile()
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(savePath);
            bf.Serialize(file, inventory);
            file.Close();
        }
        ```

### d. Hệ Thống Hội Thoại (`Dialogue System`)

-   **`ExtendedDialogueManager.cs`**:
    -   Là một **Singleton**, quản lý việc hiển thị các đoạn hội thoại.
    -   Sử dụng một hàng đợi (`Queue`) để xử lý các yêu cầu hội thoại một cách tuần tự.
    -   Dữ liệu hội thoại (`DialogueEvent`) được định nghĩa bằng `ScriptableObject`.
    -   Các sự kiện trong game (ví dụ: bắt đầu quest, tương tác với NPC) có thể gọi `ExtendedDialogueManager.Instance.QueueDialogue(dialogueEvent)` để bắt đầu một cuộc hội thoại.

---

## 4. Camera và Điều Khiển Nhân Vật

-   **Camera**:
    -   Sử dụng `Cinemachine` để tạo ra một camera ảo (`Virtual Camera`) theo dõi người chơi.
    -   Góc quay của camera được thiết lập cố định, không xoay theo nhân vật, tạo ra góc nhìn 2.5D đặc trưng.

-   **Nhân Vật (`MAPPlayerController.cs`)**:
    -   Sử dụng component `CharacterController` của Unity để xử lý di chuyển và va chạm một cách mượt mà.
    -   Hình ảnh nhân vật là một sprite 2D luôn xoay về phía camera (billboarding), tạo cảm giác nó là một phần của thế giới 3D.

## 5. Tổng Kết

Dự án được xây dựng trên nền tảng các mẫu thiết kế phổ biến trong phát triển game:
-   **Singleton Pattern**: Được sử dụng rộng rãi cho các `Manager` (Combat, Inventory, Dialogue) để đảm bảo một điểm truy cập toàn cục và duy nhất cho các hệ thống cốt lõi.
-   **Data-Driven Design (thông qua ScriptableObjects)**: Dữ liệu của game (nhân vật, vật phẩm, kỹ năng, hội thoại) được tách rời khỏi logic, cho phép các nhà thiết kế game dễ dàng chỉnh sửa và cân bằng game mà không cần can thiệp vào mã nguồn.
-   **Static Class for Cross-Scene Data**: Sử dụng lớp `static` (`FormationDataStorage`) là một giải pháp đơn giản và hiệu quả để truyền dữ liệu quan trọng giữa các cảnh.
-   **State Machine**: Giúp quản lý các trạng thái phức tạp của hệ thống chiến đấu một cách có tổ chức.

Kiến trúc này rất linh hoạt và dễ mở rộng, cho phép thêm các tính năng mới (như hệ thống chế tạo, cửa hàng, nhiệm vụ phụ) bằng cách tạo ra các `Manager` và `ScriptableObject` mới mà không làm ảnh hưởng lớn đến các hệ thống hiện có.