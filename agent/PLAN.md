# Void Runner — Kế hoạch hành động (Action Plan)

> Hyper-casual endless runner 3D + **AI Chase** · Unity 6 URP · Game Production
> ⚠️ **Trạng thái:** Đã chốt concept — CHƯA bắt đầu code. Tick checkbox khi hoàn thành từng task.

---

## 1. Concept game (đã chốt ✅ — cập nhật 2026-08-11 theo review user)

**Void Runner**: nhân vật chạy (tàu/drone — chờ user chốt kiểu) trên đường tile 3 lane vô tận, tự chạy về phía trước. Phía sau, **"Enemy" (Flying Beetle)** — khối bóng tối phình to — đuổi theo kiểu **Subway Surfers / Temple Run**: KHÔNG tự tăng tốc, chỉ TIẾN SÁT khi player đụng vật cản. Đụng 2 lần trong cửa sổ 10–15s → Enemy nuốt → Game Over.

```
Chạy + né obstacle (đụng → Enemy tiến sát) → không chạm 10–15s → Enemy nới lại → thu coin/power-up → chạm lần 2 trong cửa sổ → Enemy nuốt → Game Over → thử lại (best score)
```

**Điểm khác biệt vs. runner thường:** cơ chế "Enemy tiến sát theo lỗi của player" — mỗi lần va chạm vật cản đều có hậu quả rõ ràng (Enemy gần hơn), tạo căng thẳng tăng dần phản ánh skill.

| Thông tin | Chi tiết |
|---|---|
| Thể loại | Hyper-casual endless runner 3D + chase |
| Engine | Unity 6 · URP 17.4.0 |
| Ngôn ngữ | C# thuần — không asset store gameplay |
| Nền tảng build | **WebGL** (itch.io chính + Unity Play dự phòng) |
| Mục tiêu | Game production hoàn chỉnh, phát hành WebGL |
| Ngôn ngữ UI | **Tiếng Anh** (gameplay + menu) |

---

## 2. Hiện trạng codebase (đã khảo sát)

| Hạng mục | Trạng thái | Xử lý |
|---|---|---|
| Unity 6 + URP + Input System | ✅ Có | Giữ |
| Scene `Minigame` (Player, Enemy NavMesh, PickUp, Obstacle Ramp/Pillar, Canvas) | ✅ Có | **ĐÃ XÓA HẲN 2026-08-11** (cùng `_Archive/` + NavMesh-Ground) — không cần test NavMesh nữa (Enemy chase bỏ NavMeshAgent) |
| `PlayerController.cs` (Roll-a-Ball: AddForce, FindGameObjectWithTag) | ✅ Có | **Viết lại** (lane-switching) |
| `EnemyMovement.cs` (NavMeshAgent đuổi theo) | ✅ Có | **ĐÃ XÓA HẲN 2026-08-11** — chuyển sang `EnemyChase.cs` (2 nấc cố định, không NavMesh) |
| `Rotator.cs` (xoay PickUp) | ✅ Có | Giữ → dùng cho coin |
| `CameraController.cs` (follow đơn giản) | ✅ Có | Thay bằng Cinemachine (G3) |
| Prefab `DynamicBox`, `DynamicBox 1`, `PickUp` | ✅ Có | Giữ làm obstacle di động + coin placeholder |
| Materials (Background, Enemy, PickUp, Player...) | ✅ Có | Giữ làm placeholder |
| NavMesh setup (`NavMesh-Ground.asset`) | ✅ Có | Bake lại cho track của scene Game |
| Windows build (`Builds/`) | ✅ Có | Không dùng cho release — thay bằng WebGL |
| MainMenu scene, Object Pool, Score, PowerUp, Audio, Save, Difficulty | ❌ Chưa có | Xây mới (G1–G3) |
| Cinemachine, DOTween | ❌ Chưa có | Cài package (G0) |

---

## 3. Kiến trúc mục tiêu — Clean Architecture

> Nguyên tắc: **phân tầng, dependency hướng vào trong** — `UI → Systems → Core`. Core thuần gameplay, không biết UI. Giao tiếp qua **event / interface**, không gọi trực tiếp.

