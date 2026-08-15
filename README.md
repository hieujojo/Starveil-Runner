# 🔮 Starveil Runner — Endless Runner 3-Lane với Cơ Chế "Enemy Tiến Sát"

> **Starveil Runner** là một endless runner 3-lane được phát triển bằng **Unity 6 + URP**: điều khiển nhân vật lao về phía trước trên đường vô tận trong khi **"Enemy" (Flying Beetle)** đuổi theo phía sau. Cơ chế đặc trưng kiểu **Subway Surfers / Temple Run**: đụng chướng ngại vật → Enemy **tiến sát** hơn; né sạch 10–15 giây → Enemy **nới ra**; đụng 2 lần trong cửa sổ đó → bị nuốt chửng = Game Over. Thu coin, nhặt power-up và sống sót càng lâu càng tốt.

[![Unity](https://img.shields.io/badge/Unity-6.x-222222?logo=unity&logoColor=white)](https://unity.com)
[![C#](https://img.shields.io/badge/C%23-.NET-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP%2017-2196F3)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17/manual/index.html)

> **🚀 Chơi thử ngay:** [itch.io](https://lothric11.itch.io/starveil-runner) · [Unity Play](https://play.unity.com/en/games/00ba213a-f671-4e8d-9a57-65da13cf1e5c/webgl)

![Gameplay GIF — thay bằng video quay thật](https://via.placeholder.com/960x540/0d0a1a/ffd84d?text=GAMEPLAY+GIF+COMING+SOON)

---

## 🎮 Gameplay

```
        Track vô tận — 3 lane (mỗi lane 6m, road rộng 18m)

   ◀ lane 0        lane 1        lane 2 ▶
   ┌──────────────┬──────────────┬──────────────┐
   │              │    [COIN]    │              │  ← coin: +điểm, không bao giờ đè obstacle
   │     [DRONE]  │              │              │  ← drone: đèn đỏ + lơ lửng (spawn đồng đều theo độ khó)
   │              │     ▲        │              │  ← tàu của bạn (A/D hoặc vuốt để đổi lane)
   │              │   (ship)     │              │
   └──────────────┴──────────────┴──────────────┘
                          │
                          ▼ hướng chạy (tốc độ tăng dần)

        🪲 Flying Beetle đuổi theo phía sau (cơ chế tiến sát):
        đụng obstacle 1 lần → bọ TIẾN SÁT · né sạch 10–15s → bọ NỚI RA
        đụng 2 lần trong cửa sổ → bọ LAO TỚI BẮT = Game Over
```

## 🎮 Chi tiết gameplay

- **3 lane vô tận, road rộng 18m**: bấm `A`/`D` (hoặc mũi tên) = nhảy ngay 1 lane; **đè giữ** = trượt liên tục qua nhiều lane; nhả = tự về giữa lane — phản hồi tức thì, né chướng ngại vật mượt mà. **📱 Mobile: vuốt trái/phải để đổi lane** (và kéo chuột trên desktop cũng được)
- **Enemy = Flying Beetle đuổi theo kiểu Subway Surfers**: đụng chướng ngại vật lần 1 → Enemy **tiến sát** (phình to, đe dọa); né sạch 10–15s → Enemy **nới lại khoảng cách**; đụng **2 lần trong cửa sổ 10–15s** → Enemy nuốt chửng = **Game Over**. Đụng obstacle → tàu **nhấp nháy** (feedback rõ ràng)
- **Chướng ngại vật**: **drone bảo vệ (Robot_Guardian — Sci fi Drones)** với đèn đỏ cảnh báo + lơ lửng — spawn **đồng đều theo độ khó** (linear scheduling: mật độ trung bình đúng xác suất 0.45→0.75, không cụm trống/dày), đầu game 1 drone/tile → cuối game hay chặn 2 lane, **luôn chừa ≥1 lane an toàn** + **safe zone 20m đầu game**
- **Coin & Power-up**: gom coin tăng điểm (hàng coin **không bao giờ đè lên obstacle** — chọn lane khác), nhặt power-up (Shield / Magnet / Slow-mo)
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
| Input hiện đại | **Unity Input System** — bấm = nhảy 1 lane tức thì, đè giữ = trượt liên tục (Subway Surfers), A/D + mũi tên + **swipe mobile** (`Pointer` — touch + kéo chuột) | ✅ |
| Pause | **Overlay trong scene** (nút II góc phải + phím ESC) — `Time.timeScale = 0`, giữ nguyên trạng thái; RESUME · RESTART · slider VOLUME · MENU | ✅ |
| Âm lượng | **Slider kéo** (thay nút bật/tắt) — có ở cả MainMenu lẫn màn hình Pause, lưu qua PlayerPrefs | ✅ |
| Camera điện ảnh | **Cinemachine** (Framing Transposer) | ✅ |
| Kiến trúc | **Clean Architecture**: `UI → Systems → Core`, folder `Assets/_Project/` | ✅ |
| Điểm số / combo | event-driven `ScoreSystem` | ✅ |
| UI (HUD / Menu / Game Over) | `UIManager` + TextMeshPro — **toàn bộ tiếng Anh** | ✅ |
| Power-up | Shield / Magnet / Slow-mo | ✅ |
| Audio + Save | `AudioManager` singleton + `SaveSystem` (PlayerPrefs) | ✅ |
| Polish | Post-processing, VFX, screen shake (DOTween), VFX trail Enemy, đụng obstacle → tàu nhấp nháy, coin không đè obstacle, **Point Light cyan bám tàu (nổi bật)** | ✅ |
| Test tự động | **Unity Test Framework — 31 test** (16 EditMode + 15 PlayMode) | ✅ |
| Deploy | **WebGL build (Gzip) → [itch.io](https://lothric11.itch.io/starveil-runner) + [Unity Play](https://play.unity.com/en/games/00ba213a-f671-4e8d-9a57-65da13cf1e5c/webgl)** | ✅ Live 2026-08-12 |

---

## 💡 Why this project?

**Starveil Runner** được xây dựng để chứng minh toàn bộ kỹ năng của một **Game Developer Unity** thực chiến — từ 0 đến game **LIVE trên 2 nền tảng** (itch.io + Unity Play):

- 🧠 **Thiết kế gameplay thật**: cơ chế "Enemy tiến sát" kiểu Subway Surfers — biến một lỗi nhỏ (đụng drone) thành **áp lực sinh tử liên tục** (bọ đuổi sát, đụng lần 2 = Game Over). Không chỉ "nhặt item + né vật cản"
- 🏗️ **Kiến trúc sạch sẽ**: `UI → Systems → Core`, event-driven (`GameEvents`), ScriptableObject config, Object Pool — code dễ mở rộng, dễ test
- 🧪 **Test-driven**: 31 test tự động (EditMode + PlayMode) — chất lượng không phụ thuộc "tôi nhớ test tay"
- 🚀 **Đã deploy thật**: WebGL build tối ưu (texture 2048→1024, ~60MB), compression Gzip, xử lý white screen, publish lên itch.io + Unity Play
- 📱 **Mobile-ready**: điều khiển bằng vuốt, pause overlay, safe-area

## 🕹️ Điều khiển

| Phím / Thao tác | Hành động |
|---|---|
| `A` / `←` | Bấm: nhảy 1 lane trái · Đè giữ: trượt trái liên tục |
| `D` / `→` | Bấm: nhảy 1 lane phải · Đè giữ: trượt phải liên tục |
| `Esc` | Pause / Resume (mở màn hình Pause) |
| Nút `II` (góc trên phải) | Pause / Resume (cho touch mobile) |
| Vuốt trái / phải (📱 touch hoặc kéo chuột) | Đổi lane theo hướng vuốt |
| `R` | Restart (khi Game Over) |

> 🌐 **Ngôn ngữ UI:** toàn bộ text in-game (SCORE, COMBO, GAME OVER, RETRY...) dùng **tiếng Anh**.
> 📱 **Mobile:** điều khiển bằng vuốt — bấm nút `II` để pause, kéo **slider VOLUME** chỉnh âm lượng.

---

## 🚀 Build & Chạy thử

**Yêu cầu:** Unity 6.x, package: Cinemachine (3.1.7), AI Navigation, DOTween (Asset Store).

1. Mở project bằng **Unity Hub**
2. Mở scene `Assets/_Project/Scenes/Game.unity`
3. Bấm **▶ Play** — hoặc `File → Build Settings` để build cho nền tảng bạn muốn
4. Build WebGL: đổi platform sang **WebGL**, chọn **Compression Format: Gzip** (⚠️ KHÔNG dùng Brotli — itch.io không serve `.br` đúng header → trắng màn hình, xem CHANGELOG R7.18), rồi Build

> 🌐 **Chơi thử online — LIVE 2026-08-12:**
> - 🎮 [itch.io — Starveil Runner](https://lothric11.itch.io/starveil-runner)
> - 🎮 [Unity Play — Starveil Runner](https://play.unity.com/en/games/00ba213a-f671-4e8d-9a57-65da13cf1e5c/webgl)

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
| **G3** | Polish & Deploy: VFX, post-processing, WebGL → itch.io / Unity Play, README | ✅ Hoàn thành — **LIVE cả 2 nền tảng** (2026-08-12) |
| **G3.5** | Pause overlay (ESC + nút II) · slider âm lượng (MainMenu + Pause) · swipe mobile | ✅ Hoàn thành — test OK trên web |

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
