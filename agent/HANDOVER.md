# 📦 HANDOVER — Bàn giao dự án Starveil Runner (đọc file NÀY trước, ~5 phút)

> **Mục đích:** file duy nhất để AI mới / dev mới nắm được toàn bộ repo mà không cần đọc lại lịch sử chat.
> Đọc file này → đọc các file `agent/*.md` theo thứ tự mục 5 → mới bắt tay vào code.
> ⏱️ Nếu bạn là AI trong chat mới: chỉ cần dán phần **mục 0** vào chat là đủ để xử lý tiếp.

---

## 0. 🚀 Prompt dán vào chat mới (copy nguyên văn)

```
Tôi đang phát triển game Starveil Runner (Unity endless runner không gian) tại thư mục
`D:\Unity Project\Void Runner`. Đây là project chính để tôi apply Fresher/Junior Unity Developer,
nên mọi thứ làm theo chuẩn game production thật.

Bước 1 — Đọc NGAY file `agent/HANDOVER.md` (bàn giao dự án, ~5 phút) rồi đọc theo thứ tự
trong mục 5 của file đó: RULES.md → DECISIONS.md → REFERENCE.md (PART 4 Commit) → CHANGELOG.md (đầu file) → PLAN.md (trạng thái).

Bước 2 — Đọc cấu trúc code: Assets/_Project/Scripts/ (Core, Systems, UI, Data, Utils)
+ Assets/_Project/Editor/ (các tool tự động hóa) + Assets/_Project/Tests/.

Quy ước làm việc bất biến:
- Làm từng bước nhỏ + commit từng bước theo agent/REFERENCE.md PART 4 (Conventional Commits,
  tiếng Việt CÓ DẤU). Mỗi lần fix lỗi PHẢI ghi vào agent/CHANGELOG.md trước khi commit.
- TUYỆT ĐỐI không tự ý đổi màu model 3rd-party bằng code (MaterialFixer chuyển Standard→URP/Lit
  giữ màu gốc là ngoại lệ duy nhất). KHÔNG sửa scene/prefab bằng script ngoài Unity khi Unity
  đang mở (R7.6) — ưu tiên viết Editor tool để user chạy trong Unity.
- User thao tác Unity trực tiếp (kéo thả, chạy tool, test) — cần gì hãy dừng lại hướng dẫn user.
- Nói tiếng Việt có dấu. Nếu không chắc chắn điều gì → HỎI user, đừng tự sáng tạo.
```

---

## 1. 🎮 Dự án là gì?

| | |
|---|---|
| **Tên game** | **Starveil Runner** (tên repo cũ: `void-runner` / thư mục máy: `Roll a Ball`) |
| **Thể loại** | Hyper-casual **endless runner 3D · 3 lane** kiểu Subway Surfers / Temple Run |
| **Engine** | Unity **6** (6000.4.5f1) · **URP 17** · Input System · Cinemachine · DOTween |
| **Player** | **Tàu vũ trụ** (chọn được 1 trong 2 model: SF Fighter / Sparrow) |
| **Enemy** | **Flying Beetle** (con bọ) đuổi sau — cơ chế **2 nấc**: đụng vật cản lần 1 → bọ tiến sát + vỗ cánh nhanh; né sạch ~12s → nới ra; **đụng lần 2 trong cửa sổ → bọ lao tới bắt → Game Over** |
| **Obstacle** | **Drone bảo vệ** (Robot_Guardian — Sci fi Drones) — spawn đồng đều theo độ khó, **luôn chừa ≥1 lane an toàn** |
| **Nền tảng** | **WebGL** (chính), build Compression **Gzip** ⚠️ |
| **Trạng thái** | ✅ **LIVE 2026-08-12** — itch.io + Unity Play |
| **Mục tiêu** | Project chính để **apply Fresher/Junior Unity Developer** — làm theo chuẩn production thật |

**Links live:**
- 🎮 itch.io: `https://lothric11.itch.io/starveil-runner`
- 🎮 Unity Play: `https://play.unity.com/en/games/00ba213a-f671-4e8d-9a57-65da13cf1e5c/webgl`

---

## 2. 🏗️ Kiến trúc (Clean Architecture — dependency hướng vào trong)