```
Assets/
├── _Project/                        # ★ Toàn bộ asset của game — tách biệt plugin
│   ├── Scripts/
│   │   ├── Core/                    # ★ Gameplay thuần — không phụ thuộc UI
│   │   │   ├── Game/
│   │   │   │   ├── GameManager.cs   # State machine: Menu → Playing → GameOver
│   │   │   │   └── GameEvents.cs    # static events: OnGameOver, OnRestart...
│   │   │   ├── Player/
│   │   │   │   ├── PlayerController.cs # Lane switching (viết lại)
│   │   │   │   └── PlayerEvents.cs  # OnPlayerDied, OnLaneChanged...
│   │   │   └── World/
│   │   │       ├── TileSpawner.cs   # Object pool
│   │   │       ├── Tile.cs          # Di chuyển, recycle, spawn nội dung
│   │   │       ├── ObstacleManager.cs # Gắn obstacle ngẫu nhiên lên tile
│   │   │       ├── Obstacle.cs      # Va chạm → chết
│   │   │       ├── EnemyChase.cs     # Enemy 2 nấc cố định đuổi theo (Flying Beetle — 2026-08-12)
│   │   │       └── Pickup.cs        # Coin + Rotator
│   │   ├── Systems/                 # ★ Dịch vụ độc lập — đăng ký qua event
│   │   │   ├── Input/
│   │   │   │   └── InputReader.cs   # Wrapper Input System → event lane change
│   │   │   ├── Score/
│   │   │   │   └── ScoreSystem.cs   # Event-driven, combo multiplier
│   │   │   ├── PowerUp/
│   │   │   │   ├── PowerUpSystem.cs # Shield / Magnet / Slow-mo
│   │   │   │   └── PowerUpEffect.cs # Triển khai từng hiệu ứng
│   │   │   ├── Audio/
│   │   │   │   └── AudioManager.cs  # Singleton, DontDestroyOnLoad
│   │   │   ├── Save/
│   │   │   │   └── SaveSystem.cs    # PlayerPrefs best score + volume
│   │   │   └── Difficulty/
│   │   │       └── DifficultyManager.cs # AnimationCurve tốc độ, mật độ
│   │   ├── UI/                      # ★ Presentation — chỉ lắng nghe event
│   │   │   ├── HUD/
│   │   │   │   └── HUDView.cs       # Score, multiplier, tốc độ
│   │   │   ├── Screens/
│   │   │   │   ├── MainMenuView.cs  # Play / How to play / Highscore
│   │   │   │   └── GameOverView.cs  # Score, best, Retry/Menu
│   │   │   └── Widgets/
│   │   │       └── ScreenFader.cs   # DOTween fade
│   │   ├── Data/                    # ★ ScriptableObject definitions
│   │   │   ├── ObstacleData.cs
│   │   │   └── PowerUpData.cs
│   │   ├── Interfaces/              # ★ Contracts: IScoreProvider, IAudioService
│   │   └── Utils/
│   │       ├── ObjectPool.cs        # Generic pool tái sử dụng
│   │       └── Singleton.cs         # Base singleton
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   └── Game.unity
│   │   # (_Archive/ đã xóa 2026-08-11 — không còn scene test NavMesh)
│   ├── Prefabs/
│   │   ├── Player/  Tiles/  Obstacles/  PowerUps/  Pickups/  UI/
│   ├── ScriptableObjects/
│   │   ├── ObstacleData/  PowerUpData/
│   ├── Art/
│   │   ├── Materials/  VFX/  Textures/
│   └── Audio/
│       ├── Music/  SFX/
├── Settings/                        # (giữ nguyên — URP render assets)
├── TextMesh Pro/                    # (giữ nguyên — Unity quản lý)
└── TutorialInfo/                    # (xóa/archive — không cần cho game)
```

**Map file hiện tại → vị trí mới:**

