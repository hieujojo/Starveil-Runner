# 📚 REFERENCE — Tra cứu nhanh (tính năng · test · credits · commit · agent-guide)

> **Mục đích:** gộp 5 file tra cứu cũ (FEATURES, TESTING, CREDITS, COMMIT_TEMPLATES, AGENT_B_GUIDE)
> thành 1 file duy nhất — 2026-08-12 gộp docs.

---

# PART 1 — TÍNH NĂNG (FEATURES)

> **Mục đích:** tóm tắt từng tính năng đã hoàn thiện theo thời gian — bạn chỉ cần đọc bảng này
> là biết hệ thống nào đang chạy, hoạt động ra sao, có VFX/audio/UI gì đi kèm.
> Cập nhật sau mỗi commit tính năng (xem `CHANGELOG.md` cho lỗi/bài học, `PLAN.md` cho kế hoạch).

---

## 🎮 Tổng quan

**Void Runner** — endless runner 3D, 3 lane, tự chạy. Player = **tàu vũ trụ nhỏ**. "Hư Không" (The Void) đuổi theo **cơ chế 2 nấc** (Subway Surfers/Temple Run): đụng obstacle lần 1 → Void tiến sát; né sạch 12s → nới lại; **đụng lần 2 trong cửa sổ → Void nuốt → Game Over**. Nhặt coin + power-up, điểm cao nhất lưu lại. UI 100% tiếng Anh.

```
MainMenu → Game (chạy + né + thu thập) → Game Over → Retry / Menu
```

---

## ✅ Tính năng đã hoàn thiện (theo thứ tự thời gian)

### G1 — Core Gameplay

| # | Tính năng | File | Cách hoạt động | Ghi chú |
|---|---|---|---|---|
| 1 | Điều khiển 3 lane | `Core/Player/PlayerController.cs` | **Tàu vũ trụ nhỏ** (dựng từ primitive trong Awake) tự bay tới; A/D hoặc mũi tên đổi lane; lerp mượt + **banking nghiêng khi đổi lane**; đụng obstacle chỉ `RaiseObstacleHit` (không chết) | Rigidbody + Input System; tàu = Body/WingL/WingR/Cockpit/Engine + material neon code |
| 2 | Object pool tile | `Core/World/TileSpawner.cs` + `Tile.cs` | Pool sẵn tile, spawn trước + recycle sau lưng | Không GC spike giữa chừng |
| 3 | AI đuổi theo (Enemy) | `Core/World/EnemyChase.cs` | **Cơ chế 2 nấc cố định + BẮT** (không NavMeshAgent — track vô tận không bake được): nấc 0 giữ 16m (fix 2026-08-12 v3: 9m bị camera cắt — camera cách player 10m) → đụng obstacle lần 1 → nấc 1 áp sát 12m + **vỗ cánh nhanh hơn** (Animator.speed 2x) → né sạch 12s → nới về 16m → đụng lần 2 trong cửa sổ → **Enemy LAO TỚI BẮT** (clip `atack 1`) → chờ 1.1s → Game Over mượt | Điểm khác biệt so với runner thường — Enemy phản ánh lỗi của player, không tự tăng tốc |
| 4 | State machine | `Core/Game/GameManager.cs` | Menu → Playing → GameOver; phím R restart (tạm) | Event-driven |
| 5 | Obstacle weighted | `Core/World/ObstacleManager.cs` + `Data/ObstacleData.cs` | Spawn theo tỉ lệ, luôn chừa ≥1 lane an toàn | ScriptableObject |

### G2 — Hệ thống game

| # | Tính năng | File | Cách hoạt động | Ghi chú |
|---|---|---|---|---|
| 6 | Score + combo | `Systems/Score/ScoreSystem.cs` | Điểm theo khoảng cách (×10) + coin; combo ×2…×5, reset khi va chạm (vẫn giữ) | Event → UI, không coupling |
| 7 | Save best score + volume | `Systems/Save/SaveSystem.cs` | PlayerPrefs wrapper; best score chỉ ghi khi cao hơn | Sẵn sàng đổi JSON sau |
| 8 | 3 Power-up | `Systems/PowerUp/PowerUpSystem.cs` + `Data/PowerUpData.cs` | **Shield** (miễn nhiễm 3s), **Magnet** (hút coin 6m), **Slow-mo** (`timeScale=0.5` 3s) | Registry tĩnh coin — không GC mỗi frame |
| 9 | Audio | `Systems/Audio/AudioManager.cs` | BGM loop + 5 SFX (coin/death/powerup/lane/start); volume qua SaveSystem; DontDestroyOnLoad | Nghe `GameEvents` — zero coupling |
| 10 | Độ khó tăng dần | `Systems/Difficulty/DifficultyManager.cs` | Tốc độ 10→20 + mật độ 0.45→0.75 trong 60s qua AnimationCurve | Reset đúng khi Restart |
| 11 | MainMenu | `UI/Screens/MainMenuManager.cs` | Play / How to play / best score / sound toggle; **best score ẩn khi = 0**; load scene Game; text tiếng Anh (BEST SCORE / SOUND ON-OFF) | Scene riêng, Build Settings index 0 |

