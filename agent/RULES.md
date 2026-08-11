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
- **R0.9 — Panel popup/overlay PHẢI ĐỤC HOÀN TOÀN (alpha = 1.0), không chỉ "gần đục".** Alpha 0.92 vẫn để element menu nằm trong vùng panel (tọa độ nằm trong sizeDelta) lộ xuyên qua → "fix rồi mà vẫn khó đọc". Khi mở popup: ép alpha=1 + dimmer ≥0.8 + `SetAsLastSibling`. Kiểm tra: element menu nào có anchoredPosition nằm trong vùng panel? → che kín hoặc di chuyển. *(Bug vòng 7 2026-08-11.)*
- **R0.10 — Road width (roadHalfWidth) là hằng số ĐỒNG BỘ TOÀN CỤC — sửa phải quét MỌI chỗ hardcode:** `Tile.roadHalfWidth`, `AmbientScroller` (2 const: HealProp + BuildProps), scene `Ground scale x`, Editor tool `RefactorGameplayTool` (Ground + ambient `sideOffset`), `laneWidth` (Player/Obstacle/Pickup). Bỏ sót 1 chỗ (đặc biệt tool Editor hardcode giá trị CŨ) → chạy lại tool "phá" road mới, props đè road tái phát. *(Bug vòng 7 2026-08-11: road 14 → 18.)*
- **R0.11 — Di chuyển hyper-casual chuẩn: CẠNH LÊN = nhảy 1 lane tức thì, ĐÈ GIỮ = sweep liên tục, NHẢ = snap về lane gần nhất.** Cần phát hiện rising edge (so `_lastInputX` frame trước) chứ không chỉ trạng thái giữ; `_currentLane` phải đồng bộ NGAY ở nhánh edge (tránh stale cho MoveLeft/Right/test). Chỉ sweep (như cũ) = bấm-nhả nhanh gần như không đi → cảm giác "phải bấm 2 lần". *(Bug vòng 7 2026-08-11.)*
- **R0.12 — Camera follow KHÔNG được bám trục X khi player đổi lane** — endless runner 3-lane: camera phải đứng GIỮA ĐƯỜNG (khóa X=0, chỉ bám Z/Y) qua **CameraRig trung gian** (`CameraRig.cs`: LateUpdate ép position x=0; GameManager gán `cam.Follow = rig.transform`). Camera bám thẳng player → đổi lane = CẢNH VẬT trôi theo (tàu gần như đứng giữa) — mất cảm giác rẽ + khó căn lane. *(Bug vòng 8 2026-08-11.)*
- **R0.13 — Popup/feedback điểm ("+N") KHÔNG đặt tại vị trí world của coin** (WorldToScreenPoint) — chữ nằm trên đường che obstacle/coin → không né kịp. Đặt vị trí CỐ ĐỊNH ngoài vùng gameplay: cạnh HUD, và phải KIỂM TRA sizeDelta panel HUD (panel trải ±180 → offset <180 là đè panel). *(Bug vòng 8 2026-08-11.)*
- **R0.14 — Popup/overlay bật/tắt PHẢI có nút đóng rõ ràng (CLOSE/X), không chỉ click ra ngoài (dimmer)** — user không biết click đâu. Nút tạo bằng code idempotent (`transform.Find` trước khi tạo). *(Bug vòng 8 2026-08-11.)*
- **R0.15 — Lane width và vạch chia lane phải KHỚP nhau** — laneWidth 4.5 → vạch đứt chia lane ở ±laneWidth/2 (±2.25, ranh giới lane thật), không phải 1 vạch giữa x=0 khi road đã rộng. Đồng bộ: laneWidth scene ×3 (Player/Obstacle/Pickup) + Tile.laneWidth. *(Bug vòng 8 2026-08-11.)*

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
- **R3.11** — **Unity 6 KHÔNG còn `FontImporter`/`FontImporterCharacterSet`/`characterSet`** (CS0246/CS0103) — thay bằng **`UnityEditor.TrueTypeFontImporter.fontTextureCase`** (enum `FontTextureCase`: Dynamic/Unicode/ASCII/ASCIIUpperCase/ASCIILowerCase/CustomSet — **không có ASCIIPrintableSet**, dùng `Unicode` cho đủ Latin). Cách biết class thật: đọc `ttf.meta` — dòng `TrueTypeFontImporter:` chính là tên class. *(Bug 2026-08-11: 5 lỗi đỏ.)*
- **R3.12** — **TMP `TryAddCharacters` chỉ có overload `string` và `uint[]`** (KHÔNG có `IEnumerable<char>`/`out bool` — CS1503/CS1615). Source TMP Unity 6 nằm ở `Library/PackageCache/com.unity.ugui@*/Runtime/TMP/TMP_FontAsset.cs` (tên file có tiền tố TMP_, không phải FontAsset.cs). Trước khi dùng API lạ: grep source thật trong PackageCache hoặc `grep -a` trên `UnityEditor.dll` (namespace lưu rời trong metadata — grep đơn giản, không grep chuỗi dài). *(Bug 2026-08-11.)*

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
- **R4.15** — **KHÔNG scale ROOT của container chứa con đặt vị trí (tile/chunk/spawner)** — Unity nhân scale parent vào CẢ vị trí lẫn kích thước con (`world = parentScale × local`) → obstacle/coin bay ra ngoài đường + dẹt vô hình. Container scale = (1,1,1); muốn to/nhỏ thì scale CHILD visual. Dấu hiệu: "log spawn ĐÃ TẠO nhưng không thấy gì" → nghi scale parent. *(Bug 3 tuần 2026-08-11.)*
- **R4.17** — **Component xoay (Rotator) CHỈ gắn lên visual cần xoay (coin, obstacle) — KHÔNG gắn lên container/manager/cha có con mang vị trí world** (Managers, TileSpawner, tile) → xoay cả cây con → obstacle/coin văng X/Y lung tung dù localPosition đúng + track recycle sai (`tile=2`). Dấu hiệu: `eulerAngles` của container quay vòng theo thời gian. **Khi user nói "đã thêm X vào Y" → VERIFY trong file scene/prefab (grep GUID component, đọc block m_GameObject) — component có thể bị kéo nhầm vào object khác mà user không biết.** *(Bug 2026-08-11: Rotator trên Managers — root cause cuối của bug 3 tuần.)*
- **R4.18** — **Props/objects được Editor tool dựng cứng trong scene KHÔNG tự cập nhật khi code spawn thay đổi** — sửa logic build/spawn phải (a) chạy lại tool, HOẶC (b) **self-heal runtime** (Start nạp các con có sẵn vào pool + ép lại scale/vị trí chuẩn). Self-heal tốt hơn — không phụ thuộc user chạy tool. *(Bug 2026-08-11: props đè road + hết props sau 105m.)*
- **R4.19** — **Manager gọi `Initialize` của hệ thống con phải gọi ĐỦ tất cả** — quên 1 (vd GameManager gọi `tileSpawner.Initialize` nhưng quên `ambient.Initialize`) → hệ thống đó chết âm thầm (không lỗi, chỉ hoạt động sai một nửa — recycle không chạy). Khi hệ thống "spawn đúng nhưng phần sau không hoạt động", check Initialize/Start có chạy ở runtime không.
- **R4.20** — **Visual feedback va chạm phải đặt trên CHÍNH player** (blink/flash renderer) chứ không chỉ screen shake — người chơi cần thấy ngay "mình bị đụng". Coroutine blink phải ép hiện lại renderer ở `HandleGameOver`/`HandleRestart` (chết/restart giữa lúc blink → tàu mất tích). *(Fix 2026-08-11.)*
- **R4.21** — **2 hệ thống spawn trên cùng tile phải chia sẻ trạng thái lane** — spawn sau (coin/powerup) phải đọc lane bị chặn của spawn trước (obstacle) qua shared state (`ObstacleManager.BlockedLanes`), KHÔNG chọn random độc lập. Và đường "không spawn" (safe zone) phải clear state thủ công (`ClearBlockedLanes`) nếu không gọi TrySpawn — nếu không, spawn sau đọc stale state của tile trước. *(Bug 2026-08-11: obstacle đè coin.)*
- **R4.22** — **Teleport player khi restart set CẢ `transform.position` + `_rb.position`** — chỉ set 1 trong 2 có thể không chắc (thứ tự event/rigidbody sync) → vị trí bắt đầu khác nhau mỗi lần chơi. Orchestrator (GameManager) gọi `player.ResetToStart()` TRỰC TIẾP TRƯỚC khi `RaiseRestart` — không phụ thuộc thứ tự subscriber event. *(Bug 2026-08-11: playerZ=148.9 sau restart.)*
- **R4.23** — **Dimmer/overlay chặn click PHẢI có cơ chế đóng riêng** — raycastTarget=true chặn nút phía sau → user kẹt nếu popup không có nút close. Chuẩn UX: dimmer là Button, click vùng tối = đóng. *(Bug 2026-08-11: HowToPlay không đóng được.)*
- **R4.24** — **Safe zone đầu game (không obstacle ~20m) bắt buộc cho endless runner** — obstacle spawn ngay tile đầu = chết tức thì khi bắt đầu (bất công). *(Bug 2026-08-11.)*
- **R4.16** — **OnTriggerEnter chỉ fire khi ≥1 collider là trigger** — obstacle phải `IsTrigger:1` (+ bỏ gravity/kinematic) để player solid sphere detect được; solid-solid = OnCollisionEnter (không chạy code OnTriggerEnter). **Collider `m_Size` phải khớp mesh** (Ramp mesh 2×0.5×2 → collider size 2×0.5×2). *(Fix 2026-08-11.)*

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
- **R5.10** — **TTF importer `characterSet = Dynamic` (mặc định, .meta không có field) = BẪY khi tạo TMP font bằng code** — Unity chỉ extract ký tự ĐANG ĐƯỢC DÙNG trong scene → `CreateFontAsset` sinh font vài glyph (dù atlas 2048). Khi font mới thiếu glyph hàng loạt: kiểm tra `ttf.meta`, ép `FontImporter.characterSet = ASCIIPrintableSet` + `SaveAndReimport()` TRƯỚC khi tạo + `TryAddCharacters(32..126)`. *(Bug 2026-08-11: combo "x2" thành "HS".)*
- **R5.11** — **Label + value trong cùng panel: value stretch full (anchor 0..1/0..1) = dính label** — tách: label anchor đỉnh (0.5,1 @ y=-4, font nhỏ bold), value nửa dưới panel (y 0..0.72). *(Fix 2026-08-11.)*
- **R5.12** — **Road widen phải đồng bộ 4 chỗ** (thiếu 1 chỗ = obstacle lệch khỏi lane / prop nằm trên đường): Ground scale x, `roadHalfWidth` (Tile), `laneWidth` (Player/Obstacle/Pickup), ambient `sideOffset`. *(Fix 2026-08-11.)*
- **R5.13** — **Đổi serialized default → test hardcode fail âm thầm** — test hardcode giá trị (vd `laneWidth=2` trong PlayerControllerPlayTests) → giữ default code, muốn đổi set qua scene/tool.

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
