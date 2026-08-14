# 🚀 Starveil Runner — Kế hoạch nâng cấp "ÁP ĐẢO" (UPGRADE PLAN)

> **Mục tiêu:** biến Starveil Runner thành project vượt trội áp đảo so với fresher Unity khác — tăng tối đa tỷ lệ pass vòng CV + phỏng vấn. Đây là điểm sáng nhất trong CV nên cần đầu tư tối đa.
>
> **Ngày tạo:** 2026-08-15 · **Trạng thái:** 📋 CHƯA BẮT ĐẦU
>
> **Đọc kèm:** `agent/PLAN.md` (kế hoạch gốc — G0→G3.5 đã HOÀN THÀNH) · `agent/REFERENCE.md` (commit convention, credits) · `agent/CHANGELOG.md` (bug + bài học)

---

## ✅ Đối chiếu hiện trạng repo (2026-08-15 — xác minh TRƯỚC khi dùng số liệu, theo nguyên tắc #1)

> Kết quả kiểm tra nhanh repo lúc cập nhật plan này — tránh bịa số liệu trên CV:

| Số liệu trong plan | Thực tế repo (đã verify) |
|---|---|
| **24 tests** | ❌ **Thực tế 31 test methods** (EditMode 16 + PlayMode 15): GameEventsTests 5 · SaveSystemTests 6 · ScoreSystemEditTests 5 · EnemyChasePlayTests 5 · ObstacleCenterPlayTests 2 · PlayerControllerPlayTests 3 · ScoreSystemPlayTests 5. "24/24 xanh" là ghi nhận CŨ 2026-08-12 → **chạy lại Test Runner để xác nhận 31/31 trước khi ghi lên CV/README/badge** |
| **263 commits** | ✅ Đúng (`git log` = 263) |
| **build 125MB → 40–60MB** | ⚠️ Mới dọn ~4.5GB asset rác (2026-08-12) + tool `BuildOptimizer` (commit `5fe5008`) nhưng **chưa từng được chạy** → build deploy cuối vẫn ~128–140MB. **Đã chẩn đoán 2026-08-15:** texture chiếm 97.2% build, Nebula 4× EXR @2048 = ~100MB/139MB (~69%) → đã nâng cấp tool thêm menu "CHỈ giảm texture" (Nebula→1024 + Purple_2K + Sparrow/Beetle/Drone) → **chạy tool + build lại = ~60MB**. Ghi "40–60MB" lên CV CHỈ SAU KHI build lại + đo lại |
| **tool FPSInjectTool đo FPS** | ❌ **ĐÃ BỊ XÓA** (FPSCounter.cs + FPSInjectTool.cs + component khỏi MainMenu.unity — deploy fix 2026-08-12, xem CHANGELOG) → Mục 5 chỉ dùng **Unity Profiler**; cần counter màn hình thì phải tạo lại tool |
| **Chọn tàu ở MainMenu** | ✅ ĐÃ CÓ SẴN (Task D — ShipSelectManager + preview 3D RenderTexture + SaveSystem.SelectedShip) → Mục 1 bỏ "(nếu có)" |
| **webGLCompressionFormat** | ✅ Đang **Gzip (format 1)** — đúng chuẩn itch.io (R7.18); `Builds/WebGL` tồn tại |
| **Editor tools** | ✅ **11 file** trong `Assets/_Project/Editor/` (cleanup 2026-08-15 — đã xóa 4 tool one-shot: RefactorGameplayTool, RenameGameTitleTool, SpriteBatchConverter, KenneyFontImporter): BuildOptimizerTool, AlwaysIncludedShadersTool, SciFiObstacleSetupTool, ShipSelectSetupTool, UIOverhaulTool, VFXSetupTool, PostProcessingSetupTool, MaterialLightingSetupTool, SkyboxSetupTool, EnemyMonsterSetupTool, UIBuilderHelpers — menu gom 3 nhóm: `Tools/Starveil Runner/Setup/` · `Optimize/` · `Fix/` |

---

## 0. Nguyên tắc bất biến (KHÔNG được phá vỡ)

1. **KHÔNG bịa số liệu** — mọi con số trên CV phải có bằng chứng nhìn thấy được trong repo (ảnh / GIF / badge / link).
2. **Commit theo convention** — `feat/refactor/docs/chore/build + scope` (xem REFERENCE.md PART 4).
3. **Cập nhật PLAN.md + CHANGELOG.md** sau khi xong mỗi mục.
4. **Mỗi mục phải có "bằng chứng trình diễn"** — thứ recruiter nhìn thấy được trong 30 giây.

