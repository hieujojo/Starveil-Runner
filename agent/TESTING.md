# TESTING — Hướng dẫn kiểm thử Void Runner

> Cập nhật: 2026-08-12 · Giai đoạn hiện tại: **Tuning 60 FPS** (G3)
> Cách dùng: làm từng bước từ trên xuống, đánh dấu ✅/❌, gửi log + ảnh về cho AI review.

---

## 0. Chuẩn bị mỗi lần test

1. Unity compile xong (không còn lỗi đỏ) → **Clear Console** (nút Clear trên Console window)
2. Mở scene **MainMenu** (để test luồng Menu → Game → Game Over → Retry/Menu)
3. Bấm **PLAY**

> ⚠️ Console còn lỗi đỏ = DỪNG test ngay, gửi log cho AI fix trước.

---

## 1. Đo FPS (đang làm)

### Cách 1 — FPS counter trên màn hình (nhanh nhất)

1. Mở scene cần test (MainMenu hoặc Game)
2. Menu **Tools → Void Runner → Add FPS Counter (Open Scene)** (chạy lại an toàn — idempotent)
3. **PLAY** → góc trái trên hiện: `FPS: 60 (16.6 ms)  GC: 180 MB [F3]`
4. Bấm **F3** để ẩn/hiện counter

**Chỉ số cần đạt (máy thường):**

| Chỉ số | OK | Cần xem lại |
|---|---|---|
| FPS trong Editor | ≥ 55 | < 45 → lag thật |
| Frame time | ≤ 17 ms | > 22 ms thường xuyên |
| GC heap (MB) | không tăng dần liên tục | tăng đều theo thời gian → có allocation mỗi frame (pool thiếu) |

> ⚠️ **Quan trọng:** FPS trong Editor chỉ là con số tương đối. **Con số THẬT phải đo trên Chrome sau khi build WebGL** (Editor nhanh hơn build thật 30–50%). Sẽ làm ở bước WebGL build.

### Cách 2 — Unity Profiler (khi cần tìm nguyên nhân lag)

1. **Window → Analysis → Profiler**
2. Bấm **PLAY**, chơi ~30 giây (để độ khó tăng dần — lúc này dễ lag nhất)
3. Xem module **CPU Usage**: target **< 16.6 ms** (60 FPS)
4. Xem **GC Allocation**: phải ~0; nếu spike mạnh → có Instantiate/Destroy giữa chừng
5. Nếu nghi render: **Window → Analysis → Frame Debugger** xem draw calls (target < 200 cho WebGL mobile)

### Ghi kết quả

| Điều kiện | FPS | Frame ms | GC MB | Ghi chú |
|---|---|---|---|---|
| Menu đứng yên | | | | |
| Game 10 giây đầu | | | | |
| Game 60 giây+ (độ khó cao, nhiều drone) | | | | |
| Game Over / Retry | | | | |

---

## 2. Playtest checklist (logic đã hoàn thiện — cần test lại toàn bộ)

### A. Main Menu
- [ ] Chọn tàu: bấm **◀ ▶** (cả phím mũi tên lẫn **bấm chuột vào nút**) → tàu preview đổi đúng, tên tàu đổi
- [ ] How To Play: bấm → popup mở có nền đen mờ + **nút ✕** đóng được, không che mất chữ
- [ ] Credits: bấm → hiển thị đủ, đóng được
- [ ] Sound: bấm → icon ON/OFF đổi, có nhạc khi vào game
- [ ] Nút Play → vào được scene Game (không cần test từng scene riêng)

### B. Gameplay (scene Game)
- [ ] **Tàu**: nổi bật nhất màn hình (không bị UI nào che), to hợp lý, lửa tên lửa sau đuôi = **dải cam mượt** (không hạt vuông), dài vừa phải
- [ ] **Điều khiển**: đè A/D hoặc ←/→ → tàu chuyển lane liền (không cần bấm 2 lần), chỉ TÀU di chuyển (cảnh vật không trôi theo)
- [ ] **Xu (coin)**: nhặt được → popup **+10** hiện gần giữa màn hình (không che đường né), điểm tăng
- [ ] **Vật cản (drone)**: nằm **giữa 1 trong 3 lane** (không lệch), **luôn có ≥1 lane trống** để né kể cả độ khó cao, không đè lên xu
- [ ] **Con bọ (Enemy)**: xuất hiện từ đầu game, **màu nâu đậm** (không trắng/tím), di chuyển trái/phải **cùng lúc** với tàu (không trễ), không che khuất tàu
- [ ] **Va chạm lần 1**: tàu nhấp nháy, con bọ tiến sát 1 nấc (12m), vỗ cánh nhanh hơn — **chưa chết**
- [ ] **Né sạch 10–15s**: con bọ nới dần về khoảng cách ban đầu (16m)
- [ ] **Va chạm lần 2 (trong cửa sổ)**: con bọ **lao tới bắt** → Game Over mượt (không giật)
- [ ] **Game Over**: hiện điểm + best (ẩn khi = 0), nút RETRY/MENU hoạt động

### C. Vòng lặp
- [ ] MainMenu → Play → chết → RETRY → chơi tiếp đúng vị trí bắt đầu cố định
- [ ] MainMenu → Play → chết → MENU → về menu không lỗi
- [ ] Chơi lại nhiều lần liên tiếp: không lỗi, không lag tăng dần, coin/obstacle spawn đúng

---

## 3. Khi gửi kết quả cho AI

- Kèm **ảnh màn hình** chỗ lỗi + **log Console** (bấm Clear trước khi chạy lại để chỉ có log mới)
- Nếu có log đỏ dù đã Clear → chụp ngay, đó là lỗi thật cần ưu tiên
- Mô tả: "bước nào → thấy gì → mong đợi gì"

---

## 4. Test về sau (khi build WebGL xong)

- [ ] Build WebGL → mở trên **Chrome** (F12 → Performance → Record 30s) đo FPS thật
- [ ] Test các độ phân giải: full HD, laptop nhỏ, mobile viewport
- [ ] Test 3 trình duyệt: Chrome / Firefox / Safari