### G3 — Polish (đang làm)

| # | Tính năng | File | Cách hoạt động | Ghi chú |
|---|---|---|---|---|
| 12 | UI Kenney (Blue + font) | `Editor/MainMenuUIBuilder.cs` + `Editor/HUDUIBuilder.cs` | Tự dựng menu + HUD: sprite `panel_glass`, button Blue `button_rectangle_gloss/flat`, font `Kenney Future SDF` (sampling 128); tự gán field qua `SerializedObject` | 1608 PNG đã convert Sprite; tool chạy 1 nút |
| 13 | Game HUD + Game Over | `UI/UIManager.cs` | ScorePanel + coin icon + score + combo (ẩn khi ×1); Game Over: title + điểm + cao nhất + **nút CHƠI LẠI / MENU** | Panel fade DOTween; nút CHƠI LẠI gọi `GameManager.Restart()`, MENU load scene MainMenu |
| 14 | VFX | `Systems/VFX/VFXManager.cs` + `Editor/VFXSetupTool.cs` | **Particle**: coin burst tại vị trí coin + power-up burst (màu theo loại) — tạo 100% bằng code; **Popup điểm** "+10" nhân combo (DOTween bounce bay lên, pool 8 text, font Kenney Future) khi nhặt coin; **Screen shake** khi đâm obstacle (Cinemachine Impulse); **Vệt khói tối** theo Enemy (TrailRenderer code, nở rộng theo scale, clear khi restart). *2026-08-12 v3b: đã import Eric VFX Studio (15M) + JMO Cartoon FX (40M) — chưa tích hợp* | Sự kiện `GameEvents` (thêm `OnCoinCollectedAt(Vector3)` mang vị trí) — zero coupling; pool + `Emit()` → không GC spike |
| 15 | VFX trail Void + popup | *(gộp vào 14)* | ✅ Đã làm xong | — |
| 16 | Post-processing | `Editor/PostProcessingSetupTool.cs` + `Settings/PostProcessing/VoidRunnerProfile.asset` | **Global Volume** cả 2 scene: **Bloom** (intensity 0.35, tint xanh — coin/power-up phát sáng), **Vignette** (0.25 tối xanh — cảm giác "hư không"), **Color Adjustments** (contrast +8, saturation +6, filter lạnh); bật `renderPostProcessing` trên Main Camera (volumeTrigger + layerMask); tool 1 nút idempotent — tự sửa profile rỗng nếu có | Profile asset tạo bằng code → **phải `AddObjectToAsset` từng component** (bài học m_AtlasTextures) |
| 17 | Material/Lighting | `Editor/MaterialLightingSetupTool.cs` + `PlayerController.EnsureShipLight` | **5 material tông "hư không"**: Background tím đen, Player cyan phát sáng, Enemy (Flying Beetle) nguyên bản, PickUp vàng phát sáng, Obstacle cam phát sáng; **Directional Light** trắng lạnh 1.1 + shadow mềm; **Ambient** tím tối (Flat) + **Fog** ExponentialSquared tím nhẹ (chiều sâu); **Point Light cyan bám tàu** (tàu nổi bật nhất trên track tối — fix 2026-08-12 v3) | URP Lit + `_EMISSION` keyword + `RealtimeEmissive` GI — Bloom kích hoạt glow |
| 18 | Unity Test Framework | `Tests/EditMode` + `Tests/PlayMode` (2 asmdef + 6 file) | **24 test** (16 EditMode + 8 PlayMode): SaveSystem (best score/volume), GameEvents, ScoreSystem logic + combo tăng/clamp/reset theo thời gian thật, lane clamp | **Kết quả test thật: 24/24 xanh** ✅ |
| 19 | Assembly architecture | `Scripts/VoidRunner.Core.asmdef` + `Plugins/.../DOTween.Modules.asmdef` | Code chính thành custom assembly (test reference được); DOTween modules tách riêng | Bài học: predefined assembly không reference được từ custom asmdef |
| 20 | Kenney assets (còn 2 bộ) | `Art/kenney_ui-pack` + `kenney_ui-pack-space-expansion` | UI pack + space-expansion — **5 PNG đang dùng**: panel_glass, star, button_rectangle_flat/gloss (4 GUID trong scene) — CC0. **Đã xóa 2026-08-12 v3b**: game-icons, particle-pack, space-kit, space-station-kit (~58MB — không được tham chiếu) | Thay bằng 3 gói mới: OlegWER Asteroid + Eric VFX Studio + JMO Cartoon FX |
| 21 | WebGL + deploy | *(chưa làm)* | Build Brotli → itch.io + Unity Play + README | ⏳ Cuối cùng |

