# 📜 RULES — Quy tắc bất biến (trích xuất từ CHANGELOG + BUGS)

> **Mục đích:** tổng hợp MỌI quy tắc/bài học đã đúc kết từ các lần fix lỗi (bug chồng bug —
> ghi đầy đủ, không lọc bớt). Trước khi viết/commit code, đọc file này để KHÔNG tái phạm.
> Nguồn: `CHANGELOG.md` (bài học) + `BUGS.md` (lỗi UI/visual).
> Nếu phát hiện bug mới → ghi vào CHANGELOG/BUGS rồi thêm rule vào đây.

---

## 🎯 NHÓM 0 — ĐỊNH HƯỚNG GAME (quyết định thiết kế — 2026-08-11)

> Các rule này là kết quả review của user, được chốt thành định hướng bất biến:

- **R0.1 — Player = TÀU VŨ TRỤ NHỎ (spaceship) — ĐÃ CHỐT 2026-08-11.** Không phải trái banh (tên game gợi "kẻ chạy trốn khỏi Void"). Tàu bay lơ lửng trên đường, tông cyan neon nổi trên nền tối, đổi lane mượt. Dựng bằng primitive (thân cube + cánh) hoặc model Kenney space-kit — không cần model nhân vật phức tạp.
- **R0.2 — Void là KẺ ĐUỔI THEO ĐÍCH THỰC, không phải "banh tím trôi sau lưng".** Void = khối bóng tối (hư không) phình to, xuất hiện áp sát khi player chạm vật cản.
- **R0.3 — Track PHẢI vô tận thật sự** (TileSpawner pool recycle). Nếu đường chạy hết → bug hệ thống (Ground tĩnh 400m không được là giới hạn — chỉ là nền, không phải track).
- **R0.4 — Cơ chế chết kiểu Subway Surfers / Temple Run — 2 NẤC CỐ ĐỊNH (ĐÃ CHỐT 2026-08-11):**
  - Void giữ khoảng cách nền **9m** sau player (trong tầm camera offset -10 → nhìn thấy).
  - **NẤC 1**: player đụng vật cản → Void tiến sát còn **5m** (vẫn chưa chết).
  - **Nới lại**: player né sạch **10–15s** không đụng nữa → Void nới dần về **9m** (reset về nấc 0).
  - **CHẾT**: player đụng lần 2 TRONG CỬA SỔ 10–15s (khi Void đang ở nấc 5m) → Void nuốt → Game Over.
  - Mọi vật cản chỉ khiến Void tiến gần — KHÔNG có cơ chế chết do obstacle trực tiếp.
  - Void KHÔNG tự tăng tốc theo thời gian (bỏ cơ chế co dần 60s cũ — gây chết ở mức điểm cố định).
- **R0.5 — Toàn bộ text trong gameplay = TIẾNG ANH** (SCORE, COMBO, GAME OVER, RETRY, MENU, BEST...). Không lộn xộn Việt/Anh trong scene Game.
- **R0.6 — MainMenu: Best score chỉ hiển thị khi có dữ liệu thật (BestScore > 0).** Lần đầu chơi = 0 → ẩn (hiển thị vô nghĩa). Sau khi chơi và có điểm → mới hiện.
- **R0.7 — Game Over panel BẮT BUỘC hiện khi game kết thúc.** Nếu user không thấy màn hình game over → bug nghiêm trọng, ưu tiên fix trước.
- **R0.8 — UI nút phải có padding đủ — text không bị thụt vào viền / quá chật.** Layout phải thoáng, đọc rõ.

---

## 🛠️ NHÓM 1 — Unity 6 API (KHÔNG tái phạm)

