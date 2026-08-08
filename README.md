# 🔮 Void Runner — Endless Runner 3-Lane với AI Chase

> **Void Runner** là một endless runner 3-lane được phát triển bằng **Unity 6 + URP**, nơi bạn điều khiển quả bóng lao về phía trước trong khi **"Hư Không" (The Void)** — một khối bóng tối điều khiển bằng AI pathfinding — đuổi theo không ngừng. Né chướng ngại vật, gom coin, nhặt power-up và sống sót càng lâu càng tốt.

[![Unity](https://img.shields.io/badge/Unity-6.x-222222?logo=unity&logoColor=white)](https://unity.com)
[![C#](https://img.shields.io/badge/C%23-.NET-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP%2017-2196F3)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17/manual/index.html)

---

## 🎮 Gameplay

- **3 lane**: chuyển lane trái/phải (`A`/`D` hoặc mũi tên) để né chướng ngại vật
- **The Void đuổi theo**: AI điều khiển qua **NavMesh** — càng chạy lâu, Void càng **to và nhanh**. Bị nuốt chửng = Game Over
- **Chướng ngại vật**: Ramp cố định, Pillar cần né, DynamicBox di động (obstacle được gắn ngẫu nhiên, luôn chừa ít nhất 1 lane an toàn)
- **Coin & Power-up**: gom coin tăng điểm, nhặt power-up (Shield / Magnet / Slow-mo) — *đang phát triển*
- **Độ khó tăng dần**: tốc độ tăng theo thời gian, combo multiplier cho người chơi giỏi — *đang phát triển*

---

## ✨ Tính năng kỹ thuật nổi bật

| Tính năng | Kỹ thuật | Trạng thái |
|---|---|---|
| **The Void đuổi theo người chơi** | AI / **NavMesh pathfinding** (`NavMeshSurface` + `NavMeshAgent`) | ✅ |
| Track sinh vô tận | **Object Pool** (`ObjectPool<T>`) — không Instantiate/Destroy giữa chừng | ✅ |
| Obstacle cấu hình được | **ScriptableObject** (`ObstacleData`) + gắn ngẫu nhiên theo weight | ✅ |
| Giao tiếp hệ thống | **C# Event-driven** (`GameEvents`) — decoupled, không coupling trực tiếp | ✅ |
| State machine | `GameManager` (Menu / Playing / GameOver / Restart) | ✅ |
| Input hiện đại | **Unity Input System** (2D Vector composite, hỗ trợ A/D + mũi tên) | ✅ |
| Camera điện ảnh | **Cinemachine** (Framing Transposer) | ✅ |
| Kiến trúc | **Clean Architecture**: `UI → Systems → Core`, folder `Assets/_Project/` | ✅ |
| Điểm số / combo | event-driven `ScoreSystem` | 🚧 Giai đoạn 2 |
| UI (HUD / Menu / Game Over) | `UIManager` + TextMeshPro | 🚧 Giai đoạn 2 |
| Power-up | Shield / Magnet / Slow-mo | 🚧 Giai đoạn 2 |
| Audio + Save | `AudioManager` singleton + `SaveSystem` (PlayerPrefs) | 🚧 Giai đoạn 2 |
| Polish | Post-processing, VFX, screen shake (DOTween) | 🚧 Giai đoạn 3 |
| Deploy | **WebGL build → itch.io / Unity Play** | 🚧 Giai đoạn 3 |

---

## 🕹️ Điều khiển

| Phím | Hành động |
|---|---|
| `A` / `←` | Chuyển lane trái |
| `D` / `→` | Chuyển lane phải |
| `R` | Restart (khi Game Over) |

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
│   │   └── World/     #   TileSpawner, Tile, ObstacleManager, VoidChase
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
| **G1** | Core gameplay: lane switching, tile spawner (object pool), Void AI chase, obstacle | ✅ Code xong — scene đang hoàn thiện |
| **G2** | Hệ thống: Score (combo), UI, Power-up, Audio, Save, Difficulty | ⏳ Tiếp theo |
| **G3** | Polish & Deploy: VFX, post-processing, WebGL → itch.io / Unity Play, README | ⏳ |

---

## 🛠️ Quy ước phát triển

- **Commit convention:** Conventional Commits (xem [`agent/COMMIT_TEMPLATES.md`](./agent/COMMIT_TEMPLATES.md))
- **Changelog & bài học:** [`agent/CHANGELOG.md`](./agent/CHANGELOG.md)
- **Kế hoạch chi tiết:** [`agent/void-runner-plan.md`](./agent/void-runner-plan.md)

---

## 📄 Giấy phép

- **Code:** Toàn bộ code viết tay thuộc về tác giả.
- **Assets:** UI & âm thanh từ [Kenney.nl](https://kenney.nl) — giấy phép **CC0 (Public Domain)**, dùng thoải mái không cần ghi công. Xem `License.txt` kèm theo từng gói.
