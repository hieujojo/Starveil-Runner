# CHANGELOG — Nhật ký lỗi đã sửa (lessons learned)

> **Mục đích:** ghi lại mọi lỗi/warning đã gặp trong quá trình phát triển, cách fix và cách tránh lặp lại.
> Cập nhật mỗi lần fix lỗi, trước khi commit.

---

## 2026-08-12 (v3f.9) — VẪN "HẠT VUÔNG CAM" SAU KHI BỎ CUBE: đổi hẳn sang TrailRenderer

**Triệu chứng:** user: "vẫn là các hạt vuông vuông màu cam, tại sao vậy". Kèm DIAG log `[DIAG-FLAME] shader=Sprites/Default tex=32x32` — material exhaust ĐÚNG (giống sao/coin, đều tròn).

**Điều tra (không đoán):**
- Exhaust dùng ĐÚNG material tròn (DIAG xác nhận), sao trôi + coin burst cùng material đó user xác nhận OK → hạt code không thể là ô vuông.
- Model tàu (SF_Fighter/Sparrow) KHÔNG có sẵn ParticleSystem (đã kiểm tra prefab) → không phải model.
- Tàu đang dùng = MODEL (ShipCatalog.Load hoạt động ở editor; scene không gán shipPrefabs).
- SpeedLines màu xanh-trắng (không cam). ObstacleFX cũng dùng material tròn.
→ **Kết luận: chùm hạt cam DÀY (19 hạt/3m) + Bloom → mỗi hạt sáng nhỏ bị bloom thành khối blocky → nhìn như "hạt vuông vuông"**. Hạt rời thì OK (sao thưa, coin rải rác), chùm dày thì vỡ blocky.

**Fix (v3f.9 — đổi hẳn sang effect mới):** BỎ particle exhaust → **TrailRenderer** (cùng material mềm + gradient cam sáng→trong suốt, time 0.18s, width 0.34→0.02) — render như 1 dải ribbon mượt, về mặt kỹ thuật KHÔNG thể hiện "ô vuông". Giữ Light cam lập lòe. ResetToStart thêm `_flameTrail.Clear()` (teleport về start tránh vệt kéo dài xuyên map). Kèm DIAG mới `[DIAG-SHIP]` (xác nhận model/primitive).

**Bài học:** debug VFX phải phân biệt được: (1) material có đúng không (DIAG in shader), (2) là MESH cube hay HẠT, (3) hiệu ứng hậu kỳ (Bloom) có biến chùm hạt dày thành blocky không. Khi material đã chứng minh đúng mà user vẫn báo vuông → nghi ngờ Bloom/độ dày chùm hạt, đổi loại hiệu ứng (particle → trail) thay vì chỉnh lại cùng loại.

---

## 2026-08-12 (v3f.8) — "LỬA" vẫn là Ô VUÔNG CAM NỐI NHAU: thủ phạm là CUBE "Thruster", không phải hạt

**Triệu chứng:** user: "các hạt sao đã oke nhưng tên lửa vẫn kì — các ô vuông cam nhỏ hơn nối liền nhau; cái tên lửa sau tàu đang render bằng effect gì vậy, đổi sang effect mới đi".

**Root cause:** sau v3f.7.2, hạt exhaust ĐÃ tròn (Sprites/Default — sao trôi xác nhận tròn). Nhưng user vẫn thấy "ô vuông cam" = **cube "Thruster"** (CreatePart cube 0.18×0.7, material _flameMat emissive cam, scale flicker PerlinNoise 22Hz) — cube vuông phát sáng + flicker + Bloom → nhìn như dãy ô vuông cam nối nhau. Sao tròn vì là hạt; cube vuông vì LÀ CUBE.

**Fix (v3f.8 — đổi sang effect mới):**
- **Bỏ hẳn cube Thruster** → thay bằng **Light cam Point lập lòe** (CreateFlameLight, intensity 1.5 × PerlinNoise) + hạt exhaust giữ nguyên tròn nhưng nâng cấp: rate 35→55, size 0.14 **nở rộng ×1.8 theo thời gian sống** (sizeOverLifetime) + **mờ dần** (colorOverLifetime gradient trắng-vàng → cam → trong suốt) = hình lưỡi lửa thật.
- **Self-heal**: BuildSpaceship — nếu tàu cũ còn "Thruster" cube / "Exhaust" material cũ → Destroy rồi dựng lại hiệu ứng mới (idempotent, chạy lại an toàn).
- DIAG log `[DIAG-FLAME]` (1 lần/play) in shader + texture thực tế để xác nhận — XÓA sau khi user test OK.

**Bài học:** phân biệt HẠT (ParticleSystem) vs HÌNH (Mesh primitive) khi debug VFX — "ô vuông cam" có thể là cube phát sáng chứ không phải hạt vuông. Luôn nêu rõ "cái gì đang hiển thị" (mesh vs particle) khi user báo lỗi hình ảnh.

---

## 2026-08-12 (v3f.7.2) — VẪN HÌNH VUÔNG: đọc shader URP 17.4 thật mới ra sự thật

**Triệu chứng:** user: "vẫn là hình vuông, chỉ nhỏ hơn; lửa = 1 dãy hình vuông cam nối nhau, ko thấy tên lửa".

**Root cause (lần này ĐỌC SHADER THẬT, không đoán — bài học R3.1 nâng cấp):**
- File thật trong URP 17.4 là `Shaders/Particles/ParticlesUnlit.shader` (Shader name vẫn "Universal Render Pipeline/Particles/Unlit" — Shader.Find OK, KHÔNG phải lỗi tìm shader).
- `_SrcBlend("__src", Float) = 1.0`, `_DstBlend("__dst", Float) = 0.0` → **default = One/Zero = OPAQUE**.
- **KHÔNG có keyword `_ALPHABLEND_ON`** — fix v3f.7.1 bật keyword KHÔNG TỒN TẠI → vô tác dụng.
- **KHÔNG có lệnh `Blend [...]`** trong pass → set property gì cũng vẫn OPAQUE → hạt LUÔN VUÔNG.

**Fix:** bỏ hẳn URP Particles — dùng **`Sprites/Default`**: `Blend One OneMinusSrcAlpha` CỐ ĐỊNH + nhân vertex color (startColor) + `[PerRendererData] _MainTex` → KHÔNG cần cấu hình gì, chắc chắn TRÒN. Một fix ăn hết: sao trôi + lửa tên lửa + coin burst + vệt khói bọ.

**BÀI HỌC R3.1 (tối thượng):** khi hiệu ứng material chưa đúng → **đọc shader thật trong Library/PackageCache** (đường dẫn `Shaders/Particles/ParticlesUnlit.shader`, lưu ý tên file có thể khác tên shader display). Đừng đoán property/keyword từ trí nhớ — 2 lần đoán sai liên tiếp đều do đó.

Tinh chỉnh kèm: exhaust size 0.24→0.18 + lifetime 0.35→0.3 → tia lửa thanh mảnh, không thành "cục nối nhau".

## 2026-08-12 (v3f.7.1) — Sao trôi thành HÌNH VUÔNG + lửa văng lung tung

**Triệu chứng:** user: "sao trôi quá to + hình ô vuông, nếu thật là ô vuông thì bỏ luôn; lửa bắn ít đi, đừng văng lên trời loạn quá".

**Root cause 1 — sao vuông:** `CreateSoftParticleMaterial` tạo `new Material(shader URP Unlit)` nhưng **KHÔNG bật alpha blending** — keyword `_ALPHABLEND_ON` tắt → material render **OPAQUE** → texture tròn (RGB trắng, alpha chỉ ở kênh alpha) hiện thành **khối vuông trắng/xanh nhạt** (nhìn ảnh: "square floating blocks"). Coin burst từng dùng chung material nhưng hạt nhỏ 0.35 + bay nhanh nên không thấy rõ; sao to 0.8 → lộ rõ.

**Fix:** cấu hình blend đúng trong `CreateSoftParticleMaterial` (HasProperty guard để an toàn với fallback Sprites/Default):
```csharp
if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0f);   // 0 = Alpha
if (mat.HasProperty("_SrcBlend"))  mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
if (mat.HasProperty("_DstBlend"))  mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
mat.EnableKeyword("_ALPHABLEND_ON");
mat.SetOverrideTag("RenderType", "Transparent");
mat.renderQueue = (int)RenderQueue.Transparent;
```
BÀI HỌC R3.1 (nâng cấp): `new Material(shader)` không kế thừa cấu hình blend của shader — **material particle runtime phải tự set blend keyword + Src/DstBlend + renderQueue**, không tin default. Đồng thời giảm sao: size 0.35–0.8 → **0.15–0.35** (quá to), rate 90→**50**.

**Root cause 2 — lửa văng lung tung:** `CreateExhaustSystem` dùng shape **Sphere** (radius 0.12) → hạt phun ra **MỌI HƯỚNG** (cả lên trời) với speed -9 → loạn. Fix: shape **Cone** góc 6° (chùm hẹp về sau -Z) + rate 70→**35**, size 0.3→**0.24**, speed -9→-8, lifetime 0.4→0.35.

## 2026-08-12 (v3f.7) — XÓA VỆT TÍM: shader Particles/Additive KHÔNG tồn tại trong URP

**Triệu chứng:** user: "vệt dài tím tím sau xe, tàu vũ trụ thì phát ra tên lửa chứ, vệt quá dài, hệ sao trôi là gì tôi ko thấy".

**Root cause (R3.1 tái phát):** `SetupShipTrail` (v3f.6) dùng `CreateAdditiveSoftMaterial()` = `new Material(Shader.Find("Universal Render Pipeline/Particles/Additive"))`. Shader **`Particles/Additive` KHÔNG TỒN TẠI trong URP** (verify: chỉ có Lit / Simple Lit / Unlit) → `Shader.Find` trả null → `new Material(null)` = **error shader MÀU TÍM**. Guard cũ `if (mat == null || mat.shader == null)` **KHÔNG bắt được** vì error shader không phải null → fallback `Sprites/Default` không bao giờ chạy.

**BÀI HỌC R3.1 (nâng cấp):** check **`Shader` trước** khi `new Material` — không dựa vào `mat.shader == null`:
```csharp
Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
if (shader == null) shader = Shader.Find("Sprites/Default");
var mat = new Material(shader);
```

**Fix:**
- Xóa hẳn `SetupShipTrail` + `CreateAdditiveSoftMaterial` + `_shipTrail` + clear trail trong HandleRestart (grep: không ai gọi — xóa an toàn).
- **Lửa tên lửa ĐÃ CÓ SẴN** ở PlayerController (Thruster lập lòe PerlinNoise + exhaust hạt cam) — bị vệt tím che mất. Tăng cho rõ: exhaust rate 45→70, size 0.22→0.3, speed -7→-9, lifetime 0.35→0.4, maxParticles 80→120; Thruster dài 0.55→0.7 (cả nhánh primitive + model).
- **Sao trôi vô hình**: quá nhỏ (0.06–0.18) + box đặt cách player 14m phía trước → to hơn 0.35–0.8 + box BAO QUANH player (offset 0,1.5,0), scale (28,14,26), lifetime 1.8s, maxParticles 500.
- Scene KHÔNG serialize drift*/shipTrail* (verify awk block dừng ở trailColor) → chỉ sửa code default là áp dụng.

## 2026-08-12 (v3f.6) — VFX: vệt glow tàu + hệ sao trôi + dọn DIAG logs

- **Xóa 3 DIAG logs** (EnemyChase/ObstacleManager/PickupSpawner) — user đã xác nhận mọi thứ OK (R7.11). Xóa luôn `GetRenderBounds` dead trong ObstacleManager (chỉ DIAG dùng — R3.15).
- **Vệt glow tàu** (user: "qua làm VFX — tàu có hiệu ứng"): `VFXManager.SetupShipTrail` — TrailRenderer additive (cyan → trong suốt) bám theo player, vẽ đường bay + chuyển lane = cảm giác tốc độ. Clear khi restart.
- **Hệ sao trôi (SpaceDrift)**: ParticleSystem World quanh player — chấm nhỏ tròn (0.06–0.18) trôi ngược hướng chạy, box 26×12×30 (rộng hơn road ±9) — chiều sâu vũ trụ. Chấm NHỎ + TRÒN (không phải vệt dài — user từng chê "vệt sao ko giống sao").
- **NEW `CreateAdditiveSoftMaterial`** (URP Particles/Additive) + refactor texture chung `BuildSoftTexture` (không duplicate — code reuse).
- **Popup +20 đã tồn tại** — verify: VFXManager `OnCoinCollectedAt` → burst hạt vàng + popup `+{popupScore 10 × combo}` (số 20 = combo 2) ✓ không cần thêm.

## 2026-08-12 (v3f.5.3) — Bọ áp sát quá gần + to hơn + animation nhìn không rõ

- **Bọ áp sát quá gần khi chạm obstacle 1 lần** (DIAG: stage 1 → dist 3.0m) — user: "đang gần quá, chỉ cần chạm 1 lần trong thời gian quy định, cho ra xa 2-3m nữa". Fix: `closeDistance 3 → 5.5m` (code + scene + test `CloseDist`). Cơ chế 2 nấc GIỮ NGUYÊN: chạm 1 lần → bọ tiến sát 5.5m; chạm lần 2 trong cửa sổ → bắt.
- **Bọ to thêm lần 2** (user: "cho con bọ to lên"): `enemyTargetHeight 2.2 → 2.6` (code + scene).
- **Animation vỗ cánh nhìn không rõ** (user: "có animation gì mà nhìn ko rõ") — state `flying` chạy đúng (đã verify: KHÔNG có warning 'State not found'), nhưng clip vỗ cánh của model quá nhẹ ở khoảng cách xa → tăng `Animator.speed 1 → 1.25` (BuildEnemyVisual + ResetEnemy).
- **Bọ CHUYỂN MÀU** — đã xác minh: **KHÔNG có logic nào trong code đổi màu enemy**. 2 nguyên nhân có thể: (1) clip animation `flying` của model tự có keyframe màu/emissive (model free thường pulsing), (2) đèn đỏ của drone chiếu vào khi bọ bay ngang. Đang chờ user xác nhận hiện tượng thực tế để xử lý đúng (nếu là animation → cân nhắc thay material tĩnh; nếu là đèn → giảm range/intensity đèn drone).
- **Lưu ý:** log còn thấy `error CS` cũ của tool/test (trước commit `e76c2d8`) — tool là assembly Editor riêng nên không chặn gameplay; recompile lại sẽ sạch.

## 2026-08-12 (v3f.5) — Xóa hẳn cổng/rào; obstacle = drone duy nhất + hiệu ứng; bọ hết tím, xa + to hơn

- **BỌ MÀU TÍM** (user: "màu gốc đâu phải tím") — root cause: `EnemyChase.BuildEnemyVisual` **thiếu `MaterialFixer.EnsureURPMaterials`** (tàu đã gọi, obstacle đã gọi, ENEMY bị sót) → material Standard Built-in của gói Flying Beetle trong URP hiện TÍM/MAGENTA. Fix: thêm 1 dòng sau Instantiate. **Bài học (mở rộng R3.16): MỌI model 3rd-party đưa vào game — tàu, enemy, obstacle, monster — PHẢI đi qua MaterialFixer, không được sót 1 nhánh nào.**
- **Drone lệch +1.5m sang phải** (log DIAG: lane 4.5 → bounds center 6.0) — root cause: tool bù pivot +1.45 theo bounds EDIT-TIME, nhưng bounds RUNTIME lệch ~+1.5 (sai số 0.05 do đo khác thời điểm). Fix bền: **`Obstacle.Awake` tự căn giữa model con theo renderer bounds THẬT lúc spawn** (CenterModelOnLane — self-heal, không phụ thuộc prefab/tool). **Bài học: đừng tin số liệu bounds tính lúc EDIT-TIME để bù pivot — tự đo lại lúc RUNTIME là chắc chắn.**
- **XÓA HẲN CỔNG/RÀO** (user: "trông như cái cổng chứ đâu phải bãi mìn → xóa hẳn; chỉ drone + vài hiệu ứng là đủ cho trò chơi vũ trụ") — xóa `BarrierObstacle.prefab` (+meta), `BarrierWarning.mat`, tool bỏ hẳn nhánh Fence (constants/method ApplyWarningColor); xóa luôn 2 prefab mồ côi `Ramp.prefab`/`DynamicBox.prefab` (không ai reference); `Ramp.asset` trỏ lại → **DroneObstacle.prefab** (đúng fileID root 7065913401751965062 — cả 2 ObstacleData = drone, spawnWeight phân mật độ). Scene không cần sửa tay (R7.6).
- **NEW `ObstacleFX.cs`** — hiệu ứng ambient drone: đèn cảnh báo đỏ + hạt năng lượng cam + lơ lửng (bob) + xoay chậm quanh Y (Robot_Guardian không Animator → an toàn). Tạo **RUNTIME** trong `Obstacle.Awake` — KHÔNG nướng vào prefab (R3.1: material runtime không serialize → {fileID: 0} → màu tím — bug v3f.4).
- **Bọ xa hơn + to hơn** (user: "cho bọ xa 1 chút + to thêm"): `baseDistance 5→7` (vẫn TRƯỚC camera — camera cách player 10m), `enemyTargetHeight 1.8→2.2`; đồng bộ scene + test (`EnemyChasePlayTests.BaseDist 5→7`).
- **Lưu ý:** DIAG logs (Enemy/Obstacle/Coin) giữ 1 vòng để user verify drone đã đúng tâm lane — xóa sau xác nhận (R7.11).
- **`error CS1061: 'Bounds' does not contain a definition for 'IsValid'`** (Obstacle.cs:55) — **`Bounds` KHÔNG có phương thức `IsValid()` trong Unity** (tôi tưởng có khi viết guard). Fix: guard bằng `b.size.sqrMagnitude < 0.0001f` (GetRenderBounds trả zero-size khi không renderer). **Bài học: KHÔNG bao giờ dùng method/API không chắc chắn tồn tại — kiểm tra bằng grep trước khi dùng** (cùng lớp lỗi với `Camera.main.transform` / `rb.linearVelocity` khi chưa rõ API version).
- **11 lỗi CS sau fix (CS1073/CS1056/CS1039/CS0106/CS1513 + CS0246)** — 2 nguyên nhân của TÔI: (1) `SciFiObstacleSetupTool.cs:130` — `$\"...{x.ToString(\"F2\")}...\"` — **`\"` bên trong `{...}` của interpolated string là KHÔNG hợp lệ trong C#** (phải dùng `"` thường: `ToString("F2")`) — file ghi ra từ `write_file` bị escape lồng → parser vỡ cả khối, kéo theo CS cascade (private not valid / } expected ở các dòng sau). (2) `ObstacleCenterPlayTests.cs` thiếu `using VoidRunner.Core.World;` → CS0246 `Obstacle` không tìm thấy. **Bài học: (a) trong interpolated string C#, nested string literal bên trong `{}` dùng `"` THƯỜNG — KHÔNG dùng `\"`; (b) test mới PHẢI đủ `using` cho type dùng (grep các using của file test khác cùng asmdef); (c) sau khi tạo file, đọc lại byte thực tế (`od -c`) nếu nghi ngờ escape — braces cân bằng KHÔNG đủ để bắt lỗi string.**

## 2026-08-12 (v3f.4) — Enemy đồng bộ lane + bỏ Credit màn Game Over

- **Enemy trễ ~0.8s khi player đổi lane** (user: "con bọ phải di chuyển cùng lúc, ko thể trễ 0.5s") — root cause: `lateralFollow = 4 m/s` → băng 1 lane 4.5m mất 1.1s, trong khi player `laneChangeSpeed = 16 m/s` → 0.28s/lane. Fix: **`lateralFollow 4 → 20`** (code default + scene serialized `Game.unity` dòng 2980) — enemy đuổi lane đồng bộ player. **Bài học:** khi tăng tốc di chuyển player, phải rà soát MỌI thứ "bám theo" player (enemy, camera, target) — nếu không âm thầm tụt lại thành trễ vô hình.
- **Credit hiện trên màn hình Game Over** (user: "ở màn hình cuối game bỏ credit đi") — fix: xóa toàn bộ nhánh credit khỏi `UIManager` (field `creditsButton` + `EnsureCreditsButton` + `EnsureCreditsPanel` + `ToggleCredits` + listener OnDestroy) — panel + nút đó được tạo bằng code nên không cần dọn scene; **MainMenu GIỮ CREDITS**. **Bài học:** tính năng dùng chung (CreditsPanelBuilder) phải xác định rõ hiển thị ở đâu — Game Over chỉ nên có SCORE/BEST/RETRY/MENU.
- **RÀO CHẮN HIỆN MÀU TÍM/MAGENTA** (screenshot: "magenta/purple low-poly structures") — root cause: `ApplyWarningColor` tạo `new Material(src)` là object RUNTIME, `SaveAsPrefabAsset` không serialize được → prefab ghi `m_Materials.Array.data[0] → objectReference {fileID: 0}` = **material NULL** → renderer hiện magenta (màu báo lỗi của Unity). Đúng rule **R3.1** (ScriptableObject/material tạo bằng code phải `AssetDatabase.CreateAsset`/`AddObjectToAsset`). Fix: tạo **1 material asset dùng chung** `Materials/Obstacles/BarrierWarning.mat` (idempotent — load có thì dùng, ép màu lại mỗi lần) + Rebuild xóa luôn material cũ; `EnsureFolder` nâng cấp generic (tạo từng cấp folder). **Bài học:** mọi object edit-time được REFERENCE bởi asset lưu (prefab/material/volume) phải là ASSET — dấu hiệu nhận biết: mở file .prefab thấy `objectReference: {fileID: 0}` ở material = chắc chắn màu tím/magenta.

## 2026-08-12 (v3d) — Fix tool Asteroid: MissingReferenceException (gameObject destroyed)

> User: "đọc log, hiện tại không chạy tool được" — tool `Setup Obstacle = Asteroid (OlegWER thiên thạch)` crash ngay sau khi tạo prefab.

### Nguyên nhân (chính xác)