- **R1.1** — `Object.FindObjectOfType` / `FindFirstObjectByType` → **obsolete (CS0618)**, dùng **`FindAnyObjectByType<T>()`** cho MỌI script mới (kể cả Editor tool). *(Tái phạm 2 lần — tuyệt đối kiểm tra trước commit.)*
- **R1.2** — `Rigidbody.velocity` → **`Rigidbody.linearVelocity`** (Unity 6 đổi tên).
- **R1.3** — `Rigidbody.isKinematic` → có thể đổi tên trong 6.x — kiểm tra khi dùng.
- **R1.4** — `TMP_Text.enableWordWrapping` → **`textWrappingMode`** (`TextWrappingModes.Normal`/`NoWrap`).
- **R1.5** — Input System: phím A/D là button (chỉ +1) — muốn -1/+1 phải dùng **`AddCompositeBinding("2DVector")`** với Left/Right.
- **R1.6** — `InputAction` constructor KHÔNG có named parameter `expectedControlLayout` — kiểm tra signature thật của package đã cài.
- **R1.7** — `Time.timeScale` là global state — mọi nơi đụng đều phải restore đầy đủ (EndPowerUp, ResetAll, OnDisable). Quên restore → game chậm vĩnh viễn.

## 🧩 NHÓM 2 — asmdef / Assembly (quy tắc cứng Unity 6)

- **R2.1** — Custom asmdef **KHÔNG THỂ reference `Assembly-CSharp` (predefined)** kể cả `overrideReferences: true` → code chính phải nằm trong **asmdef THẬT** (`VoidRunner.Core.asmdef`).
- **R2.2** — Khi tạo asmdef phải liệt kê **references tường minh** (`Unity.TextMeshPro`, `Unity.InputSystem`, `Unity.Cinemachine`) — thiếu → `CS0246` hàng loạt.
- **R2.3** — Source `.cs` trong `Assets/Plugins/` compile vào `Assembly-CSharp-firstpass` → custom asmdef không reference được → cần asmdef riêng trong thư mục (`DOTween.Modules.asmdef`, `autoReferenced: true`).
- **R2.4** — Editor tools vẫn hoạt động vì `Assembly-CSharp-Editor` TỰ ĐỘNG reference mọi asmdef `autoReferenced: true`.

## 🧰 NHÓM 3 — Editor Tool (C# & Unity Editor)