| Hiện tại | Vị trí mới | Ghi chú |
|---|---|---|
| `Scripts/PlayerController.cs` | `_Project/Scripts/Core/Player/` | Viết lại ở G1 |
| `Scripts/EnemyMovement.cs` | `_Project/Scripts/Core/World/EnemyChase.cs` | Viết lại ở G1 — bản cũ đã xóa 2026-08-11 |
| `Scripts/CameraController.cs` | `_Project/Scripts/_Archive/` (tạm) | Thay Cinemachine ở G3 |
| `Scripts/Rotator.cs` | `_Project/Scripts/Core/World/Pickup.cs` | Gộp vào coin |
| `Prefabs/PickUp.prefab` | `_Project/Prefabs/Pickups/` | Coin |
| `Prefabs/DynamicBox*.prefab` | `_Project/Prefabs/Obstacles/` | Obstacle di động |
| `Materials/*` | `_Project/Art/Materials/` | Placeholder |
| `Scenes/*` | `_Project/Scenes/` | `_Archive` cho Minigame |

**Scene flow:** `MainMenu` → `Game` (chơi) → Game Over overlay → Retry / Menu

---

## 4. Kế hoạch theo giai đoạn

### Giai đoạn 0 — Nền tảng (Setup)
> Mục tiêu: project sạch, đúng cấu trúc, đủ package

- [x] Project Unity 6 + URP + GitHub repo + `.gitignore`
- [x] Thêm **Cinemachine 3.1.7** vào `Packages/manifest.json` (Unity tự import khi mở lại)
- [x] Cài **DOTween** từ Asset Store (miễn phí) — đã import `Assets/Plugins/Demigiant/DOTween` + có `DOTweenSettings.asset`
- [x] Tái cấu trúc `Assets/_Project/` theo kiến trúc mục 3: đã di chuyển Scripts → `Core/Player`, `Core/World`; Prefabs → `Pickups`, `Obstacles`; Materials → `Art/Materials`; Input actions → `Settings`; đã xóa `SampleScene`, `TutorialInfo`, `CameraController.cs`
- [x] Tạo 2 scene `MainMenu` + `Game`; đưa vào **Build Settings** (MainMenu index 0, Game index 1) — ✅ đã xong
- [x] Archive scene `Minigame` + `NavMesh-Ground.asset` → `Assets/_Project/Scenes/_Archive/` (giữ làm nơi test NavMesh bake) — **sau đó ĐÃ XÓA HẲN cả `_Archive/` 2026-08-11** (Enemy không dùng NavMesh nữa)

### Giai đoạn 1 — Core Gameplay
> Mục tiêu: **chạy được · né được · chết được** — core loop hoàn chỉnh

- [x] **`PlayerController.cs`** (viết lại): Rigidbody bóng tự lăn về trước; chuyển lane trái/phải (A/D + mũi tên); lerp mượt giữa 3 lane; `OnTriggerEnter` obstacle → chết — ✅ code xong, commit `feat(player)`
- [x] **`TileSpawner.cs`** (Object Pool): pool sẵn N tile, spawn phía trước + recycle sau lưng player — không Instantiate/Destroy giữa chừng — ✅ commit `feat(world)`
- [x] **`Tile.cs`**: activate/deactivate, `ObstacleManager` gắn obstacle khi spawn — ✅ commit `feat(world)`
- [x] **`EnemyChase.cs`** (từ `EnemyMovement.cs`): NavMeshAgent đuổi theo player; tốc độ + scale tăng dần theo thời gian; bắt kịp → Game Over — ✅ commit `feat(void)`
- [x] **`GameManager.cs`**: state machine (Menu/Playing/GameOver); event `OnGameOver`/`OnRestart`; reset track + Enemy + player; phím R restart — ✅ commit `feat(core)`
- [x] **`ObstacleManager.cs` + `ObstacleData.cs`** (SO): spawn weighted random, luôn chừa ≥1 lane an toàn; auto-add `Obstacle` marker — ✅ commit `feat(world)` + `feat(data)`
- [x] Dựng scene `Game`: ✅ ground + NavMeshSurface (bake xong) + player + void + CinemachineCamera + Tile.prefab + **Managers** (GameManager/InputReader/TileSpawner) + **ObstacleManager** (2 ObstacleData: DynamicBox + Ramp, đã gán prefab) + gắn `Obstacle` vào DynamicBox.prefab — ✅ hoàn tất, đã Play test

**✅ Milestone G1:** Core loop chạy ổn định — bóng chạy, né obstacle, Enemy đuổi, chết → restart được