- `BuildAsteroidPrefab()`: `PrefabUtility.SaveAsPrefabAsset(root, ...)` **thành công** (log có dòng `Start importing .../AsteroidObstacle.prefab`) → `Object.DestroyImmediate(root)` — **destroy cha = destroy luôn con `Model`** (asteroid) → dòng `Debug.Log` cuối vẫn truy cập `model.transform.localScale` trên object ĐÃ BỊ HỦY → `MissingReferenceException: GameObject has been destroyed` (stack: line 143 `get_transform` ← line 63 `Setup`).
- Hệ quả: prefab ĐÃ tạo xong (5.2KB) nhưng bước 3 (gán prefab vào 2 ObstacleData) **chưa chạy** → tool chưa hoàn thành.

### Cách fix

- **Capture scale TRƯỚC khi destroy**: `float modelScale = model.transform.localScale.x;` → `Debug.Log` dùng `modelScale` (không còn chạm `model.transform`) + dời `Debug.Log` lên TRƯỚC `DestroyImmediate(root)`.
- Idempotent giữ nguyên: lần chạy tới thấy prefab đã tồn tại → BỎ QUA bước build → chỉ gán vào 2 ObstacleData → **chạy lại an toàn, không cần xóa gì**.

### Bài học — QUY TẮC MỚI (R7.10)

- **`Object.DestroyImmediate(parent)` hủy luôn cả CÂY CON** — mọi tham chiếu tới child (Transform/Renderer...) sau đó = MissingReferenceException. Nếu cần giá trị từ child: **capture trước**, log sau khi destroy phải dùng biến đã lưu. Dấu hiệu: exception ở dòng `get_transform`/`get_renderer` NGAY SAU `DestroyImmediate`.

---

## 2026-08-12 (v3f.2) — Fix rào chắn: quá dài/văng khỏi đường + cùng màu lề + vật thể không đè nhau

> User test: "rào chắn hơi dài, cùng màu với lề đường cực khó nhìn, văng ra khỏi trục đường chính, các vật thể ko được phép đè lên nhau; drone lúc đầu bị che bởi màu trắng nhưng sau vài lần retry hiển thị bình thường".

### Root cause & fix

- **Rào DÀI + VĂNG KHỎI ĐƯỜNG** — tool scale đều cả 3 trục theo chiều cao (1.6) → Fence_Long_01 (bản dài) giữ tỉ lệ → bề ngang vượt laneWidth 4.5 → tràn lane khác + ra ngoài road ±9. **Fix**: `BarrierTargetWidth = 4.2` — sau scale chiều cao, đo bounds thật rồi ép bề ngang (hướng chắn lane) ≤ 4.2 (pattern NormalizeScale R4.18) → rào GỌN TRONG 1 lane, không đè vật thể bên cạnh, không văng khỏi đường.
- **CÙNG MÀU LỀ ĐƯỜNG** — lane marker cyan (0.2,0.8,1) + nền tối → rào xám trạm lẫn vào. **Fix**: `ApplyWarningColor` — ép material INSTANCE (new Material(src) — không đụng asset gói) sang **cam neon** `_BaseColor (1,0.45,0.05)` + `_EmissionColor (1,0.28,0)` + keyword `_EMISSION` → rào nổi bật, đọc rõ từ xa. Chỉ áp cho rào (drone giữ nguyên bản — đã hiển thị đúng).
- **Menu mới `Rebuild SciFi Obstacles`** — xóa 2 prefab cũ + dựng lại (ép kích thước/màu). Tool vẫn idempotent: `Setup` không đổi prefab đã tồn tại; `Rebuild` ép buộc dựng lại.
- **Drone trắng lúc đầu** — material Standard (Built-in) → `Obstacle.Awake` self-heal MaterialFixer (R3.16) fix ngay frame đầu; "trắng vài frame rồi đúng" = MaterialFixer chạy sau 1 frame — KHÔNG phải bug (đã xác nhận tự hết sau retry).

### Bài học — QUY TẮC (bổ sung R7.9/R4.18)

- **Scale model 3rd-party: ép CẢ chiều cao LẪN bề ngang** — chỉ ép chiều cao theo tỉ lệ đều = model dài sẵn tràn lane/road. Luôn đo bounds thật sau scale rồi normalize từng trục về ràng buộc (chiều cao mục tiêu + bề ngang ≤ laneWidth).
- **Obstacle phải ĐỌC ĐƯỢC trên nền road** — màu vật cản phải TƯƠNG PHẢN với lane marker/road (rào xám lẫn lane cyan → ép cam neon emission). Kiểm tra màu nền (lane marker) trước khi chốt material obstacle.
- **Menu tool idempotent cần thêm phiên bản "Rebuild"** — khi sửa thông số (kích thước/màu) mà prefab đã tồn tại: `Setup` sẽ bỏ qua (idempotent) → cần menu riêng xóa prefab + dựng lại.

---

## 2026-08-12 (v3f) — Obstacle = SciFi (Fence + Drone) thay Asteroid + cache chuyển sang ổ D

> User: "trong 1 game vũ trụ thì tự nhiên có cục thiên thạch giữa đường có kì quá ko, bạn đề xuất vài giải pháp tối ưu; tôi ko muốn vẽ bằng code, muốn dùng chính các assets có sẵn (giống dùng thư viện icon thay vì tự vẽ)". → Chốt: **Fence_Long_01 (3D Scifi Kit Starter Kit — Creepy_Cat) + Robot_Guardian (Sci fi Drones — Lukas Bobor)**.

### Việc đã làm

- **NEW `Editor/SciFiObstacleSetupTool.cs`** (idempotent): tạo 2 prefab wrapper (root SphereCollider trigger + `Obstacle` + con model scale chuẩn theo bounds thật — pattern giống AsteroidObstacleSetupTool, R7.9):
  - `BarrierObstacle.prefab` = Fence_Long_01 (rào chắn trạm), targetHeight 1.6, **tự xoay 90° quanh Y nếu trục dài theo Z → chắn NGANG lane** (không chắn dọc đường)
  - `DroneObstacle.prefab` = Robot_Guardian, targetHeight 1.2 (drone bay giữa lane — né dễ)
  - Gán **Ramp.asset → Barrier, DynamicBox.asset → Drone** (giữ obstacleType/spawnWeight; 2 ObstacleData trước đó đều trỏ Asteroid — giờ tách 2 loại)
- Material tím: không cần MaterialFixer trong tool — `Obstacle.Awake()` tự ép URP/Lit lúc spawn (R3.16, fix v3e).
- `.gitignore`: thêm 3 gói mới (Creepy_Cat ~1.15GB, Sci_fi_Drones 0.22GB, G-spot_Lab 0.34GB) + `Assets/_Recovery/` (Unity tự tạo khi crash — KHÔNG commit).
- **Cache Unity Asset Store chuyển ổ C → D** (ổ C chỉ còn 6.4GB): junction `C:\Users\Admin\AppData\Roaming\Unity\Asset Store-5.x → D:\UnityCache\AssetStore` (robocopy /MOVE + mklink /J). Verify: `<JUNCTION>` + đủ 13 gói.

### Bài học — QUY TẮC MỚI (R6.14)

- **Cache Unity Asset Store luôn ở `%APPDATA%\Unity\Asset Store-5.x` (ổ C — hardcode, không có setting đổi)** — gói tải về (bản .unitypackage) chiếm nhiều GB trên ổ C dù project nằm ổ khác. Giải pháp chuẩn: **Junction (symlink)**: `robocopy "<nguồn>" "D:\..." /E /MOVE` (an toàn — có lỗi không mất dữ liệu) → `rmdir` thư mục cũ → `mklink /J "<đường dẫn cũ>" "D:\..."` → verify bằng `dir` (thấy `<JUNCTION>`). Khi Unity đang MỞ: junction vẫn OK nhưng KHÔNG move khi đang tải/import.
- **Unity tạo `Assets/_Recovery/` khi editor crash — folder này KHÔNG phải asset thật, PHẢI ignore** (không commit lên repo).

---

## 2026-08-12 (v3e) — Dọn log diag tạm + fix material tím OBSTACLE (lỗi R3.16 tái phạm) + Select Ship to

> User: "cho select ship to thêm, đừng quá tiết kiệm UI; đọc toàn bộ log, ngoài các log in ra thì có hàng loạt log đỏ, xóa bớt mấy log cũ; vật thể màu tím chứ ko phải màu nguyên bản — tưởng tuân theo rule rồi mà sao lại lặp lại lỗi này".

### 1. Hàng loạt log đỏ — `MissingComponentException: There is no 'Renderer' attached to "AsteroidObstacle(Clone)"`

- **Nguyên nhân (chính xác)**: log diag TẠM còn sót từ đợt chẩn đoán "không có obstacle/coin" (2026-08-11) — `ObstacleManager.cs:91` và `PickupSpawner.cs:89` gọi `GetComponent<Renderer>()` trên **ROOT** obstacle/coin. AsteroidObstacle root KHÔNG có Renderer (visual nằm ở con "Model") → Unity 6 throw MissingComponentException. Prefab cũ (cube/Ramp) có Renderer ngay root nên không lỗi — đổi sang Asteroid mới lộ ra.
- **Fix**: xóa toàn bộ 9 log diag tạm (`[DiagObstacle]/[DiagCoin]/[DiagTS]/[DiagSpawn]` + log model `[Ship]/[Enemy]` + `Game Over` + `[Nebula]`) + biến `_lastDiagLog`/`_instanceCount` ở 7 file (ObstacleManager, PickupSpawner, TileSpawner, EnemyChase, PlayerController, GameManager, NebulaChanger) → Console sạch, không spam mỗi 2s.

### 2. Vật thể màu TÍM — lỗi R3.16 TÁI PHẠM (user bắt bài đúng)

- **Nguyên nhân (chính xác)**: `MaterialFixer.EnsureURPMaterials` trước đây CHỈ áp dụng cho tàu (PlayerController + ShipSelectManager) — **SÓT OBSTACLE**. Material OlegWER `Material.mat` dùng shader `m_Shader: {fileID: 46, guid: 000...}` = **Standard (Built-in)** → trong URP render TÍM/MAGENTA. AsteroidObstacle prefab dựng từ model này → obstacle tím.
- **Fix**: `Obstacle.Awake()` → `MaterialFixer.EnsureURPMaterials(gameObject)` (self-heal R4.18 — mọi obstacle spawn ra tự ép URP/Lit giữ màu gốc, cache static không leak). Không cần sửa prefab asset (nested instance) — lúc spawn đã đúng.

### 3. Select Ship to hơn (user: "đừng quá tiết kiệm UI")

- Panel 520×560 → **680×720**; Title 36→44pt; Preview 360×300 → **480×400**; ShipName 30→36pt; mũi tên 110×52 → **130×62** (font 46); nút SELECT 300×56 → **360×64** (font 34); **RenderTexture 256² → 512²** (khung to mà 256² bị mờ); camera ortho 1.6→2.0 + model scale 1.2→1.6 (model to hơn trong khung).

### Bài học — QUY TẮC MỚI (bổ sung R3.16)

- **MaterialFixer phải áp dụng cho MỌI model 3rd-party nhập mới — không chỉ tàu**: khi tích hợp gói model mới (obstacle/coin/monster) PHẢI check material (`grep 'm_Shader:' *.mat` — fileID 46 = Standard Built-in) + đảm bảo component spawn nó gọi `MaterialFixer.EnsureURPMaterials`. Cách chống tái phạm: đặt self-heal ngay trong `Awake()` của component gốc (Obstacle/Coin/PowerUp...) thay vì nhớ gọi ở từng nơi instantiate. *(Bug 2026-08-12 — user: "tưởng tuân rule rồi mà lặp lại".)*
- **Debug.Log diag tạm PHẢI xóa sau khi fix xong** — log "[DiagX]" chừa lại = spam Console (mỗi 2s) + có thể GÂY LỖI (truy cập component không tồn tại trên root khi prefab cấu trúc đổi). Quy tắc: thêm log diag → fix xong → xóa ngay trong cùng đợt.
- **`GetComponent<Renderer>()` trên root KHÔNG an toàn nếu prefab đổi cấu trúc** — visual có thể nằm ở con (Model). Muốn check renderer: `GetComponentInChildren<Renderer>()` (hoặc đừng log).

---

## 2026-08-12 (v3c) — Tích hợp obstacle = Asteroid (OlegWER thiên thạch)

> User: "cứ thực thi từng việc đi, làm từ việc obstacle trước" (sau khi tải OlegWER Asteroid thay obstacle kenney).

### Việc đã làm

- **NEW `Editor/AsteroidObstacleSetupTool.cs`** (idempotent — chạy lại không đổi):
  1. Load model `Assets/OlegWER/High-Poly_Asteroid/Prefabs/fbx.prefab` (folder GIỮ LOCAL qua gitignore — 180MB; nếu thiếu, tool báo hướng dẫn tải lại)
  2. Tạo prefab `Assets/_Project/Prefabs/Obstacles/AsteroidObstacle.prefab` (nếu chưa có): root = SphereCollider (isTrigger — Obstacle.Awake tự bật) + component `Obstacle`; con = model asteroid, scale chuẩn theo chiều cao thật (targetHeight 1.5), collider radius theo bounds thực
  3. Gán prefab mới vào CẢ 2 ObstacleData (`Ramp.asset` + `DynamicBox.asset`) qua SerializedObject — giữ nguyên obstacleType/spawnWeight (gameplay không đổi, chỉ đổi visual)
- **Không sửa scene file tay** (R7.6) — user chạy tool 1 nút trong Unity.

### Bài học (R7.8)

- **ObstacleData assets nằm ở `Assets/_Project/ScriptableObjects/`** (Ramp.asset + DynamicBox.asset) chứ KHÔNG phải `Data/` như namespace gợi ý — namespace `VoidRunner.Data` là tên C#, không phải thư mục. Trước khi sửa data: dùng `git ls-files | grep obstacle` + đọc GUID trong scene để định vị file thật.
- Model 3rd-party scale kỳ lạ (FBX scale 100) → **luôn đo bounds thật rồi chuẩn hóa** (pattern giống ShipCatalog/EnemyCatalog — R4.18).

---

## 2026-08-12 (v3b) — Dọn kenney không dùng (~58MB) + import 3 gói VFX/Obstacle mới

> User: "tôi tải 3 cái là vfx tàu, magic vfx mới (thay thế cái kenny hiện tại), obstacle (thay thế kenny hiện tại luôn), những kenny assets nào ko dùng thì xóa bớt cho đỡ nặng máy".

### Xóa 4 kenney folder không dùng (~58MB)

- **Đã verify KHÔNG được scene/prefab/material/code tham chiếu** (chỉ còn 4 GUID từ ui-pack được dùng):
  - `kenney_game-icons` (6.6M) · `kenney_particle-pack` (16M — thay bằng Eric VFX Studio)
  - `kenney_space-kit` (28M — thay bằng OlegWER Asteroid) · `kenney_space-station-kit` (7.5M)
- **GIỮ**: `kenney_ui-pack` + `kenney_ui-pack-space-expansion` (5 PNG đang dùng trong UI: panel_glass, star, button_rectangle_flat/gloss).
- `SpriteBatchConverter` Roots chỉ còn 2 folder UI (bỏ 4 path đã xóa).
- Commit `a6d550e`.

### 3 gói mới đã import (CHƯA tích hợp vào game — chờ user duyệt từng bước)

| Gói | Nội dung | Dự định dùng | Dung lượng |
|---|---|---|---|
| **OlegWER** | High-Poly_Asteroid (FBX + Material + Prefab `fbx.prefab`) | Thay obstacle code-drawn bằng thiên thạch | 180M |
| **Eric VFX Studio** | Free Game VFX (FX_Fireball, FX_Green_Hit, FX_LootDrop, FX_Orange_Slash...) | Thay particle code bằng prefab VFX có sẵn | 15M |
| **JMO Assets** | Cartoon FX Remaster (CFXR Prefabs: Explosions, Fire, Impacts, Light...) | VFX tàu (engine/trail/va chạm) | 40M |

> ⚠️ Lưu ý repo: 3 gói = ~235MB — hỏi user trước khi commit lên GitHub (repo nặng).

---

## 2026-08-12 (v3) — Fix MainMenu (2 chữ title · nút sát) + Enemy hiện đủ + cơ chế BẮT + tàu sáng/to

> User: "void runner vẫn còn 2 chữ; khoảng cách play/how to play/sound giảm còn 5-10px; tàu bị mờ — muốn nó nổi bật nhất; con bọ xuất hiện ngay từ đầu nhưng chỉ thấy phần đầu; con bọ có cảnh bắt — chạm 1 lần vỗ nhanh hơn, chạm 2 lần bắt lại rồi mới end game; đừng rung con bọ mà cho nó vỗ cánh; tàu to thêm ~10px"

### 1. "2 chữ VOID RUNNER" vẫn đè nhau (fix lần 2 — lần 1 không ăn)

- **Nguyên nhân THẬT**: lần trước chỉ sửa `m_IsActive: 0` TRONG FILE scene — nhưng **Unity đang mở scene đó → khi user Ctrl+S, Unity GHI ĐÈ file từ bản trong memory** (vẫn active=1) → chữ glow quay lại.
- **Bài học (R7.4 mới)**: KHÔNG sửa file `.unity`/`.prefab` khi Unity đang mở scene đó — mọi thay đổi scene phải qua **Editor tool** (chạy trong Unity, modify trực tiếp object) rồi user Ctrl+S.
- **Fix**: `UIOverhaulTool.FixMainMenuSpacing` → `DestroyImmediate(TitleGlow)` — xóa HẲN (title trắng đã có Shadow + Outline riêng). Tool idempotent, chạy lại không lỗi.

### 2. Nút Play/HowToPlay/Sound quá xa (gap 52–54px → 8px)

- Tool cũ ép 120/-20/-150 → gap theo mép 52px/54px (user: "quá chật" → "giãn quá" → giờ "giảm còn 5-10px").
- **Fix**: gap mép 8px — Play 120 · HowTo 24 · Sound -60 · Best -160 (giữ tương quan Best/Ship như cũ). Đồng bộ code tạo nút: `ShipSelectManager` -245, `MainMenuManager` (Credits) -245.

### 3. Con bọ chỉ thấy "phần đầu" khi bắt đầu

- **Nguyên nhân**: camera cách player 10m (offset z=-10) nhưng `baseDistance = 9m` → bọ chỉ cách camera **1m** → nằm ngoài khung hình (chỉ phần nhô lên lọt vào).
- **Fix**: `baseDistance 9 → 16m`, `closeDistance 7.5 → 12m` (bọ cách camera 6m/2m — thấy rõ cả con, áp sát vẫn không che tàu). Đồng bộ scene Game + test.

### 4. Con bọ KHÔNG vỗ cánh — đứng im (idle)

- **Nguyên nhân**: default state của Animator controller = **"idle 1"** (bọ đứng im), KHÔNG phải "flying". Instantiate xong chỉ có Animator thôi nhưng state mặc định là idle.
- **Fix**: `BuildEnemyVisual` → `_animator.Play("flying")` — ép vỗ cánh loop ngay khi dựng. KHÔNG ép rotation mỗi frame (giữ R4.17).

### 5. Cơ chế BẮT (hit lần 2) — theo đúng animation clip có sẵn

- Animator Flying Beetle có sẵn **10 clip**: `atack 1/2/3`, `death`, `flying`, `gethit`, `idle 1/2`, `roar` (folder `animation/`, FBX `@atack`...).
- **Fix** (`EnemyChase`): hit lần 1 → `_animator.speed = 2` (vỗ nhanh hơn); hit lần 2 trong cửa sổ → **`CatchAndKill()`**: lao tới player 0.3s (không teleport) + `Play("atack 1")` (cảnh bắt) → chờ `catchDelay 1.1s` → mới `RaiseGameOver()` → UIManager fade panel 0.4s (mượt, không cắt cảnh). `_catching` guard: không nhận hit mới / không trigger lần nữa; `ResetEnemy` stop coroutine + quay về flying.

### 6. Tàu bị mờ → nổi bật nhất + to thêm ~10px

- **Không có UI đè** (Score HUD top giữa, ComboText góc trái — đã verify vị trí anchor). Tàu mờ vì **THIẾU ÁNH SÁNG** trên track tối.
- **Fix**: (a) `shipTargetHeight 1.1 → 1.2` (+~10px); (b) **`EnsureShipLight`** — Point Light cyan bám tàu (con của Ship, intensity 2.2, range 7, shadows off) → tàu sáng + halo nổi bật nhất trên nền đen.

### Đề xuất assets (chưa tải — user tự quyết):

- VFX engine: **Kenney Particle Pack** (đã có) · **Cartoon FX Remaster Free** (Asset Store) · **Stylized VFX** (itch.io) · **Magic VFX** (Asset Store free)
- Trail tàu/obstacle: **Echo Trail** / dùng Trail Renderer code (không cần asset)
- Background space: **Nebula/Spaceskies** (user đã tải) — dùng làm skybox chính

---

## 2026-08-11 — Dọn file chết (sau nhiều đợt refactor) + fix chọn tàu không hiện

> User: "chưa test các task trên, có cần chạy tool nào trước khi test ko; test chọn ship thì không hiện tàu mới, vẫn hiện tàu cũ; sau vài đợt refactor nên dọn sạch các file không dùng".

### Nguyên nhân "chọn tàu không hiện" (chính xác)

- `ShipSelectManager` chưa có trong scene MainMenu (GUID count=0) và `PlayerController.shipPrefabs` chưa gán trong scene Game → **tool `Setup Ship Select` chưa chạy** → preview rỗng + vẫn tàu primitive cũ.

### Cách fix (self-heal — R4.18)

- **NEW `Systems/Save/ShipCatalog.cs`**: 1 nguồn path duy nhất (2 tàu) + `Load(index)` dùng `AssetDatabase.LoadAssetAtPath` (guarded `#if UNITY_EDITOR`, build trả null → rơi về tàu primitive).
- `ShipSelectManager.RefreshPreview` + `PlayerController.BuildSpaceship`: nếu `shipPrefabs` rỗng → fallback `ShipCatalog.Load(idx)` — **không còn phụ thuộc user nhớ chạy tool khi test**.
- `ShipSelectSetupTool` dùng `ShipCatalog.ShipPaths` (bỏ trùng lặp path — reviewer góp ý).
- Commit `de329c7`.

### Dọn file chết (user duyệt 2026-08-11)