```
Assets/_Project/
├── Scripts/
│   ├── Core/            # Gameplay lõi — KHÔNG phụ thuộc UI
│   │   ├── Game/        #   GameManager (state machine), GameEvents (static events)
│   │   ├── Player/      #   PlayerController (lane switching, tàu vũ trụ, blink va chạm)
│   │   └── World/       #   TileSpawner (object pool), Tile, ObstacleManager, EnemyChase, Coin, PickupSpawner
│   ├── Systems/         # Dịch vụ độc lập — đăng ký qua GameEvents
│   │   ├── Input/       #   InputReader (bấm = nhảy 1 lane, đè = sweep, swipe mobile)
│   │   ├── Score/       #   ScoreSystem (điểm ×10 + combo ×2..×5)
│   │   ├── PowerUp/     #   PowerUpSystem (Shield / Magnet / Slow-mo)
│   │   ├── Audio/       #   AudioManager (singleton, DontDestroyOnLoad, volume slider)
│   │   ├── Save/        #   SaveSystem (PlayerPrefs: best score + volume + selected ship) + ShipCatalog
│   │   ├── Difficulty/  #   DifficultyManager (tốc độ 10→20, mật độ 0.45→0.75 / 60s)
│   │   └── VFX/         #   VFXManager, SpeedLines (sao), NebulaChanger
│   ├── UI/              # UIManager, PauseManager, Screens/ (MainMenu, ShipSelect), CreditsPanelBuilder
│   ├── Data/            # ScriptableObject defs (ObstacleData, PowerUpData) — asset thật ở _Project/ScriptableObjects/
│   ├── Utils/           # ObjectPool<T>, BlobShadow, MaterialFixer
│   └── (VoidRunner.Core.asmdef — custom assembly, test reference được)
├── Editor/              # ★ Tool tự động hóa — menu "Tools → Starveil Runner" (Setup / Optimize / Fix)
│   ├── Setup/: UIOverhaulTool (UI Theme), SkyboxSetupTool, VFXSetupTool, MaterialLightingSetupTool,
│   ├── Setup/: PostProcessingSetupTool, ShipSelectSetupTool, EnemyMonsterSetupTool, SciFiObstacleSetupTool
│   ├── Optimize/: BuildOptimizerTool · Fix/: AlwaysIncludedShadersTool + helper UIBuilderHelpers
├── Tests/               # Unity Test Framework: 16 EditMode + 8 PlayMode (24 test)
├── Scenes/              # MainMenu.unity (index 0) → Game.unity (index 1)
├── Prefabs/ · ScriptableObjects/ · Art/ · Audio/
```

**Luồng game:** `MainMenu → Game (chạy + né + thu thập) → Game Over → Retry / Menu` · Pause overlay (ESC / nút II) · volume slider · chọn ship.

**Điểm kiến trúc quan trọng:**
- Giao tiếp qua **`GameEvents` (static events)** — không coupling trực tiếp giữa UI ↔ Core
- Track vô tận = **Object Pool** tile + Ground 6000m; Enemy **KHÔNG dùng NavMesh** (R4.1)
- ScriptableObject data asset thật nằm ở `Assets/_Project/ScriptableObjects/` (R7.8)

---

## 3. 🗂️ Hệ thống file docs — ai đọc gì

| File | Nội dung | Khi nào đọc |
|---|---|---|
| **`agent/HANDOVER.md`** (file này) | Bàn giao dự án — tổng quan nhanh | **BẮT BUỘC đầu tiên** |
| **`agent/RULES.md`** | ⚠️ **"Cấm kỵ"** — 40+ rule kỹ thuật rút từ bug đã fix (API Unity 6, asmdef, Editor tool, gameplay, UI, workflow) | **BẮT BUỘC trước khi viết code** — chống tái phạm |
| **`agent/DECISIONS.md`** | Quyết định **thiết kế game** đã chốt với user (R0.x — player=tàu, enemy 2 nấc, UI tiếng Anh...) | Trước khi đổi hành vi game |
| **`agent/REFERENCE.md`** | Tra cứu: PART 1 Tính năng · PART 2 Test checklist · PART 3 Credits/bản quyền assets · PART 4 **Commit convention** · PART 5 Onboard agent thứ 2 | Commit + test + credits |
| **`agent/CHANGELOG.md`** | Lịch sử **toàn bộ bug + nguyên nhân gốc + cách fix** (R7.x — version v3f.x) | Khi gặp bug / sắp commit |
| **`agent/PLAN.md`** | Kế hoạch G0→G3.5 + trạng thái hoàn thành | Khi cần biết còn việc gì |
| **`agent/TESTING.md`** | Checklist test tay + hướng dẫn đo FPS / Profiler | Trước mỗi build |
| **`README.md`** | Giới thiệu + gameplay + điều khiển + build + credits cho người đọc | Trước khi sửa hành vi user thấy được |

---

## 4. ⚠️ Cạm bẫy quan trọng nhất (trích RULES.md — KHÔNG tái phạm)