### Giai đoạn 2 — Hệ thống game
> Mục tiêu: game hoàn chỉnh về logic — score, power-up, âm thanh, save, độ khó

- [x] **`ScoreSystem.cs`**: score theo khoảng cách + coin; **combo multiplier** (×2, ×3...) khi không va chạm lâu; dùng `event Action<int>` → UI không coupling — ✅ commit `feat(score)`
- [x] **`UIManager.cs`**: HUD (score, multiplier, tốc độ); Game Over panel (score, best score, Retry/Menu); fade chuyển scene bằng DOTween
- [x] **`MainMenuManager.cs`**: nút Play / How to play / Highscore / Sound toggle — ✅ hoàn tất (scene MainMenu + Build Settings đã cấu hình)
- [x] **`PowerUpSystem.cs` + `PowerUpData.cs`** (SO): **Shield** (miễn nhiễm 1 va chạm, 3s), **Magnet** (hút coin quanh player), **Slow-mo** (`Time.timeScale` tạm thời — Enemy chậm lại)
- [x] **`AudioManager.cs`** (Singleton + `DontDestroyOnLoad`): BGM loop; SFX: collect, die, power-up, chuyển lane; volume lưu PlayerPrefs — ✅ hoàn tất: gắn vào scene + kéo đủ clip (BGM Kenney Music Jingles + 5 SFX Kenney), xóa AudioListener trùng trên Main Camera
- [x] **`SaveSystem.cs`**: lưu/load best score + volume bằng PlayerPrefs
- [x] **`DifficultyManager.cs`**: tốc độ tile tăng theo score (AnimationCurve); mật độ obstacle tăng dần; **giới hạn tốc độ tối đa** (fair)
- [x] Prefab power-up (Shield/Magnet/Slow-mo) + coin — đã tạo trong Unity: `Prefabs/Pickups/Coin.prefab`, `Prefabs/PowerUps/Pickup_{Shield,Magnet,SlowMo}.prefab` + 3 asset `ScriptableObjects/{Shield,Magnet,SlowMo}.asset`; `PickupSpawner` + `PowerUpSystem` gắn vào Managers — ⚠️ Coin thiếu `Rotator` (thêm sau, không ảnh hưởng chức năng)

**✅ Milestone G2:** Chơi hoàn chỉnh — score, combo, 3 power-up, âm thanh, best score lưu lại, độ khó tăng dần

### Giai đoạn 2.5 — REFACTOR GAMEPLAY (2026-08-11 — user review) ✅ ĐÃ THỰC THI
> ✅ Đã chốt thiết kế: **Player = tàu vũ trụ nhỏ** · **Enemy 2 nấc cố định + BẮT (16m → 12m, hit 2 = atack → Game Over)** — cập nhật 2026-08-12 v3 (khoảng cách cũ 9/7.5m bị camera cắt màn hình).
> ✅ Code xong + commit + push (2026-08-11) — user chạy tool `Refactor: Both Scenes` rồi test.

- [x] **R3-3 — Cơ chế Enemy 2 nấc cố định + BẮT** (quan trọng nhất — cập nhật 2026-08-12 v3):
  - Enemy giữ khoảng cách nền **16m** sau player (fix v3: camera cách player 10m → 9m cũ bị cắt màn hình)
  - Đụng vật cản lần 1 → Enemy tiến sát còn **12m** + **vỗ cánh nhanh hơn** (Animator.speed 2x — chưa chết)
  - Né sạch **10–15s** → Enemy nới dần về **16m** (reset nấc 0)
  - Đụng lần 2 TRONG cửa sổ 10–15s → Enemy **LAO TỚI BẮT** (clip `atack 1`) → chờ ~1.1s → Game Over mượt
  - Enemy KHÔNG tự tăng tốc theo thời gian (bỏ co dần 60s cũ)
  - ✅ `EnemyChase` viết lại (2 nấc + `relaxWindow` 12s + `CatchAndKill` + ép `flying` thay idle + safety net) + `PlayerController` bỏ `Die()` (đụng obstacle chỉ `RaiseObstacleHit`); ScoreSystem giữ `OnObstacleHit → ResetCombo`; **thêm 5 PlayMode test** (`EnemyChasePlayTests` — cập nhật 16/12m + catch delay)
