# Void Runner — Bảng tính năng (dành cho review)

> **Mục đích:** tóm tắt từng tính năng đã hoàn thiện theo thời gian — bạn chỉ cần đọc bảng này
> là biết hệ thống nào đang chạy, hoạt động ra sao, có VFX/audio/UI gì đi kèm.
> Cập nhật sau mỗi commit tính năng (xem `CHANGELOG.md` cho lỗi/bài học, `void-runner-plan.md` cho kế hoạch).

---

## 🎮 Tổng quan

**Void Runner** — endless runner 3D, 3 lane, tự chạy. "Hư Không" (void) đuổi theo bằng NavMesh AI. Bị nuốt/đụng obstacle → Game Over. Nhặt coin + power-up, điểm cao nhất lưu lại.

```
MainMenu → Game (chạy + né + thu thập) → Game Over → Retry / Menu
```

---

## ✅ Tính năng đã hoàn thiện (theo thứ tự thời gian)

### G1 — Core Gameplay

| # | Tính năng | File | Cách hoạt động | Ghi chú |
|---|---|---|---|---|
| 1 | Điều khiển 3 lane | `Core/Player/PlayerController.cs` | Bóng tự lăn tới; A/D hoặc mũi tên để đổi lane; lerp mượt | Rigidbody + Input System |
| 2 | Object pool tile | `Core/World/TileSpawner.cs` + `Tile.cs` | Pool sẵn tile, spawn trước + recycle sau lưng | Không GC spike giữa chừng |
| 3 | AI đuổi theo (Void) | `Core/World/VoidChase.cs` | NavMeshAgent đuổi player; tốc độ + kích thước tăng dần | Điểm khác biệt so với runner thường |
| 4 | State machine | `Core/Game/GameManager.cs` | Menu → Playing → GameOver; phím R restart (tạm) | Event-driven |
| 5 | Obstacle weighted | `Core/World/ObstacleManager.cs` + `Data/ObstacleData.cs` | Spawn theo tỉ lệ, luôn chừa ≥1 lane an toàn | ScriptableObject |

### G2 — Hệ thống game

| # | Tính năng | File | Cách hoạt động | Ghi chú |
|---|---|---|---|---|
| 6 | Score + combo | `Systems/Score/ScoreSystem.cs` | Điểm theo khoảng cách (×10) + coin; combo ×2…×5, reset khi va chạm | Event → UI, không coupling |
| 7 | Save best score + volume | `Systems/Save/SaveSystem.cs` | PlayerPrefs wrapper; best score chỉ ghi khi cao hơn | Sẵn sàng đổi JSON sau |
| 8 | 3 Power-up | `Systems/PowerUp/PowerUpSystem.cs` + `Data/PowerUpData.cs` | **Shield** (miễn nhiễm 3s), **Magnet** (hút coin 6m), **Slow-mo** (`timeScale=0.5` 3s) | Registry tĩnh coin — không GC mỗi frame |
| 9 | Audio | `Systems/Audio/AudioManager.cs` | BGM loop + 5 SFX (coin/death/powerup/lane/start); volume qua SaveSystem; DontDestroyOnLoad | Nghe `GameEvents` — zero coupling |
| 10 | Độ khó tăng dần | `Systems/Difficulty/DifficultyManager.cs` | Tốc độ 10→20 + mật độ 0.45→0.75 trong 60s qua AnimationCurve | Reset đúng khi Restart |
| 11 | MainMenu | `UI/Screens/MainMenuManager.cs` | Play / How to play / best score / sound toggle; load scene Game | Scene riêng, Build Settings index 0 |

### G3 — Polish (đang làm)

| # | Tính năng | File | Cách hoạt động | Ghi chú |
|---|---|---|---|---|
| 12 | UI Kenney (Blue + font) | `Editor/MainMenuUIBuilder.cs` + `Editor/HUDUIBuilder.cs` | Tự dựng menu + HUD: sprite `panel_glass`, button Blue `button_rectangle_gloss/flat`, font `Kenney Future SDF` (sampling 128); tự gán field qua `SerializedObject` | 1608 PNG đã convert Sprite; tool chạy 1 nút |
| 13 | Game HUD + Game Over | `UI/UIManager.cs` | ScorePanel + coin icon + score + combo (ẩn khi ×1); Game Over: title + điểm + cao nhất + **nút CHƠI LẠI / MENU** | Panel fade DOTween; nút mới thêm (Retry/Menu) |
| 14 | VFX | *(chưa làm)* | Particle coin/powerup, trail void, screen shake (Cinemachine Impulse), popup bounce | ⏳ Bước tiếp theo |
| 15 | Post-processing | *(chưa làm)* | Bloom, Vignette, Color Grading (Global Volume) | ⏳ Sau VFX |
| 16 | WebGL + deploy | *(chưa làm)* | Build Brotli → itch.io + Unity Play + README | ⏳ Cuối cùng |

---

## 🎨 Tông màu & UI

- **Font:** Kenney Future (game-y) cho toàn bộ UI — asset `Art/Fonts/Kenney Future SDF.asset`
- **Sprite:** tông **Blue** (space/tech): `panel_glass` (panel kính), `button_rectangle_gloss` (nút chính bóng), `button_rectangle_flat` (nút phụ), `star` (icon coin vàng)
- **MainMenu:** title glow xanh + 3 nút + best score gold + HowToPlay panel kính
- **HUD Game:** ScorePanel góc trái (icon coin + số to), combo x2... dưới đó, Game Over panel trung tâm

---

## 📌 Trạng thái hiện tại

- ✅ G1 + G2 hoàn tất (commit theo convention, đã push)
- ⏳ G3: đang làm **Game HUD đẹp** (tool `Build Game HUD UI` — chờ user chạy trong Unity)
- ⏭️ Tiếp theo: **VFX** → Post-processing → Material/Lighting → WebGL build → upload

*Chi tiết lỗi đã sửa + bài học: xem [`CHANGELOG.md`](CHANGELOG.md). Kế hoạch đầy đủ: [`void-runner-plan.md`](void-runner-plan.md).*