---

## 🎨 Tông màu & UI

- **Font:** Kenney Future (game-y) cho toàn bộ UI — asset `Art/Fonts/Kenney Future SDF.asset`
- **Sprite:** tông **Blue** (space/tech): `panel_glass` (panel kính), `button_rectangle_gloss` (nút chính bóng), `button_rectangle_flat` (nút phụ), `star` (icon coin vàng)
- **MainMenu:** title glow xanh + 3 nút + best score gold + HowToPlay panel kính
- **HUD Game:** ScorePanel góc trái (icon coin + số to), combo x2... dưới đó, Game Over panel trung tâm

---

## 📌 Trạng thái hiện tại

- ✅ G1 + G2 hoàn tất (commit theo convention, đã push)
- ✅ G3: **UI Kenney (menu + HUD)** + **VFX** (particle + popup điểm + screen shake + trail Void) + **Post-processing** (Bloom + Vignette + Color Grading) + **Material/Lighting** hoàn tất
- ✅ **Unity Test Framework: 24/24 test xanh** (EditMode 16 + PlayMode 8)
- 🔧 Đang làm: **3 gói mới (OlegWER Asteroid + Eric VFX + JMO Cartoon FX)** → tích hợp thay obstacle/VFX code-drawn (2026-08-12 v3b, chờ user duyệt từng bước)
- ✅ **REFACTOR GAMEPLAY (Giai đoạn 2.5) HOÀN TẤT (2026-08-11, user đã duyệt):** Enemy 2 nấc cố định + PlayMode test mới + player tàu vũ trụ + track 6000m + Game Over panel luôn hiện + UI tiếng Anh + best score ẩn khi 0 + layout nút âm thanh. Xem `PLAN.md` mục 2.5.
- ⏭️ Tiếp theo: user chạy tool `Refactor: Both Scenes` → test tay theo `REFERENCE.md` PART 2 (mục A–E + V1–V11) → Tuning/60 FPS → WebGL build → upload

*Chi tiết lỗi đã sửa + bài học: xem [`CHANGELOG.md`](CHANGELOG.md). Kế hoạch đầy đủ: [`PLAN.md`](PLAN.md).*


---

# PART 2 — TESTING (hướng dẫn test)

> **Mục đích:** checklist test toàn diện trước khi build WebGL. Chạy từng mục theo thứ tự,
> tick ✅ khi pass, ghi ❌ kèm mô tả lỗi vào cuối file (hoặc báo tôi → tôi ghi CHANGELOG).
> ⏱️ Thời gian ước tính: 15–20 phút / lượt.

---

## 📋 Quy trình test nhanh

| Bước | Scene | Việc làm |
|---|---|---|
| 1 | MainMenu | Mở game → kiểm tra menu (mục A) |
| 2 | Game | Bấm PLAY → chạy thử (mục B) |
| 3 | Game | Chết → Game Over → Retry / Menu (mục C) |
| 4 | Game | Chạy lâu 60s+ → độ khó tăng (mục D) |
| 5 | Cả 2 | Console sạch lỗi (mục E) |

---

## A. MainMenu

| # | Kiểm tra | Kết quả |
|---|---|---|
| A1 | Tựa đề **"VOID RUNNER"** font Kenney Future sắc nét — **CHỈ 1 chữ** (TitleGlow đã xóa hẳn — fix 2026-08-12 v3) | ☐ |
| A2 | Nền tối tím + hơi sương mù, menu không chói | ☐ |
| A3 | 3 nút (PLAY / HOW TO PLAY / âm thanh) có sprite Blue + hiệu ứng hover sáng | ☐ |
| A4 | Best score **ẨN khi lần đầu chơi (= 0)** — chỉ hiện khi đã có điểm thật (R0.6) | ☐ |
| A5 | Bấm **HOW TO PLAY** → panel kính hiện (nội dung tiếng Anh) + bấm lại để ẩn | ☐ |
| A6 | Bấm nút **âm thanh** → icon đổi trạng thái, BGM tắt/bật theo | ☐ |
| A7 | Bấm **PLAY** → chuyển sang scene Game | ☐ |

## B. Gameplay