---

## 1. Tổng quan — 6 mục nâng cấp

| # | Mục | Thời lượng | Ưu tiên | Trạng thái |
|---|---|---|---|---|
| 1 | **GIF gameplay + README nâng cấp + trailer video** | 1 buổi | 🔥 Ngay | ☐ |
| 2 | **Online Leaderboard (Supabase)** | 2–3 ngày | 🔥 1 | ☐ |
| 3 | **CI/CD GitHub Actions + badge** | 1–2 ngày | 🔥 2 | ☐ |
| 4 | **Android build qua LDPlayer** (không cần máy thật) | 1–2 ngày | 🥈 6 | ☐ |
| 5 | **Performance profiling doc** | 1 ngày | 🥉 5 | ☐ |
| 6 | **Enemy type mới** | 2–3 ngày | 🥉 4 | ☐ |

**Timeline đề xuất:** Mục 1 (ngay) → Mục 2 (tuần 1) → Mục 3 (tuần 2) → Mục 6 (tuần 3) → Mục 5 (tuần 3) → Mục 4 (tuần 4).
*Digital Unicorn hạn 4/10/2026 — dư ~7 tuần, làm xong cả 6 mục thoải mái.*

---

## 2. Chi tiết từng mục

---

### 🎬 Mục 1 — GIF gameplay + README nâng cấp + Video trailer (1 buổi)

**Nó là gì:** GIF = clip 5–10 giây lặp vòng. Trailer = video 30–60 giây cắt montage gameplay.

**Vì sao:** recruiter mở repo GitHub trong 30 giây quyết định ấn tượng. Có GIF chạy ngay đầu README = "game này thật, chơi được". Repo fresher khác toàn chữ, không ai đọc.

**Các bước:**

- [ ] **User — quay video gameplay** bằng OBS (miễn phí, obsproject.com): chơi vài lần, quay các cảnh:
  - Cảnh 1: chọn tàu ở MainMenu (**đã có sẵn — panel SELECT SHIP**, preview 3D xoay)
  - Cảnh 2: né obstacle liên tục + đụng 1 lần → Enemy tiến sát (cơ chế đặc trưng!)
  - Cảnh 3: nhặt coin + power-up (shield/magnet/slow-mo)
  - Cảnh 4: đụng lần 2 → Enemy lao tới bắt → Game Over
  - Cảnh 5: Game Over screen + (sau này) bảng Top 10 leaderboard
- [ ] **User — tạo GIF:** cắt clip ngắn (~5–8s) bằng OBS hoặc công cụ cắt → xuất GIF (dùng ezgif.com hoặc ffmpeg). Đặt vào `docs/` trong repo.
- [ ] **User — tạo trailer 30–60s:** montage các cảnh trên, không watermark, upload **YouTube (unlisted)** → nhúng link vào itch.io page + README + email ứng tuyển.
- [ ] **AI — nâng cấp README.md:**
  - GIF gameplay **ngay đầu trang** (trước cả badge)
  - 3–4 screenshot gameplay
  - **ASCII architecture diagram** (Core → Systems → UI) — vẽ sẵn
  - Section "Kỹ thuật nổi bật" ngắn gọn, có link file code cụ thể
  - Section "Why this project" — kể chuyện ĐÚNG SỐ LIỆU: **31 tests** (16 EditMode + 15 PlayMode), 263 commits, dọn ~4.5GB asset rác → build ~128MB zip, 60 FPS WebGL. ⚠️ KHÔNG ghi "40–60MB" — đó là mục tiêu chưa đạt (xem bảng đối chiếu ở đầu file)
- [ ] **AI — chuẩn bị kịch bản email** kèm link trailer cho các lần apply sau.

**Bằng chứng sau khi xong:** GIF chạy trên GitHub · link YouTube trailer · itch.io có video.

---

### 🏆 Mục 2 — Online Leaderboard (Supabase) (2–3 ngày)

**Nó là gì:** Supabase = cơ sở dữ liệu miễn phí trên mây (không liên quan Unity — là dịch vụ web độc lập). Game hiện lưu điểm trong máy (PlayerPrefs) — nâng cấp để **gửi điểm lên mạng + tải top 10 hiển thị trong game**.

**Vì sao:** bù lỗ hổng lớn nhất của project = **0 networking trong game**. Digital Unicorn làm game **multiplayer** — JD ghi nice-to-have số 1 là "multiplayer experience". Có leaderboard = bằng chứng **networking thật + REST API + backend integration**.

