# 🐞 Void Runner — Sổ ghi nhận Bugs (UI & Visual)

> Mục đích: tập trung **giao diện (UI) + hình ảnh (visual)** — không lặp lại lỗi đã sửa.
> Trạng thái tổng thể: **logic gameplay OK** (score/combo/power-up/audio/save chạy ổn) — **UI còn nhiều lỗi**, fix xong mới test toàn diện → deploy.

---

## 🎯 Vòng 1 — Phát hiện từ phân tích scene + ảnh (chưa fix)

> Phân tích trực tiếp file `Game.unity` + `MainMenu.unity` + ảnh game đang chạy.

### A. Game scene — HUD & Game Over

| # | Lỗi | Bằng chứng (scene) | Mức độ |
|---|---|---|---|
| G1 | **ScoreText lệch phải x=42** — không căn giữa panel, nhìn "chìm/lệch" | `m_AnchoredPosition: {x: 42, y: 0}`, sizeDelta `-80` | 🔴 Cao |
| G2 | **ScoreText vẫn màu TRẮNG** dù `HUDUpgradeTool` set vàng glow | `m_Color: #FFFFFF` (lẽ ra `#FFE640`) | 🔴 Cao |
| G3 | **ScorePanel màu xanh đen `#0C1E47`** — không khớp tông tím hư không | Image color `0.05, 0.12, 0.28` | 🟡 TB |
| G4 | **ComboText trắng** (lẽ ra cam `#FF8C42`) | `m_Color: #FFFFFF` | 🟡 TB |
| G5 | **GameOverPanel `#0A0F2D`** + nút **xanh dương** (`#268CFF`, `#1E66D8`) — tông không đồng bộ | Image colors | 🟡 TB |
| G6 | **"Điểm sáng trắng chói" giữa màn hình** (ảnh 2/3) — nghi ngờ **coin/Void emission** hoặc vật FBX còn sáng | Enemy.mat emission tím 0.35, PickUp vàng sáng 0.5 | 🔴 Cần xác minh |

### B. MainMenu scene

| # | Lỗi | Bằng chứng (scene) | Mức độ |
|---|---|---|---|
| M1 | **Title "VOID RUNNER" bị đè trùng 2 lớp** — `TitleGlow` + `TitleText` đều sz=110 trắng, gần cùng vị trí → chữ nhòe/mất nét | 2 GameObject, cả 2 `m_text: VOID RUNNER`, `y=256` vs `y=260` | 🔴 Cao |
| M2 | **Background `#050A1E` đen xanh** — không hợp tông tím | Image color | 🟡 TB |
| M3 | **Nút Play/HowToPlay/Sound màu xanh dương** (`#268CFF`, `#1E72E5`, `#1959BF`) — tông lệch | Image colors | 🟡 TB |
| M4 | **BestScoreText nằm quá thấp** (`y=-260`) — nguy cơ bị cắt màn hình ở độ phân giải thấp | anchoredPosition | 🟢 Thấp |

### C. Nguyên nhân gốc hệ thống

| # | Lỗi | Giải thích |
|---|---|---|
| S1 | **Tool sửa UI nhưng scene không giữ thay đổi** | `HUDUpgradeTool`/`HUDUIBuilder` chạy lúc scene chưa có panel mới (thứ tự tool) hoặc **quên Ctrl+S** — màu vàng/cam bị mất khi scene được tạo lại |
| S2 | **Hai tool tạo UI cạnh tranh nhau** | `HUDUIBuilder` (tạo panel cũ) vs `HUDUpgradeTool` (tô màu mới) — chạy sai thứ tự → panel cũ màu xanh dương, text trắng |
| S3 | **Thiếu 1 tool "polish toàn diện" idempotent** | Cần 1 tool duy nhất ép đúng toàn bộ màu/layout chuẩn mỗi lần chạy (giống `AmbientSetupTool` đã fix layout) |

---

## ✅ Đã fix (lịch sử các vòng trước — tham khảo `CHANGELOG.md`)

| Vòng | Lỗi | Fix |
|---|---|---|
| V0 | Prop đè lên đường | sideOffset 11 + targetHeight 3.2 + lọc model to |
| V0 | Nền đen thui | nền tím `(0.06, 0.035, 0.12)` + light 0.65 |
| V0 | Đồ lung tung | ép ghi layout chuẩn (jitter 0.15, rot 20°, scale 0.1) |
| V0 | Chói Bloom | Bloom 0.12 + threshold 1.15 + tắt postExposure |