- **Xóa 2 script chết**: `EnemyMovement.cs` (script Roll a Ball cũ dùng NavMeshAgent — vi phạm R4.1, chỉ còn `_Archive/Minigame` tham chiếu) + `AmbientScroller.cs` (đã xóa Ambient props → 0 tham chiếu code + scene).
- **Xóa cả `Scenes/_Archive/`** (Minigame + NavMesh-Ground) — không trong Build Settings, Void không dùng NavMesh nữa.
- GIỮ 11 Editor tool 1-lần (user chọn không xóa): AmbientSetupTool, CameraFollowFixTool, GameplayFixTool, HUDUIBuilder, HUDUpgradeTool, MainMenuUIBuilder, ScenePolishTool, SpriteBatchConverter, UIOverhaulTool, VFXSetupTool, RefactorGameplayTool.
- Commit `9b27501`.

### Bài học — **QUY TẮC MỚI (R3.13/R6.7)**

- **Dọn file chết phải kiểm tra 2 chiều**: (a) GUID có trong scene/prefab nào không (grep trong `Scenes` + `Prefabs`), (b) class có được code khác reference không (grep trong `Scripts`). Script có GUID=0 và refs=0 mới chắc chắn chết. Trước khi hỏi user duyệt xóa: liệt kê rõ ràng file + lý do.
- Scene `_Archive` giờ ĐÃ XÓA HẲN — không còn "nơi test NavMesh" (Void bỏ NavMeshAgent ở Task B).

---

## 2026-08-12 — Gộp docs 9 → 5 file + fix CS0579 duplicate Tooltip

> User: "thực thi gộp các file docs theo ý bạn; đang có 1 bug đỏ duplicate tooltip ở PlayerController — fix rồi tôi chạy tool".

### Bug đỏ — CS0579 Duplicate 'Tooltip' attribute (PlayerController.cs:45)

- **Nguyên nhân**: field `shipTargetHeight` vô tình có **2 attribute `[Tooltip]`** (dòng 44 cũ `[Tooltip("Chiều cao chuẩn hóa...")]` + dòng 45 mới `[SerializeField, Tooltip("Chiều cao tàu...")]` do fix tàu to trước đó). `TooltipAttribute` KHÔNG có `AllowMultiple` → C# báo **CS0579** → compile fail.
- **Fix**: gộp thành 1 attribute duy nhất (`[SerializeField, Tooltip(...)]`).
- **Bài học**: khi SỬA field có sẵn attribute — KHÔNG thêm attribute mới mà chưa xóa attribute cũ trên CÙNG field (đặc biệt `Tooltip`/`Header` — không AllowMultiple). Verify: grep 2 lần tên field trong file.

### Gộp docs: 9 → 5 file (user duyệt đề xuất 2026-08-12)

| File mới | Gộp từ | Nội dung |
|---|---|---|
| **`agent/DECISIONS.md`** (mới) | RULES NHÓM 0 + NHÓM 0b | Quyết định thiết kế bất biến (player=tàu, enemy 2 nấc, English UI...) |
| **`agent/RULES.md`** (thu gọn) | giữ NHÓM 1–7 | Quy tắc kỹ thuật (API, asmdef, editor tool, workflow) |
| **`agent/CHANGELOG.md`** (giữ) | + BUGS.md (PHỤ LỤC) | Nhật ký lỗi + bài học (1 file duy nhất) |
| **`agent/PLAN.md`** | void-runner-plan.md (git mv) | Kế hoạch task + trạng thái |
| **`agent/REFERENCE.md`** (mới) | FEATURES + TESTING + CREDITS + COMMIT_TEMPLATES + AGENT_B_GUIDE | Tra cứu nhanh (5 PART rõ ràng) |

- Xóa 6 file cũ: BUGS.md, FEATURES.md, TESTING.md, CREDITS.md, COMMIT_TEMPLATES.md, AGENT_B_GUIDE.md (đã gộp).
- Cập nhật mọi cross-link: README, DECISIONS, PLAN, RULES → file mới (không còn link chết).
- R7.3 cập nhật: bug → CHANGELOG, quyết định thiết kế → DECISIONS, kế hoạch → PLAN.

---

## 2026-08-12 — Refactor Void → Enemy + enemy duy nhất Flying Beetle + tàu to + TitleGlow 2 chữ đè nhau

> User: "sao có 2 chữ VOID RUNNER đè nhau; void chỉ nên dùng 1 kẻ thù duy nhất là model flying carnivorous (note cách model chuyển động); tàu to thêm 1 xíu; enemy đuổi gần nhưng KHÔNG che tàu; đổi void thành enemy cho tốt hơn; xóa code thì dọn sạch".

### Root cause từng lỗi + fix (commit `…`)

- **2 chữ "VOID RUNNER" đè nhau (MainMenu)** — `TitleGlow` (text cyan mờ do tool tạo làm hiệu ứng glow) đang render ĐÈ LÊN `TitleText` trắng — cả 2 cùng y=330. Fix: **`m_IsActive: 0`** trên GameObject TitleGlow (ẩn an toàn R0.16 — title trắng đã có Shadow riêng, glow phụ thừa).
- **Enemy = Flying Beetle DUY NHẤT (bỏ random 3 monster)** — user chốt "chỉ 1 kẻ thù là flying carnivorous" = **Flying Beetle** (prefab CÓ Animator controller flying loop → instantiate là cánh vỗ bay liên tục). **Note cách model chuyển động**: enemy KHÔNG cần code điều khiển animation — Animator của prefab tự chạy (flying loop); EnemyChase chỉ điều khiển VỊ TRÍ (bám sau player, đổi lane) + scale phình khi áp sát. ⚠️ KHÔNG ép `localRotation` mỗi frame — đánh nhau với root motion (R4.17).
- **Enemy đuổi gần nhưng KHÔNG che tàu** — cũ: closeDistance **5m** + closeScale 1.6 + monster 2.6 cao → khi áp sát enemy phủ kín tàu. Fix: closeDistance **7.5m** + closeScale **1.2** + enemyTargetHeight **1.8** (vẫn đe dọa, tàu thấy rõ).
- **Tàu to thêm 1 xíu** — `shipTargetHeight` 0.9 → **1.1** (code + scene Game).
- **Đổi Void → Enemy TRIỆT ĐỂ** (user duyệt): `VoidChase.cs → EnemyChase.cs` (giữ .meta = GUID không đổi → scene không vỡ), `VoidMonsterSetupTool.cs → EnemyMonsterSetupTool.cs`, `VoidChasePlayTests.cs → EnemyChasePlayTests.cs`, field `GameManager.voidChase → enemy`, `VFXManager._voidTrail/_voidTransform/SetupVoidTrail → _enemyTrail/_enemyTransform/SetupEnemyTrail`, GameObject scene "Void" → "Enemy", field scene `monsterPrefabs[] (3) → enemyPrefab (1)` + `monsterTargetHeight/monsterYaw → enemyTargetHeight/enemyYaw`. Scene Component `m_EditorClassIdentifier` cập nhật. Xóa hẳn `BuildBlackHoleVisual` (không cần — enemy có model thật).
- **Self-heal (R4.18, reviewer)**: `EnemyCatalog.Load()` (editor) — nếu pull code mới chưa chạy tool Setup Enemy → EnemyChase tự tải Flying Beetle. Giống ShipCatalog cho tàu.
- Reviewer bắt: `monsterYaw: 0` còn sót trong scene sau replace (data chết) → `enemyYaw: 0`.

### Bài học — **QUY TẮC MỚI (R0.2/R0.4 đổi tên, R3.17)**

- **Rename class Unity: `git mv` giữ `.meta` = GUID không đổi → scene/prefab vẫn tham chiếu đúng script** (chỉ đổi tên file + class + m_EditorClassIdentifier trong scene). Không tự tay gõ GUID mới (R6.1). Đồng bộ: field scene, m_EditorClassIdentifier, mọi reference trong code, tên GameObject, test, docs.
- **Khi thay array (3 prefab) → 1 prefab cố định**: quét data chết trong scene (field cũ như `monsterYaw` không còn trong class) — python replace khớp chuỗi có thể chừa lại field thừa → verify bằng grep tên field cũ.
- **Model enemy có Animator → KHÔNG ghi đè rotation mỗi frame** (root motion); điều khiển vị trí, để Animator lo động tác. Flying Beetle: instantiate prefab là bay sẵn.

---

## 2026-08-12 — Fix UI MainMenu vòng 2 (2 ảnh user test): giãn quá + Title 2 dòng + chuột ship + tàu trắng + popup lộ menu

> User: "bạn giãn quá mức cần thiết rồi, chỉ tăng thêm 1 tí so với ban nãy thôi, và đừng xuống dòng chữ VOID RUNNER nữa, cho nó nằm ngang đi; ship bấm phím mũi tên đổi được nhưng bấm CHUỘT thì không đổi; tại sao tàu thành màu TRẮNG; popup how to play có nền đen thì đồng bộ toàn bộ popup còn lại".

### Root cause từng lỗi + fix (commit `…`)

- **Title "VOID RUNNER" xuống 2 dòng + bị PLAY đè (R..R)** — fontSize **110pt** wrap trong rect 900px → "VOID / RUNNER" 2 dòng, dòng 2 chạm PlayButton. Fix `FixMainMenuSpacing`: fontSize **96** + rect **1100×160** + `textWrappingMode = NoWrap` + đẩy lên **y=330** (cả TitleGlow theo). 1 dòng ngang, không còn đè.
- **Giãn quá mức** (bản trước Play 160/HowTo 0/Sound -160) — user chỉ muốn tăng 1 tí so với gốc (60/-60/-160). Fix: **Play 120 / HowTo -20 / Sound -150 / Best -250** → gap mép 52/54/37px (gốc 32/24/17). SHIP/CREDITS -320 → **-335** (đồng bộ scene + code runtime `ShipSelectManager` + `MainMenuManager.EnsureCredits`).
- **Bấm CHUỘT mũi tên không đổi tàu (phím vẫn đổi)** — **DOUBLE-SUBSCRIBE**: `CreateArrowButton` `btn.onClick.AddListener(onClick)` VÀ `CachePanelRefs` `prevBtn.onClick.AddListener(SelectPrev)` → 1 click gọi `SelectPrev` 2 lần → 2 tàu đảo qua rồi về cũ = nhìn như không đổi. Phím hoạt động vì 1 lần/frame. Fix: `CreateArrowButton` **bỏ AddListener** (bỏ cả tham số onClick), `CachePanelRefs` **`RemoveAllListeners()` + AddListener** (idempotent — chạy lại an toàn khi panel đã tồn tại).
- **Tàu thành màu TRẮNG** (sau fix tím vòng trước) — `MaterialFixer` chỉ copy `_Color`/`_EmissionColor`, **KHÔNG copy texture** → URP/Lit không có `_BaseMap` → render trắng. Fix: copy `_MainTex` → **`_BaseMap`** (+ `_MainTex_ST` scale/offset) + `_BumpMap` → `_BumpMap` (enable `_NORMALMAP`) + `_MetallicGlossMap` (enable `_METALLICSPECGLOSSMAP`) + `_OcclusionMap` (enable `_OCCLUSIONMAP`) + `_Glossiness` → `_Smoothness` + `_Metallic` + `_EmissionMap`. Giữ nguyên vẻ gốc của model.
- **Popup ship lộ menu sau lưng (không nhất quán với HowToPlay)** — `ShipSelectManager.TogglePanel` dùng `SetAsFirstSibling` (dimmer chìm DƯỚI menu) — khác `MainMenuManager` đã fix `SetAsLastSibling`. Fix: dimmer `SetAsLastSibling` TRƯỚC panel (đồng bộ 100% với HowToPlay/Credits).
- Reviewer góp ý: `FixMainMenuSpacing` dùng `GetRootGameObjects()[0]` (canvas có thể không phải root 0) → lặp qua MỌI root như `OverhaulMainMenu`; `_OCCLUSIONMAP` keyword cho `_OcclusionMap`.

### Bài học — **QUY TẮC MỚI (R5.20/R5.21)**

- **Button tạo bằng code: subscribe onClick ĐÚNG 1 NƠI** — tạo button ở 1 hàm, subscribe ở 1 hàm khác (refs-cache) → nếu 2 hàm cùng AddListener = **double-subscribe**: 1 click = handler chạy 2 lần = với 2 lựa chọn đảo qua rồi về cũ, nhìn như "không hoạt động" (phím vẫn hoạt động → triệu chứng lạ). Fix chuẩn: `RemoveAllListeners()` trước `AddListener()` (idempotent).
- **Convert shader Standard → URP/Lit phải copy CẢ texture, không chỉ màu** — `_MainTex → _BaseMap` (+ `_MainTex_ST` → `_BaseMap_ST`), `_BumpMap`, `_MetallicGlossMap`, `_OcclusionMap`, `_EmissionMap`, `_Glossiness → _Smoothness`, `_Metallic`; kèm enable keyword tương ứng (`_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`, `_EMISSION`) — không enable keyword = texture copy vô tác dụng (shader_feature_local). Triệu chứng: tàu tím (shader không compile) → sau khi convert → TRẮNG = chỉ copy màu, quên texture.
- **Title dài phải kiểm tra wrap** — fontSize to + rect hẹp = tự xuống dòng; dòng 2 đè lên element dưới. Fix: NoWrap + rect đủ rộng (1100px cho 11 ký tự 96pt) + đẩy lên. Kiểm tra bằng ảnh user chụp (chữ bị đè nửa là dấu hiệu wrap).

---

## 2026-08-12 — Fix UI MainMenu theo 4 ảnh user test (bước 1)

> User gửi 4 ảnh: (1) nút menu sát nhau, (2) CLOSE to che chữ HowToPlay + font khó đọc, (3) Select ship CLOSE to + không đổi tàu bằng phím mũi tên + tàu màu TÍM, (4) Credits chữ nhỏ + CLOSE to + mọi popup lộ menu phía sau.

### Nguyên nhân gốc từng lỗi + fix

- **Tàu TÍM (không phải màu gốc)** — material SF Fighter/Sparrow dùng **shader Standard (Built-in)** (`m_Shader: fileID 45, guid 000...`) → trong URP render tím/magenta (shader không compile). Fix: **NEW `Utils/MaterialFixer.cs`** — convert mọi material không-URP → `Universal Render Pipeline/Lit`, giữ `_Color`/`_EmissionColor`, **cache static** (không leak, không tạo mới mỗi frame); áp dụng trong `PlayerController.BuildModelShip` + `ShipSelectManager.RefreshPreview`.
- **Không đổi được tàu bằng phím mũi tên** — ShipSelectManager chỉ có nút `<`/`>` bấm chuột, chưa xử lý keyboard. Fix: `Update()` bắt `Keyboard.current[key].wasPressedThisFrame` (←/→ + A/D) — project dùng **Input System only** (activeInputHandler=1) nên KHÔNG dùng legacy `Input.GetKeyDown`.
- **Popup lộ menu phía sau (VOID RUNNER vẫn thấy)** — `dimmer.SetAsFirstSibling()` chìm dimmer xuống DƯỚI các nút menu → menu vẽ lên trên → không bị tối. Fix: **dimmer `SetAsLastSibling()` TRƯỚC panel** (dimmer trên menu, panel trên dimmer) + alpha 0.85→**0.93**. Select ship CHƯA có dimmer → thêm `ShipSelectDimmer`.
- **Bug ẩn: click dimmer khi Credits mở lại BẬT HowToPlay** — dimmer dùng chung onClick hardcode `ToggleHowToPlay`. Fix: `CloseActivePopup()` đóng popup ĐANG MỞ.
- **CLOSE to che chữ** (3 chỗ: HowToPlay/Select ship/Credits) → nút **X nhỏ 40-44px** góc trên phải. ⚠️ Dùng chữ `X` (ASCII) KHÔNG dùng `✕` U+2715 — font chỉ pack ASCII 32..126 → ✕ ra ô vuông □ (R5.2).
- **Nút menu sát nhau** — Play y=60/HowTo -60/Sound -160 → mép-mép chỉ 24-32px. Tool mới **`Fix MainMenu Spacing`** (trong UIOverhaulTool): Play 160/HowTo 0/Sound -160/Best -250 (gap mép 72-84px) + HowToPlayText font 30→**36** + lineSpacing 1.15 + SHIP/CREDITS → -320 (đồng bộ scene + code runtime).
- **Credits chữ nhỏ** — body font 20→22 + panel 760×660 + lineSpacing 1.12 + title 44→48. (Reviewer: 24pt tràn ~19 dòng → 22 vừa.)

### Commit
- `49d8ad9` fix(ui) — 6 file. Scene Game/MainMenu được user chạy tool + Ctrl+S → có shipPrefabs/monsterPrefabs/ShipSelectManager với giá trị thật — commit kèm đợt docs.

---

## 2026-08-12 — Log đỏ RefactorGameplayTool + dọn 6 Editor tool không dùng

> User: "vẫn còn 1 log đỏ liên quan RefactorGameplayTool, điều tra ngay; tool nào không còn sử dụng thì xóa — tool phải tái dùng được nhiều lần, không phải 1 lần".

### Log đỏ — `RefactorGameplayTool.cs(155,66) CS0246 AmbientScroller`

- Nguyên nhân: tool vẫn gọi `WidenRoadAndMoveAmbientOut()` tham chiếu `AmbientScroller` (đã xóa ở đợt dọn trước) → compile fail.
- Fix: bỏ tham chiếu ambient → `WidenRoad()` chỉ set laneWidth cho Player/Obstacle/Pickup; đồng thời fix warning CS0618 (`FindObjectsByType` với `FindObjectsSortMode` deprecated → 1 tham số) ở RefactorGameplayTool + ShipSelectManager. Commit `b99eeee`.
- **Tool GIỮ vì tái dùng được** (idempotent, chạy nhiều lần): RefactorGameplayTool, UIOverhaulTool, KenneyFontImporter, MaterialLightingSetupTool, PostProcessingSetupTool, ShipSelectSetupTool, VoidMonsterSetupTool, SkyboxSetupTool, SpriteBatchConverter, VFXSetupTool, UIBuilderHelpers.

### Dọn 6 Editor tool không còn dùng (user duyệt)

- **HUDUIBuilder + MainMenuUIBuilder** — dựng UI tông BLUE cũ, đã bị UIOverhaulTool thay thế (comment trong UIOverhaulTool ghi rõ). Không ai gọi (chỉ nhắc tên trong comment).
- **HUDUpgradeTool** — nâng cấp HUD cũ, UIOverhaulTool đã làm chuẩn hơn.
- **CameraFollowFixTool + GameplayFixTool + ScenePolishTool** — tool fix 1 lần đã hoàn thành.
- **GIỮ UIBuilderHelpers** — KenneyFontImporter vẫn dùng (`CreateFontAssetCore`/`ReadGuid`/`RestoreGuid`).
- Commit `f643f6f`. Kiểm tra lại: log sạch (build success gần cuối, không lỗi CS).

---

## 2026-08-11 — CS0246 AmbientSetupTool sau đợt dọn file (2 log đỏ)

> User: "đang có 2 log lỗi đỏ, đọc log rồi fix nhé".

### Nguyên nhân (chính xác)

- **Lỗi THẬT — `AmbientSetupTool.cs(112,41)` CS0246 `AmbientScroller could not be found`**: sau khi xóa `AmbientScroller.cs` (đợt dọn file, user duyệt), Editor tool `AmbientSetupTool` vẫn tham chiếu class đó → compile fail → 2 log đỏ.
- **Lỗi CŨ — `ShipSelectSetupTool.cs(8,21)` CS0234**: nằm TRƯỚC `Tundra build success` trong log → đã fix từ trước, chỉ còn trong log cũ (R6.6: lỗi sau success mới là thật). Clear Console là hết.

### Cách fix

- **Xóa `AmbientSetupTool.cs` + `.meta`** — tool chỉ có 1 menu "Setup Ambient in Game Scene", không ai gọi, Ambient đã xóa hẳn khỏi scene (count=0) → vô dụng, không sửa được (chỉ làm mỗi việc gắn AmbientScroller). Commit `61433f0`.

### Bài học — **QUY TẮC MỚI (R3.14 / bổ sung R6.7)**

- **Khi xóa 1 script (class), PHẢI quét Editor tools tham chiếu nó trước** (`grep -rln '<TênClass>' Assets/_Project/Editor/`) — tool tham chiếu class đã xóa = compile fail toàn project. Nếu tool chỉ phục vụ đúng cái đã xóa → xóa luôn tool.
- Khi user báo "N log đỏ": phân biệt lỗi thật/cũ bằng vị trí so với `Tundra build success` cuối (R6.6) + mtime log.

---

## 2026-08-11 — CS0234: ShipSelectSetupTool không compile được (safe mode)

> User: "hiện có 1 log lỗi, ưu tiên fix đi".

### Nguyên nhân (chính xác)

- `Assets/_Project/Editor/ShipSelectSetupTool.cs(8,21): error CS0234: The type or namespace name 'Screens' does not exist in the namespace 'VoidRunner.UI'`.
- File tool đang `using VoidRunner.UI.Screens;` nhưng **tất cả** script UI trong thư mục `Scripts/UI/Screens/` (MainMenuManager, ShipSelectManager) khai báo namespace `VoidRunner.UI` — thư mục `Screens/` chỉ là tổ chức vật lý, KHÔNG phải namespace. `VoidRunner.UI.Screens` không tồn tại → compile fail → Unity bắt vào safe mode.

### Cách fix

- Bỏ dòng `using VoidRunner.UI.Screens;` (dòng 8) khỏi tool — `using VoidRunner.UI;` đã có sẵn nên `ShipSelectManager`/`MainMenuManager` vẫn resolve được.

### Bài học — **QUY TẮC MỚI (R0.x)**

- **Namespace phải khớp chính xác với file, không khớp với thư mục.** Trước khi viết `using X.Y.Z` phải `grep -n '^namespace'` file đích để xác minh namespace THẬT.
- Khi log báo lỗi ở dòng có `using` → nghi ngờ sai namespace trước tiên (CS0234/CS0246).

---

## 2026-08-11 — Ẩn TẠM cảnh vật 2 bên lề (Ambient props) — chờ user review UI

> User: "có background rồi thì cảnh vật 2 bên lề trông lơ lửng ở giữa vũ trụ không hợp lý — xóa cảnh vật 2 bên, nhưng để an toàn hãy ẩn/xóa tạm thời — tôi sẽ review UI trực tiếp rồi mới quyết định xóa hẳn hay không".

### Đã xong (không commit gì nặng — 1 dòng scene)

