# 📜 DECISIONS — Quyết định thiết kế bất biến (thiết kế game)

> **Mục đích:** tổng hợp MỌI quyết định thiết kế đã chốt với user (tách khỏi RULES.md kỹ thuật —
> 2026-08-12 gộp docs). Trước khi đổi hành vi/thiết kế game, đọc file này.
> Nguồn gốc: RULES.md NHÓM 0 + NHÓM 0b (đã tách ra).

## 🎯 NHÓM 0 — ĐỊNH HƯỚNG GAME (quyết định thiết kế — 2026-08-11)

> Các rule này là kết quả review của user, được chốt thành định hướng bất biến:

- **R0.1 — Player = TÀU VŨ TRỤ NHỎ (spaceship) — ĐÃ CHỐT 2026-08-11.** Không phải trái banh (tên game gợi "kẻ chạy trốn khỏi Void"). Tàu bay lơ lửng trên đường, tông cyan neon nổi trên nền tối, đổi lane mượt. Dựng bằng primitive (thân cube + cánh) hoặc model Kenney space-kit — không cần model nhân vật phức tạp.
- **R0.2 — ENEMY là KẺ ĐUỔI THEO ĐÍCH THỰC, không phải "banh tím trôi sau lưng".** (Đổi tên từ "Void" → "Enemy" 2026-08-12 — user quyết định.) Enemy = **Flying Beetle** (1 kẻ thù DUY NHẤT, user chốt "flying carnivorous") — có Animator flying loop, xuất hiện áp sát khi player chạm vật cản. Model chuyển động: Animator của prefab tự chạy (cánh vỗ bay) — code chỉ điều khiển VỊ TRÍ (bám sau player, đổi lane) + scale phình khi áp sát; KHÔNG ép localRotation mỗi frame (root motion — R4.17).
- **R0.3 — Track PHẢI vô tận thật sự** (TileSpawner pool recycle). Nếu đường chạy hết → bug hệ thống (Ground tĩnh 400m không được là giới hạn — chỉ là nền, không phải track).
- **R0.4 — Cơ chế chết kiểu Subway Surfers / Temple Run — 2 NẤC CỐ ĐỊNH (ĐÃ CHỐT 2026-08-11):**
  - Enemy giữ khoảng cách nền **9m** sau player (trong tầm camera offset -10 → nhìn thấy).
  - **NẤC 1**: player đụng vật cản → Enemy tiến sát còn **7.5m** (fix 2026-08-12: cũ 5m → enemy CHE mất tàu player; 7.5m vẫn áp sát đe dọa nhưng tàu thấy rõ).
  - **Nới lại**: player né sạch **10–15s** không đụng nữa → Enemy nới dần về **9m** (reset về nấc 0).
  - **CHẾT**: player đụng lần 2 TRONG CỬA SỔ 10–15s (khi Enemy đang ở nấc 1) → Enemy nuốt → Game Over.
  - Mọi vật cản chỉ khiến Enemy tiến gần — KHÔNG có cơ chế chết do obstacle trực tiếp.
  - Enemy KHÔNG tự tăng tốc theo thời gian (bỏ cơ chế co dần 60s cũ — gây chết ở mức điểm cố định).
  - Scale enemy: baseScale 1 / closeScale **1.2** (cũ 1.6 quá to che tàu) / enemyTargetHeight **1.8** — đủ đe dọa, không che tàu (~1.1).
