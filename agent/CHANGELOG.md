# CHANGELOG — Nhật ký lỗi đã sửa (lessons learned)

> **Mục đích:** ghi lại mọi lỗi/warning đã gặp trong quá trình phát triển, cách fix và cách tránh lặp lại.
> Cập nhật mỗi lần fix lỗi, trước khi commit.

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