- **Không xóa, không sửa code AmbientScroller** — chỉ set GameObject **`Ambient`** (cha của 28 prop, fileID 2079828960) sang **`m_IsActive: 0`** trong `Assets/_Project/Scenes/Game.unity`. Đây là cách ẩn AN TOÀN NHẤT: 28 prop vẫn nguyên trong file scene, mọi code recycle/self-heal vẫn còn, khôi phục = **tích lại checkbox "Ambient" trong Hierarchy** (hoặc sửa `m_IsActive: 0 → 1`).
- Giữ nguyên: LaneMarker (vạch neon do Tile tạo runtime — là phần đường, không phải cảnh vật lề), skybox đã gắn.

### Bài học — **QUY TẮC MỚI**

- **Khi user muốn "xóa" một thứ nhưng chưa chắc: ẩn bằng `m_IsActive: 0` trên GameObject CHA thay vì xóa object/file/code** — 100% reversible, review xong tích lại là có lại. Không bao giờ delete object/props trong scene khi user chỉ nói "để tôi review trước". (Không có tiến trình "undo" cho scene khi đã lưu.)
- **Cảnh vật lề kiểu "cột trụ/trạm vũ trụ" KHÔNG hợp với game có skybox vũ trụ thật** — có nền tinh vân rồi, prop đứng lơ lửng 2 bên trông giả tạo. Hướng thay thế đúng thể loại: speed-lines / hạt sao vụt ngang / các khối thiên thạch ở xa (parallax) — chờ user chốt.

---

## 2026-08-11 — Task D: Speed-lines thay props lề (vệt sao 2 bên) + chốt asset tàu/void

> User chốt: (1) thực thi cả 3 ý tưởng tận dụng 2 gói (D speed-lines → B nebula theo độ khó → C nền UI cinematic) làm TỪNG task, task nào ổn định 0 bug UI mới qua task tiếp theo; (2) tàu = **Free SF Fighter (CGPitbull)** (bỏ Sci-Fi Space Fighter PBR — research: quá tối giản, chưa đủ rating); (3) Void = tải asset monster free (3 lựa chọn: Level 1 Monster Pack / Free Fantasy Spider / 3D Alien Monster Character codersan).

### Đã xong (Task D — commit `…`)

- **`SpeedLines.cs` (mới, `Systems/VFX`)** — thay props lề (đã ẩn): 2 hệ ParticleSystem (L/R) dọc 2 bên track, renderer **Stretch** (velocityScale 0.14, lengthScale 1.1) → hạt bay ngược -Z thành vệt sáng kéo dài, tốc độ theo `DifficultyManager.CurrentSpeed × 1.15` (càng chơi lâu càng vụt nhanh — cảm giác tăng tốc đúng chất hyperspace). Vị trí `x = ±11.5` (ngoài road ±9, trong tầm FOV 68 — nửa bề ngang thấy ~11.9; góp ý reviewer: 12.5 sát mép màn hình → 11.5). Material mềm tái sử dụng `VFXManager.CreateSoftParticleMaterial` (internal, cùng asmdef).
- **`GameManager.EnsureSpaceFX()`** — Start tạo GO `SpaceFX` + `AddComponent<SpeedLines>` (idempotent `transform.Find("SpaceFX")`), không cần kéo thả scene. Restart-safe: SpaceFX là con GameManager (tồn tại xuyên restart), player chỉ reset vị trí không destroy → cache ref OK; thêm re-fetch player nếu Start miss (góp ý reviewer).

### Bài học — **QUY TẮC MỚI**

- **Props lề thay bằng hiệu ứng TỐC ĐỘ khi đã có skybox** — game vũ trụ không cần "cột trụ" đứng 2 bên (giả tạo), cần vệt lao nhanh: ParticleSystem + `renderMode = Stretch` + `velocityScale` + `lengthScale` là cách không cần asset. Tốc độ hạt nên theo `DifficultyManager.CurrentSpeed` (đồng bộ nhịp tăng độ khó).
- **Không cần thêm component vào scene thủ công khi đã có GameManager** — component runtime tạo qua `EnsureXxx()` (Start, idempotent `transform.Find`) giảm 100% thao tác Unity + tránh lỗi "quên gắn". Pattern này giống `EnsureCameraRig` — chuẩn dự án.
- **Đặt object ở RÌA tầm nhìn (sát visible half-width FOV) = mất hút khi hơi xa** — tính lại: FOV 68, camera y=8 → nửa bề ngang thấy được ~11.9 ở mặt road; nên đặt trong khoảng 11–11.5, không phải 12.5.

---

## 2026-08-11 — Xóa hẳn Ambient + fix vệt sao + Credits UI + Task B (Nebula theo độ khó)

> User test Task D OK ("UI oke rồi") → yêu cầu: xóa hẳn cảnh vật lúc trước, thêm credits game, vệt sao đang thành "1 dãy" không giống sao → chỉnh lại; xong thì thực thi Task B.

### Đã xong

- **Xóa HẲN Ambient** khỏi Game.unity (script python `tools/remove_ambient.py` dùng 1 lần rồi xóa): parse block, xóa GO 2079828960 + Transform 2079828961 + MonoBehaviour 2079828962 + **115 block** (28 prop children gồm GO/Transform/MeshRenderer/MeshFilter) + gỡ `- {fileID: 2079828961}` khỏi m_Children Managers. Backup `.ambient-bak` + verify 0 tham chiếu còn lại rồi xóa backup. User đã duyệt xóa hẳn (review xong).
- **Fix vệt sao SpeedLines**: renderMode `Stretch` (velocityScale 0.14 + lengthScale 1.1 + emission 90 → DẢI sáng liên tục dính nhau, không giống sao) → `Billboard` chấm tròn rời rạc (startSize 0.09, emission 70, maxParticles 280) — đúng cảm giác "sao bay" rải rác.
- **Credits UI (MainMenu)**: `MainMenuManager.EnsureCredits()` — nút CREDITS tím y=-320 (dưới BestScore -230 — vị trí đầu -250 sẽ ĐÈ BestScore) + panel CreditsPanel 760×560 tím đen đục + text danh sách third-party assets (Kenney CC0, Nebula/SpaceSkies EULA — khớp agent/CREDITS.md) + nút CLOSE. Tất cả tạo bằng code idempotent, `ToggleCredits` dùng chung dimmer.
- **Task B — Nebula đổi theo độ khó**: `NebulaChanger.cs` (mới) — subscribe `DifficultyManager.OnDifficultyChanged` → `level=(speed-10)/10` → chọn `nebula[floor(level×4)]` → `RenderSettings.skybox` (idempotent `_currentIndex`, null-safe). `SkyboxSetupTool` refactor `CreateNebulaMaterial(tex,mat,name)` + menu mới **"Setup Nebula Difficulty"**: tạo 4 material Nebula01..04.mat từ 4 cubemap + AddComponent NebulaChanger lên GO Managers + gán mảng qua SerializedObject (idempotent).

### Bài học — **QUY TẮC MỚI**

- **Xóa hẳn object/scene block bằng script parse có backup + verify số dư tham chiếu = 0** — không sửa tay từng block (28 prop × 4 component = 115 block); luôn backup trước, verify sau (đếm fileID còn xuất hiện), rồi mới xóa backup. Dùng `python` (Windows: `python3` alias WindowsApps không chạy).
- **Vệt "speed-line" KHÔNG dùng renderMode Stretch với lengthScale lớn + emission cao** — Stretch + lengthScale 1.1 + rate 90 biến thành dải sáng dính nhau, không giống sao. Muốn giống sao: Billboard chấm tròn nhỏ, rate vừa phải.
- **UI element tạo bằng code phải kiểm tra vị trí so với element KHÁC cùng cột** — nút CREDITS y=-250 đè BestScoreText y=-230 → đẩy xuống -320 (dưới mọi thứ). Vẽ trước khi fix: liệt kê tọa độ các element gần đó.
- **Task B event-driven đúng chuẩn**: DifficultyManager đã phát `OnDifficultyChanged` → NebulaChanger chỉ subscribe/đổi skybox khi thực sự đổi (`_currentIndex`) — không poll, không spam log.

---

## 2026-08-11 — Bản quyền assets: tạo agent/CREDITS.md + README (Nebula/SpaceSkies/Kenney)

> User: "nhớ ghi bản quyền nhé, assets đó không phải của tôi" — 2 gói mới (Nebula, SpaceSkies) không thuộc tác giả.

### Đã xong

- Tạo **`agent/CREDITS.md`** — bảng đầy đủ mọi third-party asset: Nebula Skyboxes (Unity Asset Store EULA), SpaceSkies Free (PULSAR BYTES — Standard Unity EULA), toàn bộ Kenney packs (CC0 Public Domain). Ghi rõ: tác giả, license, link, lưu ý (cấm redistribute standalone / dùng AI training / logo Kenney không CC0).
- **README.md** — phần Giấy phép mở rộng: Code (tác giả) + link CREDITS.md + liệt kê 2 gói mới + Kenney CC0.
- Có kèm mẫu ghi công (THIRD-PARTY ASSETS) để dán vào màn hình Credits / khi publish.

### Bài học

- **Mọi asset bên thứ ba phải có file CREDITS riêng** (tác giả + license + link) — đặc biệt khi import gói từ itch.io/Asset Store; không bao giờ coi asset nhập về là của mình. Unity Asset Store dùng Standard EULA (không bắt buộc ghi công nhưng cấm redistribute/AI); Kenney = CC0.

---

## 2026-08-11 — Skybox: gắn Nebula/SpaceSkies (user import 2 gói) + fix camera ClearFlags Solid Color

> User import 2 gói: 'Nebula Skyboxes' (4 cubemap .exr, chưa có material) + 'SpaceSkies Free' (3 bộ Pink/Green/Purple, material sẵn). Yêu cầu: gắn vào game cho hết "bầu trời trống/vô hồn".

### Root cause & fix (commit `…`)

- **Skybox không hiện dù gán material** — Game scene camera đang `m_ClearFlags: 2` (Solid Color) → **skybox CHỈ vẽ khi camera clear = Skybox** (m_ClearFlags: 1). Gán `RenderSettings.skyboxMaterial` mà camera vẫn Solid Color = vô ích. Fix: đổi Game camera `2 → 1` trực tiếp trong scene (MainMenu đã là 1 sẵn).
- **Nebula chưa có material** — gói chỉ có 4 file `.exr` cubemap (import sẵn dạng Cube, 2048px). Fix: `Editor/SkyboxSetupTool.cs` (mới) — menu `Tools/Void Runner/Setup Skybox (Nebula)` tự tạo material `Skybox/Cubemap` (`_Tex = Nebula_02_Cubemap.exr`) tại `Assets/_Project/Materials/Skybox/NebulaSkybox.mat` (idempotent — có rồi thì load); menu `Setup Skybox (SpaceSkies Purple)` dùng material sẵn của gói (tông tím khớp hư không, nhẹ hơn). Cả 2 menu: gán `RenderSettings.skybox` + ép MỌI camera `ClearFlags = Skybox` (kể cả camera ẩn — FindObjectsInactive.Include) + MarkSceneDirty. Chạy cho cả Game + MainMenu.

### Bài học — **QUY TẮC MỚI**

- **Skybox không hiện = kiểm tra camera ClearFlags TRƯỚC** — gán material vào RenderSettings là chưa đủ: camera phải `ClearFlags = Skybox` (1), nếu `Solid Color` (2) thì skybox không bao giờ được vẽ. Tool gán skybox PHẢI ép luôn camera clearFlags.
- **Gói "skybox" dạng `.exr` cubemap thường KHÔNG kèm material** — phải tự tạo `Material(Shader.Find("Skybox/Cubemap"))` + `SetTexture("_Tex", cubemap)`. Kiểm tra `.meta` texture: `textureShape: 2` = Cube, `maxTextureSize` = độ phân giải import (exr này đã 2048 sẵn).

---

## 2026-08-11 — Vòng 8: nút CLOSE HowToPlay + camera trôi ngang theo tàu + popup +N che đường + lane rộng 4.5

> User: HowToPlay ổn nhưng KHÔNG có nút tắt; road rộng rồi thì dải 3 lane xanh cũng phải rộng theo (ko vượt road); bấm di chuyển mà cảnh vật di chuyển theo tàu; popup điểm "+10" chèn thẳng vào UI che obstacle/coin → phải ra khỏi trục đường.

### Root cause & fix (commit `…`)

- **HowToPlay không có nút tắt** — chỉ có click dimmer (vùng tối). Fix: `MainMenuManager.EnsureCloseButton()` — tạo Button "CLOSE" tím góc phải panel (anchor 1,1 @ (-24,-24), 150×56, label TMP fallback font) bằng code, idempotent (`transform.Find("CloseButton")`).
- **Camera TRÔI NGANG theo tàu ("cảnh vật di chuyển theo")** — `CinemachineCamera.TrackingTarget = Player` + `CinemachineFollow` bám CẢ trục X → tàu đổi lane (x di chuyển) → camera xoay/trôi theo → trên màn hình CẢNH VẬT di chuyển, tàu gần như đứng giữa. Fix: `CameraRig.cs` (mới) — LateUpdate ép `position = (0, target.y, target.z)` (KHÓA X=0); `GameManager.EnsureCameraRig()` (Start, idempotent): tạo rig nếu thiếu + `cam.Follow = rig.transform`. Camera luôn đứng giữa đường, chỉ tàu di chuyển trên màn hình.
- **Popup điểm "+N" che đường** — `VFXManager.ShowPopup` dùng `WorldToScreenPoint(vị trí coin)` → chữ "+10" nằm TRÊN đường, che obstacle/coin phía trước (người chơi không thấy kịp để né). Fix: popup về VỊ TRÍ CỐ ĐỊNH ngoài đường — anchor (0.5,1) @ **(260,-60)** (bên phải ScorePanel, ngoài vùng panel ±180 — góp ý reviewer: (150,-50) vẫn nằm trong panel); bỏ hẳn WorldToScreenPoint + `_cam`. Vẫn giữ burst hạt tại vị trí coin.
- **Dải phân cách 3 lane xanh chưa rộng theo road 18m** — laneWidth vẫn 3 (lane ở ±3) trong khi road ±9, vạch chia chỉ 1 vạch giữa x=0. Fix: laneWidth 3→**4.5** đồng bộ scene (PlayerController 4246, ObstacleManager 1261, PickupSpawner 1361) + `Tile.laneWidth=4.5` → vạch đứt chia lane ở **±laneWidth/2 = ±2.25** (giữa lane -4.5/0/+4.5), vẫn trong road ±9 (không vượt trục chính).

### Bài học — **QUY TẮC MỚI**

- **Camera follow không được bám trục X khi player đổi lane** — endless runner 3-lane: camera phải đứng giữa đường (khóa X=0, chỉ bám Z/Y), qua RIG trung gian; nếu camera bám thẳng player, đổi lane = cảnh vật trôi theo, mất cảm giác tàu đang rẽ + khó căn lane. Dấu hiệu: "bấm di chuyển mà cảnh vật di chuyển".
- **Popup/feedback điểm KHÔNG đặt tại vị trí world của coin/obstacle** (WorldToScreenPoint) — chữ điểm nằm trên đường che tầm nhìn né tránh. Đặt vị trí CỐ ĐỊNH ngoài vùng gameplay (cạnh HUD, ngoài vùng panel — kiểm tra sizeDelta panel trước khi chọn offset).
- **Popup/overlay bật/tắt phải có nút đóng rõ ràng (CLOSE/X)** — chỉ click ra ngoài (dimmer) là không đủ, user không biết. Nút tạo bằng code idempotent (transform.Find trước khi tạo).
- **Lane width và vạch chia lane phải khớp nhau** — laneWidth 4.5 → vạch chia ở ±2.25 (ranh giới giữa lane thật), không phải vạch giữa cố định x=0 khi road đã rộng. Đồng bộ: laneWidth scene ×3 + Tile.laneWidth + vạch marker.

---

## 2026-08-11 — Vòng 7: HowToPlay vẫn khó đọc + road rộng 18m + di chuyển phản hồi tức thì

> User test kỹ (400+ log), báo: HowToPlay chưa được fix, cảnh vật 2 bên vô hồn (đề xuất tải background), road vẫn nhỏ, di chuyển chưa mượt (muốn bấm = đi 1 tí, đè lâu = rẽ sang).

### Root cause & fix (commit `…`)

- **HowToPlay "chưa được fix"** — vòng trước đã thêm dimmer nhưng panel nền **alpha 0.92 vẫn trong suốt 8%** + các nút menu (PlayButton y=60, BestScore y=-230) nằm TRONG vùng panel 720×480 → **lộ xuyên qua panel** → vẫn đọc rối. Fix: `MainMenuManager.ToggleHowToPlay` khi mở → ép `panel Image alpha = 1.0` (đục hoàn toàn, che kín menu sau) + dimmer 0.72 → **0.85** (menu tối sâu hơn).
- **Road vẫn nhỏ** — roadHalfWidth 7 (road 14m) so với khung hình FOV 68 vẫn hẹp. Fix đồng bộ 4 chỗ (bài học R: road rộng phải đồng bộ): `Tile.roadHalfWidth` 7→**9** (road 18m), `AmbientScroller` 2 const roadHalfWidth 7→**9** (props tự đẩy ra ngoài mép road mới qua `HealProp`/`BuildProps`), scene `Ground scale x 14→18`, tool `RefactorGameplayTool` Ground 18 + ambient sideOffset 9.5→12.5 (đồng bộ — chạy lại tool không phá road mới). laneWidth giữ 3 (lane ở ±3, road rộng thoáng hơn).
- **Di chuyển chưa mượt** — cơ chế cũ chỉ sweep 6 m/s (0.5s/lane): bấm-nhả nhanh gần như không đi, đè giữ thì chậm. Fix `PlayerController`: **cạnh lên (rising edge) → nhảy NGAY 1 laneWidth** (tap = đi 1 lane tức thì) + đè giữ → trượt liên tục (sweepSpeed 6→**9**) + nhả → snap lane gần nhất; `laneChangeSpeed` 8→**16** (phản hồi nhanh hơn); đồng bộ `_currentLane` ngay ở nhánh edge (tránh stale cho MoveLeft/Right/test — góp ý reviewer). Scene: laneChangeSpeed 16 / sweepSpeed 9.

### Bài học — **QUY TẮC MỚI**

- **Panel popup phải ĐỤC hoàn toàn (alpha 1.0), không chỉ "gần đục"** — alpha 0.92 vẫn để element menu nằm trong vùng panel lộ xuyên qua (PlayButton/BestScore nằm ngay trong 720×480) → "đã fix mà vẫn khó đọc". Kiểm tra: element menu nào có tọa độ nằm trong vùng panel → phải che kín hoặc di chuyển.
- **Sửa hằng số road width: quét TOÀN BỘ chỗ hardcode** (Tile, AmbientScroller const ×2, scene Ground scale, Editor tool Refactor — tool cũ hardcode 14 sẽ PHÁ road mới nếu chạy lại). Đồng bộ kèm cả `sideOffset` của ambient.
- **Cơ chế di chuyển chuẩn hyper-casual: rising edge = nhảy 1 lane, đè giữ = sweep, nhả = snap** — cần phát hiện cạnh lên của phím (so sánh input frame trước), không chỉ trạng thái giữ. Đồng bộ state lane ngay khi nhảy.

---

## 2026-08-11 — Fix nhanh sau vòng 5 vấn đề: 1 error + 2 warning (commit `307d10c`)

> User báo 1 log đỏ + 2 warning trước khi test. Đọc log → đúng trúng 2 chỗ vừa sửa.

### Đã fix

- **1 error CS1061** — `PickupSpawner.cs(118)`: `IReadOnlyCollection<int>.Contains` không tồn tại → **thiếu `using System.Linq;`** (Contains là extension method của LINQ).
- **2 warning CS0618** — `TileSpawner.cs(57,61)`: `FindObjectsByType<T>(FindObjectsSortMode)` **obsolete trong Unity 6** → bỏ tham số, dùng `FindObjectsByType<T>()`.

### Bài học — **QUY TẮC MỚI**

- **`IReadOnlyCollection<T>` KHÔNG có method `Contains`** — đó là extension của LINQ (`System.Linq`). Khi dùng `Contains` trên `IReadOnlyCollection`/`IEnumerable` nhớ `using System.Linq;` (HashSet/List có sẵn thì không cần). *(Bug 2026-08-11.)*
- **Unity 6: `FindObjectsByType<T>(FindObjectsSortMode)` BỊ DEPRECATED** — dùng `FindObjectsByType<T>()` hoặc `FindObjectsByType<T>(FindObjectsInactive)`. Các tool Editor cũ (`RefactorGameplayTool`, `GameplayFixTool`) vẫn còn warning này — fix dần khi chạm tới (không ưu tiên).

---

## 2026-08-11 — Vòng 5 vấn đề: HowToPlay khó đọc + obstacle đè coin + player bắt đầu khác nhau + cảnh vật lúc có lúc không

> User đọc 166 log, báo 5 vấn đề. Điều tra từng gốc rễ → fix 7 file + góp ý reviewer.

### Root cause & fix (commit `f7b704d` + reviewer)

- **HowToPlay popup đè lên menu khó đọc** — panel nền alpha 0.92 nhưng main menu 2 bên vẫn sáng → `MainMenuManager.EnsureDimmer()`: Image đen alpha 0.72 phủ fullscreen + panel `SetAsLastSibling` vẽ trên. **Góp ý reviewer:** dimmer raycastTarget=true sẽ CHẶN nút HowToPlay phía sau → user kẹt không đóng được → thêm `Button` trên dimmer: click vùng tối = đóng popup (UX chuẩn).
- **Obstacle ĐÈ coin** — 2 hệ thống chọn lane NGẪU NHIÊN ĐỘC LẬP (log: tile 0 obstacle lane=-3 + coin lane=-3 CÙNG lane). Fix: `ObstacleManager._blockedLanes` (HashSet, clear mỗi TrySpawn) + `PickupSpawner.PickLaneAvoidingObstacles()` — coin/powerup chọn lane KHÔNG trùng obstacle (wire qua `TileSpawner.Initialize → BindObstacleManager`). **Góp ý reviewer:** safe-zone không gọi TrySpawn → BlockedLanes stale từ tile trước → coin tránh lane vô cớ → thêm `ClearBlockedLanes()` gọi khi inSafeZone.
- **Player bắt đầu KHÁC NHAU mỗi lần** — log `[DiagSpawn] tile=1 playerZ=148.9 nextZ=138.9` = sau restart player KHÔNG về 0 (HandleRestart set `_rb.position` nhưng thứ tự event/rigidbody không chắc) → track dựng quanh vị trí cũ. Fix: `PlayerController.ResetToStart()` public — set CẢ `transform.position` LẪN `_rb.position` về `_startPos` (scene (0,1,0) cố định); `GameManager.Restart()` gọi TRỰC TIẾP `player.ResetToStart()` TRƯỚC `RaiseRestart` + `StartTrack` (không phụ thuộc thứ tự subscriber). Safe zone `TileSpawner.safeZoneAhead=20m`: tile đầu không spawn obstacle (trước đây obstacle spawn ngay z=1.7 → chết tức thì).
- **Cảnh vật 2 bên lúc có lúc không** — `AmbientScroller` chỉ self-heal 1 lần ở `Start()` → sau restart props vẫn nằm quanh vị trí cũ (z~150). Fix: subscribe `GameEvents.OnRestart` → `BuildProps()` lại quanh vị trí player (đã reset). `BuildProps` phân nhánh `Destroy` (runtime) / `DestroyImmediate` (editor).