- **R0.5 — Toàn bộ text trong gameplay = TIẾNG ANH** (SCORE, COMBO, GAME OVER, RETRY, MENU, BEST...). Không lộn xộn Việt/Anh trong scene Game.
- **R0.6 — MainMenu: Best score chỉ hiển thị khi có dữ liệu thật (BestScore > 0).** Lần đầu chơi = 0 → ẩn (hiển thị vô nghĩa). Sau khi chơi và có điểm → mới hiện.
- **R0.7 — Game Over panel BẮT BUỘC hiện khi game kết thúc.** Nếu user không thấy màn hình game over → bug nghiêm trọng, ưu tiên fix trước.
- **R0.8 — UI nút phải có padding đủ — text không bị thụt vào viền / quá chật.** Layout phải thoáng, đọc rõ.
- **R0.9 — Panel popup/overlay PHẢI ĐỤC HOÀN TOÀN (alpha = 1.0), không chỉ "gần đục".** Alpha 0.92 vẫn để element menu nằm trong vùng panel (tọa độ nằm trong sizeDelta) lộ xuyên qua → "fix rồi mà vẫn khó đọc". Khi mở popup: ép alpha=1 + dimmer ≥0.8 + `SetAsLastSibling`. Kiểm tra: element menu nào có anchoredPosition nằm trong vùng panel? → che kín hoặc di chuyển. *(Bug vòng 7 2026-08-11.)*
- **R0.10 — Road width (roadHalfWidth) là hằng số ĐỒNG BỘ TOÀN CỤC — sửa phải quét MỌI chỗ hardcode:** `Tile.roadHalfWidth`, scene `Ground scale x`, Editor tool `RefactorGameplayTool` (Ground + sideOffset), `laneWidth` (Player/Obstacle/Pickup). *(AmbientScroller đã xóa 2026-08-11 — không còn trong danh sách đồng bộ.)* Bỏ sót 1 chỗ (đặc biệt tool Editor hardcode giá trị CŨ) → chạy lại tool "phá" road mới, props đè road tái phát. *(Bug vòng 7 2026-08-11: road 14 → 18.)*
- **R0.11 — Di chuyển hyper-casual chuẩn: CẠNH LÊN = nhảy 1 lane tức thì, ĐÈ GIỮ = sweep liên tục, NHẢ = snap về lane gần nhất.** Cần phát hiện rising edge (so `_lastInputX` frame trước) chứ không chỉ trạng thái giữ; `_currentLane` phải đồng bộ NGAY ở nhánh edge (tránh stale cho MoveLeft/Right/test). Chỉ sweep (như cũ) = bấm-nhả nhanh gần như không đi → cảm giác "phải bấm 2 lần". *(Bug vòng 7 2026-08-11.)*
- **R0.12 — Camera follow KHÔNG được bám trục X khi player đổi lane** — endless runner 3-lane: camera phải đứng GIỮA ĐƯỜNG (khóa X=0, chỉ bám Z/Y) qua **CameraRig trung gian** (`CameraRig.cs`: LateUpdate ép position x=0; GameManager gán `cam.Follow = rig.transform`). Camera bám thẳng player → đổi lane = CẢNH VẬT trôi theo (tàu gần như đứng giữa) — mất cảm giác rẽ + khó căn lane. *(Bug vòng 8 2026-08-11.)*
- **R0.13 — Popup/feedback điểm ("+N") KHÔNG đặt tại vị trí world của coin** (WorldToScreenPoint) — chữ nằm trên đường che obstacle/coin → không né kịp. Đặt vị trí CỐ ĐỊNH ngoài vùng gameplay: cạnh HUD, và phải KIỂM TRA sizeDelta panel HUD (panel trải ±180 → offset <180 là đè panel). *(Bug vòng 8 2026-08-11.)*
- **R0.14 — Popup/overlay bật/tắt PHẢI có nút đóng rõ ràng (CLOSE/X), không chỉ click ra ngoài (dimmer)** — user không biết click đâu. Nút tạo bằng code idempotent (`transform.Find` trước khi tạo). *(Bug vòng 8 2026-08-11.)*
- **R0.15 — Lane width và vạch chia lane phải KHỚP nhau** — laneWidth 4.5 → vạch đứt chia lane ở ±laneWidth/2 (±2.25, ranh giới lane thật), không phải 1 vạch giữa x=0 khi road đã rộng. Đồng bộ: laneWidth scene ×3 (Player/Obstacle/Pickup) + Tile.laneWidth. *(Bug vòng 8 2026-08-11.)*
- **R0.16 — Khi user muốn "xóa" thứ gì nhưng còn do dự: ẨN BẰNG `m_IsActive: 0` trên GameObject CHA, KHÔNG xóa object/file/code.** 100% reversible (tích lại checkbox là có lại), review xong mới quyết định xóa hẳn. Đã áp dụng cho `Ambient` (cha 28 prop) 2026-08-11. *(Bug 2026-08-11.)*
- **R0.17 — Cảnh vật lề dạng "cột trụ/trạm" KHÔNG hợp khi đã có skybox vũ trụ** — prop đứng lơ lửng 2 bên trông giả tạo. Hướng thay thế đúng thể loại endless runner vũ trụ: **speed-lines / hạt sao vụt ngang / parallax** (xem REFERENCE.md PART 1 — Tính năng). Nếu giữ props: phải là vật thể "thuộc vũ trụ" (thiên thạch, mảnh vỡ, đài radar) chứ không phải cột đứng.