- [x] **R3-4 — Game Over panel luôn hiện**: fix `UIManager.ShowGameOver` — bỏ early-return khi ScoreSystem null, dời `_panelGroup` setup lên trước (nguyên nhân gốc "không thấy màn hình game over" trước đó là Enemy không bao giờ bắt kịp — bug camera/NavMesh đã fix)
- [x] **R3-1 — Player = TÀU VŨ TRỤ NHỎ (đã chốt)**: `PlayerController.BuildSpaceship()` — primitive (Body/WingL/WingR/Cockpit/Engine) + material neon cyan code, tắt banh cũ, banking khi đổi lane; giữ Rigidbody
- [x] **R3-2 — Track vô tận thật**: Ground 400m → 6000m qua tool (400m chỉ đủ 15–30s chơi); tile recycle vốn vô tận
- [x] **R3-5 — UI tiếng Anh toàn bộ** gameplay + menu (SCORE/BEST/RETRY/SOUND ON-OFF/HowToPlay English...) — code + tool `RefactorGameplayTool`
- [x] **R3-7 — Best score ẩn khi = 0** (chỉ hiện khi có điểm thật)
- [x] **R3-6 — Layout nút âm thanh** (SoundButton 340×76 + padding 18px, font 32 NoWrap)

### Giai đoạn 3 — Polish & Deploy
> Mục tiêu: game đẹp, có link demo gắn CV