### Bài học — **QUY TẮC MỚI**

- **2 hệ thống spawn cùng tile phải chia sẻ trạng thái lane** — obstacle chọn lane ngẫu nhiên độc lập + coin chọn lane ngẫu nhiên độc lập = luôn có xác suất chồng (obstacle đè coin). Hệ thống spawn sau (coin) phải đọc lane đã bị spawn trước (obstacle) chặn. Khi thêm cơ chế tránh, đừng quên đường "không spawn" (safe zone) — state phải được clear.
- **Teleport player khi restart phải set CẢ `transform.position` + `_rb.position`** — chỉ set `_rb.position` có thể không chắc chắn (thứ tự event/rigidbody sync) → vị trí khác nhau mỗi lần chơi. Và orchestrator (GameManager) nên gọi reset TRỰC TIẾP trước khi phát event — không phụ thuộc thứ tự subscriber.
- **Dimmer (overlay chặn click) PHẢI có cơ chế đóng riêng** — raycastTarget=true chặn nút phía sau → user kẹt nếu popup không có nút close. Chuẩn UX: click vào vùng tối = đóng (dimmer là Button).
- **Safe zone đầu game là bắt buộc cho endless runner** — obstacle spawn ngay tile đầu (z≈1.7) = chết tức thì khi bắt đầu, cảm giác "không công bằng". Chừa 20m đầu không obstacle (coin vẫn có).

---

## 2026-08-11 — Vòng fix sau khi thấy obstacle/coin: props đè road + hết props + void không hố đen + không hiệu ứng va chạm

> User chơi thấy obstacle/coin hiện đúng (fix Rotator thành công) nhưng báo 4 vấn đề mới. Đọc 89 log → xử 3 code + xác nhận 1 đã đúng.

### Đã xác nhận ĐÚNG (không sửa)

- **Vật cản luôn chừa ≥1 lane trống** — `ObstacleManager.TrySpawn`: `blockedLanes = Random.Range(1, laneCount)` (=1 hoặc 2 khi laneCount=3) + `HashSet` (không trùng lane) → mọi tile luôn còn ít nhất 1 lane an toàn. Log chứng minh: tile 160 chặn ±3 (giữa trống), tile 180 chặn 0/-3 (lane +3 trống)... ✓

### Đã fix (commit `5b7bec9` + `sửa reviewer`)

- **Props đè lên road + chỉ phần đầu có cảnh vật — 2 ROOT CAUSE:**
  1. Props trong scene là bản dựng bằng code CŨ (chỉ chuẩn chiều cao, `x=±9.5` cố định, bề ngang khổng lồ tràn vào road). Fix: `AmbientScroller.HealProp()` — tự ép từng prop về scale chặn cả chiều cao lẫn bề ngang (`NormalizeScale = min(targetHeight/cao, targetWidth/ngang)`) + đặt x theo bounds THỰC (`x = side × max(sideOffset, roadHalfWidth + halfWidth + margin)`).
  2. **`GameManager` KHÔNG gọi `AmbientScroller.Initialize`** (chỉ Editor tool gọi) → `_props` rỗng lúc runtime → `Update` recycle return sớm → props dừng cách ~105m → "chỉ đầu render". Fix: `Start()` self-heal — tự nạp toàn bộ con có sẵn trong scene vào `_props` (kèm HealProp) + tự tìm player qua `FindAnyObjectByType<PlayerController>` nếu null. Không cần chạy lại tool.
- **Không có hiệu ứng va chạm** — `OnObstacleHit` trước chỉ có screen shake. Fix: `PlayerController` subscribe `OnObstacleHit` → coroutine `BlinkShip` nhấp nháy toàn bộ renderer thân tàu 4 lần (cache `_shipRenderers` ở cả 2 nhánh BuildSpaceship; `HandleGameOver`/`HandleRestart` ép hiện lại phòng trường hợp chết/restart giữa lúc blink — góp ý reviewer).
- **Void = banh tím, không giống hố đen** — `VoidChase.BuildBlackHoleVisual()` idempotent: ẩn mesh banh tím, dựng lõi đen (sphere đen tuyệt đối, phát tím nhẹ) + **đĩa bồi tụ** (cylinder dẹt phát sáng tím neon, nghiêng 75°, quay 40°/s) + **hạt bị hút vào tâm** (ParticleSystem, `velocityOverLifetime.radial = -1.5` kéo về tâm, material mềm tái sử dụng `VFXManager.CreateSoftParticleMaterial`). Void phình to khi áp sát → đĩa to theo (đe dọa rõ).

### Bài học — **QUY TẮC MỚI**

- **Không bao giờ tin "props/scene đã đúng vì tool đã dựng"** — props dựng cứng trong scene bằng tool KHÔNG tự cập nhật khi code đổi (tool đã chạy với code cũ). Nếu sửa code spawn/build: (a) chạy lại tool, HOẶC (b) thêm self-heal runtime (Start nạp con có sẵn + ép lại chuẩn). Self-heal mạnh hơn — không phụ thuộc user chạy tool.
- **Manager gọi Initialize của hệ thống con: phải gọi cho TẤT CẢ** — GameManager gọi `tileSpawner.Initialize` nhưng quên `ambient.Initialize` → ambient chết âm thầm (no error, chỉ không recycle). Khi 1 hệ thống "hoạt động sai một nửa" (spawn đúng nhưng recycle không chạy), check xem Initialize/Start có được gọi ở runtime không.
- **Hiệu ứng va chạm phải có visual feedback trên CHÍNH PLAYER** (blink/flash) chứ không chỉ screen shake — người chơi cần thấy "mình vừa bị đụng" ngay trên nhân vật.
- **"Hố đen" = lõi đen + đĩa bồi tụ phát sáng + hạt bị hút** — visual bằng primitive + emission + particle (không cần asset); đĩa nghiêng quay chậm tạo cảm giác hút vật chất.

---

## 2026-08-11 — ROOT CAUSE CUỐI CÙNG: obstacle/coin "văng lung tung" = Rotator gắn nhầm lên Managers (cha của mọi tile)

> User chạy lại với diag mới (113 log): fix scale đã chạy (`tileScale=(1,1,1)`) nhưng obstacle/coin WORLD position vẫn lệch X/Y lung tung.

### Root cause (đã xác minh 100% bằng log + scene)

- `[DiagObstacle]` log có `tileRot=(357.36, 355.37, 352.94)` ≈ gần 360° và nhiều giá trị xoay lung tung (297, 345, 256...) → **TILE ĐANG QUAY VÒNG LIÊN TỤC**.
- `Rotator.cs` (xoay `(15°, 30°, 45°)/giây` — file user tưởng đã thêm vào **coin**) thực tế bị gắn lên GameObject **"Managers"** (`m_GameObject: {fileID: 288287876}`) trong Game scene — Managers là cha của TileSpawner → cha của TOÀN BỘ tiles → cả track + obstacle + coin + void **quay vòng**.
- Hậu quả dây chuyền: obstacle/coin là con tile → world position nhân theo rotation → văng X/Y lung tung (y từ -2.61 đến +3.90); tile xoay → vị trí z đổi liên tục → `tile=2` (recycle nhầm, track chỉ giữ 2 tile); player chạy 14,500 điểm không bao giờ đụng obstacle.
- **Vì sao mất 3 tuần:** 2 root cause CHỒNG NHAU — (1) tile scale nhân vào con (đã fix `3d1a794`), (2) Rotator trên Managers khiến mọi thứ quay. Fix xong scale thì Rotator lộ ra.

### Đã fix (commit `43f2936`)

- Xóa block Rotator (fileID `288287887`) khỏi GameObject Managers trong `Game.unity` (gỡ cả component khỏi m_Component list + m_Script GUID `0a1e4dc7...`).

### Bài học — **QUY TẮC MỚI**

- **Rotator (hoặc bất kỳ component xoay visual nào) CHỈ gắn lên đúng GameObject có visual cần xoay (coin, obstacle, particle) — TUYỆT ĐỐI không gắn lên container/manager/cha có con mang vị trí world** (Managers, TileSpawner, tile) → xoay cả cây con, phá toàn bộ thế giới. Dấu hiệu: `transform.eulerAngles` của tile/container quay vòng theo thời gian + con cái world position lệch lung tung dù localPosition đúng.
- **Khi user nói "tôi đã thêm component X vào Y", hãy VERIFY component thực sự nằm ở đâu trong scene** (grep GUID script trong scene/prefab, tìm block m_GameObject của component) — component có thể bị kéo thả nhầm vào object khác mà user không biết. Đừng tin lời nói, hãy tin file trên đĩa.
- **Hai bug chồng nhau che dấu nhau** — fix xong bug A mà triệu chứng còn, đừng kết luận "fix không ăn"; tìm bug B có cùng triệu chứng (scale → rotation).

---

## 2026-08-11 — ROOT CAUSE "không thấy vật cản/xu" — tile scale nhân vào con (bug 3 tuần)

> User hỏi "fix lỗi ko hiển thị xu (coin) và vật cản" — bug dai dẳng nhiều vòng. Lần này tìm ra gốc rễ thật.

### Root cause (đã xác minh bằng đọc file prefab/scene)

- **`Tile.Awake` ép `localScale = (14, 0.1, 10)` trên ROOT tile** → Unity nhân scale parent vào **VỊ TRÍ lẫn KÍCH THƯỚC** của mọi con: obstacle/coin spawn ở lane x=3 thực tế ở **world x=42** (xa ngoài đường ±7) và bị **dẹt cao 0.1** → chúng VẪN spawn (log `ĐÃ TẠO` có) nhưng vô hình. Lane marker cũng ra x=±92 → đường trống trơn. Triệu chứng "không thấy vật cản/xu" đúng 100%.
- Kèm theo: **DynamicBox.prefab** có `Rigidbody + m_UseGravity:1 + collider solid (IsTrigger:0)` → obstacle rơi + player `OnTriggerEnter` không bao giờ fire (solid-solid = OnCollision). **Ramp.prefab** cũng solid.

### Đã fix (commit `3d1a794`)

- **Tile.cs**: `localScale = (1,1,1)` (KHÔNG scale root); road visual chuyển sang **child "Road"** (cube 14×0.1×10, di chuyển mesh/material từ root, bỏ collider root 1×1×1); `Deactivate` giữ cả LaneMarker lẫn Road khi recycle (nếu xóa Road → tile mất mặt đường sau vòng đầu).
- **DynamicBox.prefab**: collider → **trigger** + `UseGravity:0` + `IsKinematic:1` (không rơi, không bị đẩy, vẫn trigger với player dynamic RB).
- **Ramp.prefab**: collider → **trigger** + sửa `m_Size (1,1,1)` → `(2,0.5,2)` khớp mesh (hit đúng tầm nhìn).
- **ObstacleManager**: spawn y 0 → **0.5** (nằm trên mặt road; trigger nên không cần vật lý đặt xuống).
- Coin prefab vốn đã trigger + spawn y=0.8 — sau khi parent scale = 1 thì hiện đúng (vị trí world = local).

### Bài học — **QUY TẮC MỚI (NHÓM 4)**

- **KHÔNG BAO GIỜ scale ROOT của container chứa con được đặt vị trí (tile/chunk/spawner)** — scale parent nhân vào cả vị trí và kích thước con (`world = parentScale × local`). Container scale = (1,1,1); muốn to nhỏ thì scale CHILD (mesh/con tạo visual). Dấu hiệu bug: "spawn đúng (log có) nhưng không thấy" → kiểm tra scale parent. *(Bug 3 tuần 2026-08-11.)*
- **OnTriggerEnter chỉ fire khi ≥1 collider là trigger** (solid-solid = OnCollisionEnter) — obstacle nên là trigger + không gravity (hoặc kinematic) để player solid sphere detect được mà không bị bump/đẩy.
- **Collider phải khớp mesh** — Ramp collider (1,1,1) nhưng mesh (2,0.5,2): hit nhỏ hơn tầm nhìn. `m_Size` collider = scale mesh.

---

## 2026-08-11 — Fix API sai khi sửa font (5 lỗi đỏ → safe mode): FontImporter KHÔNG còn trong Unity 6

> User báo 5 log đỏ sau khi clear → đều trong `UIBuilderHelpers.cs` (do fix font vòng trước dùng API cũ).

### Đã xong

- **Lỗi 1 (3 lỗi): `CS0246 FontImporter` + `CS0103 FontImporterCharacterSet`** — Unity 6 **ĐÃ XÓA/KHÔNG CÒN `FontImporter`** (class + enum). Xác minh qua meta file: importer thật là **`TrueTypeFontImporter`** (`class in UnityEditor`, inherit AssetImporter) và property đúng là **`fontTextureCase`** (kiểu `FontTextureCase`), KHÔNG phải `characterSet`. Enum `FontTextureCase` cũng **KHÔNG có `ASCIIPrintableSet`** — dùng `Unicode` (đủ Latin). Xác minh DLL: `grep -a 'UnityEditor.TrueTypeFontImporter' UnityEditor.dll`.
- **Lỗi 2 (2 lỗi): `CS1503` (List<char> → uint[]) + `CS1615` (out)** — TMP bản này **CHỈ có `TryAddCharacters(string)` / `TryAddCharacters(uint[])`** — KHÔNG có overload `IEnumerable<char>` hay `out bool` (xác minh source: `Library/PackageCache/com.unity.ugui@*/Runtime/TMP/TMP_FontAsset.cs` dòng 1776/1790/1998/2012). Fix: dùng `TryAddCharacters(string)` với ASCII 32..126 qua StringBuilder.
- **Cách xác minh API chuẩn (đã làm):** (1) đọc meta file biết importer thật; (2) `grep -a` trên `UnityEditor.dll` (tìm string type theo cách grep đơn giản, namespace lưu rời); (3) đọc source package trong `Library/PackageCache` — TMP Unity 6 nằm trong `com.unity.ugui/Runtime/TMP/TMP_FontAsset.cs` (KHÔNG phải FontAsset.cs!).

### Bài học — **QUY TẮC MỚI (bổ sung R3.10)**

- **Unity 6 KHÔNG còn `FontImporter`/`FontImporterCharacterSet`/`characterSet`** — thay bằng `UnityEditor.TrueTypeFontImporter.fontTextureCase` (enum `FontTextureCase`: Dynamic/Unicode/ASCII/..., không có ASCIIPrintableSet). Kiểm tra meta file (`TrueTypeFontImporter:` chính là class để dùng) trước khi viết code.
- **TMP `TryAddCharacters` trong Unity 6.4 chỉ có overload `string` và `uint[]`** (KHÔNG có `IEnumerable<char>`/`out bool`) — lỗi `CS1503`/`CS1615` khi gọi sai overload. Luôn grep source thật trong `Library/PackageCache` trước khi dùng API lạ.
- **Tên file TMP là `TMP_FontAsset.cs`** (không phải `FontAsset.cs`) — tìm source TMP: `Library/PackageCache/com.unity.ugui@*/Runtime/TMP/`.

---

## 2026-08-11 — Vòng 6: gốc rễ font 8 glyph, input đè giữ, đuôi tàu, HUD spacing, road rộng

> User chạy 2 tool xong: vẫn có warning; ComboText "x2" giờ hiện "HS" cam; "SCORE" dính số; road quá nhỏ; muốn ĐÈ phím di chuyển liên tục + đuôi tàu có hiệu ứng.

### Đã xong (3 commit)

- **Root cause "HS" = font Kenney Future SDF regenerate 2048 NHƯNG chỉ 8 ký tự** (không có `x`/`2` → ComboText "x2" render qua fallback → glyph loạn "HS" + warning TMP). Gốc rễ: `Kenney Future.ttf` importer để **`characterSet = Dynamic` (mặc định, .meta không có field)** → Unity chỉ extract ký tự ĐANG ĐƯỢC DÙNG trong scene → `characterInfo` gần rỗng → `CreateFontAsset` chỉ tạo ~8 glyph dù atlas 2048. Fix trong `CreateFontAssetCore`: ép `FontImporter.characterSet = ASCIIPrintableSet` + `SaveAndReimport()` TRƯỚC khi tạo + `TryAddCharacters(32..126)` belt-and-suspenders + `LogWarning` nếu pack không đủ.
- **Input ĐÈ GIỮ = trượt liên tục** (Subway Surfers): InputReader bỏ event rời rạc `LaneLeft/LaneRight` + repeat 0.12s → poll `MoveInput` mỗi frame; PlayerController đè giữ = `_targetX` trượt liên tục (`sweepSpeed` 6, clamp ±maxX), nhả = snap `Round(_targetX/laneWidth)` về lane gần nhất + đồng bộ `_currentLane`. GameManager bỏ wiring lane. Giữ `MoveLeft/MoveRight` cho tests.
- **Đuôi tàu**: ngọn lửa `Thruster` cone lập lòe (PerlinNoise theo `Time.time`, tắt khi dead, bật khi restart) + `Exhaust` ParticleSystem (loop, rate 45, startSpeed -7 về sau, `Particles/Unlit` + soft material **tái sử dụng** `VFXManager.CreateSoftParticleMaterial` — đổi `internal static`, bỏ duplicate).
- **HUD**: ScoreLabel "SCORE" lên sát đỉnh panel (anchor 0.5,1 @ y=-4, font 20 bold) + ScoreText xuống **nửa dưới panel** (anchor x 0..1 / y 0..0.72) — label và số tách rõ (trước đây ScoreText stretch FULL panel nên số dính ngay dưới label).
- **Road rộng 10 → 14** ("đường quá nhỏ"): tool set Ground scale x=14 + `laneWidth` 2→3 cho PlayerController/ObstacleManager/PickupSpawner (SerializedObject) + AmbientScroller `sideOffset` 7→9.5 (prop ra ngoài mép road ±7); Tile.cs `roadHalfWidth` 5→7 (lane marker + scale tile tự theo). Code default `laneWidth` GIỮ 2 vì `PlayerControllerPlayTests` hardcode 2 (đổi default = test fail) — set qua scene/tool.

### Bài học — **QUY TẮC MỚI**

- **TTF importer `characterSet = Dynamic` là BẪY khi tạo TMP font bằng code** — Unity chỉ extract ký tự ĐANG ĐƯỢC DÙNG trong scene → `CreateFontAsset` sinh font vài glyph (dù atlas to). Khi font tạo bằng code thiếu glyph hàng loạt: kiểm tra `ttf.meta` (không có `characterSet:` = Dynamic) → ép `FontImporter.characterSet = ASCIIPrintableSet` + `SaveAndReimport()` trước khi tạo.
- **Label + value trong cùng panel: value stretch full panel (anchor 0..1/0..1) = dính label** — tách bằng anchor: label đỉnh (0.5,1 @ y=-4, font nhỏ bold), value nửa dưới (y 0..0.72).
- **Road rộng phải đồng bộ 4 chỗ** (thiếu 1 chỗ = lệch): Ground scale x, `roadHalfWidth` (Tile), `laneWidth` (Player/Obstacle/Pickup), ambient `sideOffset`.
- **Serialized default đổi → test hardcode fail âm thầm** — test hardcode `laneWidth=2` nên giữ default code = 2, muốn đổi chỉ set qua scene/tool (không sửa default ảnh hưởng test).

---

## 2026-08-11 — Compile error: Regex.Replace overload 4 tham số với `count` KHÔNG tồn tại trong BCL Unity 6

> User báo 1 log đỏ trước khi chạy tool → `UIBuilderHelpers.cs(120,45): error CS1503: Argument 3: cannot convert from 'string' to 'int'`.

### Đã xong

- **Root cause:** dòng `System.Text.RegularExpressions.Regex.Replace(text, "guid: [0-9a-f]{32}", "guid: " + oldGuid, 1)` — overload `(string, string, string, int count)` **KHÔNG có trong BCL mà Unity 6 biên dịch** (chỉ có `(input, pattern, replacement)` 3 tham số + `(input, pattern, replacement, RegexOptions)`), trình biên dịch không tìm được overload phù hợp → báo lỗi Argument 3. Đây là lỗi phát sinh từ chính fix font trước đó (thêm `RestoreGuid`).
- **Fix:** bỏ Regex hoàn toàn, dùng **string ops thuần** — `text.IndexOf("guid: ")` + `Substring` chèn guid cũ (`.meta` luôn có ĐÚNG 1 dòng `guid:` đầu file → an toàn, không cần Regex + count).

### Bài học — **QUY TẮC MỚI**

- **Unity 6 BCL không có `Regex.Replace(string, string, string, int count)`** — chỉ 3-arg hoặc `RegexOptions` variant. Muốn replace có số lần giới hạn: bỏ count (pattern xuất hiện đúng 1 lần thì 3-arg vẫn ổn) hoặc dùng string ops. Kiểm tra nhanh API BCL trước khi viết: nếu không chắc overload tồn tại → chọn cách đơn giản nhất (IndexOf/Substring/Replace).
- **Mọi file `.meta` có đúng 1 dòng `guid: <32hex>`** — thao tác GUID trong Editor tool nên dùng IndexOf+Substring, không cần Regex.

---

## 2026-08-11 — Font Kenney Future SDF THIẾU GLYPH — combo "x2" hiện lỗi "H2" (đã fix, chờ user regenerate)

> User chơi thấy text lạ "H2" màu cam góc trái, trông bị mirror/glitch = **ComboText "x2"** đang render qua **fallback font**.

### Đã xong

