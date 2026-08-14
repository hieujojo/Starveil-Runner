# 📜 RULES — Quy tắc kỹ thuật bất biến (trích xuất từ CHANGELOG + BUGS)

> **Mục đích:** tổng hợp MỌI quy tắc/bài học kỹ thuật (API, asmdef, editor tool, workflow git).
> Trước khi viết/commit code, đọc file này để KHÔNG tái phạm.
> Nguồn: `CHANGELOG.md` (bài học — BUGS.md cũ đã gộp vào đó 2026-08-12).
> ⚠️ Quyết định THIẾT KẾ game → `DECISIONS.md` (tách ra 2026-08-12).
> Nếu phát hiện bug mới → ghi vào CHANGELOG rồi thêm rule vào đây.

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
- **R3.13** — **`using X.Y.Z` phải khớp namespace THẬT của file đích, KHÔNG khớp thư mục.** Thư mục `Scripts/UI/Screens/` chứa file khai `namespace VoidRunner.UI` (thư mục chỉ là tổ chức vật lý) → `using VoidRunner.UI.Screens;` trong Editor tool = **CS0234 → safe mode**. Trước khi viết `using` cho class lạ: `grep -n '^namespace' <file đích>` xác minh. Lỗi CS0234/CS0246 ở dòng `using` → nghi sai namespace trước tiên. *(Bug 2026-08-11: ShipSelectSetupTool.)*
- **R3.14** — **Khi xóa 1 script (class), PHẢI quét Editor tools tham chiếu nó TRƯỚC** (`grep -rln '<TênClass>' Assets/_Project/Editor/`) — tool tham chiếu class đã xóa = **CS0246 compile fail toàn project**. Nếu tool chỉ phục vụ đúng thứ đã xóa → xóa luôn tool. *(Bug 2026-08-11: xóa AmbientScroller.cs → AmbientSetupTool vỡ; 2026-08-12: RefactorGameplayTool vẫn gọi AmbientScroller → phải sửa.)*
- **R3.15** — **Editor tool PHẢI idempotent (chạy lại an toàn nhiều lần) — đây là tiêu chí GIỮ tool.** Tool one-shot đã hoàn thành hoặc bị tool khác thay thế (vd HUDUIBuilder/MainMenuUIBuilder tông blue cũ bị UIOverhaulTool thay thế) → XÓA, tránh menu Tools mọc loạn + khó bảo trì. Khi xóa tool: kiểm tra không file khác gọi nó (chỉ comment thì OK), giữ helper dùng chung nếu tool khác còn dùng. *(2026-08-12.)*
- **R3.16** — **Model 3rd-party (FBX) import thường dùng shader Built-in Standard → trong URP hiện MÀU TÍM/MAGENTA** (shader không compile — không phải lỗi màu, không phải logic). Fix: quét renderer → convert material sang `URP/Lit` giữ màu gốc (`MaterialFixer` — cache static). Kiểm tra material: `grep 'm_Shader:' *.mat` — fileID 45/46 guid 000... = Standard Built-in. ⚠️ **Áp dụng cho MỌI model 3rd-party (tàu/obstacle/coin/monster), không chỉ tàu** — chống tái phạm: đặt self-heal `MaterialFixer.EnsureURPMaterials(gameObject)` ngay trong `Awake()` của component gốc (Obstacle/Coin...) thay vì nhớ gọi từng nơi instantiate. *(Tái phạm 2026-08-12: sót OBSTACLE — user bắt bài "tưởng tuân rule rồi".)*
- **R3.17** — **Rename class Unity: `git mv` GIỮ `.meta` = GUID KHÔNG đổi → scene/prefab vẫn tham chiếu đúng script** (không cần sửa m_Script guid). Sau rename phải đồng bộ: tên file, tên class, `m_EditorClassIdentifier` trong scene, mọi reference code, tên GameObject, test, docs. KHÔNG tự gõ GUID mới (R6.1). *(2026-08-12: VoidChase → EnemyChase.)*
- **R3.18** — **KHÔNG thêm attribute `[Tooltip]`/`[Header]`/`[SerializeField]` mới vào field ĐÃ có attribute tương tự — các attribute này KHÔNG có `AllowMultiple` → CS0579 Duplicate** (compile fail toàn project). Khi sửa field có sẵn attribute: gộp vào 1 attribute duy nhất (`[SerializeField, Tooltip("...")]`), xóa attribute cũ. Verify: grep tên field đếm số `[Tooltip` gần đó. *(Bug 2026-08-12: shipTargetHeight 2 Tooltip.)*

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
- **R5.14** — **Popup/dimmer: dimmer phải `SetAsLastSibling` TRƯỚC panel** (dimmer che menu, panel che dimmer). `SetAsFirstSibling` chìm dimmer DƯỚI menu → menu không bị tối → "popup vẫn lộ STARVEIL RUNNER phía sau" (bug tưởng là alpha không đủ, thực ra là sibling order). Kiểm tra: dimmer có nằm TRÊN mọi element menu không.
- **R5.15** — **Dimmer dùng chung cho nhiều popup → onClick KHÔNG hardcode 1 popup** — phải đóng popup đang mở (`CloseActivePopup`). Click dimmer khi Credits mở mà gọi `ToggleHowToPlay` = BẬT HowToPlay thay vì đóng Credits (bug ẩn 2026-08-12).
- **R5.16** — **Font chỉ pack ASCII 32..126 (Kenney Future) → không dùng ký tự Unicode (✕ U+2715, ▶) — ra ô vuông □.** Dùng chữ `X` cho nút đóng.
- **R5.17** — **Nút đóng popup = dấu X NHỎ (40-44px) góc trên phải, không phải nút CLOSE to** — CLOSE to che chữ trong panel (HowToPlay/Credits/Select ship đều bị). Vị trí: pivot (1,1) pos (-8,-8).
- **R5.18** — **Text panel (HowToPlay) font pixel 30pt nhỏ khó đọc → tăng 36 + lineSpacing 1.15.** Credits body font 20→22 kèm panel to hơn (760×660) — 24 tràn ~19 dòng.
- **R5.19** — **Input System only (activeInputHandler=1): bắt phím dùng `Keyboard.current[key].wasPressedThisFrame`, KHÔNG dùng legacy `Input.GetKeyDown`** (vô hiệu). ShipSelectManager cần `using UnityEngine.InputSystem;` (asmdef đã reference Unity.InputSystem).
- **R5.20** — **Button tạo bằng code: subscribe onClick ĐÚNG 1 NƠI** — nếu 2 hàm cùng AddListener (hàm tạo + hàm cache-refs) = **double-subscribe**: 1 click = handler chạy 2 lần → với 2 lựa chọn đảo qua rồi về cũ = nhìn như "bấm không hoạt động" (phím vẫn hoạt động — triệu chứng gây nhầm). Fix chuẩn: hàm cache-refs dùng `RemoveAllListeners()` trước `AddListener()` (idempotent, chạy lại an toàn khi panel đã tồn tại). *(Bug 2026-08-12: ShipSelectManager chuột mũi tên không đổi tàu.)*
- **R5.21** — **Convert shader Standard → URP/Lit phải copy CẢ texture, KHÔNG chỉ màu** — `_MainTex → _BaseMap` (+ `_MainTex_ST → _BaseMap_ST`), `_BumpMap`, `_MetallicGlossMap`, `_OcclusionMap`, `_EmissionMap`, `_Glossiness → _Smoothness`, `_Metallic`; kèm **enable keyword** tương ứng (`_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`, `_EMISSION`) — không enable keyword = texture copy vô tác dụng (shader_feature_local). Triệu chứng: tàu TÍM (shader không compile) → sau khi convert → **TRẮNG** = chỉ copy màu, quên texture. *(Bug 2026-08-12.)*

