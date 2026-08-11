# CHANGELOG — Nhật ký lỗi đã sửa (lessons learned)

> **Mục đích:** ghi lại mọi lỗi/warning đã gặp trong quá trình phát triển, cách fix và cách tránh lặp lại.
> Cập nhật mỗi lần fix lỗi, trước khi commit.

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
