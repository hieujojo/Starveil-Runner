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

## 🔜 Kế hoạch fix vòng tiếp theo

1. Viết tool **`UIOverhaulTool`** (idempotent, chạy lại an toàn) ép chuẩn:
   - **Game**: ScoreText căn giữa + vàng glow + viền tím · ScorePanel/GameOverPanel tông tím đen · Combo cam · nút tím/cyan
   - **MainMenu**: xóa TitleGlow trùng (hoặc biến thành lớp glow tím phía sau) · nền tím đen · nút tông đồng bộ · BestScoreText đưa lên an toàn
2. Chạy tool → Ctrl+S → **user chụp ảnh lại** → đối chiếu bug còn sót.
3. Xác minh "điểm sáng trắng" (G6) — giảm emission coin/Void nếu cần.