## ⚙️ NHÓM 6 — Workflow Unity / Git (thủ tục bất biến)

- **R5.22 — Shader chỉ dùng RUNTIME (Shader.Find) PHẢI được thêm vào Always Included Shaders nếu không có .mat asset nào tham chiếu** — nếu không, WebGL build STRIP shader đó → `Shader.Find` trả null → `new Material(null)` = `ArgumentNullException: shader` → model/VFX biến mất im lặng (build vẫn pass, không lỗi ở Editor). Kiểm tra: `grep -rl '<tên shader>' Assets --include='*.mat'` = 0 file → phải thêm. Tool có sẵn: `Tools/Starveil Runner/Fix/Always Included Shaders`. Khi deploy WebGL lỗi render mà Editor OK → đọc browser console bằng headless Chrome (`--enable-unsafe-swiftshader` + CDP) để bắt exception thật. *(Bug 2026-08-15: Sprites/Default + URP/Unlit bị strip → mất bóng/VFX/vạch lane trên Unity Play.)*

- **R6.1** — File .cs mới khi Unity đang mở **chưa có .meta** → commit code trước, chờ Unity sinh .meta, commit .meta sau (KHÔNG tự tay gõ GUID).
- **R6.2** — File scene/prefab đang mở trong Unity thì đĩa chưa cập nhật — phải **Ctrl+S** rồi mới grep/commit.
- **R6.3** — Trailing whitespace file Unity tự sinh (`.meta`, scene) → **loại trừ khỏi `git diff --check`**, không sửa file hệ thống.
- **R6.4** — `git diff --check` báo lỗi ở vendor (DOTween), `.slnx` → bỏ qua, chỉ fail khi code C# của mình.
- **R6.5** — Safe mode KHÔNG xóa log cũ — kiểm tra thật: `grep 'error CS' Editor.log` = 0 + có dòng compile chạy → an toàn.
- **R6.6** — Lỗi CS cũ không tự biến mất khỏi `Editor.log` — so vị trí lỗi với dòng `Tundra build success` cuối (lỗi SAU success mới là thật).
- **R6.7** — Scene `Minigame` + `NavMesh-Ground.asset` → **ĐÃ XÓA HẲN cùng `_Archive/` 2026-08-11** (Void bỏ NavMeshAgent — không còn nhu cầu test NavMesh). Không còn scene archive trong project.
- **R6.8** — Prefab/scene luôn commit kèm code liên quan trong CÙNG commit — không commit .meta riêng lẻ.
- **R6.9** — Asset đang mở trong Unity thì đĩa chưa cập nhật (Ctrl+S trước khi commit).
- **R6.10** — Test SaveSystem phải xóa PlayerPrefs trong `[SetUp]` TRƯỚC (không chỉ TearDown SAU).
- **R6.11** — Warning `Assembly ... not valid. Loading skipped` khi mở lại Unity = vô hại (DLL cũ) → Clear Console.
- **R6.12** — **GIT: KHÔNG chạy nhiều `git commit` song song (spawn_agents parallel)** — tranh chấp `.git/index.lock` (`fatal: Unable to create index.lock`) + `git add` của tiến trình này có thể bị `git commit` của tiến trình khác cuốn vào (commit dính file lạ). Luôn chạy git tuần tự — 1 lệnh git/lần spawn. *(Tái phạm được 2026-08-11.)*
- **R6.13** — **`MonoBehaviour.enabled = false` TRONG TEST gọi `OnDisable()` ĐỒNG BỘ** — nếu singleton có `OnDisable` set `Instance = null`, test sẽ mất Instance ngay. Muốn "có Instance nhưng Start không chạy": disable xong khôi phục `Instance` (reflection `<Instance>k__BackingField`, `BindingFlags.NonPublic|Static`) + State qua `GetSetMethod(true)`. *(Gặp khi fix 4 test VoidChase 2026-08-11.)*
- **R6.14** — **Cache Unity Asset Store hardcode ở `%APPDATA%\Unity\Asset Store-5.x` (ổ C — KHÔNG có setting đổi)** — các gói `.unitypackage` tải về chiếm nhiều GB trên ổ C dù project nằm ổ khác; file tải DỞ tạo thêm `.tmp` (gấp ~2 lần) → nghẽn đĩa → Import lỗi `Couldn't decompress`. Giải pháp chuẩn: **Junction** — `robocopy "<nguồn>" "D:\UnityCache\AssetStore" /E /MOVE` (an toàn) → `rmdir "<nguồn>"` → `mklink /J "<nguồn>" "D:\UnityCache\AssetStore"` → verify `dir` thấy `<JUNCTION>`. Khi tải lại gói: xóa cache hỏng (file .unitypackage + .tmp) rồi Download lại, đợi 100% trước khi Import. *(2026-08-12: ổ C 6.4GB + Scifi Kit tải hỏng.)*
- **R6.15** — **Unity tự tạo `Assets/_Recovery/` khi editor crash — KHÔNG phải asset thật, PHẢI thêm vào `.gitignore`** (nếu commit sẽ đẩy rác + file .meta lạ lên repo). Kiểm tra khi thấy untracked lạ trong Assets.