| # | Kiểm tra | Kết quả |
|---|---|---|
| B1 | Game bắt đầu: player tự chạy, có BGM + không lỗi Console | ☐ |
| B2 | Bấm **A/D hoặc ←/→** → đổi lane mượt (lerp, không giật) | ☐ |
| B3 | Nhặt coin → **"+10" bay lên** (vàng, bounce) + bụi vàng bắn ra + SFX coin | ☐ |
| B4 | Score tăng liên tục theo quãng đường + coin | ☐ |
| B5 | **Combo**: chạy 5s không va chạm → combo ×2 (hiện "x2" dưới score), nhặt coin → "+20" | ☐ |
| B6 | Nhặt **Shield** (xanh) → vòng hạt xanh, trong 3s đâm obstacle **không chết** | ☐ |
| B7 | Nhặt **Magnet** (đỏ) → hút coin xa về phía player (bụi đỏ) | ☐ |
| B8 | Nhặt **SlowMo** (tím) → game chậm lại 3s (bụi tím) | ☐ |
| B9 | Đâm obstacle → **camera rung nhẹ** (screen shake) + SFX death | ☐ |
| B10 | Sau lưng có **Enemy = Flying Beetle** đuổi theo (cánh vỗ bay) + **vệt khói tối** phía sau | ☐ |
| B11 | Coin/player/obstacle **phát sáng** trong bóng tối (Bloom) | ☐ |
| B12 | Không thấy hộp vật lý lạ, không xuyên sàn, không bay lung tung | ☐ |

## C. Game Over + restart

| # | Kiểm tra | Kết quả |
|---|---|---|
| C1 | Chết → panel Game Over hiện (fade mượt), hiện **SCORE + BEST** (vàng, tiếng Anh) | ☐ |
| C2 | Bấm **CHƠI LẠI** → game chạy lại từ đầu, score reset, combo reset, **vệt khói Enemy không kéo dài xuyên map** | ☐ |
| C3 | Bấm **MENU** → quay về MainMenu | ☐ |
| C4 | Vào lại game → **Best score đã lưu** (PlayerPrefs) | ☐ |
| C5 | Chơi 2 lần liên tiếp → không lỗi singleton (audio không nhân đôi, manager không trùng) | ☐ |

## D. Độ khó & ổn định

| # | Kiểm tra | Kết quả |
|---|---|---|
| D1 | Chạy đủ **60s** → tốc độ tăng dần (cảm nhận rõ rệt), mật độ obstacle dày hơn | ☐ |
| D2 | Chạy 60s+ → FPS vẫn mượt (không giật mạnh / không tụt FPS dần) | ☐ |
| D3 | Luôn có ≥1 lane an toàn (không bao giờ bị obstacle chặn hết cả 3 lane) | ☐ |
| D4 | Score > 10.000 chạy tiếp bình thường (không tràn số) | ☐ |

## E. Console & build

| # | Kiểm tra | Kết quả |
|---|---|---|
| E1 | **Console = 0 lỗi đỏ** (cho phép warning `m_AtlasTextures` cũ đã hết) | ☐ |
| E2 | Không có `NullReferenceException`, `UnassignedReferenceException` khi chơi | ☐ |
| E3 | (Sau build WebGL) FPS ổn trên browser, âm thanh phát được | ☐ |
| E4 | (Sau build WebGL) Game chạy được bằng cả bàn phím — không cần click vào canvas trước | ☐ |

---

## 📝 Ghi lỗi tìm thấy

> Sao chép mẫu dưới đây cho mỗi lỗi, ghi vào cuối file rồi báo tôi (tôi fix + ghi CHANGELOG).

```
### Lỗi: [mô tả ngắn]
- Mục: [VD: B3]
- Mức độ: [Nghiêm trọng / Trung bình / Nhẹ]
- Bước tái hiện: [1. ... 2. ... 3. ...]
- Console log (nếu có): [paste]
- Ảnh/video: [đường dẫn]
```

---

## 🐞 Nhật ký bug phát hiện khi test

> Mỗi lần test, ghi bug tìm thấy vào đây (kèm trạng thái) — tránh lặp lại, tạo nguồn tài liệu
> cho QA. Bug đã fix sẽ được đánh dấu ✅.

### Bug: `BestScore_DefaultsToZero` fail trong EditMode tests (SaveSystemTests)
- Mục: **EditMode tests (Unity Test Runner)**
- Mức độ: Nhẹ (lỗi test bị ảnh hưởng bởi dữ liệu cũ — KHÔNG phải lỗi logic game)
- Ngày phát hiện: 2026-08-10
- Bước tái hiện: 1. Chơi game thật (ghi best score vào PlayerPrefs) 2. Mở Test Runner → EditMode → Run All 3. Test `BestScore_DefaultsToZero` fail (đọc phải score cũ)
- Nguyên nhân: Test chỉ xóa PlayerPrefs trong `[TearDown]` (SAU test) — KHÔNG xóa TRƯỚC. Test đầu tiên đọc phải dữ liệu save thật còn sót từ lần chơi trước.
- Cách fix: Thêm `[SetUp]` gọi `PlayerPrefs.DeleteAll() + Save()` trước mỗi test.
- Trạng thái: ✅ Đã fix (2026-08-10)

---

## 🔄 Cơ chế Enemy mới — Subway Surfers / Temple Run (sau refactor)

