# Void Runner — Kế hoạch hành động (Action Plan)

> Hyper-casual endless runner 3D + **AI Chase** · Unity 6 URP · Game Production
> ⚠️ **Trạng thái:** Đã chốt concept — CHƯA bắt đầu code. Tick checkbox khi hoàn thành từng task.

---

## 1. Concept game (đã chốt ✅)

**Void Runner**: quả bóng lăn trên đường tile 3 lane, tự chạy về phía trước. Phía sau, **"Hư Không" (The Void)** — khối bóng tối phình to, nhanh dần — đuổi theo bằng **AI pathfinding (NavMeshAgent)**. Bị nuốt → Game Over.

```
Chạy + né obstacle → thu coin / power-up → chạy thoát Void → chết → thử lại (best score)
```

**Điểm khác biệt vs. runner thường:** có AI chase (NavMesh) — kỹ thuật ít fresher có, tận dụng luôn enemy AI của codebase hiện tại.

| Thông tin | Chi tiết |
|---|---|
| Thể loại | Hyper-casual endless runner 3D + chase |
| Engine | Unity 6 · URP 17.4.0 |
| Ngôn ngữ | C# thuần — không asset store gameplay |
| Nền tảng build | **WebGL** (itch.io chính + Unity Play dự phòng) |
| Mục tiêu | Game production hoàn chỉnh, phát hành WebGL |

---

## 2. Hiện trạng codebase (đã khảo sát)

| Hạng mục | Trạng thái | Xử lý |
|---|---|---|
| Unity 6 + URP + Input System | ✅ Có | Giữ |
| Scene `Minigame` (Player, Enemy NavMesh, PickUp, Obstacle Ramp/Pillar, Canvas) | ✅ Có | Archive làm scene test NavMesh → xóa khỏi Build Settings |
| `PlayerController.cs` (Roll-a-Ball: AddForce, FindGameObjectWithTag) | ✅ Có | **Viết lại** (lane-switching) |
| `EnemyMovement.cs` (NavMeshAgent đuổi theo) | ✅ Có | **Chuyển thành `VoidChase.cs`** |
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
│   │   │       ├── VoidChase.cs     # NavMeshAgent đuổi theo (từ EnemyMovement)
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
│   │   ├── Game.unity
│   │   └── _Archive/                # Scene Minigame + NavMesh-Ground (test)
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
| `Scripts/EnemyMovement.cs` | `_Project/Scripts/Core/World/VoidChase.cs` | Viết lại ở G1 |
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
- [ ] Tạo 2 scene `MainMenu` + `Game`; đưa vào **Build Settings** (danh sách build đã dọn trống) — ⚠️ thao tác trong Unity
- [x] Archive scene `Minigame` + `NavMesh-Ground.asset` → `Assets/_Project/Scenes/_Archive/` (giữ làm nơi test NavMesh bake)

### Giai đoạn 1 — Core Gameplay
> Mục tiêu: **chạy được · né được · chết được** — core loop hoàn chỉnh

- [x] **`PlayerController.cs`** (viết lại): Rigidbody bóng tự lăn về trước; chuyển lane trái/phải (A/D + mũi tên); lerp mượt giữa 3 lane; `OnTriggerEnter` obstacle → chết — ✅ code xong, commit `feat(player)`
- [x] **`TileSpawner.cs`** (Object Pool): pool sẵn N tile, spawn phía trước + recycle sau lưng player — không Instantiate/Destroy giữa chừng — ✅ commit `feat(world)`
- [x] **`Tile.cs`**: activate/deactivate, `ObstacleManager` gắn obstacle khi spawn — ✅ commit `feat(world)`
- [x] **`VoidChase.cs`** (từ `EnemyMovement.cs`): NavMeshAgent đuổi theo player; tốc độ + scale tăng dần theo thời gian; bắt kịp → Game Over — ✅ commit `feat(void)`
- [x] **`GameManager.cs`**: state machine (Menu/Playing/GameOver); event `OnGameOver`/`OnRestart`; reset track + Void + player; phím R restart — ✅ commit `feat(core)`
- [x] **`ObstacleManager.cs` + `ObstacleData.cs`** (SO): spawn weighted random, luôn chừa ≥1 lane an toàn; auto-add `Obstacle` marker — ✅ commit `feat(world)` + `feat(data)`
- [x] Dựng scene `Game`: ✅ ground + NavMeshSurface (bake xong) + player + void + CinemachineCamera + Tile.prefab + **Managers** (GameManager/InputReader/TileSpawner) + **ObstacleManager** (2 ObstacleData: DynamicBox + Ramp, đã gán prefab) + gắn `Obstacle` vào DynamicBox.prefab — ✅ hoàn tất, đã Play test