## 📝 NHÓM 7 — Commit / Docs

- **R7.1** — Commit convention: `<type>(<scope>): <subject>` — subject tiếng Việt CÓ DẤU, không viết hoa đầu, không dấu chấm cuối. (Xem `REFERENCE.md` PART 4 — Commit.)
- **R7.2** — Mỗi lần fix lỗi → ghi CHANGELOG (nguyên nhân + cách fix + cách tránh lặp) TRƯỚC khi commit.
- **R7.3** — Bug mới → ghi CHANGELOG.md; quyết định thiết kế → DECISIONS.md; kế hoạch → PLAN.md.
- **R7.4** — Review README sau mỗi thay đổi ảnh hưởng hành vi người dùng / setup / kiến trúc.
- **R7.5** — Trước khi refactor lớn: cập nhật docs trước, user duyệt → mới code.
- **R7.6** — **KHÔNG sửa file `.unity`/`.prefab` bằng script ngoài Unity khi Unity đang MỞ scene/prefab đó** — Unity giữ bản trong memory và GHI ĐÈ file khi Ctrl+S → thay đổi biến mất (bug 2026-08-12: TitleGlow ẩn bằng file nhưng quay lại). Mọi thay đổi scene/phối cảnh phải qua **Editor tool** (chạy trong Unity), rồi user Ctrl+S.
- **R7.7** — **Model 3rd-party có Animator controller — phải kiểm tra DEFAULT STATE** (controller có thể mặc định `idle` thay vì `flying`/`run` → nhân vật đứng im dù có Animator). Sau khi instantiate: `animator.Play("tên state mong muốn")` để ép trạng thái đúng (bug 2026-08-12: Flying Beetle default = `idle 1` → bọ không vỗ cánh).
- **R7.8** — **ScriptableObject data (ObstacleData/PowerUpData) nằm ở `Assets/_Project/ScriptableObjects/`** (Ramp.asset, DynamicBox.asset...) — namespace `VoidRunner.Data` chỉ là tên C#, KHÔNG phải thư mục `Data/`. Trước khi sửa data asset: định vị bằng `git ls-files | grep <tên>` + đối chiếu GUID trong scene, không đoán theo namespace (bug 2026-08-12: tưởng ObstacleData ở `Data/` — thực tế không có folder đó).
- **R7.9** — **Model 3rd-party FBX có scale kỳ lạ (vd scale 100)** — luôn đo `Renderer.bounds` thật sau instantiate rồi chuẩn hóa về kích thước mục tiêu (pattern giống ShipCatalog/EnemyCatalog, R4.18). Không hardcode scale từ file prefab import.
- **R7.11** — **Debug.Log diag tạm (`[DiagX]`) PHẢI xóa trong CÙNG đợt fix xong** — chừa lại = spam Console (mỗi 2s) + có thể GÂY LỖI: log truy cập `GetComponent<Renderer>()` trên ROOT prefab KHÔNG an toàn khi cấu trúc đổi (visual nằm ở con "Model" → `MissingComponentException` trong Unity 6). Muốn check renderer từ root: `GetComponentInChildren<Renderer>()`. *(Bug 2026-08-12: hàng loạt log đỏ sau khi đổi obstacle → Asteroid.)*
- **R7.10** — **`Object.DestroyImmediate(parent)` hủy luôn CẢ CÂY CON** — mọi tham chiếu tới child (`transform`/`renderer`...) sau đó = `MissingReferenceException: GameObject has been destroyed`. Nếu cần giá trị từ child: **capture trước** (`float s = child.transform.localScale.x;`) và chỉ log/dùng biến đã lưu sau destroy. Dấu hiệu: exception ở dòng `get_transform()`/`get_renderer()` NGAY SAU lệnh `DestroyImmediate`. *(Bug 2026-08-12: AsteroidObstacleSetupTool.)*

---

*File này là "cấm kỵ" khi code — đọc trước khi viết bất kỳ script nào. Bổ sung rule mới khi học được bài học mới.*