> ✅ Refactor gameplay ĐÃ CODE (2026-08-11) — các mục này test được ngay.
> 🧪 **Test tự động đi kèm**: `EnemyChasePlayTests` (PlayMode, 5 test) — đã chạy/validate trước khi test tay.
> ⚠️ **2026-08-12 v3**: khoảng cách đổi 9→16m / 7.5→12m (camera cắt màn hình); hit 2 = cảnh bắt (atack) chờ 1.1s mới Game Over — chờ ~1.5s khi test V3.

| # | Kiểm tra | Kết quả |
|---|---|---|
| V1 | Đụng vật cản lần 1 → **KHÔNG chết**, Enemy tiến sát (16→12m) + **vỗ cánh nhanh hơn** | ☐ |
| V2 | Không chạm vật cản trong **10–15s** → Enemy NỚI LẠI khoảng cách ban đầu | ☐ |
| V3 | Đụng **2 lần trong cửa sổ 10–15s** → Enemy **lao tới bắt (clip atack 1)** → ~1.1s sau Game Over panel fade mượt | ☐ |
| V4 | Game Over panel **LUÔN hiện** khi chết (không bao giờ "chết mà không thấy màn hình") | ☐ |
| V5 | Enemy **không tự tăng tốc** theo thời gian — chỉ tiến sát khi player lỗi | ☐ |
| V6 | Player = **tàu vũ trụ nhỏ** (không còn banh xanh), banking khi đổi lane, nhìn hợp lý | ☐ |
| V7 | Track chạy **> 400m không hết đường** (Ground 6000m + tile recycle) | ☐ |
| V8 | **Toàn bộ text gameplay + menu = tiếng Anh** (RETRY/SCORE/BEST/SOUND ON-OFF/HowToPlay) | ☐ |
| V9 | Best score **ẩn khi = 0** ở MainMenu; hiện khi đã có điểm | ☐ |
| V10 | Nút âm thanh: text không thụt vào viền, không quá chật | ☐ |
| V11 | Enemy **phình to hơn khi tiến sát** (nấc 1 đe dọa — vẫn thấy cả con bọ, không che tàu) | ☐ |

---

---

## ✨ Task A/B/D (2026-08-11/12 — Credits · Enemy quái vật · Chọn tàu)

> ⚠️ **Chạy tool TRƯỚC khi test** (mỗi tool 1 lần, idempotent):
> 1. Mở scene **Game** → `Tools/Void Runner/Setup Enemys` (gán 3 quái vật) + `Setup Ship Select` (gán 2 tàu)
> 2. Mở scene **MainMenu** → `Tools/Void Runner/Setup Ship Select` (tạo ShipSelectManager + gán prefab)
> 3. **Ctrl+S** cả 2 scene. *(Nếu quên: ShipCatalog tự tải khi chưa gán — nhưng build cần tool.)*

| # | Kiểm tra | Kết quả |
|---|---|---|
| T1 | MainMenu: nút **CREDITS** (bên phải, cùng hàng SHIP) → panel tím/đen + viền cyan + tiêu đề vàng, liệt kê đủ third-party assets (Kenney/Nebula/SpaceSkies + 5 model) | ☐ |
| T2 | Panel Credits có nút **CLOSE** đóng được; dimmer tối phía sau, không kẹt | ☐ |
| T3 | Game Over: có nút **CREDITS** phía dưới (ẩn cùng panel, không lộ khi chơi) → mở được panel | ☐ |
| T4 | MainMenu: nút **SHIP** (bên trái, cùng hàng CREDITS) → panel SELECT SHIP: preview 3D tàu xoay + tên tàu (SF FIGHTER/SPARROW) | ☐ |
| T5 | Panel chọn tàu: nút **< >** đổi tàu (preview đổi theo), **SELECT** lưu chọn, **CLOSE** đóng | ☐ |
| T6 | Bấm PLAY → Game: tàu hiển thị = model đã chọn (SF Fighter hoặc Sparrow — KHÔNG phải tàu primitive cũ), có flame đuôi + exhaust | ☐ |
| T7 | Vào game: Enemy = **QUÁI VẬT** (1 trong 3: Monster/Flying Beetle/Spider) đuổi theo, không còn banh tím; scale hợp lý | ☐ |
| T8 | Quái vật KHÔNG xoay lung tung (Animator chạy bình thường), không có collider con chặn đường | ☐ |
| T9 | Chọn tàu khác → về MainMenu → PLAY lại → tàu mới vẫn giữ (SaveSystem.SelectedShip) | ☐ |
| T10 | Console 0 lỗi đỏ khi mở cả 2 panel + đổi tàu | ☐ |

---

## 🛠️ Công cụ test tự động (đang xem xét — tham khảo)