**✅ Milestone G1:** Core loop chạy ổn định — bóng chạy, né obstacle, Void đuổi, chết → restart được

### Giai đoạn 2 — Hệ thống game
> Mục tiêu: game hoàn chỉnh về logic — score, power-up, âm thanh, save, độ khó

- [x] **`ScoreSystem.cs`**: score theo khoảng cách + coin; **combo multiplier** (×2, ×3...) khi không va chạm lâu; dùng `event Action<int>` → UI không coupling — ✅ commit `feat(score)`
- [x] **`UIManager.cs`**: HUD (score, multiplier, tốc độ); Game Over panel (score, best score, Retry/Menu); fade chuyển scene bằng DOTween
- [ ] **`MainMenuManager.cs`**: nút Play / How to play / Highscore / Sound toggle
- [x] **`PowerUpSystem.cs` + `PowerUpData.cs`** (SO): **Shield** (miễn nhiễm 1 va chạm, 3s), **Magnet** (hút coin quanh player), **Slow-mo** (`Time.timeScale` tạm thời — Void chậm lại)
- [x] **`AudioManager.cs`** (Singleton + `DontDestroyOnLoad`): BGM loop; SFX: collect, die, power-up, chuyển lane; volume lưu PlayerPrefs — ✅ code xong; ⚠️ cần gắn vào scene + kéo clip (SFX có sẵn Kenney, BGM cần tải)
- [x] **`SaveSystem.cs`**: lưu/load best score + volume bằng PlayerPrefs
- [x] **`DifficultyManager.cs`**: tốc độ tile tăng theo score (AnimationCurve); mật độ obstacle tăng dần; **giới hạn tốc độ tối đa** (fair)
- [x] Prefab power-up (Shield/Magnet/Slow-mo) + coin — đã tạo trong Unity: `Prefabs/Pickups/Coin.prefab`, `Prefabs/PowerUps/Pickup_{Shield,Magnet,SlowMo}.prefab` + 3 asset `ScriptableObjects/{Shield,Magnet,SlowMo}.asset`; `PickupSpawner` + `PowerUpSystem` gắn vào Managers — ⚠️ Coin thiếu `Rotator` (thêm sau, không ảnh hưởng chức năng)

**✅ Milestone G2:** Chơi hoàn chỉnh — score, combo, 3 power-up, âm thanh, best score lưu lại, độ khó tăng dần

### Giai đoạn 3 — Polish & Deploy
> Mục tiêu: game đẹp, có link demo gắn CV

- [ ] VFX: Particle khi collect coin/power-up; trail theo Void; **screen shake** khi va chạm (Cinemachine Impulse); DOTween popup bounce khi score tăng
- [ ] URP Post-processing: **Bloom, Vignette, Color Grading** (Global Volume)
- [ ] Material PBR tối giản đồng bộ (tông "hư không": tím/đen phát sáng); lighting + skybox nhất quán
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
| `VoidChase` | AI đuổi theo | NavMeshAgent; `speed` và `transform.localScale` tăng theo thời gian; cap tốc độ |
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
| AI Navigation (2.0.12) | NavMesh cho Void chase | ✅ Có sẵn |
| URP (17.4.0), Input System, TextMesh Pro | Nền tảng | ✅ Có sẵn |

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
| **AI / NavMesh pathfinding** | `VoidChase` | Điểm khác biệt — void đuổi theo, tốc độ tăng dần |
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

> Quy ước đầy đủ (type, scope, quy tắc, validation): xem **[`COMMIT_TEMPLATES.md`](COMMIT_TEMPLATES.md)**.

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
