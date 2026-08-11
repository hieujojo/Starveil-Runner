# © CREDITS — Assets bên thứ ba (Third-Party Assets)

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
| **Kenney Space Kit** | Kenney | **CC0 1.0** | [kenney.nl/assets/space-kit](https://kenney.nl/assets/space-kit) | Mô hình 3D ambient 2 bên đường |
| **Kenney Space Station Kit** | Kenney | **CC0 1.0** | [kenney.nl/assets/space-station-kit](https://kenney.nl/assets/space-station-kit) | Mô hình 3D trạm vũ trụ |
| **Kenney Game Icons** | Kenney | **CC0 1.0** | [kenney.nl/assets/game-icons](https://kenney.nl/assets/game-icons) | Icon UI (coin, power-up...) |
| **Kenney Particle Pack** | Kenney | **CC0 1.0** | [kenney.nl/assets/particle-pack](https://kenney.nl/assets/particle-pack) | Texture particle (burst, exhaust) |
| **Kenney Fonts (Kenney Future)** | Kenney | **CC0 1.0** | [kenney.nl/assets/kenney-fonts](https://kenney.nl/assets/kenney-fonts) | Font UI / HUD |
| **Kenney Audio Packs** (music/sfx) | Kenney | **CC0 1.0** | [kenney.nl/assets](https://kenney.nl/assets) | Nhạc nền + hiệu ứng âm thanh |

> ℹ️ **Kenney CC0 = Public Domain** — được dùng thoải mái cho mọi mục đích (kể cả thương mại),
> **không bắt buộc ghi công**. File `License.txt` nằm kèm trong từng thư mục gói.
> ⚠️ Logo Kenney KHÔNG thuộc CC0 — không dùng logo trong game.

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
- UI, Space Kit, Fonts, Particle, Game Icons, Audio by Kenney (CC0 Public Domain)
- Font "Kenney Future" by Kenney (CC0)
```

> Khi build WebGL lên itch.io / Unity Play, nên thêm phần Credits này vào README hiển thị
> hoặc một màn hình Credits trong game.