| Công cụ | Loại | Dành cho | Có cần thiết? |
|---|---|---|---|
| **Unity Test Framework** (có sẵn trong Unity) | Unit/Integration test (C#) | Logic game: score, combo, spawn, power-up | ⭐ Nên làm (test logic nhanh, miễn phí) |
| **Playwright** (sau khi có WebGL build) | E2E trên browser | Load game, bấm UI, check **Console 0 lỗi**, chụp màn hình | ⭐ Nên làm (tự động verify mỗi lần build) |
| **AltTester** | E2E Unity SDK | Đọc GameObject/text score trong game, bấm nút theo path | ⭕ Chỉ khi cần test sâu |
| **Appium** | Mobile test | Game mobile native | ❌ Không (game là WebGL) |

> **Gợi ý tối ưu cho project này:** Unity Test Framework cho logic (score/spawn/combo)
> + Playwright cho WebGL (console error + UI smoke test). Cả 2 đều miễn phí.
> Chi tiết: xem phần trả lời của tôi về công cụ test trong conversation.


---

# PART 3 — CREDITS (bản quyền assets)

> Tất cả các asset bên dưới **KHÔNG thuộc về tác giả dự án** — chúng được sử dụng theo giấy phép
> tương ứng của từng nhà phát triển. File này liệt kê đầy đủ để tuân thủ attribution khi
> phát hành game (itch.io / Unity Play / WebGL build).

---

## 🎨 Gói asset đang dùng trong game

| Asset | Tác giả | Giấy phép | Link | Ghi chú |
|---|---|---|---|---|
| **Nebula Skyboxes** (4 cubemap `.exr`) | Xem ghi chú bên dưới | Unity Asset Store Standard EULA (theo nguồn tải) | [Nebula Skyboxes — Unity Asset Store](https://assetstore.unity.com/packages/2d/textures-materials/sky/nebula-skyboxes-219924) | Skybox tinh vân cho Game + MainMenu |
| **SpaceSkies Free** | PULSAR BYTES | **Standard Unity Asset Store EULA** | [SpaceSkies Free — Unity Asset Store](https://assetstore.unity.com/packages/2d/textures-materials/sky/spaceskies-free-80503) | Skybox sao (Pink/Green/Purple) — fallback nhẹ |
| **Kenney UI Pack** | Kenney (kenney.nl) | **CC0 1.0 (Public Domain)** | [kenney.nl/assets/ui-pack](https://kenney.nl/assets/ui-pack) | Sprite UI chính |
| **Kenney UI Pack — Space Expansion** | Kenney | **CC0 1.0** | [kenney.nl/assets/ui-pack-space-expansion](https://kenney.nl/assets/ui-pack-space-expansion) | Sprite UI mở rộng |
| **OlegWER — High-Poly Asteroid** | OlegWER | Standard Unity Asset Store EULA (theo nguồn tải) | Tìm "High-Poly Asteroid" trên Unity Asset Store | ⭐ Thay obstacle code-drawn bằng thiên thạch (import 2026-08-12) |
| **Eric VFX Studio — Free Game VFX** | Eric VFX Studio | Standard Unity Asset Store EULA | [assetstore.unity.com](https://assetstore.unity.com) | ⭐ Thay particle code bằng prefab VFX (FX_Fireball, FX_Green_Hit...) (import 2026-08-12) |
| **JMO Assets — Cartoon FX Remaster** | JMO Assets | Standard Unity Asset Store EULA | [assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) | ⭐ VFX tàu (Explosions, Fire, Impacts...) (import 2026-08-12) |
| **Kenney Fonts (Kenney Future)** | Kenney | **CC0 1.0** | [kenney.nl/assets/kenney-fonts](https://kenney.nl/assets/kenney-fonts) | Font UI / HUD |
| **Kenney Audio Packs** (music/sfx) | Kenney | **CC0 1.0** | [kenney.nl/assets](https://kenney.nl/assets) | Nhạc nền + hiệu ứng âm thanh |
| **Free SF Fighter** | CGPitbull | **Standard Unity Asset Store EULA** | [assetstore.unity.com/packages/3d/vehicles/space/free-sf-fighter-11711](https://assetstore.unity.com/packages/3d/vehicles/space/free-sf-fighter-11711) | Tàu player tùy chọn #1 (Ship Select) |
| **Star Sparrow Modular Spaceship** | Ebal Studios | **Standard Unity Asset Store EULA** | [assetstore.unity.com/packages/3d/vehicles/space/star-sparrow-modular-spaceship-73167](https://assetstore.unity.com/packages/3d/vehicles/space/star-sparrow-modular-spaceship-73167) | Tàu player tùy chọn #2 (Ship Select) |
| **Level 1 Monster Pack** | — | **Standard Unity Asset Store EULA** | [assetstore.unity.com/packages/3d/characters/creatures/level-1-monster-pack-77703](https://assetstore.unity.com/packages/3d/characters/creatures/level-1-monster-pack-77703) | Void monster #1 (Monster Skin1) |
| **Free Fantasy Spider** | — | **Standard Unity Asset Store EULA** | [assetstore.unity.com/packages/3d/characters/creatures/free-fantasy-spider-10104](https://assetstore.unity.com/packages/3d/characters/creatures/free-fantasy-spider-10104) | Void monster #3 (spider) |
| **Flying Beetle** | — | **Standard Unity Asset Store EULA** | Tải free trên Unity Asset Store (tìm "Flying Beetle") | Void monster #2 — có animation bay |

> ℹ️ **5 gói model trên (SF_Fighter / Sparrow_Fighter / Monster / Flying Beetle / fantasySpider)
> KHÔNG nằm trong repo** (tổng ~2.5GB — GitHub chặn file >100MB, xem `.gitignore`).
> Khi clone project cần tự tải lại từ link trên rồi chạy tool `Void Runner → Setup…` để gán lại.
> Nếu thiếu model: game vẫn chạy (fallback: tàu primitive, Void = black hole).

> ℹ️ **Kenney CC0 = Public Domain** — được dùng thoải mái cho mọi mục đích (kể cả thương mại),
> **không bắt buộc ghi công**. File `License.txt` nằm kèm trong từng thư mục gói.
> ⚠️ Logo Kenney KHÔNG thuộc CC0 — không dùng logo trong game.
> ℹ️ **Đã xóa 2026-08-12 v3b** (~58MB, không được tham chiếu): Kenney Space Kit, Space Station Kit,
> Game Icons, Particle Pack — thay bằng OlegWER Asteroid + Eric VFX Studio + JMO Cartoon FX.

---

## 📌 Lưu ý bản quyền quan trọng

1. **SpaceSkies Free** — dùng theo **Standard Unity Asset Store EULA**: được dùng trong dự án thương mại
   khi asset đã được *nhúng vào sản phẩm hoàn chỉnh* (game của bạn). **CẤM**: bán lại/redistribute gói
   dưới dạng standalone, dùng texture đơn lẻ làm sản phẩm riêng, hoặc dùng cho AI/ML training
   (theo điều khoản Unity). Không bắt buộc ghi công, nhưng đã ghi trong file này.

2. **Nebula Skyboxes** — license theo **nguồn tải** (Unity Asset Store Standard EULA). Xác nhận lại
   từ trang asset trước khi publish; nếu tải từ itch.io, license có thể là "free/CC BY — tùy gói".

3. **Âm nhạc & SFX** — các file trong `Assets/_Project/Audio/` lấy từ Kenney (CC0) — kiểm tra lại
   `License.txt` kèm trong thư mục audio để chắc chắn.

4. **Assets self-made** — toàn bộ script C# (`Assets/_Project/Scripts/`), material, prefab tự dựng,
   texture procedural tạo bằng code thuộc về tác giả dự án.

---

## ✍️ Đề xuất ghi công trong màn hình credits / README

```text
THIRD-PARTY ASSETS
- SpaceSkies Free by PULSAR BYTES (Unity Asset Store EULA)
- Nebula Skyboxes (Unity Asset Store EULA)
- UI Pack + Font "Kenney Future" + Audio by Kenney (CC0 Public Domain)
- High-Poly Asteroid by OlegWER (Unity Asset Store EULA)
- Free Game VFX by Eric VFX Studio (Unity Asset Store EULA)
- Cartoon FX Remaster by JMO Assets (Unity Asset Store EULA)
- SF Fighter by CGPitbull · Sparrow by Ebal Studios · Flying Beetle · Monster Pack · Fantasy Spider (Unity Asset Store EULA)
```

> Khi build WebGL lên itch.io / Unity Play, nên thêm phần Credits này vào README hiển thị
> hoặc một màn hình Credits trong game.


---

# PART 4 — COMMIT TEMPLATES (quy ước commit)

## Delivery và Commits

- Với mỗi yêu cầu thay đổi file trong repository, hãy thực hiện công việc, validate, rồi tự động tạo commit.
- Không commit nếu validation thất bại. Báo lỗi và sửa trước khi commit.
- Giữ mỗi commit tập trung vào một thay đổi nhất quán.

## Quy ước Commit

### Định dạng
```
<type>(<scope>): <subject>
```

### Các Type được phép
| Type | Khi nào dùng |
|---|---|
| `feat` | Thêm tính năng mới (gameplay, hệ thống, UI) |
| `fix` | Sửa bug |
| `refactor` | Refactor code, không thay đổi logic |
| `chore` | Cập nhật package, cấu hình, build settings, gitignore |
| `opt` | Tối ưu hiệu năng, FPS, memory, GC allocation |
| `test` | Thêm/sửa test |
| `build` | Build WebGL và triển khai (itch.io, Unity Play) |
| `docs` | README, plan, tài liệu |

### Các Scope được phép
| Scope | Mô tả |
|---|---|
| `core` | GameManager, state machine, luồng game, GameEvents |
| `player` | PlayerController, điều khiển lane, physics |
| `world` | TileSpawner, Tile, object pool, track |
| `void` | VoidChase, NavMesh AI, tốc độ/kích thước void |
| `obstacle` | ObstacleManager, ObstacleData, loại obstacle |
| `pickup` | Coin, thu thập, Rotator |
| `powerup` | PowerUpSystem, PowerUpData, hiệu ứng shield/magnet/slow-mo |
| `score` | ScoreSystem, combo multiplier, event score |
| `audio` | AudioManager, SFX, BGM, volume |
| `save` | SaveSystem, PlayerPrefs, best score |
| `difficulty` | DifficultyManager, AnimationCurve, tuning |
| `ui` | HUD, MainMenu, GameOver, fade, màn hình |
| `scene` | Setup scene, camera, light, NavMesh bake |
| `prefab` | Tạo/cập nhật prefab |
| `data` | ScriptableObject instances |
| `vfx` | Particle, post-processing, screen shake, trail |
| `config` | Packages/manifest.json, ProjectSettings, .gitignore |
| `deps` | Dependencies và lockfile |
| `build` | WebGL build settings, deploy |

### Ví dụ
```
feat(player): viết lại điều khiển chuyển lane 3 làn
feat(world): thêm TileSpawner dùng object pool
feat(void): thêm AI đuổi theo, tốc độ tăng dần theo thời gian
fix(world): vá lỗi hở giữa các tile khi spawn nhanh
opt(world): giảm GC alloc trong vòng lặp recycle tile
feat(ui): thêm màn Game Over hiển thị best score
chore(config): cài package Cinemachine và DOTween
build(build): build WebGL v1.0 với Brotli và publish itch.io
docs(readme): cập nhật README với link demo
```

### Quy tắc
1. Subject dùng tiếng Việt **có đầy đủ dấu** (không viết tắt không dấu), nhất quán trong 1 PR — ví dụ `feat(player): thêm khả năng chuyển lane` (không phải `feat(player): them kha nang chuyen lane`)
2. Subject **KHÔNG** viết hoa chữ đầu
3. Subject **KHÔNG** có dấu chấm cuối
4. Viết commit body khi cần giải thích thêm logic hoặc lý do thay đổi
5. Asset của Unity (`prefab`, `scene`, `.meta`) luôn commit **kèm trong cùng commit** với code liên quan — không commit file `.meta` riêng lẻ, không xóa `.meta` của asset đang được tham chiếu

## Validation

- Chạy các kiểm tra hẹp nhất trước, sau đó mở rộng khi cần.
- Luôn chạy `git diff --check` trước khi commit.
- ⚠️ Unity: `git diff --check` sẽ luôn báo trailing whitespace ở file `.meta` (Unity sinh sẵn `userData: `/`assetBundleName: ` có space cuối — là chuẩn) và thư mục vendor (`Assets/Plugins/Demigiant/DOTween`) — **bỏ qua các cảnh báo này**, chỉ quan tâm cảnh báo từ code C# của mình.
- Thay đổi **C# script**: mở Unity Editor và xác nhận Console **không có lỗi compile** (hoặc chạy Unity batchmode `-batchmode -quit` để verify import). Playtest nhanh tính năng liên quan.
- Thay đổi **scene/prefab**: mở scene trong Unity, kiểm tra **không missing script / missing reference**, playtest nhanh luồng liên quan.
- Thay đổi **package/config**: kiểm tra `Packages/manifest.json` hợp lệ và Unity import không lỗi.
- Thay đổi **build**: build WebGL và chạy thử trên trình duyệt.
- Không commit nếu validation thất bại — báo lỗi và sửa trước.

## Documentation

- Review `README.md` sau mỗi thay đổi.
- Cập nhật `README.md` trong cùng commit khi có thay đổi về setup, cấu hình, lệnh, kiến trúc, hoặc hành vi người dùng thấy được.
- Không chỉnh tài liệu cho các thay đổi nội bộ không ảnh hưởng đến cách dùng repository.


---

# PART 5 — AGENT B GUIDE (onboard agent thứ 2)

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
2. `agent/DECISIONS.md` — quyết định thiết kế game đã chốt (player=tàu, enemy 2 nấc...).
3. `agent/CHANGELOG.md` — lịch sử bug theo vòng.
4. `agent/PLAN.md` — kế hoạch tính năng + trạng thái hoàn thành.
5. `agent/REFERENCE.md` (chính file này) — PART 2 TESTING + PART 4 Commit (format bắt buộc).

## 3. Luật git (bất biến — bài học R6.12)

1. **Chỉ Agent A commit + push `main`.** Agent B làm xong → **báo user** để Agent A commit, HOẶC tạo branch riêng `agent-b`.
2. **Commit nhỏ + push sớm**: 1 tính năng/bug = 1 commit, không ôm đống.
3. Commit message: theo `REFERENCE.md` PART 4, tiếng Việt có dấu, prefix `feat/fix/docs/refactor/chore` + scope.
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