- [x] **UI Kenney (Blue + font Kenney Future)**: 2 gói `kenney_ui-pack` + `kenney_ui-pack-space-expansion` (1608 PNG, CC0) → đã convert Sprite bằng Editor tool (`Tools/Void Runner/Convert Kenney UI PNG to Sprites`) — UI MainMenu tông Blue, font game-y `Kenney Future SDF` (sampling 128), tự dựng bằng `Editor/MainMenuUIBuilder.cs` — ✅ hoàn tất (scene đã lưu + commit)
- [x] **Game HUD + Game Over panel đẹp**: `Editor/HUDUIBuilder.cs` (dựng ScorePanel + coin icon + combo + panel GAME OVER + nút CHƠI LẠI/MENU, tự gán field UIManager); `UI/UIManager.cs` thêm RetryButton/MenuButton (Restart qua GameManager, Menu load scene MainMenu) — ✅ hoàn tất (scene đã lưu + commit)
- [x] VFX: Particle khi collect coin/power-up + **screen shake** khi va chạm (Cinemachine Impulse) — ✅ xong (`VFXManager` tạo particle bằng code, không prefab; screen shake qua Impulse; tool `Setup VFX in Game Scene`)
- [x] VFX: **trail theo Enemy** (TrailRenderer tạo bằng code, nở rộng theo scale, clear khi restart) + **popup điểm** khi nhặt coin (DOTween bounce, pool, nhân combo, font Kenney Future) — ✅ xong
- [x] URP Post-processing: **Bloom, Vignette, Color Grading** (Global Volume) — ✅ hoàn tất: tool `PostProcessingSetupTool` tự dựng Global Volume + profile (`Settings/PostProcessing/VoidRunnerProfile.asset`) với 3 override + bật `renderPostProcessing` trên camera; đã chạy cho Game + MainMenu
- [x] Material PBR tối giản đồng bộ (tông "hư không": tím/đen phát sáng); lighting + skybox nhất quán — ✅ tool `MaterialLightingSetupTool` (5 material phát sáng + Light lạnh + ambient/fog tím), đã chạy 2 scene
- [x] **Unity Test Framework** — 24 test (EditMode 16 + PlayMode 8), **kết quả 24/24 xanh**; asmdef `VoidRunner.Core` (code chính) + `DOTween.Modules` (bài học predefined assembly)
- [x] **Kenney assets 6 bộ** (ui-pack, space-expansion, game-icons, particle-pack, space-kit, space-station-kit — CC0) — ✅ đã copy vào `Art/kenney_*`, đang convert + dựng HUD đẹp + ambient 2 bên đường
- [x] **Task A — Credits thiết kế lại đẹp** (MainMenu + Game Over): `CreditsPanelBuilder` dùng chung — panel tím/đen + viền cyan + tiêu đề vàng + danh sách third-party assets (khớp REFERENCE.md PART 3 — Credits); nút CREDITS + GameOverCreditsButton (ẩn cùng gameOverPanel); fix double-subscribe — ✅ commit `faed21f` → 2026-08-12 v3f.4: **bỏ GameOverCreditsButton khỏi Game Over** (user: "ở màn hình cuối game bỏ credit đi") — MainMenu GIỮ CREDITS
- [x] **Task B — Enemy = QUÁI VẬT** (đã random 1 trong 3 — ĐỔI 2026-08-12: còn 1 enemy DUY NHẤT Flying Beetle, tool `Setup Enemy` gán 1 prefab) — ✅ commit `cd42fa9`
- [x] **Task D — CHỌN TÀU ở MainMenu có preview 3D**: SaveSystem.SelectedShip; PlayerController.shipPrefabs (model SF Fighter/Sparrow thay primitive); ShipSelectManager (panel preview RenderTexture 256 + camera layer ShipPreview(6), nút < > SELECT CLOSE, tên tàu); tool `Setup Ship Select`; ShipCatalog (1 nguồn path + self-heal khi chưa gán prefab) — ✅ commit `decaa0e` + `de329c7`
- [x] **Task C — VẬT CẢN = SciFi kit** (KHÔNG dùng asteroid — user: "cục thiên thạch giữa đường kì quá"): `SciFiObstacleSetupTool` — Ramp → **BarrierObstacle** (Fence_Long_01, 3D Scifi Kit Starter Kit — cam neon cảnh báo, bề ngang ≤4.2 gọn trong lane) + DynamicBox → **DroneObstacle** (Robot_Guardian, Sci fi Drones — bù pivot center đúng tâm lane); menu `Tools/Void Runner/Rebuild SciFi Obstacles` (idempotent) — ✅ commit `cbbf933` `04e8526` `48292ba` (2026-08-12) → **v3f.5: XÓA HẲN cổng/rào** (user: "cổng chứ đâu phải bãi mìn") — obstacle DUY NHẤT = **DroneObstacle** (Robot_Guardian) + `ObstacleFX` (đèn đỏ + hạt năng lượng + lơ lửng/xoay, tạo runtime); Ramp.asset → drone (cả 2 ObstacleData = drone, spawnWeight phân mật độ); xóa BarrierObstacle/Ramp.prefab/DynamicBox.prefab mồ côi — ✅ commit (v3f.5)
- [ ] Tuning: độ khó fair; Unity Profiler đảm bảo **60 FPS**; test nhiều độ phân giải
- [ ] **WebGL build**: Resolution responsive · **Compression: Brotli** · WebGL 2.0 · Linear color space · chỉnh Initial Memory hợp lý (256–512 MB), hạn chế GC spike
- [ ] **Upload itch.io** (chính): tài khoản → New project → Kind: HTML → nén thư mục WebGL `.zip` → mô tả + screenshot + GIF → Publish
- [ ] **Upload Unity Play** (dự phòng): play.unity.com → Upload → Publish → copy link
- [ ] **README GitHub** (template mục 9) + 2 link demo + screenshot/GIF

**✅ Milestone G3:** 2 link WebGL chạy được · repo sạch · README đầy đủ

---

## 5. Spec kỹ thuật file-by-file (tham chiếu khi code)