**Cách hoạt động:** player chết → game gửi điểm lên Supabase (POST) → màn hình Game Over tải top 10 (GET) hiển thị "Top 10 players".

**Các bước:**

- [ ] **User — tạo tài khoản Supabase miễn phí** (supabase.com → Sign up → New project → đặt tên + password database → vùng Singapore cho latency thấp VN). Lấy **Project URL** + **anon public key** (Settings → API).
- [ ] **AI — viết schema SQL** (SQL Editor → New query → paste → Run):
  ```sql
  create table public.leaderboard (
    id bigint generated by default as identity primary key,
    player_name text not null default 'anonymous',
    score int not null,
    created_at timestamptz not null default now()
  );
  alter table public.leaderboard enable row level security;
  create policy "public read" on public.leaderboard for select using (true);
  create policy "public insert" on public.leaderboard for insert with check (true);
  ```
- [ ] **AI — viết C# script** `Systems/Leaderboard/LeaderboardService.cs` (thuần, không phụ thuộc UI):
  - Dùng **UnityWebRequest** (có sẵn trong Unity, không cần package nào)
  - `POST {url}/rest/v1/leaderboard` — gửi `{player_name, score}` + header `apikey` + `Authorization: Bearer`
  - `GET {url}/rest/v1/leaderboard?order=score.desc&limit=10` — lấy top 10
  - Cấu hình URL + key qua **ScriptableObject** (theo style ObstacleData sẵn có — không hardcode)
  - Xử lý lỗi offline: nếu không có mạng → bỏ qua im lặng, game vẫn chơi bình thường
- [ ] **AI — tích hợp UI:** Game Over screen hiện "TOP 10 PLAYERS" (fetch khi game over) + ô nhập tên 3 ký tự kiểu arcade (hoặc tên mặc định) + nút retry. TextMeshPro + font Kenney sẵn có.
- [ ] **AI — PlayMode test** cho LeaderboardService (mock phản hồi) — theo chuẩn **31 test sẵn có** (16 EditMode + 15 PlayMode). Lưu ý: file `.cs` mới phải nằm trong asmdef `VoidRunner.Core` (R2.1/R2.2 — khai báo references đầy đủ), ScriptableObject config đặt ở `Assets/_Project/ScriptableObjects/` (R7.8).
- [ ] **User — test trên WebGL:** build lại → chơi trên itch.io → xác nhận điểm xuất hiện trong Supabase Table Editor.
- [ ] **AI — cập nhật CV:** thêm "Supabase (REST API)" vào Skills + 1 dòng trong project Starveil: "online leaderboard (Supabase REST API)".

**⚠️ Trung thực:** không ghi "game multiplayer" — chỉ ghi "online leaderboard". Đừng thổi phồng thành game online đầy đủ.

**Bằng chứng sau khi xong:** chơi game trên itch.io → chết → thấy top 10 toàn cầu · ảnh chụp bảng điểm trong Supabase.

---

### 🤖 Mục 3 — CI/CD GitHub Actions + badge (1–2 ngày)

**Nó là gì:** robot tự động. Mỗi lần push code lên GitHub → máy chủ GitHub **tự mở Unity, tự build WebGL, tự chạy toàn bộ test (hiện tại 31)** → báo kết quả qua **badge xanh** trên README: `✓ build passing · 31/31 tests`.

**Vì sao:** fresher gần như không ai có. Tín hiệu "người hiểu quy trình team thật" — studio trọng hơn cả feature. Bonus: bằng chứng cho hướng apply DevOps/full-stack.

**Các bước:**

- [ ] **User — tạo GitHub Actions secret:** vào repo Starveil-Runner trên GitHub → Settings → Secrets and variables → Actions → thêm:
  - `UNITY_EMAIL` (email đăng nhập Unity)
  - `UNITY_PASSWORD` (mật khẩu Unity — cần bật "Sign in with Unity ID" + tài khoản Personal license miễn phí)
  - `UNITY_SERIAL` (nếu có license serial; Personal thì để trống)
- [ ] **AI — viết `.github/workflows/build-test.yml`** dùng **GameCI** (game-ci/unity-builder + unity-test-runner — chuẩn ngành):
  - Job 1: `run tests` — chạy EditMode + PlayMode (**31 test** — con số tự động đếm từ Test Runner, không hardcode), báo pass/fail
  - Job 2: `build WebGL` — build + upload artifact (tải về được)
  - Trigger: push lên `main` + pull request
  - Badge: `https://img.shields.io/badge/tests-31/31-brightgreen` — **sau khi chạy lại Test Runner xác nhận 31/31 xanh** (con số 24/24 cũ đã lỗi thời; nếu có test fail phải fix trước rồi mới ghi badge)