1. **WebGL Compression = Gzip (format 1).** Unity tự đổi về **Brotli (2)** mỗi khi mở Build Settings → **white screen trên itch.io**. Kiểm tra `grep webGLCompressionFormat ProjectSettings/ProjectSettings.asset` **trước mọi build**. (R7.18)
2. **Không sửa `.unity`/`.prefab` bằng script ngoài Unity khi Unity đang mở file đó** — Unity ghi đè khi Ctrl+S. Mọi thay đổi scene phải qua **Editor tool** chạy trong Unity. (R7.6)
3. **Model 3rd-party (FBX) import shader Built-in Standard → màu TÍM/MAGENTA trong URP.** Fix: `MaterialFixer.EnsureURPMaterials` → URP/Lit **copy CẢ texture** (R5.21). Gắn self-heal trong `Awake()` của component gốc (Obstacle/Coin...). **KHÔNG đổi màu model bằng code.** (R3.16)
4. **Editor tool PHẢI idempotent** (chạy lại an toàn). Tool one-shot đã thay thế → XÓA. (R3.15)
5. **`Object.FindAnyObjectByType<T>()`** — KHÔNG dùng `FindObjectOfType` (obsolete). `Rigidbody.linearVelocity` (không phải `.velocity`). (R1.x)
6. **Unity 6 KHÔNG còn `FontImporter`/`characterSet`** → dùng `TrueTypeFontImporter.fontTextureCase`. (R3.11)
7. **ScriptableObject con tạo bằng code phải `AssetDatabase.AddObjectToAsset`** — thiếu → mất hiệu ứng âm thầm. (R3.1)
8. **`git add` TỪNG FILE cụ thể — KHÔNG `git add .`** (cuốn file hệ thống). Git chạy **tuần tự** (R6.12). Không commit `Assets/_Recovery/`, `Library/`, `*.log`, `.tmp`.
9. **Điểm mô hình:** `enemyTargetHeight 2.9` (bọ), khoảng cách nấc 0 = 16m / nấc 1 = 12m (bị camera cắt nếu nhỏ hơn). Đừng tự ý đổi.
10. **Thứ tự update chống rung:** player bật `RigidbodyInterpolation.Interpolate` + EnemyChase chạy `LateUpdate` (đồng nhịp camera). (v3f.10.2)

> Chi tiết đầy đủ 40+ rule: đọc **`agent/RULES.md`**. Rule mới khi gặp bug mới → ghi CHANGELOG rồi thêm vào RULES.

---

## 5. 📖 Thứ tự đọc docs khi bắt đầu phiên mới

1. `agent/HANDOVER.md` (file này)
2. `agent/RULES.md` — cấm kỵ kỹ thuật
3. `agent/DECISIONS.md` — quyết định thiết kế
4. `agent/REFERENCE.md` — **PART 4 (Commit format bắt buộc)** + PART 1 (tính năng đã làm) + PART 3 (credits)
5. `agent/CHANGELOG.md` — vài entry đầu (bug gần nhất)
6. `agent/PLAN.md` — trạng thái kế hoạch
7. Đọc code theo nhu cầu task (mục 2)

---

## 6. 🔄 Quy trình làm việc chuẩn (user đặt luật — tôn trọng tuyệt đối)

1. **Hỏi/nghe yêu cầu → làm TỪNG BƯỚC NHỎ → test → commit → báo user review.** Tính năng phức tạp → chia nhỏ thành nhiều commit.
2. **Ưu tiên tìm NGUYÊN NHÂN CHÍNH XÁC trước khi fix** — không đoán mò. Dùng Debug.Log để trace nếu cần (nhớ xóa log diag trước khi xong — R7.11).
3. **Mọi bug phát hiện → ghi `agent/CHANGELOG.md` (nguyên nhân + fix + cách tránh)** TRƯỚC khi commit. Khuyến khích phân tích gốc rễ "bug chồng bug".
4. **Cần thao tác trong Unity → dừng lại hướng dẫn user** (user thích tự làm Inspector / chạy tool).
5. **Commit:** Conventional Commits, subject tiếng Việt CÓ DẤU, không viết hoa đầu, không dấu chấm cuối. Type: `feat/fix/refactor/chore/opt/test/build/docs`. (REFERENCE.md PART 4)
6. **Trước khi refactor lớn:** cập nhật docs trước → user duyệt → mới code. (R7.5)
7. **Assets 3rd-party (2.5GB model: SF_Fighter, Sparrow, Flying Beetle, Monster, Spider) KHÔNG nằm trong repo** (GitHub chặn >100MB, xem .gitignore). Clone về cần tự tải + chạy tool Setup lại. Thiếu model → game vẫn chạy (fallback).

---

## 7. 📌 Trạng thái dự án (2026-08-12)

- ✅ G0→G3.5 hoàn thành: core loop, score/combo, power-up, audio, save, difficulty, UI tiếng Anh, pause + volume slider + swipe mobile, chọn ship, VFX/post-processing, **24/24 test xanh**
- ✅ Build WebGL Gzip → **LIVE itch.io + Unity Play**
- ✅ Build size ~128MB (nén zip) — có tool `Build Optimizer` để tối ưu còn ~40-60MB nếu cần
- ⏭️ Hướng phát triển tiếp (nếu user muốn): tối ưu build size, nền tảng mobile native, analytics...