---

## 🌌 NHÓM 0b — TẬN DỤNG 2 GÓI ASSET ĐÃ TẢI (Nebula + SpaceSkies) — 2026-08-11

> User import 2 gói (Nebula Skyboxes: 4 cubemap .exr — SpaceSkies Free: 3 bộ Pink/Green/Purple × 6 mặt × 3 độ phân giải). Cả 2 gói CHỈ chứa skybox/background — không có model/sprites.

- **R0b.1 — Skybox có thể đổi theo scene/mood:** Game scene dùng Nebula (sâu, tinh vân), MainMenu dùng SpaceSkies Purple (nhẹ, tông hư không). Tool `SkyboxSetupTool` đã hỗ trợ cả 2 (menu `Tools/Void Runner/Setup Skybox ...`).
- **R0b.2 — Nebula có 4 cubemap (01–04): đổi dần theo độ khó / mốc điểm** — càng vào sâu (DifficultyManager tăng) càng chuyển nebula đậm hơn → cảm giác "đi sâu vào hư không".
- **R0b.3 — Tận dụng texture mặt (SpaceSkies) làm nền UI**: Game Over / MainMenu background có thể dùng 1 mặt texture skybox làm ảnh nền (cinematic) thay vì nền trơn.
- **R0b.4 — Khi có skybox rồi: bỏ ý tưởng "props lề" — thay bằng hiệu ứng không gian** (xem REFERENCE.md PART 1 — starfield parallax / speed lines) cho hợp lý.
- **R0b.5 — ĐÃ XÓA HẲN Ambient (props lề) khỏi scene Game 2026-08-11** (user duyệt sau khi test UI OK) — thay bằng SpeedLines (vệt sao 2 bên). Nếu muốn dựng lại cảnh vật lề: phải là vật thể "thuộc vũ trụ" (thiên thạch, mảnh vỡ), không phải cột trụ đứng.
- **R0b.6 — Speed-lines phải là chấm sao Billboard rời rạc, KHÔNG dùng renderMode Stretch + lengthScale lớn + emission cao** — Stretch tạo dải sáng liên tục dính nhau (không giống sao). Billboard + startSize 0.09 + rate 70 = sao bay rải rác đúng nghĩa. *(Bug Task D 2026-08-11.)*
- **R0b.7 — Nebula đổi theo độ khó (Task B)**: `NebulaChanger` subscribe `DifficultyManager.OnDifficultyChanged` → level=(speed-start)/(max-start) → `nebula[floor(level×4)]` → RenderSettings.skybox. Tool `Setup Nebula Difficulty` tạo 4 material (Nebula01..04.mat) + gắn component lên Managers (SerializedObject array) — idempotent. Nhớ `using VoidRunner.Systems.VFX;` khi Editor tool reference script Systems (thiếu = CS0246).
- **R0b.8 — Credits/third-party attribution PHẢI hiển thị trong game** (nút CREDITS MainMenu → panel liệt kê assets — dữ liệu khớp agent/REFERENCE.md (PART 3 — Credits)): Kenney CC0 + Nebula/SpaceSkies EULA + "Developed with Unity". Tạo bằng code idempotent (EnsureCredits). Vị trí nút phụ kiểm tra với element cùng cột (BestScore -230 → nút CREDITS -280).

---
