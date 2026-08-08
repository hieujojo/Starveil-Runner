# CHANGELOG — Nhật ký lỗi đã sửa (lessons learned)

> **Mục đích:** ghi lại mọi lỗi/warning đã gặp trong quá trình phát triển, cách fix và cách tránh lặp lại.
> Cập nhật mỗi lần fix lỗi, trước khi commit.

---

## 2026-08-09 — G2 bắt đầu: ScoreSystem

### Đã xong

- `Systems/Score/ScoreSystem.cs`: score theo khoảng cách chạy (`deltaZ × 10 × multiplier`) + coin (`coinScore × multiplier`); combo ×2…×5 tăng theo `comboInterval` (5s) sống liên tục, reset khi dính obstacle; event-driven `OnScoreChanged`/`OnComboChanged` — UI subscribe, không coupling.

### Bài học

- Đo điểm theo **`deltaZ` thực tế** (`player.position.z` mỗi frame) thay vì `speed * dt` — độc lập với DifficultyManager (tốc độ thay đổi theo thời gian) sắp tới, không cần sửa ScoreSystem khi tăng tốc.
- **File .cs mới tạo khi Unity đang mở sẽ chưa có `.meta`** — commit code trước, quay lại Unity để editor sinh `.meta`, commit `.meta` sau (không tự tay gõ GUID).

---

## 2026-08-08 — ObstacleData wiring (Obstacle.cs, ObstacleData.cs, ObstacleManager.cs)

### Ghi chú kỹ thuật

- **`Obstacle.cs`** giờ có `[SerializeField] private ObstacleData _data` + property `Data` + method `SetData(data)` — component biết mình thuộc loại obstacle nào (phục vụ G2: shield, vfx, xử lý theo type).
- **`ObstacleData.cs`** thêm enum `ObstacleType { Pillar, Ramp, Dynamic }` + field `obstacleType`.
- **`ObstacleManager.SpawnOnTile`** gọi `comp.SetData(data)` sau khi spawn — data lúc runtime luôn khớp với data đã pick theo weight.
- **Lưu ý tránh trùng lặp:** `ObstacleData.isDynamic` (bool) đang chồng lấn ngữ nghĩa với `ObstacleType.Dynamic` (enum) — gộp lại trong refactor tương lai (giữ cả hai hiện tại, chưa phá vỡ cấu hình cũ).

---

## 2026-08-09 — Bước 8 hoàn tất (scene Game đủ hệ thống)

### Đã xong

- Tạo `Ramp.prefab` (cube dẹt + component `Obstacle`) — asset `Ramp.asset` giờ có prefab thật (GUID khớp `.prefab.meta`).
- Gán prefab vào cả 2 `ObstacleData` (`DynamicBox.asset` + `Ramp.asset`) — trước đó `prefab: {fileID: 0}` (null).
- Scene `Game.unity` đã có **ObstacleManager** với 2 asset trong list.
- Xóa file rác `DynamicBox 1.prefab` (bản copy lỗi, Rigidbody mass 0.1 sẽ bay lung tung).

### Bài học

- **Asset đang mở trong Unity thì file trên đĩa chưa cập nhật** — phải `Ctrl+S` (File → Save) mới ghi xuống đĩa để commit được. Kiểm tra asset luôn đọc từ đĩa.
- Khi asset `prefab: {fileID: 0}` nghĩa là **chưa kéo prefab vào Inspector** — không phải lỗi code. Kiểm tra cả GUID khớp giữa asset và `.prefab.meta`.

---

## 2026-08-07 — Giai đoạn 1: 11 script core gameplay

### Lỗi compile

| # | Lỗi | File | Nguyên nhân | Cách fix | Tránh lặp lại |
|---|---|---|---|---|---|
| 1 | `CS1739: The best overload for 'InputAction' does not have a parameter named 'expectedControlLayout'` | `InputReader.cs` | Constructor `InputAction` của Input System 1.19 **không có** named parameter `expectedControlLayout` | Bỏ named arg: `new InputAction("Move", InputActionType.Value)` — layout tự suy ra từ composite | Không dùng named arg lạ trong constructor thư viện; kiểm tra signature thực tế của package đã cài |
| 2 | `CS0103: The name 'CreateTile' does not exist in the current context` | `TileSpawner.cs` | Refactor đơn giản hóa đã **xóa method `CreateTile`** nhưng pool vẫn `factory: CreateTile` | Thêm lại method `CreateTile()` (Instantiate + SetActive(false)) | Khi refactor bỏ method, phải xóa cả chỗ gọi; chạy compile sau mỗi lần sửa file |

### Warning đã xử lý (API deprecated trong Unity 6.4)

| # | Warning | File | Cách fix | Tránh lặp lại |
|---|---|---|---|---|
| 3 | `CS0618: Object.FindObjectOfType/FindFirstObjectByType is obsolete` | `GameManager.cs` | Dùng **`FindAnyObjectByType<T>()`** (không sorting, không deprecate) | Unity 6: KHÔNG dùng `FindObjectOfType` hay `FindFirstObjectByType` — chỉ `FindAnyObjectByType` |
| 4 | `CS0618: Rigidbody.velocity is obsolete` | `PlayerController.cs` | Dùng **`Rigidbody.linearVelocity`** | Unity 6: `velocity` → `linearVelocity` (API đổi tên) |

### Warning / quy trình đã xử lý

| # | Vấn đề | Cách xử lý | Tránh lặp lại |
|---|---|---|---|
| 5 | `git diff --check` báo trailing whitespace ở file `.meta` (Unity sinh) + vendor DOTween + `.slnx` | Bộ lọc khi chạy: bỏ qua `\.meta:`, `Demigiant`, `Roll a ball\.slnx`, dòng `LF will be replaced` | Trước commit, chạy diff-check kèm filter — chỉ fail khi lỗi nằm trong code C# của mình |

### Ghi chú kỹ thuật khác (tránh vấp lại)

- **Unity 6 (`6000.4.5f1`)** đã deprecate một loạt API cũ — trước khi dùng API nào, ưu tiên kiểm tra warning. Bảng nhanh: `FindObjectOfType` → `FindAnyObjectByType` · `Rigidbody.velocity` → `Rigidbody.linearVelocity` · `isKinematic` → `linearDamping`/`angularDamping` cũng có thể đổi tên trong 6.x (kiểm tra khi dùng).
- **DOTween KHÔNG có trên Unity registry / OpenUPM** — phải cài từ **Asset Store** (gói `com.demigiant.dotween` trả 404 trên OpenUPM). Sau khi cài bản mới ≥1.2.815: nếu lỗi xuất hiện → **restart Unity + Tools → Demigiant → DOTween Utility Panel → Setup**.
- **`write_file` thất bại khi ghi đè file có sẵn với CRLF** (ví dụ `PlayerController.cs` cũ) → dùng bash heredoc `cat > file << 'EOF'` để ghi đè an toàn.
- **Folder `_Project/` phải nằm TRONG `Assets/`** (`Assets/_Project`) — folder ngoài Assets không được Unity import. Lần đầu tạo nhầm ở root, đã sửa.
- **Hộp thoại "Script Updating Consent"** khi Unity nghi ngờ script dùng API cũ → **bấm "No"** (code đã viết theo API Unity 6 hiện tại, để Unity tự sửa dễ hỏng file).
- **Input System composite**: phím A/D là button (chỉ cho giá trị +1) — muốn có -1/+1 cho trái/phải **phải dùng composite** `2DVector` (`AddCompositeBinding("2DVector").With("Left"/"Right", ...)`).
