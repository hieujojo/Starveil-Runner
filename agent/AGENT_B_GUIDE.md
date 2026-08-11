# AGENT_B_GUIDE.md — Hướng dẫn onboard agent AI thứ 2 (supercode / SWE agent)

> ⚠️ **ĐỌC BẮT BUỘC TRƯỚC KHI SỬA BẤT KỲ FILE NÀO.** File này là "hiến pháp" chống xung đột giữa 2 agent.

## 1. Vai trò & phân quyền (không được vi phạm)

| Khu vực | Chủ sở hữu | Agent B được phép? |
|---|---|---|
| `Assets/_Project/Scripts/Core/World/*` (Tile, TileSpawner, ObstacleManager, PickupSpawner, VoidChase...) | **Agent A** (đang debug) | ❌ KHÔNG, trừ khi Agent A bàn giao |
| `Assets/_Project/Scripts/Core/Player/*`, `Systems/*`, `UI/*` | Agent A | ❌ mặc định — hỏi trước |
| `Assets/_Project/Scenes/*.unity` (YAML scene) | Cả hai, NHƯNG **1 người 1 lúc** | ⚠️ scene YAML rất dễ conflict — phải báo Agent A trước khi sửa |
| `Assets/_Project/Editor/*` (tool) | Agent A | ❌ |
| `Assets/_Project/Prefabs/*`, `Assets/_Project/Art/*`, `Assets/_Project/Audio/*` | Cả hai | ✅ nếu là import asset, không đụng .meta |
| `agent/*.md` (docs) | Cả hai | ✅ NHƯNG phải đọc RULES trước |
| `README.md` | Agent A | ❌ |

**Quy tắc vàng:** file nào đang được agent kia sửa thì tuyệt đối không đụng. Nếu không chắc → hỏi user.

## 2. Đọc trước (bắt buộc, theo thứ tự)

1. `agent/RULES.md` — **cấm kỵ** (R1.x–R6.x): 20+ bug đã fix, không được tái phạm. Đây là nguồn quan trọng nhất.
2. `agent/CHANGELOG.md` — lịch sử bug theo vòng.
3. `agent/void-runner-plan.md` — kế hoạch tính năng + trạng thái hoàn thành.
4. `agent/COMMIT_TEMPLATES.md` — format commit bắt buộc (tiếng Việt, có dấu, theo conventional).
5. `agent/TESTING.md` — hướng dẫn test + checklist thủ công.

## 3. Luật git (bất biến — bài học R6.12)

1. **Chỉ Agent A commit + push `main`.** Agent B làm xong → **báo user** để Agent A commit, HOẶC tạo branch riêng `agent-b`.
2. **Commit nhỏ + push sớm**: 1 tính năng/bug = 1 commit, không ôm đống.
3. Commit message: theo `COMMIT_TEMPLATES.md`, tiếng Việt có dấu, prefix `feat/fix/docs/refactor/chore` + scope.
4. `git add` TỪNG FILE cụ thể — **KHÔNG BAO GIỜ `git add .`** (cuốn nhầm file hệ thống — bài học cũ).
5. File hệ thống cấm commit: `*.tmp`, file `.log`, `Library/`, file `.asset` lớn bị Unity tự sinh.

## 4. Quy trình làm việc chuẩn (user đã đặt luật)

- Làm **từng bước nhỏ** → test → **commit** → báo user review.
- Tính năng phức tạp → **chia nhỏ** thành nhiều commit.
- **Mọi bug khi phát hiện → ghi ngay vào `agent/CHANGELOG.md`** + rút rule nếu là bài học (khuyến khích "bug chồng bug" — phân tích gốc rễ, không fix vá).
- **Ưu tiên tìm NGUYÊN NHÂN CHÍNH XÁC trước khi fix** (user cực kỳ nghiêm khắc vụ này — không được đoán mò, dùng console.log nếu cần).
- Trước khi sửa Unity scene/prefab: **báo user thao tác Unity nếu cần** (kéo thả, chạy tool) — user thích tự làm phần Inspector.

## 5. Bối cảnh project hiện tại (CẬP NHẬT 2026-08-11)

- **Game**: Void Runner — endless runner kiểu Subway Surfers, 3 lane, player = tàu vũ trụ tự bay, Void (kẻ thù) đuổi sau.
- **Cơ chế va chạm**: obstacle KHÔNG giết — đụng lần 1 = Void tiến sát; lần 2 trong cửa sổ 10–15s = Game Over.
- **Cấu trúc**: Clean Architecture nhẹ — `Core/` (Game, Player, World), `Systems/` (Input, Audio, Save, Score, Difficulty, VFX, PowerUp), `UI/`, `Data/` (ScriptableObjects), `Editor/` (tool tự động hóa), `Tests/`.
- **Package**: URP (Universal Render Pipeline) + Cinemachine + Input System (asset `InputSystem_Actions.inputactions`) + TextMeshPro + DOTween (Plugins).
- **Scene**: `MainMenu.unity` (entry) → `Game.unity` (play). Build settings đã có cả 2.

### ✅ BUG ĐÃ GIẢI QUYẾT (2026-08-11 — commit `43f2936`)
**Root cause cuối của bug "không thấy vật cản/xu" (kể cả 3 tuần):** `Rotator.cs` (xoay 15/30/45°/giây — user tưởng thêm vào coin) bị gắn nhầm lên GameObject **"Managers"** (cha TileSpawner → cha toàn bộ tiles) → cả track + obstacle + coin quay vòng liên tục (`tileRot ≈ 360°` trong log) → world position con lệch X/Y lung tung + `tile=2`. **Đã xóa Rotator khỏi Managers trong `Game.unity`.**

**Trạng thái hiện tại cần user xác nhận:** sau khi pull/push `43f2936`, chơi thử phải thấy obstacle + coin hiển thị đúng trên đường. Nếu còn lỗi → đọc `agent/CHANGELOG.md` mục mới nhất (R4.17) rồi xử theo.

## 6. Mẹo làm việc với Unity + agent

- User có thể KHÔNG gửi file — thường gửi **ảnh chụp Console/Inspector**. Yêu cầu user gửi log text nếu cần đọc kỹ.
- Đừng sửa YAML scene thủ công nếu có thể — ưu tiên viết **Editor tool** (`Assets/_Project/Editor/*`) để user chạy qua `Tools → Void Runner`.
- File `.meta` của Unity: KHÔNG sửa tay, trừ khi là tạo file mới (Unity tự sinh khi focus).
- Verify: dùng `grep` đếm `{`/`}` cho C# trước khi kết luận "đúng".