- **R3.1** — **`mọi ScriptableObject con tạo bằng code (texture/material/VolumeComponent) đều phải `AssetDatabase.AddObjectToAsset`** — thiếu → file ghi `{fileID: 0}` → mở lại Unity MẤT hiệu ứng âm thầm (font rỗng, post-processing không chạy). Check: `GetAssetPath(sub) == GetAssetPath(parent)` guard chống add trùng.
- **R3.2** — `SceneManager` (UnityEngine.SceneManagement) vs `EditorSceneManager` (UnityEditor.SceneManagement) — **KHÔNG using cả 2** (ambiguous CS0104); dùng fully-qualified `UnityEngine.SceneManagement.SceneManager.GetActiveScene()`.
- **R3.3** — Namespace lồng của package: `BindingMode`/`AngularDampingMode`/`TrackerSettings` nằm ở **`Unity.Cinemachine.TargetTracking`**, không phải `Unity.Cinemachine` — trước khi viết tool grep namespace thật trong `Library/PackageCache`.
- **R3.4** — `GlyphRenderMode` ở **`UnityEngine.TextCore.LowLevel`** (không phải TMPro) — `using UnityEngine.TextCore.LowLevel;`.
- **R3.5** — `File.Exists` cần `using System.IO;`.
- **R3.6** — Compile error trong Editor tool → menu `Tools/Void Runner` không hiện mục mới + **Unity vào SAFE MODE** → thoát bằng nút "Exit Safe Mode" sau khi fix.
- **R3.7** — `Image` không tự convert sang `GameObject` — phải `.gameObject` khi gán field kiểu GameObject.
- **R3.8** — Editor script dùng class runtime phải `using` đúng namespace (`VoidRunner.UI`...).
- **R3.9** — Bỏ tham số method không dùng (dead parameter) — reviewer bắt.
- **R3.10** — **Unity BCL KHÔNG có `Regex.Replace(string, string, string, int count)`** — chỉ 3-arg hoặc variant `RegexOptions` (lỗi `CS1503: Argument 3: cannot convert from 'string' to 'int'`). Muốn giới hạn số lần replace: bỏ count (pattern xuất hiện 1 lần thì 3-arg vẫn ổn) hoặc **dùng string ops** (`IndexOf`/`Substring`/`Replace`). `.meta` luôn có đúng 1 dòng `guid:` → thao tác GUID trong Editor tool dùng IndexOf+Substring, không cần Regex. *(Bug 2026-08-11.)*

## 🎬 NHÓM 4 — Gameplay / World (bài học về game feel)

- **R4.1** — **Endless runner KHÔNG dùng NavMeshAgent cho kẻ thù đuổi theo** — track vô tận (tile recycle) không có NavMesh bake phủ hết → Void đứng yên khi hết vùng → "kẻ thù biến mất". Kẻ thù đuổi TRỰC TIẾP (giữ khoảng cách / theo tốc độ).
- **R4.2** — **Tile prefab scale z=0 = bẫy vô hình** → "cảm giác đứng yên / đường không chuyển động". Kiểm tra scale tile + **lane marker** (vạch neon trượt) — yếu tố quan trọng nhất tạo cảm giác tốc độ.
- **R4.3** — Props ngoài tầm FOV = vô hình — `sideOffset` phải tính theo FOV thật (FOV 68 thấy ±~9 ở cự ly camera→player).
- **R4.4** — Cơ chế chết kiểu "kẻ thù nuốt" phải có **safety net** (kiểm tra khoảng cách trực tiếp < ngưỡng → GameOver), không chỉ dựa collider overlap (đổi lane có thể không bao giờ chạm).
- **R4.5** — Khởi tạo giá trị trong `Awake`, không phải `Start` (tránh đọc giá trị cũ frame đầu).
- **R4.6** — `ResetRamp` phải reset cả giá trị hiện tại + phát event (tránh obstacle dày bất thường sau restart).
- **R4.7** — Static event subscribe/unsubscribe CÂN BẰNG (OnEnable/OnDisable) — mọi subscriber phải kiểm tra.
- **R4.8** — Đo score theo `deltaZ` thực tế (không `speed*dt`) — độc lập với DifficultyManager.
- **R4.9** — `Camera.main` mỗi lần gọi = FindGameObjectWithTag — cache trong Start.
- **R4.10** — Không `FindObjectsByType` mỗi frame → dùng **static registry** (`Coin.Active`).
- **R4.11** — Popup pool phải **kill tween cũ trước khi tái sử dụng** + nhân theo combo (`Multiplier`).
- **R4.12** — TrailRenderer + URP/Unlit hiện TRẮNG (shader không sample vertex color) → dùng `Particles/Unlit`.
- **R4.13** — `ParticleSystem.Emit()` bypass emission module → bỏ dead config.
- **R4.14** — `[UnityTest] IEnumerator` bắt buộc có ≥1 `yield return` (CS0161).

## 🎨 NHÓM 5 — UI / TMP / Visual

- **R5.1** — UI bị che thường do **sibling order** — element vẽ sau (SetAsLastSibling) nằm trên; ép lên vẽ cuối khi bị che không rõ nguyên nhân.
- **R5.2** — Không dùng ký tự icon ngoài bảng glyph của font TMP (▶ → □ + warning). Dùng ký tự có sẵn (`>`, `»`) hoặc font đủ glyph.
- **R5.3** — GameObject ẩn sẵn (`ComboText`, `GameOverPanel`) phải có `m_IsActive: 0` — quên tắt → hiện ngay từ đầu game.
- **R5.4** — Font TMP phải có `m_AtlasTextures` fileID ≠ 0 trên đĩa (kiểm tra sau khi tạo bằng code).
- **R5.5** — CanvasScaler 1920×1080 Match 0.5; EventSystem phải dùng **Input System UI module** (project dùng Input System).
- **R5.6** — Nút phải có component `Button` thật (Text-TMP thuần không phải button — object picker không hiện).
- **R5.7** — `AudioListener` chỉ 1 per scene — nếu gắn AudioManager (DontDestroyOnLoad) có RequireComponent(AudioListener) phải xóa listener trên Main Camera.
- **R5.8** — **TMP font atlas phải đủ lớn cho TOÀN BỘ ký tự dùng** — 1024² + sampling 128 chỉ chứa ~40 glyph (thiếu chữ thường, 'x', '2' → text hiện "lạ/vỡ" qua fallback). Dùng **2048²** (`CreateFontAsset(font, 128, 9, SDFAA, 2048, 2048)`). Kiểm tra nhanh: `grep 'm_Unicode:' <font>.asset` — đếm chữ thường (97-122) / digit (48-57) / hoa (65-90). *(Bug 2026-08-11: combo "x2" thành "H2".)*
- **R5.9** — **Editor tool regenerate asset (DeleteAsset+CreateAsset) sinh GUID MỚI → gãy mọi tham chiếu scene âm thầm** — phải lưu guid cũ (đọc `.meta`) trước khi xóa + restore sau khi tạo lại (`Regex` thay `guid: [0-9a-f]{32}` + `AssetDatabase.ImportAsset(path, ForceUpdate)`). *(Bug 2026-08-11: font.)*

## ⚙️ NHÓM 6 — Workflow Unity / Git (thủ tục bất biến)

- **R6.1** — File .cs mới khi Unity đang mở **chưa có .meta** → commit code trước, chờ Unity sinh .meta, commit .meta sau (KHÔNG tự tay gõ GUID).
- **R6.2** — File scene/prefab đang mở trong Unity thì đĩa chưa cập nhật — phải **Ctrl+S** rồi mới grep/commit.
- **R6.3** — Trailing whitespace file Unity tự sinh (`.meta`, scene) → **loại trừ khỏi `git diff --check`**, không sửa file hệ thống.
- **R6.4** — `git diff --check` báo lỗi ở vendor (DOTween), `.slnx` → bỏ qua, chỉ fail khi code C# của mình.
- **R6.5** — Safe mode KHÔNG xóa log cũ — kiểm tra thật: `grep 'error CS' Editor.log` = 0 + có dòng compile chạy → an toàn.
- **R6.6** — Lỗi CS cũ không tự biến mất khỏi `Editor.log` — so vị trí lỗi với dòng `Tundra build success` cuối (lỗi SAU success mới là thật).
- **R6.7** — Scene `Minigame` + `NavMesh-Ground.asset` → archive, không nằm trong Build Settings.
- **R6.8** — Prefab/scene luôn commit kèm code liên quan trong CÙNG commit — không commit .meta riêng lẻ.
- **R6.9** — Asset đang mở trong Unity thì đĩa chưa cập nhật (Ctrl+S trước khi commit).
- **R6.10** — Test SaveSystem phải xóa PlayerPrefs trong `[SetUp]` TRƯỚC (không chỉ TearDown SAU).
- **R6.11** — Warning `Assembly ... not valid. Loading skipped` khi mở lại Unity = vô hại (DLL cũ) → Clear Console.
- **R6.12** — **GIT: KHÔNG chạy nhiều `git commit` song song (spawn_agents parallel)** — tranh chấp `.git/index.lock` (`fatal: Unable to create index.lock`) + `git add` của tiến trình này có thể bị `git commit` của tiến trình khác cuốn vào (commit dính file lạ). Luôn chạy git tuần tự — 1 lệnh git/lần spawn. *(Tái phạm được 2026-08-11.)*
- **R6.13** — **`MonoBehaviour.enabled = false` TRONG TEST gọi `OnDisable()` ĐỒNG BỘ** — nếu singleton có `OnDisable` set `Instance = null`, test sẽ mất Instance ngay. Muốn "có Instance nhưng Start không chạy": disable xong khôi phục `Instance` (reflection `<Instance>k__BackingField`, `BindingFlags.NonPublic|Static`) + State qua `GetSetMethod(true)`. *(Gặp khi fix 4 test VoidChase 2026-08-11.)*

## 📝 NHÓM 7 — Commit / Docs

- **R7.1** — Commit convention: `<type>(<scope>): <subject>` — subject tiếng Việt CÓ DẤU, không viết hoa đầu, không dấu chấm cuối. (Xem `COMMIT_TEMPLATES.md`.)
- **R7.2** — Mỗi lần fix lỗi → ghi CHANGELOG (nguyên nhân + cách fix + cách tránh lặp) TRƯỚC khi commit.
- **R7.3** — Bug mới → ghi BUGS.md; quyết định thiết kế → RULES.md + plan.
- **R7.4** — Review README sau mỗi thay đổi ảnh hưởng hành vi người dùng / setup / kiến trúc.
- **R7.5** — Trước khi refactor lớn: cập nhật docs trước, user duyệt → mới code.

---

*File này là "cấm kỵ" khi code — đọc trước khi viết bất kỳ script nào. Bổ sung rule mới khi học được bài học mới.*