- [ ] **AI — thêm badge lên README** (cạnh badge Unity/C#/URP sẵn có).
- [ ] **User — bấm nút:** push lên → vào tab **Actions** xem robot chạy → xác nhận build pass.
- [ ] **AI — kiểm tra log build**: nếu fail vì license/package thì fix.

**⚠️ Lưu ý:** GameCI cần license activation — nếu vướng, phương án dự phòng: chỉ chạy `unity-test-runner` (test) trước, build WebGL để sau.

**Bằng chứng sau khi xong:** badge xanh trên README + tab Actions hiện "All checks passed".

---

### 📱 Mục 4 — Android build qua LDPlayer (KHÔNG cần máy thật) (1–2 ngày)

**Giải pháp thay thế máy thật:** dùng **LDPlayer** (giả lập Android miễn phí chạy trên PC). **Android Studio KHÔNG cần** (đó là tool viết app Java/Kotlin — Unity tự build APK, LDPlayer chỉ để chạy thử).

**Vì sao:** 80% studio game VN (Bounce, 9AM) làm game mobile. Có APK chạy được + video trên "điện thoại" = dòng fresher không thể giả.

**Các bước:**

- [ ] **User — cài module Android Build Support:** Unity Hub → Installs → ⚙️ cạnh Unity 6 → Add modules → tick **Android Build Support** (gồm SDK, NDK, OpenJDK) → Install (~2–4GB).
- [ ] **User — cài LDPlayer:** tải ldplayer.net (miễn phí) → cài → mở (đợi lần đầu khởi động).
- [ ] **AI — chuẩn bị build settings:** hướng dẫn/nhắc File → Build Profiles → Android → target **IL2CPP** + **ARM64** + texture compression ASTC (nếu ai chưa cấu hình). Bật swipe input sẵn có (đã có Pointer swipe từ G3.5).
- [ ] **User — Build APK** trong Unity (~10–30 phút) → file `.apk` trong `Builds/Android/`.
- [ ] **User — cài vào LDPlayer:** kéo thả file APK vào cửa sổ LDPlayer → tự cài → mở game → chơi thử (kiểm tra swipe, pause nút II, âm lượng).
- [ ] **User — quay video** game chạy trên LDPlayer (OBS) → dùng cho trailer + email.
- [ ] **AI — tối ưu mobile nhỏ:** HUD safe-area (PLAN.md G3.5 còn note "tùy chọn tương lai"), nút bấm đủ to cho touch.
- [ ] **AI — cập nhật CV:** thêm "**Android build tested on LDPlayer emulator**" (⚠️ KHÔNG ghi "real device" — phải trung thực).

**Bằng chứng sau khi xong:** APK file trong repo/Builds + video game chạy trên LDPlayer + dòng trên CV.

---

### 📊 Mục 5 — Performance profiling doc (1 ngày)

**Nó là gì:** dùng **Unity Profiler** (cửa sổ có sẵn trong Unity — đo chính xác thời gian từng phần: rendering, logic, GC) → chụp màn hình kết quả → lưu thành tài liệu trong repo.

**Vì sao:** câu hỏi phỏng vấn chắc chắn: *"làm sao giữ 60 FPS?"* — đưa ảnh bằng chứng thay vì nói suông. Người khác nói, bạn có hình.

**Các bước:**

- [ ] **AI — tạo khung `docs/PERFORMANCE.md`**: template với các section trống + hướng dẫn chụp.
- [ ] **User — chụp ảnh Profiler:** Window → Analysis → Profiler → bật play → chụp:
  - Tab CPU: tổng ms + các đỉnh (GC spike?)
  - Tab Memory / Alloc: GC allocation mỗi frame
  - Tab Rendering: draw calls
  - ⚠️ **FPSInjectTool/FPSCounter ĐÃ BỊ XÓA khỏi repo (2026-08-12)** — đo bằng Unity Profiler là chính; nếu muốn số FPS trên màn hình → tạo LẠI tool (tool cũ đã xóa, không dùng lại file được)
- [ ] **AI — điền tài liệu**: phân tích số liệu, ghi chú "Object Pool → 0 GC spike", "tile recycle → không Instantiate/Destroy", so sánh trước/sau **cleanup ~4.5GB asset rác** (bằng chứng: commit `5fe5008` BuildOptimizerTool + CHANGELOG 2026-08-12) — **build deploy cuối ~132MB, ghi đúng con số; 40–60MB chỉ ghi là mục tiêu nếu chưa build lại**.
- [ ] **AI — cập nhật README** link tới `docs/PERFORMANCE.md`.

**Bằng chứng sau khi xong:** file `docs/PERFORMANCE.md` có ảnh Profiler thật + README link.

---

### 👾 Mục 6 — Enemy type mới (2–3 ngày)

**Nó là gì:** thêm 1 loại địch **hành vi khác** con bọ đuổi theo hiện tại — ví dụ **drone patroller** bay ngang qua các lane (phải né kiểu khác).

**Vì sao:** chứng minh **game design sense** (biết tạo biến thể gameplay, không chỉ 1 cơ chế) + game có chiều sâu replay.

**Các bước (AI code, theo đúng style Clean Architecture sẵn có):**

- [ ] Thiết kế: **PatrollerDrone** — bay ngang lane ở vị trí cố định phía trước, di chuyển trái-phải theo thời gian; đụng = đếm như obstacle thường (Enemy tiến sát); xuất hiện từ mốc score X trở đi (DifficultyManager)
- [ ] Viết `Core/World/PatrollerDrone.cs` — thuần logic, giao tiếp qua event (theo EnemyChase/Obstacle chuẩn)
- [ ] Data: thêm `ObstacleData` mới (spawnWeight theo độ khó) — ScriptableObject sẵn có
- [ ] Prefab: dựng bằng tool Editor (theo chuẩn `SciFiObstacleSetupTool` idempotent) — dùng asset Sci Fi Drones có sẵn
- [ ] Test: thêm PlayMode test cho hành vi patroller (theo `EnemyChasePlayTests`)
- [ ] Tuning: đảm bảo vẫn **luôn chừa ≥1 lane an toàn** + 60 FPS. Lưu ý theo DECISIONS: đụng PatrollerDrone = đếm 1 lỗi như obstacle thường (R0.4 — KHÔNG chết trực tiếp); spawn phải chia sẻ `ObstacleManager.BlockedLanes` + giữ safe zone 20m đầu game (R4.21/R4.24)
- [ ] **AI — cập nhật README** bảng tính năng + **CV**: "2 enemy types with distinct behaviors"

**Bằng chứng sau khi xong:** gameplay video có 2 loại địch · README cập nhật · test mới pass.

---

## 3. Phân công tóm tắt

| Việc | AI làm | User làm |
|---|---|---|
| README + ASCII diagram + tài liệu | ✅ | — |
| Quay video / GIF / trailer | Kịch bản | ✅ Quay + cắt |
| Leaderboard code + SQL + test | ✅ | Tạo tài khoản Supabase + dán 2 giá trị |
| CI/CD workflow file + badge | ✅ | Tạo 3 secrets + bấm nút push |
| Android | Hướng dẫn + tối ưu HUD | Cài module + LDPlayer + build + quay |
| Performance doc | Khung + phân tích | Chụp ảnh Profiler |
| Enemy mới | Toàn bộ code + test | Test chơi thử |

---

## 4. Checklist tổng (đánh dấu khi xong)

- [ ] Mục 1: GIF trên README + trailer YouTube + itch.io có video
- [ ] Mục 2: Leaderboard chạy trên WebGL + điểm vào Supabase thật
- [ ] Mục 3: Badge xanh trên README + Actions pass
- [ ] Mục 4: APK chạy trên LDPlayer + video + CV cập nhật
- [ ] Mục 5: `docs/PERFORMANCE.md` có ảnh Profiler
- [ ] Mục 6: 2 enemy types + test pass
- [ ] Cập nhật README/PLAN/CHANGELOG + commit sạch
- [ ] Cập nhật CV Unity (`edit-cv-to-match-jd/cv/unity.html`) + email mẫu

---

## 5. Hướng dẫn phiên làm việc mới

1. Đọc file này trước → rồi `agent/PLAN.md` → `agent/REFERENCE.md` (PART 4 commit) → `agent/CHANGELOG.md`
2. Làm theo thứ tự mục 1 → 2 → 3 → 6 → 5 → 4 (dễ → khó, thấy kết quả sớm)
3. Mỗi mục xong → commit + cập nhật trạng thái ☐ → ✅ ở file này
4. Nhắc user: KHÔNG bịa số liệu, ghi trung thực (LDPlayer ≠ real device)
