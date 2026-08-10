# 🧪 TESTING — Hướng dẫn test Void Runner

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
| A1 | Tựa đề **"VOID RUNNER"** font Kenney Future sắc nét (không ô vuông □) | ☐ |
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
| B10 | Sau lưng có **Void tím hồng** đuổi theo + **vệt khói tối** phía sau | ☐ |
| B11 | Coin/player/obstacle **phát sáng** trong bóng tối (Bloom) | ☐ |
| B12 | Không thấy hộp vật lý lạ, không xuyên sàn, không bay lung tung | ☐ |

## C. Game Over + restart

| # | Kiểm tra | Kết quả |
|---|---|---|
| C1 | Chết → panel Game Over hiện (fade mượt), hiện **SCORE + BEST** (vàng, tiếng Anh) | ☐ |
| C2 | Bấm **CHƠI LẠI** → game chạy lại từ đầu, score reset, combo reset, **vệt khói Void không kéo dài xuyên map** | ☐ |
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

## 🔄 Cơ chế Void mới — Subway Surfers / Temple Run (sau refactor)

> ✅ Refactor gameplay ĐÃ CODE (2026-08-11) — các mục này test được ngay.
> 🧪 **Test tự động đi kèm**: `VoidChasePlayTests` (PlayMode, 5 test) — đã chạy/validate trước khi test tay.

| # | Kiểm tra | Kết quả |
|---|---|---|
| V1 | Đụng vật cản lần 1 → **KHÔNG chết**, Void tiến sát hơn (cảm nhận rõ) | ☐ |
| V2 | Không chạm vật cản trong **10–15s** → Void NỚI LẠI khoảng cách ban đầu | ☐ |
| V3 | Đụng **2 lần trong cửa sổ 10–15s** → Void nuốt → Game Over panel hiện | ☐ |
| V4 | Game Over panel **LUÔN hiện** khi chết (không bao giờ "chết mà không thấy màn hình") | ☐ |
| V5 | Void **không tự tăng tốc** theo thời gian — chỉ tiến sát khi player lỗi | ☐ |
| V6 | Player = **tàu vũ trụ nhỏ** (không còn banh xanh), banking khi đổi lane, nhìn hợp lý | ☐ |
| V7 | Track chạy **> 400m không hết đường** (Ground 6000m + tile recycle) | ☐ |
| V8 | **Toàn bộ text gameplay + menu = tiếng Anh** (RETRY/SCORE/BEST/SOUND ON-OFF/HowToPlay) | ☐ |
| V9 | Best score **ẩn khi = 0** ở MainMenu; hiện khi đã có điểm | ☐ |
| V10 | Nút âm thanh: text không thụt vào viền, không quá chật | ☐ |
| V11 | Void **phình to hơn khi tiến sát** (nấc 1 đe dọa) | ☐ |

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