| File | Trách nhiệm | Điểm kỹ thuật cần chú ý |
|---|---|---|
| `GameManager` | State machine + luồng game | Enum State; event `OnGameOver`/`OnRestart`; singleton nhẹ hoặc tham chiếu qua scene |
| `PlayerController` | Lane switching | Rigidbody + velocity forward; `Mathf.Lerp`/`MoveTowards` theo X; chặn input khi chết; không `FindGameObjectWithTag` trong runtime |
| `TileSpawner` | Object pool | `Queue<Tile>`; spawn theo `tileLength`; recycle khi `transform.position.z < player.z - N` |
| `EnemyChase` | AI đuổi theo | **2 nấc cố định 16m→12m + BẮT** (Enemy duy nhất: Flying Beetle — Animator ép `flying` vỗ cánh, code chỉ điều vị trí); KHÔNG NavMeshAgent — đuổi trực tiếp giữ khoảng cách + safety net; hit 2 → `atack 1` → 1.1s → Game Over; nới lại sau 10–15s |
| `ObstacleManager` | Spawn obstacle | Đọc từ `ObstacleData[]`; tỉ lệ xuất hiện tăng theo Difficulty; luôn chừa ≥1 lane an toàn |
| `ScoreSystem` | Score + combo | `event Action<int> OnScoreChanged`; combo reset khi va chạm; score theo distance `+= speed * dt` |
| `PowerUpSystem` | Hiệu ứng power-up | Đọc `PowerUpData` (SO); coroutine cho duration; slow-mo dùng `Time.timeScale` + khôi phục |
| `AudioManager` | Âm thanh | Singleton `DontDestroyOnLoad`; `AudioSource` riêng BGM/SFX; volume từ PlayerPrefs |
| `SaveSystem` | Lưu dữ liệu | `PlayerPrefs.GetInt/SetInt` wrapper — dễ thay bằng JSON sau |
| `DifficultyManager` | Độ khó | `AnimationCurve` speed theo score; mật độ obstacle; speed cap |
| `UIManager` | HUD + panel | Lắng nghe `OnScoreChanged` (event — không coupling); DOTween fade |

---

## 6. Packages & Assets