---

## ✅ Vòng 1 — ĐÃ FIX (UIOverhaulTool + CameraFollowFixTool)

| # | Lỗi | Fix | Trạng thái |
|---|---|---|---|
| G1 | ScoreText lệch x=42 | UIOverhaulTool căn giữa | 🔧 chờ user chạy |
| G2 | ScoreText trắng | UIOverhaulTool vàng glow + viền tím | 🔧 chờ user chạy |
| G3–G5 | Tông xanh dương lệch | UIOverhaulTool tông tím/cyan | 🔧 chờ user chạy |
| M1 | Title trùng 2 lớp | UIOverhaulTool: TitleGlow cyan mờ 35% | 🔧 chờ user chạy |
| M2–M4 | Nền/nút xanh, best score thấp | UIOverhaulTool tông tím + y=-230 | 🔧 chờ user chạy |
| **G7** | **🔴 Camera KHÔNG chạy theo bóng** — CinemachineCamera THIẾU `CinemachineFollow` (chỉ có RotationComposer = chỉ xoay nhìn, không di chuyển) → bóng chạy xa biến mất khỏi màn hình | `CameraFollowFixTool` thêm body component FollowOffset (0,7,-10) damping 0.5 | 🔧 chờ user chạy |
| **G8** | **2 AudioListener** trong MainMenu — AudioManager (đúng) + **Main Camera THỪA 1 cái** | `GameplayFixTool` xóa listener trên Main Camera (cả 2 scene) | 🔧 chờ user chạy |

## ✅ Vòng 2 — ĐÃ FIX (2026-08-11 — gameplay feel)

> Phát hiện khi user test thật: Void không xuất hiện, điểm số bị che, 2 bên trống → cảm giác đứng yên.

| # | Lỗi | Nguyên nhân gốc | Fix | Trạng thái |
|---|---|---|---|---|
| V2-1 | **🔴 KHÔNG thấy kẻ thù Void đuổi theo** | VoidChase dùng **NavMeshAgent** — track VÔ TẬN (tile recycle) → NavMesh bake chỉ phủ vùng cố định → player chạy xa là **NavMesh hết vùng, Void đứng yên** tụt sau màn hình vĩnh viễn | VoidChase bỏ NavMeshAgent → đuổi trực tiếp: giữ sau lưng player 9m co dần tới **1.5m** (Void áp sát + nuốt player cuối game) + safety net `swallowDistance 1.6` | 🔧 chờ user chạy tool + test |
| V2-2 | **Tile vô hình → mất cảm giác chuyển động** | Tile prefab scale **z=0** → khối cube dẹt không render → chỉ còn Ground tĩnh → nhìn như đứng yên | Tile.cs `Awake` ép `scale z=length` + thêm **LaneMarker neon** (2 vạch mép + vạch đứt giữa) trượt theo tile khi recycle | 🔧 chờ test |
| V2-3 | **2 bên đường trống trải** | props `sideOffset 11` nằm NGOÀI tầm camera (FOV 60 thấy ±8) → không bao giờ thấy props | sideOffset **7** + targetHeight **4.5** + countPerSide **14** + spacing 7.5 + FOV **68** + nền sáng `(0.1,0.06,0.2)` + light **0.8** | 🔧 chờ user chạy tool |
| V2-4 | **Điểm số bị che** | ScorePanel góc trái nằm DƯỚI các element khác (sibling order) + bị che bởi panel | ScorePanel đưa lên **giữa-đỉnh** (anchor 0.5,1) + `SetAsLastSibling` (vẽ trên cùng — không gì che được) | 🔧 chờ user chạy tool |

## 🔜 Kế hoạch fix vòng 3 (theo thứ tự ưu tiên — từng cái một)

1. **User chạy 4 tool** (Game scene): `Fix Void Chase` → `Setup Ambient` → `Polish Scene` → `Overhaul UI`; MainMenu: `Overhaul UI` + `Fix Audio Listener` → Ctrl+S cả 2
2. **Test từ MainMenu**: ▶ Play → bấm PLAY → Game scene → chạy/né/thu coin → Void đuổi theo → chết → Game Over → CHƠI LẠI/MENU
3. Xác minh "điểm sáng trắng" (G6) — giảm emission coin/Void nếu cần
4. Test gameplay toàn diện (Menu → chơi → chết → retry) → mới deploy
