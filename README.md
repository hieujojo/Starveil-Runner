# 🔮 Void Runner — Endless Runner 3-Lane với Cơ Chế "Enemy Tiến Sát"

> **Void Runner** là một endless runner 3-lane được phát triển bằng **Unity 6 + URP**: điều khiển nhân vật lao về phía trước trên đường vô tận trong khi **"Enemy" (Flying Beetle)** đuổi theo phía sau. Cơ chế đặc trưng kiểu **Subway Surfers / Temple Run**: đụng chướng ngại vật → Enemy **tiến sát** hơn; né sạch 10–15 giây → Enemy **nới ra**; đụng 2 lần trong cửa sổ đó → bị nuốt chửng = Game Over. Thu coin, nhặt power-up và sống sót càng lâu càng tốt.

[![Unity](https://img.shields.io/badge/Unity-6.x-222222?logo=unity&logoColor=white)](https://unity.com)
[![C#](https://img.shields.io/badge/C%23-.NET-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP%2017-2196F3)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17/manual/index.html)

---

## 🎮 Gameplay

- **3 lane vô tận, road rộng 18m**: bấm `A`/`D` (hoặc mũi tên) = nhảy ngay 1 lane; **đè giữ** = trượt liên tục qua nhiều lane; nhả = tự về giữa lane — phản hồi tức thì, né chướng ngại vật mượt mà
- **Enemy = Flying Beetle đuổi theo kiểu Subway Surfers**: đụng chướng ngại vật lần 1 → Enemy **tiến sát** (phình to, đe dọa); né sạch 10–15s → Enemy **nới lại khoảng cách**; đụng **2 lần trong cửa sổ 10–15s** → Enemy nuốt chửng = **Game Over**. Đụng obstacle → tàu **nhấp nháy** (feedback rõ ràng)
- **Chướng ngại vật**: Ramp + DynamicBox gắn ngẫu nhiên, **luôn chừa ≥1 lane an toàn** + **safe zone 20m đầu game** (không obstacle — không chết tức thì)
- **Coin & Power-up**: gom coin tăng điểm (hàng coin **không bao giờ đè lên obstacle** — chọn lane khác), nhặt power-up (Shield / Magnet / Slow-mo)
- **Obstacle**: **thiên thạch Asteroid** (OlegWER High-Poly Asteroid) — chạy tool `Tools/Void Runner/Setup Obstacle = Asteroid` để tạo prefab + gán vào ObstacleData
- **Độ khó công bằng**: tốc độ nền tăng dần + combo multiplier — cái chết do **lỗi của bạn**, không phải ngẫu nhiên

---

## ✨ Tính năng kỹ thuật nổi bật

| Tính năng | Kỹ thuật | Trạng thái |
|---|---|---|
| **Enemy = Flying Beetle — cơ chế tiến sát + BẮT khi player lỗi** | Chase trực tiếp + **2 nấc cố định** (16m → 12m khi đụng + vỗ cánh nhanh hơn, nới lại sau 12s sạch, đụng lần 2 → Enemy **lao tới bắt** (clip atack) → Game Over mượt) + model Flying Beetle (Animator bay — ép `flying` thay vì idle) | ✅ |
| Player = **tàu vũ trụ nhỏ** | Dựng từ primitive + material neon (không cần model), banking khi đổi lane | ✅ |
| Track sinh vô tận | **Object Pool** (`ObjectPool<T>`) + Ground 6000m — chạy mãi không hết đường | ✅ |
| Obstacle cấu hình được | **ScriptableObject** (`ObstacleData`) + gắn ngẫu nhiên theo weight, luôn chừa ≥1 lane trống, safe zone 20m đầu game | ✅ |
| Giao tiếp hệ thống | **C# Event-driven** (`GameEvents`) — decoupled, không coupling trực tiếp | ✅ |
| State machine | `GameManager` (Menu / Playing / GameOver / Restart) | ✅ |
| Input hiện đại | **Unity Input System** — bấm = nhảy 1 lane tức thì, đè giữ = trượt liên tục (Subway Surfers), A/D + mũi tên | ✅ |
| Camera điện ảnh | **Cinemachine** (Framing Transposer) | ✅ |
| Kiến trúc | **Clean Architecture**: `UI → Systems → Core`, folder `Assets/_Project/` | ✅ |
| Điểm số / combo | event-driven `ScoreSystem` | ✅ |
| UI (HUD / Menu / Game Over) | `UIManager` + TextMeshPro — **toàn bộ tiếng Anh** | ✅ |
| Power-up | Shield / Magnet / Slow-mo | ✅ |
| Audio + Save | `AudioManager` singleton + `SaveSystem` (PlayerPrefs) | ✅ |
| Polish | Post-processing, VFX, screen shake (DOTween), VFX trail Enemy, đụng obstacle → tàu nhấp nháy, coin không đè obstacle, **Point Light cyan bám tàu (nổi bật)** | ✅ |
| Test tự động | **Unity Test Framework — 24 test + 5 PlayMode test Enemy 2 nấc** | ✅ |
| Deploy | **WebGL build → itch.io / Unity Play** | ⏳ Giai đoạn 3 |

---

## 🕹️ Điều khiển

| Phím | Hành động |
|---|---|
| `A` / `←` | Bấm: nhảy 1 lane trái · Đè giữ: trượt trái liên tục |
| `D` / `→` | Bấm: nhảy 1 lane phải · Đè giữ: trượt phải liên tục |
| `R` | Restart (khi Game Over) |

> 🌐 **Ngôn ngữ UI:** toàn bộ text in-game (SCORE, COMBO, GAME OVER, RETRY...) dùng **tiếng Anh**.

---

## 🚀 Build & Chạy thử

**Yêu cầu:** Unity 6.x, package: Cinemachine (3.1.7), AI Navigation, DOTween (Asset Store).

1. Mở project bằng **Unity Hub**
2. Mở scene `Assets/_Project/Scenes/Game.unity`
3. Bấm **▶ Play** — hoặc `File → Build Settings` để build cho nền tảng bạn muốn
4. Build WebGL: đổi platform sang **WebGL**, chọn **Compression Format: Brotli**, rồi Build

> 🌐 **Chơi thử online** (link sẽ được cập nhật khi hoàn tất Giai đoạn 3):
> - [itch.io](https://itch.io) — *chưa publish*
> - [Unity Play](https://play.unity.com) — *chưa publish*

---

## 📁 Cấu trúc project

```
Assets/_Project/
├── Scripts/
│   ├── Core/          # Gameplay lõi (thuần logic, không phụ thuộc UI)
│   │   ├── Game/      #   GameManager, GameEvents
│   │   ├── Player/    #   PlayerController (lane switching)
│   │   └── World/     #   TileSpawner, Tile, ObstacleManager, EnemyChase
│   ├── Systems/       # Input, Score, PowerUp, Audio, Save, Difficulty
│   ├── UI/            # HUD, Screens, Widgets
│   ├── Data/          # ScriptableObject definitions
│   ├── Interfaces/    # Contracts giữa các layer
│   └── Utils/         # ObjectPool<T>, Singleton
├── Scenes/            # Game.unity (MainMenu sẽ thêm)
├── Prefabs/           # Player, Tiles, Obstacles, Pickups, PowerUps, UI
├── ScriptableObjects/ # ObstacleData, PowerUpData
├── Art/               # Materials, VFX, Textures
└── Audio/             # Music, SFX
```

---

## 🗺️ Lộ trình phát triển

| Giai đoạn | Nội dung | Trạng thái |
|---|---|---|
| **G0** | Setup: Clean Architecture, Cinemachine, DOTween | ✅ Hoàn thành |
| **G1** | Core gameplay: lane switching, tile spawner (object pool), Enemy chase, obstacle | ✅ Code xong |
| **G2** | Hệ thống: Score (combo), UI, Power-up, Audio, Save, Difficulty | ✅ Code xong |
| **G2.5** | **Refactor gameplay theo review user** (Enemy 2 nấc, player tàu vũ trụ, track vô tận, UI English, best score ẩn) | ✅ Hoàn thành (2026-08-11) |
| **G3** | Polish & Deploy: VFX, post-processing, WebGL → itch.io / Unity Play, README | ⏳ |

---

## 🛠️ Quy ước phát triển

- **Commit convention:** Conventional Commits (xem [`agent/REFERENCE.md`](./agent/REFERENCE.md) (PART 4 — Commit))
- **Changelog & bài học:** [`agent/CHANGELOG.md`](./agent/CHANGELOG.md)
- **Kế hoạch chi tiết:** [`agent/PLAN.md`](./agent/PLAN.md)

---

## 📄 Giấy phép & Credits

- **Code:** Toàn bộ code viết tay thuộc về tác giả.
- **Assets bên thứ ba** (KHÔNG thuộc về tác giả — xem đầy đủ tại [`agent/REFERENCE.md`](./agent/REFERENCE.md) (PART 3 — Credits)):
  - **Nebula Skyboxes** (skybox tinh vân) — Unity Asset Store EULA
  - **SpaceSkies Free** by **PULSAR BYTES** — Unity Asset Store EULA
  - **Kenney** (UI Pack, Fonts, Audio — đã xóa Space Kit/Particle/Game Icons 2026-08-12) — giấy phép **CC0 (Public Domain)**, dùng thoải mái không cần ghi công.
  - **OlegWER High-Poly Asteroid** · **Eric VFX Studio Free Game VFX** · **JMO Assets Cartoon FX Remaster** — Unity Asset Store EULA (đã import 2026-08-12).