| Package | Lý do | Nguồn |
|---|---|---|
| Cinemachine | Camera follow mượt + Impulse screen shake | Package Manager |
| DOTween | Fade UI, popup bounce, game feel | Asset Store / OpenUPM |
| AI Navigation (2.0.12) | NavMesh cho Enemy chase | ✅ Có sẵn |
| URP (17.4.0), Input System, TextMesh Pro | Nền tảng | ✅ Có sẵn |
| kenney_ui-pack + space-expansion | Sprite UI (button/panel/icon) tông Blue | [kenney.nl](https://kenney.nl) — CC0, miễn phí |
| kenney_game-icons | 425 icon (menu/HUD/power-up) | [kenney.nl/assets/game-icons](https://kenney.nl/assets/game-icons) — CC0 |
| kenney_particle-pack | 193 sprite particle (bụi sao, glow) | [kenney.nl/assets/particle-pack](https://kenney.nl/assets/particle-pack) — CC0 |
| kenney_space-kit | 772 sprite + FBX (cột trụ, kiến trúc — ambient 2 bên đường) | [kenney.nl/assets/space-kit](https://kenney.nl/assets/space-kit) — CC0 |
| kenney_space-station-kit | 104 sprite + FBX (trạm vũ trụ lơ lửng) | [kenney.nl/assets/space-station-kit](https://kenney.nl/assets/space-station-kit) — CC0 |
| kenney_music-jingles | BGM 8-bit | [kenney.nl](https://kenney.nl/assets/music-jingles) — CC0 |
| Editor tools (`_Project/Editor/`) | Setup scene/UI/font/skybox tự động — 10 tool idempotent (chạy lại an toàn) | Tự viết (menu `Tools/Void Runner/`): KenneyFontImporter, MaterialLightingSetupTool, PostProcessingSetupTool, RefactorGameplayTool, ShipSelectSetupTool, SkyboxSetupTool, SpriteBatchConverter, UIOverhaulTool, VFXSetupTool, VoidMonsterSetupTool + UIBuilderHelpers |

> Nguyên tắc: **không dùng asset gameplay từ Asset Store** — mọi logic tự viết (thể hiện skill).

---

## 7. Build & Deploy chi tiết

**WebGL Build Settings:**
```
Resolution : 1280×720 (responsive)
Compression: Brotli
Color Space: Linear (URP)
Graphics   : WebGL 2.0
Initial Memory: 256–512 MB (tránh crash do OOM)
```

**itch.io (chính):**
1. Tạo tài khoản [itch.io](https://itch.io) → Dashboard → New project → Kind: **HTML**
2. Nén thư mục WebGL build thành `.zip` → Upload
3. Mô tả + controls + screenshot + GIF gameplay → Publish

**Unity Play (dự phòng):**
1. [play.unity.com](https://play.unity.com) → Upload → chọn thư mục WebGL build
2. Điền thông tin → Publish → copy link

> Lý do ưu tiên itch.io: trang project chuyên nghiệp, embed game trực tiếp, tùy biến, được nhà tuyển dụng công nhận. Unity Play tiện nhưng giới hạn kích thước file và ít chuyên nghiệp hơn.

---

## 8. Skills chứng minh

| Kỹ thuật | Nơi áp dụng | Ghi chú phỏng vấn |
|---|---|---|
| **AI / Enemy chase** | `EnemyChase` | Enemy đuổi theo 2 nấc (chạm vật cản → tiến sát), nới lại khi né sạch 10–15s; Task B: hiển thị quái vật random |
| Object Pool | `TileSpawner` | Không GC spike, 60 FPS WebGL |
| ScriptableObject | `ObstacleData`, `PowerUpData` | Tách data / logic |
| C# Event System | `ScoreSystem` → UI | Decoupled architecture |
| Singleton Pattern | `AudioManager` | Xuyên scene |
| Rigidbody Physics | `PlayerController` | Lane lerp mượt |
| Scene Management | Menu → Game → GameOver | Luồng game hoàn chỉnh |
| URP Post-processing | Giai đoạn 3 | Bloom, Vignette, Color Grading |
| Cinemachine | Camera + Impulse | Unity ecosystem packages |
| DOTween | UI fade, game feel | Tween thay vì viết tay |
| WebGL Build + Deploy | itch.io / Unity Play | Demo chạy thẳng trên browser |

---

## 9. README template

```markdown
# Void Runner

> Endless runner 3D + AI chase · Unity 6 URP

[PLAY ON ITCH.IO](link) · [PLAY ON UNITY PLAY](link)

## Gameplay
- Chạy thoát khỏi "Hư Không" — void đuổi theo bằng AI pathfinding
- Né obstacle trên 3 lane (Ramp, Pillar, Dynamic Box)
- Thu coin + power-up: Shield / Magnet / Slow-mo
- Tốc độ tăng dần, combo multiplier khi né liên tiếp

## Kỹ thuật nổi bật
- NavMesh AI: void đuổi theo player, tốc độ + kích thước tăng dần
- Object Pool: tile tái sử dụng, không GC spike
- ScriptableObject: tách data obstacle/power-up khỏi logic
- Event-driven: ScoreSystem → UIManager không coupling trực tiếp
- Singleton AudioManager tồn tại xuyên scene

## Tech Stack
Unity 6 · URP · C# · Cinemachine · DOTween · TextMesh Pro · NavMesh
```

---

## 10. Commit convention

> Quy ước đầy đủ (type, scope, quy tắc, validation): xem **[`REFERENCE.md`](REFERENCE.md) (PART 4 — Commit)**.

```
feat(player): viết lại điều khiển chuyển lane 3 làn
feat(world): thêm TileSpawner dùng object pool
feat(void): thêm AI đuổi theo, tốc độ tăng dần theo thời gian
feat(score): thêm ScoreSystem với combo multiplier
feat(powerup): thêm hệ thống power-up (shield, magnet, slow-mo)
fix(world): vá lỗi hở giữa các tile khi spawn nhanh
refactor(obstacle): tách ObstacleData thành ScriptableObject
chore(config): cấu hình URP post-processing và DOTween
build(build): build WebGL v1.0 (Brotli) và publish
```

---

## 11. Định nghĩa "Hoàn thành" (Definition of Done)

- [ ] Core loop: chạy → né → thu thập → chết → thử lại, 60 FPS ổn định
- [ ] Đủ hệ thống: score + combo, 3 power-up, audio + volume, best score
- [ ] Menu → Game → Game Over → Retry/Menu mượt, có fade
- [ ] WebGL build chạy tốt trên Chrome/Firefox/Safari
- [ ] 2 link demo (itch.io + Unity Play) + README + screenshot/GIF
- [ ] Commit theo convention, repo sạch

---

*Kế hoạch này là source of truth khi code — tick checkbox từng task và commit theo mục 10. Code chỉ bắt đầu sau khi người duyệt OK.*