- **Root cause:** `TMP_FontAsset.CreateFontAsset(font, 128, 9, SDFAA, 1024, 1024)` — atlas **1024² + sampling 128 + padding 9 chỉ chứa ~40/95 ký tự ASCII** → font trên đĩa chỉ có **30-41 ký tự**: `0 : A B C D E H I L M N O P R S T U V W Y _ a h m n t Â …` → **THIẾU 'x', '2', các chữ thường còn lại** → ComboText "x2" + HowToPlay English (toàn chữ thường) render qua fallback (glyph lệch tỉ lệ → nhìn như "H2" mirror). Score "1,017" hiện đúng vì ScoreText dùng font khác đủ digit.
- **Fix (3 thay đổi, commit):**
  1. `UIBuilderHelpers.CreateFontAssetCore` — atlas **1024 → 2048** (đủ ~196 ô → toàn bộ ASCII).
  2. Thêm **`ReadGuid`/`RestoreGuid`**: DeleteAsset+CreateAsset sinh **GUID MỚI** → mọi text trong scene mất font → lưu guid cũ trước khi xóa, restore vào `.meta` mới (Regex thay `guid: [0-9a-f]{32}` + `AssetDatabase.ImportAsset(ForceUpdate)`).
  3. `CreateFontAssetIfMissing` **tự heal**: check `characterTable.Count >= 80` (không chỉ atlasTexture — font thiếu glyph vẫn có atlas) + log số ký tự sau khi tạo để phát hiện sớm.
- **User cần chạy lại tool** `Tools → Void Runner → Create TMP Font (Kenney Future)` để tái tạo font 2048 (kèm `Refactor: Game Scene` cho HUD layout).

### Bài học — **QUY TẮC MỚI**

- **TMP font atlas 1024² + sampling 128 = chỉ đủ ~40 glyph** — khi text hiện ký tự "lạ/vỡ" (thường là chữ thường hoặc ký tự đặc biệt) → nghi ngờ **font thiếu glyph**, không phải lỗi layout. Kiểm tra nhanh: `grep 'm_Unicode:' <font>.asset` — đếm chữ thường (97-122) / digit (48-57) / chữ hoa (65-90).
- **Editor tool regenerate asset (DeleteAsset+CreateAsset) sinh GUID MỚI** — mọi tham chiếu scene (text TMP, prefab...) gãy âm thầm (không lỗi console, text rơi về font mặc định). Khi regenerate asset đang được scene reference: **lưu guid cũ (đọc .meta) → restore sau khi tạo lại**.

---

## 2026-08-11 — Vòng test tay: tàu lật, đè phím không liên tục, điểm vỡ khung, combo che góc, không vật cản/xu

> User test tay (Phần C) báo 6 vấn đề. 4 fix xong + commit `cb6f6e3`, 1 đang chẩn đoán (vật cản/xu — log diag), 1 chờ user review (ambient 2 hàng — KHÔNG sửa theo yêu cầu).

### Đã fix

- **Tàu vũ trụ lật lên xuống liên tục** → `PlayerController.Awake` thêm `_rb.constraints = FreezeRotation` + zero `angularVelocity` (nguyên nhân: SphereCollider vẫn còn trên root — physics làm quả cầu LĂN trên Ground → root xoay → tàu con bị lật theo; tàu KHÔNG cần lăn). Reset angularVelocity khi Restart.
- **Bấm/đè A-D không đổi lane liên tục** (phải bấm 2 lần mới qua 2 lane) → `InputReader` viết lại: poll `ReadValue<Vector2>()` trong `Update`, bấm phát qua lane ngay, **ĐÈ GIỮ → lặp mỗi `repeatInterval` 0.12s** (kiểu Subway Surfers — cảm giác di chuyển mượt).
- **Điểm quá to vỡ khung chứa điểm** → `RefactorGameplayTool.FixHudLayout`: ScorePanel 300→360 rộng, ScoreText font 58→40, căn giữa (idempotent).
- **Text "x2" (combo) che nửa góc trái** → ComboText đang là con CANVAS, anchor (0,1)@(34,-150) = góc trái màn hình → tool đưa xuống **DƯỚI panel điểm, căn giữa (0.5,1)@(0,-110)**, font 36.

### Đang chẩn đoán (chưa rõ nguyên nhân — đã thêm log tạm)

- **KHÔNG có vật cản + xu dù wiring scene ĐÚNG 100%** (ObstacleManager 2 data + prefab thật, PickupSpawner coinPrefab + 3 powerup, TileSpawner tilePrefab + obstacleManager, DifficultyManager 0.45, Tile.Awake lane markers OK, ObjectPool OK — đã grep/đọc toàn bộ). Tiles spawn (đường chạy + lane marker trượt) nhưng không thấy obstacle/coin → thêm `[DiagSpawn]`/`[DiagObstacle]`/`[DiagCoin]` log (mỗi 2s) để xác định khâu nào chặn. → user chơi 15s gửi log.

### Bài học (dự kiến — chờ kết quả diag)

- **Rigidbody + collider sphere vẫn LĂN dù không dùng AddForce** — khi player đổi từ "banh lăn" sang "tàu bay", phải đóng băng xoay (`FreezeRotation`) nếu không muốn vật lý xoay.
- **`performed` event chỉ fire 1 lần mỗi lần bấm** — muốn "đè giữ = lặp", phải poll trạng thái trong Update + repeat timer.

---

## 2026-08-11 — Tool Refactor "Đổi 0 text" — FindObjectsByType bỏ qua GameObject ẩn

### Đã xong

- User chạy `Refactor: Game Scene` → log `Đổi 0 text sang tiếng Anh` dù scene vẫn còn `CHƠI LẠI` / `ĐIỂM` / `CAO NHẤT`. Nguyên nhân: **`Object.FindObjectsByType<T>()` mặc định chỉ tìm GameObject ACTIVE** — còn `GameOverPanel` (chứa RetryButton/FinalScoreText/BestScoreText) đang **inactive** (`m_IsActive: 0`, tắt sẵn để ẩn) → toàn bộ text Việt bên trong nó bị bỏ qua → đổi 0.
- **Fix:** `RewriteTexts` dùng `FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)` — quét cả object ẩn. Ground 6000m đã chạy đúng (root active) — tool idempotent, chạy lại không hại.

### Bài học — **QUY TẮC MỚI**

- **`FindObjectsByType`/`FindAnyObjectByType` MẶC ĐỊNH KHÔNG quét GameObject inactive** — mọi UI tool sửa text dưới panel đang ẩn (GameOverPanel, HowToPlayPanel, popup...) phải dùng **`FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)`**. Dấu hiệu: tool chạy OK nhưng "Đổi 0 text" hoặc không tác động tới panel ẩn.

---

## 2026-08-11 — Fix 4 test PlayMode VoidChasePlayTests (lỗi test, không phải lỗi game)

### Đã xong

- **4/5 test `VoidChasePlayTests` đỏ** (Stage0 xanh): `FirstHit_MovesVoidCloser`, `CleanRun_AfterRelaxWindow`, `SecondHit_WithinWindow`, `Hit_AfterRelaxed`. Nguyên nhân: trong `SetUp` test dùng `gm.enabled = false` để chặn `GameManager.Start` — nhưng **`enabled = false` kích hoạt `OnDisable()` NGAY LẬP TỨC** → `GameManager.OnDisable()` có `if (Instance == this) Instance = null` → `Instance` bị null → `VoidChase.HandleObstacleHit`/`Update` gate theo `GameManager.Instance` → return sớm → Void không bao giờ tiến sát → 4 test cần "đụng" đều fail (Stage0 pass vì không cần Instance).
- **Fix:** sau khi `gm.enabled = false`, khôi phục `GameManager.Instance` (backing field `<Instance>k__BackingField` của auto-property, reflection `BindingFlags.NonPublic | Static`) + `State = Playing` (qua `GetSetMethod(true)`) — test môi trường đúng, Start vẫn không chạy.

### Bài học — **QUY TẮC MỚI**

- **`MonoBehaviour.enabled = false` TRONG TEST gọi `OnDisable()` đồng bộ** — nếu singleton có `OnDisable` set `Instance = null` (đúng chuẩn cho production), test sẽ mất Instance ngay. Muốn "có Instance nhưng Start không chạy": disable xong phải **khôi phục Instance + State bằng reflection** (hoặc không disable mà dựng đủ tham chiếu cho Start — phức tạp hơn).
- **Test cơ chế void phải cung cấp `GameManager.Instance.State == Playing`** — nếu gate state mà không có Instance, test "pass giả" cho kịch bản đứng yên nhưng fail cho kịch bản cần Update chạy.

---

## 2026-08-11 — THỰC THI GIAI ĐOẠN 2.5 — REFACTOR GAMEPLAY (user đã duyệt plan)

> User duyệt toàn bộ plan docs → code theo đúng R0.1–R0.8. 7 task đã code + commit + push.

### Đã xong

- **R3-3 — VoidChase.cs viết lại cơ chế 2 NẤC CỐ ĐỊNH** (Subway Surfers/Temple Run): bỏ hoàn toàn "co dần 60s" cũ (gây chết ở mức điểm cố định). NẤC 0 giữ 9m; đụng obstacle lần 1 → NẤC 1 tiến sát 5m + mở cửa sổ `relaxWindow` 12s; né sạch hết cửa sổ → nới về 9m; **đụng lần 2 trong cửa sổ → Void nuốt → Game Over**. Void không tự tăng tốc theo thời gian. Scale phình to khi áp sát (đe dọa). Giữ safety net `swallowDistance` + guard trạng thái trong `OnTriggerEnter`.
- **R3-1 — PlayerController.cs = TÀU VŨ TRỤ NHỎ**: dựng từ primitive (thân + cánh trái/phải + buồng lái + động cơ phát sáng) trong Awake — idempotent (`transform.Find("Ship")`); tắt MeshRenderer trái banh cũ; material neon tạo bằng code (URP Lit + `_EMISSION`); **banking nghiêng nhẹ khi đổi lane** (visual child, không đụng collider). **Đụng obstacle KHÔNG chết** — chỉ `RaiseObstacleHit` (bỏ `Die()`); Shield vẫn miễn nhiễm.
- **R3-4 — UIManager.ShowGameOver luôn hiện panel**: bỏ early-return khi `_scoreSystem == null`; dời `_panelGroup` setup lên trước; lưu `SaveSystem.BestScore` độc lập với ScoreSystem.
- **R3-5 — UI tiếng Anh toàn bộ**: `UIManager` (SCORE: / BEST:), `MainMenuManager` (BEST SCORE: / SOUND: ON-OFF), tool `RefactorGameplayTool` đổi text scene (RETRY, SCORE: 0, BEST: 0, HowToPlay English, SOUND: ON).
- **R3-7 — MainMenuManager.RefreshBestScore ẩn khi = 0**: `bestScoreText.gameObject.SetActive(BestScore > 0)`.
- **R3-6 — Layout nút âm thanh**: `RefactorGameplayTool` SoundButton 300×66 → 340×76, text stretch + padding 18px/6px, font 32, NoWrap, căn giữa (hết thụt vào viền).
- **R3-2 — Track vô tận thật**: tool kéo Ground 400m → 6000m (400m chỉ đủ chơi ~15–30s rồi "hết đường"; track thật là tile recycle vô tận).
- **VoidChasePlayTests.cs (5 test PlayMode)**: stage 0 giữ 9m / đụng lần 1 tiến 5m không chết / né sạch nới về 9m / đụng lần 2 trong cửa sổ = Game Over / đụng sau khi đã nới lại là "lần 1 mới" không chết. GameManager dùng disabled + reflect State=Playing để tránh Start noise.

### Bài học — **QUY TẮC MỚI**

- **Git: KHÔNG chạy nhiều `git commit` song song (spawn_agents parallel)** — tranh chấp `.git/index.lock` → lỗi `fatal: Unable to create index.lock`; tệ hơn, `git add` của tiến trình này có thể bị `git commit` của tiến trình khác cuốn vào (commit dính file lạ). **Luôn chạy git tuần tự — 1 lệnh/lần spawn.** (Đã gặp 2026-08-11: 3 commit song song → 1 fail lock, 1 dính file của nhánh khác.)
- **Cơ chế chết phản ánh skill**: "đụng lỗi → hậu quả (Void tiến sát) → nới lại khi né sạch" tạo căng thẳng công bằng — đúng yêu cầu user (Subway Surfers không phải Temple Run ngẫu nhiên).
- **Player visual tự dựng từ primitive chạy được ngay mà không cần model**: body/wings/cockpit/engine + material neon code — đủ đẹp cho hyper-casual, không tốn asset.

---

## 2026-08-11 — QUYẾT ĐỊNH REFACTOR GAMEPLAY (user review toàn diện — CHƯA code)

> User test thật và báo 8 vấn đề → tạm dừng deploy, chuyển hướng refactor cơ chế cốt lõi.
> **Chưa fix gì trong vòng này — chỉ cập nhật docs + chờ user duyệt plan.**

### Yêu cầu user (8 điểm)

1. **Player không hợp lý**: game tên "Void Runner" nhưng nhân vật chính là trái banh xanh → đổi player thành chủ thể phù hợp (tàu/drone/phi hành gia — chờ chốt)
2. **Đường chạy hết**: track KHÔNG vô tận (Ground tĩnh 400m hoặc tile recycle lỗi) → phải vô tận thật
3. **Void đuổi kiểu cũ sai**: banh tím tự tăng tốc chậm → chạm player ở mức điểm cố định. User muốn cơ chế **Subway Surfers/Temple Run**: đụng obstacle lần 1 → Void TIẾN SÁT; không chạm 10–15s → Void NỚI LẠI; chạm 2 lần trong cửa sổ → Game Over. Void KHÔNG tự tăng tốc
4. **KHÔNG thấy màn hình kết thúc game** → điều tra + fix Game Over panel luôn hiện
5. **Tiếng Việt/Anh lộn xộn** → toàn bộ text gameplay + menu = TIẾNG ANH
6. **Nút âm thanh**: text thụt vào viền, quá chật → fix layout/padding
7. **Best score hiển thị = 0 ngay từ đầu (vô nghĩa)** → chỉ hiện khi BestScore > 0
8. *(đi kèm)* Test toàn diện sau khi fix → mới deploy

### Đã làm (docs only)

- Tạo `agent/RULES.md` — trích xuất toàn bộ quy tắc từ CHANGELOG + BUGS (bug chồng bug, kể cả rule định hướng game mới R0.1–R0.8)
- `agent/BUGS.md` — thêm vòng 3 (8 vấn đề + phân tích + hướng fix đề xuất)
- `agent/void-runner-plan.md` — thêm **Giai đoạn 2.5 REFACTOR GAMEPLAY** (chờ duyệt)
- `agent/FEATURES.md` + `agent/TESTING.md` — cập nhật cơ chế mới + test checklist
- `README.md` — mô tả gameplay mới

### Bài học (mới)

- **Review toàn diện của user > milestone plan**: user chơi thật và phát hiện vấn đề thiết kế cốt lõi (player, cơ chế chết) mà plan không lường trước — luôn test với góc nhìn "người chơi" trước khi tuyên bố hoàn thành giai đoạn.
- **Cơ chế chết "tự tăng tốc kẻ thù theo thời gian" dễ gây chết ở mức điểm cố định** — cơ chế phản ánh skill (đụng lỗi → hậu quả) tạo căng thẳng công bằng hơn (Subway Surfers không phải Temple Run ngẫu nhiên).
- **Docs là giao diện trao đổi với user**: user đọc .md để duyệt — trước refactor lớn phải cập nhật đầy đủ docs trước, không code vội.

---

## 2026-08-11 — Vòng 2 gameplay feel: Void, lane marker, props, score bị che

### Đã xong

- **VoidChase.cs — bỏ NavMeshAgent hoàn toàn** (bug nghiêm trọng: track VÔ TẬN do tile recycle, NavMesh bake chỉ phủ vùng cố định → player chạy xa là NavMesh hết vùng, Void đứng yên → người chơi KHÔNG BAO GIỜ thấy kẻ thù). Cách mới: Void bám theo player trực tiếp, giữ sau lưng 9m → co dần tới 1.5m trong 60s + `swallowDistance` 1.6m safety net (khoảng cách < ngưỡng → RaiseGameOver — chắc chắn Void nuốt được player cuối game). Bỏ `using UnityEngine.AI`, bỏ `[RequireComponent(NavMeshAgent)]`.
- **Tile.cs — fix scale z=0** (tile prefab scale z=0 → khối cube dẹt vô hình → chỉ còn Ground tĩnh → MẤT CẢM GIÁC CHUYỂN ĐỘNG). Awake ép `localScale (10, 0.1, length)` + thêm **LaneMarker neon** (2 vạch cyan 2 mép + vạch đứt giữa — `CreatePrimitive`, bỏ collider, shared material static). Deactivate chỉ destroy obstacle/pickup con, GIỮ LaneMarker.
- **AmbientSetupTool**: sideOffset 11 → **7** (FOV 68 thấy ±9 — 11 nằm ngoài tầm nhìn!), targetHeight 3.2 → 4.5, spacing 9 → 7.5, countPerSide 10 → 14.
- **ScenePolishTool**: nền `(0.1, 0.06, 0.2)` (sáng hơn, hết đen thui), FOV 60 → **68**, light 0.65 → **0.8**.
- **UIOverhaulTool**: ScorePanel đưa lên **giữa-đỉnh màn hình** (anchor 0.5,1, y=-45, 300×90) + `SetAsLastSibling` — vẽ trên cùng, KHÔNG element nào che được điểm số nữa.
- **GameplayFixTool.cs (mới)**: (1) xóa NavMeshAgent THỪA trên Void (script mới không dùng) + đặt Void đúng sau lưng player (z-9, ngay sau camera = nhìn thấy) + ép cấu hình qua SerializedObject; (2) xóa AudioListener THỪA trên Main Camera (cả MainMenu + Game — giữ 1 listener duy nhất).

### Bài học — **QUY TẮC CỨNG mới**

- **Endless runner KHÔNG được dùng NavMeshAgent cho kẻ thù đuổi theo** — track vô tận (tile recycle) không bao giờ có NavMesh bake phủ hết; kẻ thù phải đuổi theo player TRỰC TIẾP (giữ khoảng cách / theo tốc độ). Nếu thấy "kẻ thù biến mất sau vài chục giây" → nghĩ ngay NavMesh.
- **Tile prefab scale z=0 là bẫy vô hình** — cube scale 0 chiều nào đó không render (hoặc render méo). Khi "cảm giác đứng yên / đường không chuyển động", kiểm tra scale của tile + có vạch kẻ đường (lane marker) không — vạch neon trượt theo tile là yếu tố quan trọng nhất tạo cảm giác tốc độ.
- **props ngoài tầm FOV = vô hình** — sideOffset phải tính theo FOV thật: FOV 68 thấy ±~9 ở cự ly camera→player; đặt prop xa hơn = không bao giờ thấy.
- **UI bị che thường do sibling order** — element vẽ SAU (SetAsLastSibling) luôn nằm trên; khi element bị che không rõ nguyên nhân, đừng chỉ sửa vị trí — ép lên vẽ cuối cùng.
- **Cơ chế chết kiểu "kẻ thù nuốt" phải có safety net** — nếu chỉ dựa collider overlap, việc đổi lane/bám ngang có thể khiến không bao giờ chạm; thêm kiểm tra khoảng cách trực tiếp (< ngưỡng → GameOver).

---

## 2026-08-11 — 2 lỗi compile gây safe mode (CameraFollowFixTool)

### Đã xong

- **Fix `CS0103: The name 'BindingMode'/'AngularDampingMode' does not exist in the current context`** (2 lỗi đỏ → buộc vào safe mode) trong `Assets/_Project/Editor/CameraFollowFixTool.cs`. Nguyên nhân: `BindingMode` và `AngularDampingMode` nằm trong namespace **`Unity.Cinemachine.TargetTracking`**, KHÔNG phải `Unity.Cinemachine` (chỉ `CinemachineCamera`, `CinemachineFollow` ở namespace `Unity.Cinemachine`). File chỉ có `using Unity.Cinemachine;` → 2 enum này không resolve. **Fix: thêm `using Unity.Cinemachine.TargetTracking;`.**

### Bài học — **QUY TẮC CỨNG của Cinemachine 3 (Unity 6)**

- Trong Cinemachine 3 (Unity 6), các enum `BindingMode`, `AngularDampingMode` và struct `TrackerSettings` nằm trong **`Unity.Cinemachine.TargetTracking`** — khi viết Editor tool thao tác `CinemachineFollow.TrackerSettings` phải `using` cả `Unity.Cinemachine` LẪN `Unity.Cinemachine.TargetTracking`. Kiểm tra nhanh trước khi viết: `grep -rln 'enum BindingMode' Library/PackageCache/com.unity.cinemachine@*/Runtime/` → luôn trả về file `TargetTracking.cs` với `namespace Unity.Cinemachine.TargetTracking`.
- **Khi tool mới gây safe mode:** 2 lỗi `CS0103` cùng 1 file Editor = 95% thiếu `using` namespace lồng của package (Cinemachine chia namespace sâu). Trước khi commit tool mới, nên grep namespace thật của mọi type lạ trong `Library/PackageCache`.

---

## 2026-08-10 — Test framework + asmdef (ngày sửa lỗi compile nhiều nhất)

### Đã xong

- `Assets/_Project/Tests/`: **Unity Test Framework** — 2 asmdef (EditMode + PlayMode) + 6 file test, tổng **24 test**: EditMode 16 (SaveSystem 6, GameEvents 5, ScoreSystem 5) + PlayMode 8 (combo tăng/clamp/reset, score theo distance, lane clamp). Kết quả test thật: **EditMode 16/16 + PlayMode 8/8 xanh**.
- **`VoidRunner.Core.asmdef`** — code chính chuyển từ `Assembly-CSharp` (predefined) sang custom assembly để test reference được.
- **`DOTween.Modules.asmdef`** — module DOTween (source code) rời khỏi `Assembly-CSharp-firstpass`.
- `Editor/MaterialLightingSetupTool.cs` — 5 material tông "hư không" (phát sáng neon) + Directional Light lạnh + ambient/fog tím, idempotent, đã chạy cho 2 scene.

### Bài học — **QUY TẮC CỨNG của Unity 6 về asmdef (không bao giờ quên nữa!)**

- **Custom asmdef KHÔNG THỂ reference `Assembly-CSharp` (predefined) — kể cả khi `overrideReferences: true`.** Đây là quy tắc bất khả thay đổi của Unity (không phải lỗi cấu hình). Hậu quả: test assembly reference `Assembly-CSharp` bị Unity im lặng bỏ qua → `CS0234: 'Core' does not exist in namespace 'VoidRunner'`. **Fix đúng chuẩn: code chính phải nằm trong asmdef THẬT** (tạo `VoidRunner.Core.asmdef`). Editor tools vẫn hoạt động vì predefined `Assembly-CSharp-Editor` TỰ ĐỘNG reference mọi asmdef có `autoReferenced: true`.
- **Khi tạo asmdef cho code chính, phải liệt kê references tường minh cho mọi package dùng** — `Unity.TextMeshPro`, `Unity.InputSystem`, `Unity.Cinemachine` (dù chúng có `autoReferenced: true`, liệt kê tường minh là chuẩn — loại bỏ mọi nghi ngờ). Nếu thiếu → `CS0246: TMPro/TextMeshProUGUI/InputAction/CinemachineImpulseSource not found` ở hàng loạt file.
- **Source code (.cs) trong `Assets/Plugins/` bị compile vào `Assembly-CSharp-firstpass` (predefined)** — custom asmdef KHÔNG reference được nó! DOTween dll core (Plugins) vẫn thấy được nhưng `DOTweenModuleUI.cs` (source trong Plugins/Modules) thì không → `CS1929: CanvasGroup.DOFade` lỗi "best overload ShortcutExtensions.DOFade(Material)". **Fix: tạo asmdef riêng trong thư mục Modules** (`DOTween.Modules.asmdef`, `autoReferenced: true`) rồi `VoidRunner.Core` reference nó.
- **`[UnityTest] IEnumerator` bắt buộc có ít nhất 1 `yield return`** — nếu không → `CS0161: not all code paths return a value`. Dù test không cần chờ frame vẫn phải có `yield return null;`.
- **Test SaveSystem phải xóa PlayerPrefs trong `[SetUp]` (TRƯỚC), không chỉ `[TearDown]` (SAU)** — test `BestScore_DefaultsToZero` fail vì đọc phải dữ liệu save THẬT còn sót từ lần chơi trước.
- **Warning `Assembly ... not valid. Loading of assembly skipped` khi mở lại Unity** — VÔ HẠI: Unity quét `Library/ScriptAssemblies/` có DLL test/package cũ không khớp version → báo rồi tự dọn. Biến mất sau lần mở sau; bấm Clear Console.
- **Chuỗi lỗi "lên từ 11 → 17 lỗi" khi đọc log** — các lỗi CS cũ KHÔNG tự biến mất khỏi `Editor.log`; khi kiểm tra phải so vị trí dòng lỗi với dòng `Tundra build success` cuối (lỗi nằm SAU success mới là thật).

---

## 2026-08-10 — G3: Post-processing (Bloom + Vignette + Color Adjustments)

### Đã xong

- `Editor/PostProcessingSetupTool.cs`: menu `Tools/Void Runner/Setup Post-Processing in Open Scene` — tự dựng **Global Volume** (isGlobal) + tạo/load profile `Assets/_Project/Settings/PostProcessing/VoidRunnerProfile.asset` với 3 override: **Bloom** (intensity 0.35, threshold 0.8, tint xanh), **Vignette** (0.25, tối xanh đen), **Color Adjustments** (contrast +8, saturation +6, filter lạnh); tự bật `renderPostProcessing = true` + gán `volumeTrigger`/`volumeLayerMask` trên Main Camera; idempotent (profile có rồi thì chỉ `EnsureOverrides` đảm bảo đủ 3 override); `Undo.RegisterCreatedObjectUndo` cho GameObject mới.
- API đã xác minh trực tiếp từ URP 17.4: `VolumeProfile.Add<T>(bool overrides)` + `TryGet<T>` + `Volume.isGlobal/sharedProfile` + `UniversalAdditionalCameraData.renderPostProcessing/volumeTrigger/volumeLayerMask` (namespace `UnityEngine.Rendering.Universal`).

### Bài học — **TÁI PHẠM 2 lỗi đã ghi** (đã fix, ghi đậm để không lặp nữa)

- **`Object.FindFirstObjectByType` obsolete (CS0618) — TÁI PHẠM lần 2!** Đã ghi ở G1 và G3 Editor Tools nhưng vẫn dùng lại khi viết tool mới. Quy tắc tuyệt đối: **mọi script Unity 6 mới chỉ dùng `FindAnyObjectByType`** — tự kiểm tra trước khi commit.
- **`SceneManager` không resolve khi chỉ có `using UnityEditor.SceneManagement;` (CS0103)** — `SceneManager` thuộc `UnityEngine.SceneManagement`, còn `UnityEditor.SceneManagement` chỉ có `EditorSceneManager` (MarkSceneDirty/OpenScene...). Fix: **dùng fully-qualified `UnityEngine.SceneManagement.SceneManager.GetActiveScene()`** — KHÔNG thêm `using UnityEngine.SceneManagement;` cạnh `using UnityEditor.SceneManagement;` vì cả 2 đều có class `SceneManager` → lỗi ambiguous CS0104.
- **Hệ quả của compile error trong Editor tool: menu `Tools/Void Runner` không hiện mục mới** — Unity giữ menu cũ nhưng KHÔNG load được menu item của assembly lỗi. Dấu hiệu: menu thiếu mục + status bar/Console có `error CS`. Sau khi fix, phải chờ Unity compile lại (menu item xuất hiện sau ~1-2 giây khi click Tools).
- **Tham số không dùng trong method (dead parameter)** — `EnsureGlobalVolume(string sceneName)` không dùng `sceneName` → bỏ tham số cho sạch (reviewer bắt được).

---

## 2026-08-09 — G3: Editor Tools (UI Kenney + font)

### Đã xong

- `Editor/SpriteBatchConverter.cs`: batch convert **1608 PNG** của 2 gói Kenney (`kenney_ui-pack` + `kenney_ui-pack-space-expansion`) sang **Sprite mode** (textureType: Sprite, mipmap off, alphaIsTransparency) — menu `Tools/Void Runner/Convert Kenney UI PNG to Sprites`; idempotent (đã là Sprite thì bỏ qua).
- `Editor/KenneyFontImporter.cs`: tạo TMP font asset từ `Kenney Future.ttf` — menu `Tools/Void Runner/Create TMP Font (Kenney Future)`; dùng overload `CreateFontAsset(font, 128, 9, SDFAA, 1024, 1024)` (sampling 128 cho nét chữ sắc khi hiển thị lớn).
- `Editor/MainMenuUIBuilder.cs`: tự dựng lại UI MainMenu (background `panel_glass`, title + glow, 3 Button sprite Blue `button_rectangle_gloss/flat`, best score, HowToPlayPanel) rồi **tự gán 6 field vào MainMenuManager** qua `SerializedObject` — không cần kéo thả tay; **tự tạo font nếu chưa có** (không phụ thuộc thứ tự tool).

### Bài học (quan trọng — 2 vòng sửa lỗi)

- **Editor script dùng class runtime phải có `using` namespace đúng** — `MainMenuManager` ở `VoidRunner.UI` → thiếu `using VoidRunner.UI;` gây `CS0246`. Kiểm tra: mọi class dùng phải có using đầy đủ.
- **`GlyphRenderMode` KHÔNG nằm trong `TMPro` namespace** — nó ở **`UnityEngine.TextCore.LowLevel`** (Unity 6/TMP mới). Thiếu using → `CS0103`. Bảng: `TMP_FontAsset.CreateFontAsset` overload đầy đủ cần `GlyphRenderMode` + `AtlasPopulationMode` → nhớ `using UnityEngine.TextCore.LowLevel;`.
- **`File.Exists` cần `using System.IO;`** — Editor script hay dùng, dễ quên.
- **Lỗi compile chặn Unity load project → tự vào SAFE MODE** — không phải dữ liệu hỏng; thoát bằng nút "Exit Safe Mode" sau khi fix xong. Bài học: **trước khi commit Editor script, rà soát toàn bộ `using` + API không dùng deprecated** (chạy compiler mental check).
- **`Object.FindFirstObjectByType` obsolete (CS0618)** — đã ghi từ G1 nhưng tái phạm: dùng `FindAnyObjectByType`. Khi viết tool Editor mới phải kiểm tra lại bảng API này.
- **`TMP_Text.enableWordWrapping` obsolete (CS0618)** — Unity 6 TMP mới: dùng **`textWrappingMode`** (`TextWrappingModes.Normal` / `NoWrap`).
- **`Image` không tự convert sang `GameObject`** — khi truyền vào field kiểu `GameObject` phải `.gameObject` (`CS1503`).
- **Sprite name lookup dùng `AssetDatabase.FindAssets(name + " t:Sprite")`** — ưu tiên `/Blue/` trước `/Grey/`; trả null → Image chỉ có màu (không crash) — chấp nhận được.
- **Convert 1608 PNG thay đổi `.meta` hàng loạt** (textureType: Sprite) — commit riêng 1 lần, không gộp với code.

---

## 2026-08-09 — G3: VFX (particle + screen shake)

### Đã xong

- `Systems/VFX/VFXManager.cs`: singleton — **2 particle burst tạo 100% bằng code** (không prefab/material asset): coin (14 hạt vàng, speed 5) + power-up (22 hạt, màu theo loại Shield=xanh/Magnet=đỏ/SlowMo=tím); texture tròn mềm tạo runtime bằng `Texture2D` radial alpha + shader `Universal Render Pipeline/Particles/Unlit` (fallback `Sprites/Default`); **screen shake** qua `CinemachineImpulseSource.GenerateImpulseWithVelocity` + tự `AddComponent<CinemachineImpulseListener>` vào `CinemachineCamera`; lắng nghe `GameEvents` (OnCoinCollectedAt/OnPowerUpActivated/OnObstacleHit/OnRestart) — zero coupling; singleton reset `Instance` trong OnDisable.
- `Editor/VFXSetupTool.cs`: menu `Tools/Void Runner/Setup VFX in Game Scene` — tự tìm GameObject chứa GameManager → gắn VFXManager; chạy 1 nút. **(v2: idempotent + tự gán font Kenney Future cho popup)**
- Đã chạy tool: VFXManager gắn vào scene Game (commit scene).

### VFX bổ sung (cùng ngày)

- **Score popup**: nhặt coin → text "+N" (nhân combo multiplier ×2–×5, đọc `ScoreSystem.Multiplier`) bay lên + bounce + mờ dần bằng DOTween Sequence; **object pool 8 text TMP** tạo bằng code trên Canvas (không Instantiate/Destroy giữa chừng); **kill tween cũ khi tái dùng pool** (DOTween.Kill cả target Graphic + RectTransform).
- **Vệt khói theo Void**: tìm `VoidChase` → `AddComponent<TrailRenderer>` bằng code; `startWidth` cập nhật mỗi frame theo `localScale.x` (Void nở dần); **`Clear()` khi OnRestart** (void teleport về đầu map — không kéo vệt dài xuyên map); material dùng chung `CreateSoftParticleMaterial()` (Particles/Unlit sample vertex color — URP/Unlit mặc định KHÔNG sample vertex color → trail sẽ hiện trắng, tránh dùng).
- `GameEvents` thêm `OnCoinCollectedAt(Vector3)` — mang **vị trí coin** cho VFX (burst + popup đúng chỗ); giữ nguyên `OnCoinCollected(int)` cho ScoreSystem (không phá).

### Bài học / lưu ý

- **`Unity.Cinemachine` (Cinemachine 3)** — namespace KHÔNG còn là `Cinemachine`; `CinemachineImpulseListener` là extension gắn trực tiếp lên GameObject có `CinemachineCamera`, `CinemachineImpulseSource` là MonoBehaviour gắn lên bất kỳ GO. API: `GenerateImpulseWithVelocity(Vector3)`.
- **`ParticleSystem.Emit()` bypass emission module** — `SetBursts/rateOverTime/duration` là dead config khi dùng `Emit()` trực tiếp → bỏ, đỡ rối.
- **`Camera.main` mỗi lần gọi là `FindGameObjectWithTag`** — cache `_cam` trong Start (Magnet hút nhiều coin cùng lúc → tránh gọi liên tục).
- **Popup pool phải kill tween cũ trước khi tái sử dụng** — nếu không, text có thể giữ alpha/scale cũ hoặc OnComplete cũ tắt nhầm popup.
- **Popup điểm nên nhân theo combo** — ScoreSystem cộng `coinScore × Multiplier`; popup cứng "+10" sẽ sai khi combo ×2–×5 → đọc `ScoreSystem.Multiplier`.
- **TrailRenderer + URP/Unlit hiện TRẮNG** vì shader không sample vertex color — phải dùng `Particles/Unlit` (hoặc shader có vertex color) khi tô màu bằng startColor/endColor.
- **Safe mode KHÔNG xóa log cũ** — khi mở lại Unity, Console có thể vẫn hiện lỗi của phiên trước (timestamp cũ). Cách kiểm tra thật: grep `error CS` trong `Editor.log`, nếu = 0 và có dòng compile chạy → an toàn. (Đã gặp: user tưởng còn lỗi nhưng log đã sạch.)
- **`UnassignedReferenceException: m_AtlasTextures of TMP_FontAsset has not been assigned` — KHÔNG vô hại, là lỗi THẬT (đã sửa hiểu lầm cũ!)** — nguyên nhân: `TMP_FontAsset.CreateFontAsset()` tạo texture + material **trong memory**, nhưng tool chỉ gọi `AssetDatabase.CreateAsset(fontAsset)` → **thiếu `AddObjectToAsset`** → file `.asset` ghi `m_AtlasTextures: {fileID: 0}` + `m_Material: {fileID: 0}`. Cùng phiên Unity: texture còn trong memory → UI vẫn đẹp; **mở lại Unity: load từ disk → font rỗng** → text không hiện chữ + exception. Fix: sau `CreateAsset` phải `AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset)` + `AddObjectToAsset(fontAsset.material, fontAsset)` rồi mới `SaveAssets`. Kiểm tra font trên đĩa: grep `m_AtlasTextures:` phải có fileID khác 0.
- **CÙNG BUG ÁP DỤNG CHO VolumeProfile** — `profile.Add<Bloom/Vignette/ColorAdjustments>(true)` tạo VolumeComponent trong memory; thiếu `AddObjectToAsset` → file ghi `components: {fileID: 0}` → **mở lại Unity là post-processing KHÔNG có tác dụng** (không lỗi console, chỉ âm thầm mất hiệu ứng!). Fix: sau `CreateAsset(profile)` phải `AddObjectToAsset(comp, profile)` cho từng component (guard bằng `GetAssetPath(comp) == GetAssetPath(profile)` để tránh add trùng). **Bài học chung: mọi ScriptableObject con tạo bằng code (texture/material/component) đều phải AddObjectToAsset** — tự rà soát các Editor tool đang tạo sub-asset.
- **.meta của script mới sinh khi Unity import** — commit code trước, chờ Unity sinh .meta, commit .meta sau (đúng quy trình đã ghi từ G2).

---

## 2026-08-09 — G2: MainMenuManager + scene MainMenu

### Đã xong

- `UI/Screens/MainMenuManager.cs`: Play (load scene Game), How to play (toggle panel), best score (SaveSystem), sound toggle (SaveSystem.Volume + AudioManager.SetVolume); subscribe/remove listener cân bằng.
- Scene `MainMenu.unity`: Canvas (Scaler 1920×1080 Match 0.5) + EventSystem (**Input System UI module** — bắt buộc vì project dùng Input System, KHÔNG dùng StandaloneInputModule cũ) + AudioManager copy từ Game (DontDestroyOnLoad) + Build Settings: MainMenu index 0, Game index 1.

### Bài học (từ lỗi user gặp)

- **"Select Button" trống dù đã tạo nút:** 3 "nút" thực chất được tạo bằng **UI → Text - TextMeshPro** nên chỉ có TMP text, KHÔNG có component `Button` — object picker lọc theo đúng loại component nên không hiện. Fix: chọn object → Add Component → **Button** (UI). Kiểm tra scene: grep `m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button` phải = 3.
- **Warning `\u25B6` (▶) not found in font:** LiberationSans SDF **không có glyph ▶** (U+25B6) → TMP thay bằng `□` + log warning. Fix: KHÔNG dùng ký tự icon ngoài bảng glyph của font TMP. Nếu muốn icon: dùng ký tự có sẵn (`>`, `»`) hoặc cài font có glyph đầy đủ (vd Noto Sans Symbols).
- **File scene chưa được Ctrl+S thì grep trên đĩa vẫn thấy state cũ** — phải chờ user Save rồi mới kiểm tra/commit.
- **Field `soundButtonText` là text con nằm TRONG nút âm thanh** — nhắc user kéo đúng text con (hoặc vì các nút tự nó là TMP text nên gán thẳng object nút vào field Text).

---

## 2026-08-09 — G2: AudioManager

### Đã xong

- `Systems/Audio/AudioManager.cs`: singleton + `DontDestroyOnLoad` — 2 AudioSource (BGM loop + SFX one-shot, tự tạo nếu để trống); lắng nghe `GameEvents` (coin, obstacle hit, power-up, lane switch, game start) với named method để unsubscribe cân bằng; volume đọc/ghi qua `SaveSystem.Volume`; `PlaySfx` public + pitch biến thiên nhẹ (`sfxPitchRandom`) cho game feel.
- `COMMIT_TEMPLATES.md`: quy ước mới — subject tiếng Việt **có đầy đủ dấu**.

### Bài học / lưu ý

- **Scene đã có `AudioListener` trên Main Camera** — AudioManager có `RequireComponent(AudioListener)`, khi gắn vào sẽ có **2 listener → warning**. Phải xóa `AudioListener` khỏi Main Camera (AudioManager là DontDestroyOnLoad, giữ listener duy nhất).
- **SFX có sẵn 2 gói Kenney** (`Audio/SFX/kenney_interface-sounds` + `kenney_sci-fi-sounds`) — gán clip từ đây; **BGM**: link cũ `kenney.nl/assets/background-music` bị **404** — dùng link mới `kenney.nl/assets/music-jingles` (86 jingle 8-bit, CC0).
- **DontDestroyOnLoad + singleton:** check `Instance != this` rồi `Destroy` bản trùng; `OnDisable` phải reset `Instance` — tránh singleton zombie khi scene reload.
- **Verify gán clip trong scene:** sau khi user kéo clip, grep block component trong `Game.unity` — mỗi field phải có `guid` riêng (không phải `{fileID: 0}`); đếm số `AudioListener` trong scene phải = 1.

---

## 2026-08-09 — G2: PowerUpSystem

### Đã xong

- `Data/PowerUpData.cs` (SO): enum `PowerUpType { Shield, Magnet, SlowMo }` + cấu hình (duration, magnetRadius, slowMoScale, spawnWeight) — tạo asset qua menu `VoidRunner/PowerUp Data`.
- `Systems/PowerUp/PowerUpSystem.cs`: singleton — Shield (miễn nhiễm va chạm trong 3s), Magnet (hút coin trong bán kính về player), SlowMo (`Time.timeScale = 0.5` tạm thời); event `OnPowerUpActivated/OnPowerUpExpired` + `GameEvents.OnPowerUpActivated`; reset khi Restart/GameOver.
- `Core/World/Coin.cs`: trigger pickup, tự đăng ký vào `Coin.Active` (registry tĩnh), phát `RaiseCoinCollected(1)`.
- `Core/World/PowerUpPickup.cs`: trigger pickup — gọi `PowerUpSystem.Activate(data)` rồi tự hủy.
- `Core/World/PickupSpawner.cs`: spawn hàng coin (1 lane ngẫu nhiên) + power-up hiếm (weighted) lên tile — TileSpawner gọi song song với ObstacleManager.
- `PlayerController.cs`: kiểm tra `PowerUpSystem.Instance.IsShieldActive` trước khi `Die()`.

### Assets / prefab (user làm trong Unity)

- 3 asset `ScriptableObjects/{Shield,Magnet,SlowMo}.asset` (PowerUpData) — GUID đã khớp với 3 prefab pickup.
- `Prefabs/Pickups/Coin.prefab`: Coin component + Is Trigger ✅ — **thiếu `Rotator`** (coin không xoay; thêm sau, không ảnh hưởng chức năng).
- `Prefabs/PowerUps/Pickup_{Shield,Magnet,SlowMo}.prefab`: mỗi prefab có `PowerUpPickup` + `data` gán đúng asset (Shield→dd03, Magnet→b737, SlowMo→f066).
- Scene: `PowerUpSystem` + `PickupSpawner` gắn vào Managers; `coinPrefab` + `powerUpTypes` (3 asset) đã kéo đủ.

### Bài học (từ code-review)

- **Mọi chỗ đụng `Time.timeScale` đều phải restore đầy đủ:** EndPowerUp (hết hạn), ResetAll (restart/game over) **và OnDisable** — nếu SlowMo đang chạy mà component bị tắt/scene unload, quên restore sẽ làm game chậm vĩnh viễn.
- **KHÔNG `FindObjectsByType<Coin>` mỗi frame khi Magnet hoạt động** (tạo GC array mỗi frame — tệ cho WebGL). Thay bằng **static registry**: `List<Coin> Active` + coin tự Add/Remove trong OnEnable/OnDisable.
- **`Time.timeScale` là global state** — mọi hệ thống dùng `Time.deltaTime` (DifficultyManager, combo timer...) đều chậm theo khi SlowMo; đó là ý đồ (cả thế giới chậm), nhưng phải nhớ nó ảnh hưởng toàn cục.
- Shield hiện cho **miễn nhiễm toàn bộ va chạm trong 3s** (không chỉ 1 lần) — khớp comment code, plan đã ghi rõ; nếu muốn đúng "1 va chạm" phải tiêu hao shield khi trúng (chưa làm).

---

## 2026-08-09 — G2: DifficultyManager

### Đã xong

- `Systems/Difficulty/DifficultyManager.cs`: singleton — tốc độ player `10→20` + mật độ obstacle `0.45→0.75` tăng dần theo `AnimationCurve` trong `rampDuration` (60s); giới hạn tốc độ tối đa (fair play); event-driven `OnDifficultyChanged(float speed, float spawnChance)` — chỉ phát event khi giá trị đổi > 0.001 (tránh spam); reset ramp khi `OnGameStarted`/`OnRestart`.
- `PlayerController.cs`: subscribe `OnDifficultyChanged` → `_currentSpeed` (giữ `forwardSpeed` làm tốc độ nền); `ForwardSpeed` trả `_currentSpeed` — **VoidChase tự động đuổi nhanh theo** (không phải sửa).
- `ObstacleManager.cs`: `CurrentSpawnChance` ưu tiên đọc từ DifficultyManager, fallback về `spawnChance` cấu hình (không phụ thuộc cứng).

### Bài học (từ code-review)

- **Khởi tạo giá trị trong `Awake`, không phải `Start`** — `GameManager.Start()` → `StartTrack()` spawn tile đầu tiên trong cùng pha Start; nếu khởi tạo trong Start mà chạy sau GameManager.Start, tile đầu tiên đọc `CurrentSpawnChance = 0` → không spawn obstacle ở đầu game.
- **`ResetRamp` phải reset cả giá trị hiện tại + phát event** — nếu chỉ reset `_runTime`, sau Restart có 1 frame tile mới đọc giá trị cũ (mật độ cao nhất) → obstacle dày bất thường ngay sau khi chơi lại.
- **Static event phải unsubscribe cân bằng** (OnEnable/OnDisable) — PlayerController đã làm đúng; kiểm tra mọi subscriber static event.
- **Giữ `spawnChance` của ObstacleManager làm fallback** dù DifficultyManager có `startSpawnChance` — 2 giá trị trùng (0.45) cần giữ đồng bộ khi chỉnh (chú thích Tooltip đã ghi rõ).

---

## 2026-08-09 — G2: UIManager + Canvas HUD

### Đã xong

- `Systems/Save/SaveSystem.cs`: static wrapper PlayerPrefs — `BestScore` (chỉ ghi khi cao hơn) + `Volume` (clamp 0–1).
- `UI/UIManager.cs`: HUD score/combo (subscribe `OnScoreChanged`/`OnComboChanged` từ ScoreSystem), Game Over panel fade bằng DOTween `CanvasGroup`, lưu best score khi chết; event-driven, không coupling.
- Scene `Game.unity`: dựng Canvas HUD (ScoreText + ComboText, `ComboText` tắt sẵn) + `GameOverPanel` (tắt sẵn, nền đen alpha 0.8, FinalScoreText + BestScoreText); gắn `UIManager` vào `Managers` với đủ 5 field.

### Bài học

- **Khi làm theo hướng dẫn dựng UI tay, kiểm tra lại 2 điều trước khi test:** (1) các field trong Inspector đã kéo đủ chưa — dùng `grep fileID` trong scene để xác nhận không còn `{fileID: 0}`; (2) GameObject ẩn sẵn (`ComboText`, `GameOverPanel`) phải có `m_IsActive: 0` — nếu quên tắt, UI sẽ hiện ngay từ đầu game.
- **File TMP Fallback asset bị Unity 6 tự upgrade format** (serializedVersion 6→8) khi mở project — là thay đổi hệ thống hợp lệ, commit luôn, không cần sửa.
- **Trailing whitespace trong file Unity tự sinh** (asset/material) — thêm file đó vào danh sách loại trừ của `git diff --check` thay vì sửa file hệ thống.

---

## 2026-08-09 — G2 bắt đầu: ScoreSystem

### Đã xong

- `Systems/Score/ScoreSystem.cs`: score theo khoảng cách chạy (`deltaZ × 10 × multiplier`) + coin (`coinScore × multiplier`); combo ×2…×5 tăng theo `comboInterval` (5s) sống liên tục, reset khi dính obstacle; event-driven `OnScoreChanged`/`OnComboChanged` — UI subscribe, không coupling.

### Bài học

- Đo điểm theo **`deltaZ` thực tế** (`player.position.z` mỗi frame) thay vì `speed * dt` — độc lập với DifficultyManager (tốc độ thay đổi theo thời gian) sắp tới, không cần sửa ScoreSystem khi tăng tốc.
- **File .cs mới tạo khi Unity đang mở sẽ chưa có `.meta`** — commit code trước, quay lại Unity để editor sinh `.meta`, commit `.meta` sau (không tự tay gõ GUID).

---

## 2026-08-08 — ObstacleData wiring (Obstacle.cs, ObstacleData.cs, ObstacleManager.cs)

### Ghi chú kỹ thuật

- **`Obstacle.cs`** giờ có `[SerializeField] private ObstacleData _data` + property `Data` + method `SetData(data)` — component biết mình thuộc loại obstacle nào (phục vụ G2: shield, vfx, xử lý theo type).
- **`ObstacleData.cs`** thêm enum `ObstacleType { Pillar, Ramp, Dynamic }` + field `obstacleType`.
- **`ObstacleManager.SpawnOnTile`** gọi `comp.SetData(data)` sau khi spawn — data lúc runtime luôn khớp với data đã pick theo weight.
- **Lưu ý tránh trùng lặp:** `ObstacleData.isDynamic` (bool) đang chồng lấn ngữ nghĩa với `ObstacleType.Dynamic` (enum) — gộp lại trong refactor tương lai (giữ cả hai hiện tại, chưa phá vỡ cấu hình cũ).

---

## 2026-08-09 — Bước 8 hoàn tất (scene Game đủ hệ thống)

### Đã xong

- Tạo `Ramp.prefab` (cube dẹt + component `Obstacle`) — asset `Ramp.asset` giờ có prefab thật (GUID khớp `.prefab.meta`).
- Gán prefab vào cả 2 `ObstacleData` (`DynamicBox.asset` + `Ramp.asset`) — trước đó `prefab: {fileID: 0}` (null).
- Scene `Game.unity` đã có **ObstacleManager** với 2 asset trong list.
- Xóa file rác `DynamicBox 1.prefab` (bản copy lỗi, Rigidbody mass 0.1 sẽ bay lung tung).

### Bài học

- **Asset đang mở trong Unity thì file trên đĩa chưa cập nhật** — phải `Ctrl+S` (File → Save) mới ghi xuống đĩa để commit được. Kiểm tra asset luôn đọc từ đĩa.
- Khi asset `prefab: {fileID: 0}` nghĩa là **chưa kéo prefab vào Inspector** — không phải lỗi code. Kiểm tra cả GUID khớp giữa asset và `.prefab.meta`.

---

## 2026-08-07 — Giai đoạn 1: 11 script core gameplay

### Lỗi compile

| # | Lỗi | File | Nguyên nhân | Cách fix | Tránh lặp lại |
|---|---|---|---|---|---|
| 1 | `CS1739: The best overload for 'InputAction' does not have a parameter named 'expectedControlLayout'` | `InputReader.cs` | Constructor `InputAction` của Input System 1.19 **không có** named parameter `expectedControlLayout` | Bỏ named arg: `new InputAction("Move", InputActionType.Value)` — layout tự suy ra từ composite | Không dùng named arg lạ trong constructor thư viện; kiểm tra signature thực tế của package đã cài |
| 2 | `CS0103: The name 'CreateTile' does not exist in the current context` | `TileSpawner.cs` | Refactor đơn giản hóa đã **xóa method `CreateTile`** nhưng pool vẫn `factory: CreateTile` | Thêm lại method `CreateTile()` (Instantiate + SetActive(false)) | Khi refactor bỏ method, phải xóa cả chỗ gọi; chạy compile sau mỗi lần sửa file |

### Warning đã xử lý (API deprecated trong Unity 6.4)

| # | Warning | File | Cách fix | Tránh lặp lại |
|---|---|---|---|---|
| 3 | `CS0618: Object.FindObjectOfType/FindFirstObjectByType is obsolete` | `GameManager.cs` | Dùng **`FindAnyObjectByType<T>()`** (không sorting, không deprecate) | Unity 6: KHÔNG dùng `FindObjectOfType` hay `FindFirstObjectByType` — chỉ `FindAnyObjectByType` |
| 4 | `CS0618: Rigidbody.velocity is obsolete` | `PlayerController.cs` | Dùng **`Rigidbody.linearVelocity`** | Unity 6: `velocity` → `linearVelocity` (API đổi tên) |

### Warning / quy trình đã xử lý

| # | Vấn đề | Cách xử lý | Tránh lặp lại |
|---|---|---|---|
| 5 | `git diff --check` báo trailing whitespace ở file `.meta` (Unity sinh) + vendor DOTween + `.slnx` | Bộ lọc khi chạy: bỏ qua `\.meta:`, `Demigiant`, `Roll a ball\.slnx`, dòng `LF will be replaced` | Trước commit, chạy diff-check kèm filter — chỉ fail khi lỗi nằm trong code C# của mình |

### Ghi chú kỹ thuật khác (tránh vấp lại)

- **Unity 6 (`6000.4.5f1`)** đã deprecate một loạt API cũ — trước khi dùng API nào, ưu tiên kiểm tra warning. Bảng nhanh: `FindObjectOfType` → `FindAnyObjectByType` · `Rigidbody.velocity` → `Rigidbody.linearVelocity` · `isKinematic` → `linearDamping`/`angularDamping` cũng có thể đổi tên trong 6.x (kiểm tra khi dùng).
- **DOTween KHÔNG có trên Unity registry / OpenUPM** — phải cài từ **Asset Store** (gói `com.demigiant.dotween` trả 404 trên OpenUPM). Sau khi cài bản mới ≥1.2.815: nếu lỗi xuất hiện → **restart Unity + Tools → Demigiant → DOTween Utility Panel → Setup**.
- **`write_file` thất bại khi ghi đè file có sẵn với CRLF** (ví dụ `PlayerController.cs` cũ) → dùng bash heredoc `cat > file << 'EOF'` để ghi đè an toàn.
- **Folder `_Project/` phải nằm TRONG `Assets/`** (`Assets/_Project`) — folder ngoài Assets không được Unity import. Lần đầu tạo nhầm ở root, đã sửa.
- **Hộp thoại "Script Updating Consent"** khi Unity nghi ngờ script dùng API cũ → **bấm "No"** (code đã viết theo API Unity 6 hiện tại, để Unity tự sửa dễ hỏng file).
- **Input System composite**: phím A/D là button (chỉ cho giá trị +1) — muốn có -1/+1 cho trái/phải **phải dùng composite** `2DVector` (`AddCompositeBinding("2DVector").With("Left"/"Right", ...)`).

---

## 🐞 PHỤ LỤC — Sổ Bugs UI/Visual (gộp từ BUGS.md — 2026-08-12)

> BUGS.md cũ đã gộp vào đây để 1 file duy nhất. Nội dung dưới là lịch sử UI/visual.

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

## ✅ Vòng 1 — ĐÃ FIX (UIOverhaulTool + CameraFollowFixTool)

| # | Lỗi | Fix | Trạng thái |
|---|---|---|---|
| G1 | ScoreText lệch x=42 | UIOverhaulTool căn giữa | 🔧 chờ user chạy |
| G2 | ScoreText trắng | UIOverhaulTool vàng glow + viền tím | 🔧 chờ user chạy |
| G3–G5 | Tông xanh dương lệch | UIOverhaulTool tông tím/cyan | 🔧 chờ user chạy |
| M1 | Title trùng 2 lớp | UIOverhaulTool: TitleGlow cyan mờ 35% | 🔧 chờ user chạy |
| M2–M4 | Nền/nút xanh, best score thấp | UIOverhaulTool tông tím + y=-230 | 🔧 chờ user chạy |
| **G7** | **🔴 Camera KHÔNG chạy theo bóng** — CinemachineCamera THIẾU `CinemachineFollow` (chỉ có RotationComposer = chỉ xoay nhìn, không di chuyển) → bóng chạy xa biến mất khỏi màn hình | `CameraFollowFixTool` thêm body component FollowOffset (0,7,-10) damping 0.5 | 🔧 chờ user chạy |
| **G8** | **2 AudioListener** trong MainMenu — AudioManager (đúng) + **Main Camera THỪA 1 cái** | `GameplayFixTool` xóa listener trên Main Camera (cả 2 scene) | 🔧 chờ user chạy |

## ✅ Vòng 2 — ĐÃ FIX (2026-08-11 — gameplay feel)

> Phát hiện khi user test thật: Void không xuất hiện, điểm số bị che, 2 bên trống → cảm giác đứng yên.

| # | Lỗi | Nguyên nhân gốc | Fix | Trạng thái |
|---|---|---|---|---|
| V2-1 | **🔴 KHÔNG thấy kẻ thù Void đuổi theo** | VoidChase dùng **NavMeshAgent** — track VÔ TẬN (tile recycle) → NavMesh bake chỉ phủ vùng cố định → player chạy xa là **NavMesh hết vùng, Void đứng yên** tụt sau màn hình vĩnh viễn | VoidChase bỏ NavMeshAgent → đuổi trực tiếp: giữ sau lưng player 9m co dần tới **1.5m** (Void áp sát + nuốt player cuối game) + safety net `swallowDistance 1.6` | 🔧 chờ user chạy tool + test |
| V2-2 | **Tile vô hình → mất cảm giác chuyển động** | Tile prefab scale **z=0** → khối cube dẹt không render → chỉ còn Ground tĩnh → nhìn như đứng yên | Tile.cs `Awake` ép `scale z=length` + thêm **LaneMarker neon** (2 vạch mép + vạch đứt giữa) trượt theo tile khi recycle | 🔧 chờ test |
| V2-3 | **2 bên đường trống trải** | props `sideOffset 11` nằm NGOÀI tầm camera (FOV 60 thấy ±8) → không bao giờ thấy props | sideOffset **7** + targetHeight **4.5** + countPerSide **14** + spacing 7.5 + FOV **68** + nền sáng `(0.1,0.06,0.2)` + light **0.8** | 🔧 chờ user chạy tool |
| V2-4 | **Điểm số bị che** | ScorePanel góc trái nằm DƯỚI các element khác (sibling order) + bị che bởi panel | ScorePanel đưa lên **giữa-đỉnh** (anchor 0.5,1) + `SetAsLastSibling` (vẽ trên cùng — không gì che được) | 🔧 chờ user chạy tool |

## ✅ Vòng 3 — REVIEW TOÀN DIỆN của user (2026-08-11) — ĐÃ FIX (vòng 4 bên dưới)

> User review toàn diện sau khi test thật → user duyệt plan → **đã code + commit + push (vòng 4)**.
> Chi tiết từng fix: `CHANGELOG.md` mục "THỰC THI GIAI ĐOẠN 2.5".

### 🔴 Gameplay (refactor lớn — cơ chế cốt lõi)

| # | Vấn đề user báo | Phân tích | Hướng fix đề xuất |
|---|---|---|---|
| R3-1 | **Player là "trái banh xanh" không hợp lý** với tên game Void Runner | Player hiện là sphere cyan (`Player.mat`), Rigidbody lăn. Tên game gợi "kẻ chạy" — banh không phù hợp chủ thể | ✅ **ĐÃ CHỐT: tàu vũ trụ nhỏ** — thân cube + cánh (primitive) hoặc model Kenney `craft_speederB` (đã có trong ambient), tông cyan, giữ Rigidbody nhưng bỏ xoay lăn |
| R3-2 | **Đường chạy 1 mức cố định rồi HẾT — không vô tận** | Track dựa trên TileSpawner pool recycle (đúng thiết kế vô tận) NHƯNG có `Ground` tĩnh 400m → khi player chạy quá 400m, hết nền → cảm giác "hết đường". Hoặc tile recycle có lỗi | Verify tile recycle thật (player chạy > 400m không hết). Nếu Ground tĩnh là giới hạn → bỏ/tách: nền phải vô hạn hoặc vô hình, track do tile quyết định |
| R3-3 | **Void đuổi theo là "banh tím", tốc độ tăng rất chậm, sẽ chạm player ở 1 mức điểm cố định** | VoidChase hiện giữ khoảng cách 9m→1.5m co dần theo thời gian (60s) — tức là "chạy đủ lâu là chết", không phản ánh skill người chơi | ✅ **ĐÃ CHỐT: 2 nấc cố định** (R0.4): nền 9m → đụng lần 1 → 5m → né sạch 10–15s → nới về 9m → đụng lần 2 trong cửa sổ → Game Over. Void không tự tăng tốc |
| R3-4 | **KHÔNG thấy màn hình kết thúc game** | UIManager có trong scene (grep thấy 1) + GameOverPanel có trong scene (1). Có thể GameOverPanel không hiện vì: player chết do Void nuốt nhưng event/panel không chạy, hoặc panel bị che, hoặc field chưa gán | Điều tra: (1) `GameEvents.RaiseGameOver` có được gọi khi Void nuốt không; (2) `UIManager.ShowGameOver` có chạy không; (3) GameOverPanel có bị che/bị ẩn. Fix cho panel luôn hiện khi GameOver |

### 🎨 UI / MainMenu

| # | Vấn đề user báo | Phân tích | Hướng fix đề xuất |
|---|---|---|---|
| R3-5 | **Tiếng Việt/Tiếng Anh lộn xộn** — cần thống nhất TIẾNG ANH trong gameplay | Game scene: `SCORE`, `GAME OVER`, `MENU` (EN) nhưng `CAO NHẤT`, `CHƠI LẠI`? (Việt). MainMenu: `VOID RUNNER`, `PLAY`, `HOW TO PLAY` (EN) + âm thanh (Việt) | **Toàn bộ text gameplay = TIẾNG ANH**: SCORE, COMBO, GAME OVER, RETRY, MENU, BEST, HIGH SCORE, SOUND: ON/OFF. MainMenu cũng tiếng Anh (đồng nhất) |
| R3-6 | **Nút âm thanh: text bị thụt vào trong, viền xanh bo tròn, quá chật** | SoundButton (MainMenu) — text `ÂM THANH: BẬT` bị thụt so với viền button (padding âm/quá nhỏ), layout chật | Fix layout nút: padding text hợp lý (không sát viền), size button đủ rộng, căn giữa. Kiểm tra RectTransform + padding |
| R3-7 | **Best score hiển thị ngay từ đầu (bằng 0) — vô nghĩa** | MainMenuManager.RefreshBestScore luôn set text `ĐIỂM CAO NHẤT: 0` | Chỉ hiển thị best score khi `SaveSystem.BestScore > 0` (đã chơi và có điểm). Lần đầu chơi → ẩn text hoặc hiện placeholder |
| R3-8 | *(đi kèm)* Game Over panel có thể chưa hiện được đúng (liên quan R3-4) | — | Test toàn bộ luồng chết → panel → retry/menu sau khi fix R3-4 |

## ✅ Vòng 4 — ĐÃ FIX (2026-08-11 — thực thi Giai đoạn 2.5, user đã duyệt plan)

> User duyệt toàn bộ docs → code theo R0.1–R0.8. Code xong + commit + push.
> ⚠️ User còn phải CHẠY TOOL `Tools → Void Runner → Refactor: Both Scenes` + Ctrl+S để scene áp dụng
> (Ground 6000m, English texts, SoundButton layout) rồi test tay.

| # | Vấn đề | Fix | Trạng thái |
|---|---|---|---|
| R3-1 | Player = banh xanh | `PlayerController.BuildSpaceship()` — tàu vũ trụ primitive (Body/WingL/WingR/Cockpit/Engine) + neon cyan code, tắt banh cũ, banking đổi lane | ✅ code xong — chờ test |
| R3-2 | Đường chạy hết (Ground 400m) | `RefactorGameplayTool` kéo Ground 400m → **6000m** | 🔧 chờ user chạy tool |
| R3-3 | Void "banh tím tự tăng tốc" → chết ở mức điểm cố định | `VoidChase` viết lại **2 nấc cố định** (9m → 5m khi đụng, nới về 9m sau 12s sạch, đụng lần 2 trong cửa sổ = Game Over); bỏ co dần 60s | ✅ code xong + 5 PlayMode test |
| R3-4 | KHÔNG thấy Game Over panel | Nguyên nhân gốc: trước đây Void không bao giờ bắt kịp (bug NavMesh/camera) nên không có game over. `UIManager.ShowGameOver` bỏ early-return khi ScoreSystem null + `_panelGroup` setup sớm → panel luôn hiện | ✅ code xong |
| R3-5 | Việt/Anh lộn xộn | UIManager/MainMenuManager text English (SCORE/BEST/SOUND ON-OFF) + tool đổi text scene (RETRY/SCORE: 0/BEST: 0/HowToPlay English) | ✅ code + 🔧 chờ user chạy tool |
| R3-6 | Nút âm thanh thụt viền, chật | `RefactorGameplayTool` SoundButton 300×66 → 340×76, text stretch + padding 18/6px, font 32 NoWrap | 🔧 chờ user chạy tool |
| R3-7 | Best score = 0 hiển thị vô nghĩa | `MainMenuManager.RefreshBestScore` ẩn text khi `BestScore <= 0` | ✅ code xong |

> 📌 **Việc còn lại của user:** chạy tool `Refactor: Both Scenes` (2 scene) → Ctrl+S → test theo `TESTING.md` V1–V11.

---

## 🆕 Vòng 5 — User test tay (2026-08-11): 6 vấn đề

| # | Vấn đề | Trạng thái |
|---|---|---|
| V5-1 | **Tàu vũ trụ lật lên xuống liên tục** — sphere lăn trên Ground → root xoay → tàu lật | ✅ Fix: `FreezeRotation` + zero angularVelocity |
| V5-2 | **Đè A/D không đổi lane liên tục** (bấm 1 lần qua 1 lane) | ✅ Fix: InputReader poll trong Update + repeat 0.12s |
| V5-3 | **Điểm quá to vỡ khung chứa điểm** (font 58 > panel 300) | ✅ Fix tool: panel 360x90 + font 40 |
| V5-4 | **Text "x2" (combo) che nửa góc trái** — ComboText anchor (0,1)@(34,-150) con Canvas | ✅ Fix tool: xuống dưới điểm (0.5,1)@(0,-110) |
| V5-5 | **KHÔNG có vật cản + xu** — wiring scene đúng 100% nhưng không spawn | 🔍 Đang chẩn đoán: đã thêm log `[DiagSpawn]`/`[DiagObstacle]`/`[DiagCoin]`, user chơi 15s gửi log |
| V5-6 | **2 hàng cảnh vật trái/phải** (ambient) — lỗi tái diễn nhiều lần | ⏳ **Chờ user REVIEW code** (`AmbientScroller.cs` + `AmbientSetupTool.cs` + config scene) trước khi cho phép sửa |
