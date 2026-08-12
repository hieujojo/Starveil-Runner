using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Systems.VFX;
using VoidRunner.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoidRunner.Core.World
{
    /// <summary>
    /// ENEMY (đổi tên từ "Void" 2026-08-12 — user: "nên đổi từ void thành enemy cho tốt hơn") —
    /// kẻ đuổi theo player theo cơ chế 2 NẤC CỐ ĐỊNH (kiểu Subway Surfers / Temple Run — R0.4):
    ///
    ///   NẤC 0 (nền):  giữ baseDistance (9m) sau lưng player — chỉ bám theo lane,
    ///                 KHÔNG tự tăng tốc theo thời gian.
    ///   Đụng vật cản lần 1 → NẤC 1: enemy tiến sát còn closeDistance (7.5m) + mở cửa sổ
    ///                 relaxWindow (12s). Enemy phình to hơn (đe dọa).
    ///   Né sạch 12s   → enemy nới dần về baseDistance (reset NẤC 0).
    ///   Đụng lần 2 TRONG cửa sổ → enemy nuốt player → Game Over.
    ///
    /// KHÔNG dùng NavMeshAgent (fix 2026-08-11): track là VÔ TẬN (tile recycle),
    /// NavMesh bake chỉ phủ 1 vùng cố định → player chạy xa là enemy đứng yên,
    /// không bao giờ thấy. Enemy bám player trực tiếp theo vị trí.
    ///
    /// Enemy DUY NHẤT = Flying Beetle (fix 2026-08-12 — user: "chỉ dùng 1 kẻ thù là model
    /// flying carnivorous"): prefab CÓ Animator (controller flying loop) → instantiate là cánh
    /// vỗ bay liên tục. Động tác enemy = Animator của model (flying/idle), KHÔNG ép rotation
    /// mỗi frame (đánh nhau với root motion — R4.17).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyChase : MonoBehaviour
    {
        [Header("Khoảng cách")]
        [SerializeField] private Transform player;

        [SerializeField, Tooltip("Khoảng cách nền (nấc 0) sau lưng player — FIX 2026-08-12 v3f.3: 16→5m (16m = 6m SAU camera → không bao giờ thấy). v3f.5: 5→7m (user: \"cho con bọ xa hơn 1 chút\") — vẫn TRƯỚC camera, thoáng hơn không lấn màn")]
        private float baseDistance = 7f;

        [SerializeField, Tooltip("Khoảng cách khi đụng vật cản lần 1 (nấc 1) — FIX 2026-08-12 v3f.5.3: 3→5.5m (user: \"đang gần quá — chỉ cần chạm 1 lần trong thời gian quy định là bọ tiến sát, cho ra xa 2-3m nữa\"). 5.5m vẫn < base 7m (thấy rõ bọ tiến sát) nhưng không lấn màn")]
        private float closeDistance = 5.5f;

        [SerializeField, Tooltip("Dưới ngưỡng này (khoảng cách z thật) = enemy nuốt player — safety net")]
        private float swallowDistance = 1.6f;

        [SerializeField, Tooltip("Cửa sổ né sạch (giây) để enemy nới lại về nấc 0 — 10–15s")]
        private float relaxWindow = 12f;

        [SerializeField, Tooltip("Tốc độ co/nới khoảng cách (m/s) — enemy trượt mượt giữa 2 nấc")]
        private float distanceLerpSpeed = 3f;

        [SerializeField, Tooltip("Tốc độ bám ngang theo lane của player (m/s) — FIX 2026-08-12 v3f.4: 4→20 (player đổi lane 4.5m @16m/s ≈ 0.28s/lane nhưng enemy 4m/s mất 1.1s = trễ ~0.8s — user: \"con bọ phải di chuyển cùng lúc với player, ko thể trễ 0.5s\"). 20m/s ≈ 0.22s = đồng bộ với player)")]
        private float lateralFollow = 20f;

        [Header("Hình dạng")]
        [SerializeField, Tooltip("Scale ở nấc 0 (nền)")]
        private float baseScale = 1f;

        [SerializeField, Tooltip("Scale ở nấc 1 (áp sát) — phình nhẹ đe dọa nhưng KHÔNG che tàu (fix 2026-08-12: 1.6 quá to)")]
        private float closeScale = 1.2f;

        [Header("Enemy model (duy nhất)")]
        [Tooltip("Flying Beetle (Assets/Flying Beetle/prefab) — có Animator, instantiate là bay. Tool Setup Enemy tự gán.")]
        [SerializeField] private GameObject enemyPrefab;

        [Tooltip("Chiều cao chuẩn hóa của enemy (đơn vị) — FIX 2026-08-12 v3f.5.3: 2.2→2.6 (user: \"cho con bọ to lên\" — lần 2); vẫn sau player nên không che tàu")]
        [SerializeField] private float enemyTargetHeight = 2.6f;

        [Tooltip("Xoay thêm quanh Y (độ) nếu model quay mặt sai hướng. FIX 2026-08-12 v3f.10: mặc định 180 — Flying Beetle forward là -Z, phải quay 180° mới nhìn về player (+Z).")]
        [SerializeField] private float enemyYaw = 180f;

        [Header("Cơ chế bắt (hit lần 2)")]
        [SerializeField, Tooltip("Hệ số vỗ cánh nhanh hơn khi đụng obstacle lần 1 (Animator.speed)")]
        private float hitSpeedUp = 2f;
        [SerializeField, Tooltip("Thời gian (giây) chạy animation atack trước khi Game Over — bắt mượt không cắt cảnh")]
        private float catchDelay = 1.1f;

        private Vector3 _startPos;
        private int _stage;                 // 0 = nền, 1 = áp sát (đã đụng 1 lần)
        private float _currentDistance;
        private float _relaxTimer;
        private bool _catching;             // đang thực hiện cảnh bắt (hit lần 2) — không cho trigger/lunge lần nữa
        private Animator _animator;         // Animator của Flying Beetle (ép flying + speed khi hit)

        public void Setup(Transform playerRef) => player = playerRef;

        private void Awake()
        {
            _startPos = transform.position;
            _currentDistance = baseDistance;

            // Collider là trigger — player đi vào là bị nuốt (không đẩy vật lý)
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Enemy = Flying Beetle (fix 2026-08-12 — bỏ random 3 monster, user chốt 1 enemy duy nhất).
            // Self-heal (R4.18, góp ý reviewer 2026-08-12): nếu chưa gán prefab (tool Setup Enemy chưa
            // chạy — ví dụ vừa pull code mới) → tự tải qua EnemyCatalog (editor). Giống ShipCatalog.
            if (enemyPrefab == null) enemyPrefab = EnemyCatalog.Load();

            if (enemyPrefab != null)
            {
                BuildEnemyVisual();
            }
            else
            {
                // Không có prefab (build không editor) → ẩn mesh root, giữ collider trigger nuốt player
                MeshRenderer rootMr = GetComponent<MeshRenderer>();
                if (rootMr != null) rootMr.enabled = false;
            }
        }

        /// <summary>
        /// Fix 2026-08-12: dựng enemy = Flying Beetle duy nhất (không random 3 như cũ).
        /// Prefab có Animator (flying loop) → bay liên tục. Scale chuẩn theo chiều cao mục tiêu
        /// (1.8 — nhỏ hơn để không che tàu player), vô hiệu collider con (chỉ root trigger nuốt).
        /// Idempotent — chạy lại không nhân đôi.
        /// </summary>
        private void BuildEnemyVisual()
        {
            // Ẩn mesh root cũ (banh tím) — trước mọi early-return
            MeshRenderer rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            // Dọn legacy "Enemy" từ code cũ (guard mới tìm "EnemyModel") — nếu runtime child còn
            // sót qua lần chơi sau (vd Enter Play Mode Options → Reload Scene: Off) thì guard mới
            // không thấy "EnemyModel" → dựng thêm 1 con nữa → 2 con bọ đè nhau. Dọn trước khi build.
            Transform legacy = transform.Find("Enemy");
            if (legacy != null) Destroy(legacy.gameObject);

            Transform existing = transform.Find("EnemyModel");
            if (existing != null) return; // idempotent — đã dựng rồi

            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.name = "Enemy";

            // FIX 2026-08-12 v3f.10 (user: "con bọ quay mặt đi sai hướng"): model Flying Beetle
            // forward là -Z → muốn nhìn về player (+Z) phải quay 180°. Xoay hướng trên CONTAINER
            // "EnemyModel" thay vì chính prefab — Animator (clip flying/atack) có thể ghi đè
            // rotation mỗi frame, set trên prefab không chắc chắn. Container nằm NGOÀI Animator
            // → hướng quay luôn đúng dù animation có tự xoay bên trong.
            var model = new GameObject("EnemyModel");
            model.transform.SetParent(transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, enemyYaw, 0f);
            enemy.transform.SetParent(model.transform, false);
            enemy.transform.localPosition = Vector3.zero;
            enemy.transform.localRotation = Quaternion.identity; // Animator tự lo chuyển động bên trong

            // FIX 2026-08-12 v3f.5 (user: "sao con bọ màu tím vậy — màu gốc đâu phải tím"): model 3rd-party
            // (Flying Beetle) dùng shader Standard Built-in → TÍM/MAGENTA trong URP. Tàu đã gọi MaterialFixer
            // (PlayerController), obstacle đã gọi (Obstacle.Awake) — ENEMY bị SÓT → thêm (R3.16 self-heal).
            MaterialFixer.EnsureURPMaterials(enemy);

            // FIX 2026-08-12 v3f.9.3 (user: "con bọ vẫn màu trắng"): prefab Flying Beetle tham chiếu
            // material 3d5d520e (41 lần) nhưng KHÔNG tồn tại trong project → các phần đó render
            // material mặc định TRẮNG. Ép mọi renderer dùng chung material nâu (từ Body đã convert,
            // texture nâu thật của model) — KHÔNG đổi màu, chỉ sửa material missing.
            EnsureEnemyBrownMaterial(enemy);

            // FIX 2026-08-12 v3f.9.2 (user: "cho con bọ màu nâu đậm"): texture gốc của Flying Beetle
            // ĐÃ LÀ nâu đậm (phân tích pixel: 36.5% RGB 48,16,16 + ~17% nâu — tên file "orange" chỉ là
            // tên cũ, cam = 0%) nhưng scene vũ trụ tối làm nó nhìn như đen. KHÔNG đổi màu material
            // (rule user: "tuyệt đối không đổi màu bằng code") — thêm ĐÈN ẤM chiếu vào để texture nâu
            // thật lo rõ thành "nâu đậm" thấy được.
            EnsureEnemyLight(enemy);

            // FIX 2026-08-12 v3 (user: "đừng rung con bọ mà cho nó vỗ cánh"): default state của
            // Animator controller là "idle 1" (bọ đứng im) → ép chạy state "flying" (vỗ cánh loop).
            // KHÔNG ép rotation mỗi frame (R4.17) — Animator lo phần chuyển động, code chỉ điều vị trí.
            _animator = enemy.GetComponent<Animator>();
            if (_animator != null)
            {
                _animator.Play("flying", 0, 0f);
                // FIX 2026-08-12 v3f.5.3 (user: "animation gì trong lúc chuyển động nhìn ko rõ"):
                // tăng tốc animation để thấy rõ cánh vỗ. v3f.10.1 (user: "cho animation con bọ chậm
                // lại tí"): 1.25 → 1.0 (tốc độ gốc của clip — bay tự nhiên, không vỗ loạn)
                _animator.speed = 1f;
            }

            // Vô hiệu hóa collider con — chỉ root collider trigger nuốt player (không đụng vật lý)
            foreach (var col in enemy.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Chuẩn hóa scale theo chiều cao thật (nhiều FBX import to/nhỏ khác nhau)
            Bounds b = GetRenderBounds(enemy);
            if (b.size.y > 0.001f)
            {
                float s = enemyTargetHeight / b.size.y;
                enemy.transform.localScale = Vector3.one * s;
            }

            // v3f.10.1 (user: "sao sau khi sửa mất bóng con bọ luôn"): GIỮ shadow THẬT từ light
            // (soft shadow đã hoạt động và user thấy trước fix) — KHÔNG tắt shadowCastingMode như
            // v3f.10 (blob thay thế bị chìm dưới mặt road top y=0.05 → vô hình). Bọ dùng shadow
            // thật, blob chỉ dùng cho tàu (bóng tàu quá nhỏ/mờ với shadow thật).
        }

        /// <summary>
        /// v3f.9.3 (user: "con bọ vẫn màu trắng"): prefab tham chiếu material missing (3d5d520e, 41 lần)
        /// → ép mọi renderer dùng chung material có texture nâu thật (Body.mat đã convert qua
        /// MaterialFixer). Idempotent + an toàn null. KHÔNG đổi màu — vẫn texture gốc của model.
        /// </summary>
        private static void EnsureEnemyBrownMaterial(GameObject enemy)
        {
            // Tìm material đã convert có _BaseMap (texture nâu thật) làm material chung
            Material brown = null;
            foreach (var r in enemy.GetComponentsInChildren<Renderer>())
            {
                if (brown != null) break;
                if (r == null) continue;
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                    {
                        brown = m;
                        break;
                    }
                }
            }
            if (brown == null) return; // không tìm thấy texture — giữ nguyên

            // Ép mọi renderer dùng material nâu (che hết slot missing → hết trắng)
            foreach (var r in enemy.GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = brown;
                r.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// v3f.9.2 (user: "cho con bọ màu nâu đậm"): Point Light vàng ấm bám theo enemy để texture
        /// nâu đậm gốc của model lo rõ trên nền vũ trụ tối. KHÔNG đổi màu material — chỉ chiếu sáng
        /// màu thật (đúng rule user cấm đổi màu bằng code). Idempotent — đã có "EnemyLight" thì thôi.
        /// </summary>
        private static void EnsureEnemyLight(GameObject enemy)
        {
            if (enemy.transform.Find("EnemyLight") != null) return;

            var go = new GameObject("EnemyLight");
            go.transform.SetParent(enemy.transform, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, 0.5f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.42f, 1f); // vàng ấm — nâu lo rõ
            light.intensity = 2.2f;
            light.range = 7f;
            light.shadows = LightShadows.None; // nhẹ — không tốn shadow map
        }

        /// <summary>Bounds world gộp mọi renderer (dùng để chuẩn hóa scale enemy).</summary>
        private static Bounds GetRenderBounds(GameObject go)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool has = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (has) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; has = true; }
            }
            return has ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetEnemy;
            GameEvents.OnRestart += ResetEnemy;
            GameEvents.OnObstacleHit += HandleObstacleHit;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetEnemy;
            GameEvents.OnRestart -= ResetEnemy;
            GameEvents.OnObstacleHit -= HandleObstacleHit;
        }

        private void ResetEnemy()
        {
            _stage = 0;
            _relaxTimer = 0f;
            _currentDistance = baseDistance;
            transform.position = _startPos;
            transform.localScale = Vector3.one * baseScale;

            // FIX 2026-08-12 v3: reset cơ chế bắt — dừng coroutine, quay về vỗ cánh thường
            StopAllCoroutines();
            _catching = false;
            if (_animator != null)
            {
                _animator.speed = 1f; // đồng bộ với BuildEnemyVisual (v3f.10.1: chậm lại)
                _animator.Play("flying", 0, 0f);
            }
        }

        /// <summary>
        /// Player đụng vật cản (R0.4) — FIX 2026-08-12 v3 (user: "chạm 1 lần vỗ nhanh hơn,
        /// chạm 2 lần bắt lại và kết thúc game"):
        /// - NẤC 0 → NẤC 1: enemy tiến sát + vỗ cánh NHANH HƠN (Animator.speed 2x) + mở cửa sổ né sạch.
        /// - Đã NẤC 1 (đang trong cửa sổ) → NẤC 2: enemy LAO TỚI bắt player (Play atack),
        ///   chờ catchDelay (1.1s — animation bắt diễn ra mượt) rồi mới Game Over.
        /// </summary>
        private void HandleObstacleHit()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (_catching) return; // đang bắt — không nhận hit mới

            if (_stage == 0)
            {
                _stage = 1;
                _relaxTimer = relaxWindow;
                if (_animator != null) _animator.speed = hitSpeedUp; // vỗ cánh nhanh hơn
            }
            else
            {
                StartCoroutine(CatchAndKill());
            }
        }

        /// <summary>
        /// NẤC 2 — cảnh bắt (hit lần 2 trong cửa sổ): enemy lao tới player, chạy animation
        /// "atack 1" (clip tấn công/bắt của Flying Beetle), BÁM DÍNH theo player suốt catchDelay
        /// (fix reviewer 2026-08-12: nếu đứng yên, player chạy tiếp 10–20 m/s → lệch 11–22m →
        /// nhìn như bắt vào khoảng không) rồi mới RaiseGameOver (UIManager fade panel mượt).
        /// </summary>
        private System.Collections.IEnumerator CatchAndKill()
        {
            _catching = true;
            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.Play("atack 1", 0, 0f); // cảnh bắt vật thể
            }

            // Lao tới player trong ~0.3s (không teleport) — target cập nhật mỗi frame theo player
            Vector3 startPos = transform.position;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                Vector3 targetPos = (player != null ? player.position : startPos) + Vector3.up * 0.8f;
                transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t / 0.3f));
                yield return null;
            }

            // Chờ animation bắt diễn ra — BÁM DÍNH player mỗi frame (không để bọ đứng yên mà
            // player chạy xa → cảnh bắt giữ liền mạch, mượt).
            float elapsed = 0f;
            while (elapsed < catchDelay)
            {
                elapsed += Time.deltaTime;
                if (player != null) transform.position = player.position + Vector3.up * 0.8f;
                yield return null;
            }
            GameEvents.RaiseGameOver();
        }

        // v3f.10.2 (user: "con bọ cứ rung rung khó nhìn"): Update → LATEUPDATE. Trong Update,
        // bọ đọc player.position CỦA FRAME TRƯỚC (camera Cinemachine chạy LateUpdate) → mỗi frame
        // bọ lệch 1 nhịp so với camera → rung. LateUpdate chạy cùng thời điểm camera → đồng bộ.
        // Kèm player Rigidbody interpolation (PlayerController) — 2 fix đi đôi hết jitter.
        private void LateUpdate()
        {
            if (player == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (_catching) return; // đang bắt — coroutine điều khiển vị trí, không bám nữa

            // NẤC 1: đếm ngược cửa sổ né sạch — hết hạn (player không đụng gì nữa) → nới về NẤC 0
            if (_stage == 1)
            {
                _relaxTimer -= Time.deltaTime;
                if (_relaxTimer <= 0f)
                {
                    _stage = 0;
                }
            }

            float targetDistance = _stage == 1 ? closeDistance : baseDistance;
            _currentDistance = Mathf.MoveTowards(_currentDistance, targetDistance, distanceLerpSpeed * Time.deltaTime);

            // Mục tiêu: sau lưng player đúng `_currentDistance` (trục Z = hướng chạy), y giữ nguyên
            Vector3 target = player.position - Vector3.forward * _currentDistance;

            // Bám ngang theo lane player từ từ (không teleport ngang)
            target.x = Mathf.MoveTowards(transform.position.x, player.position.x, lateralFollow * Time.deltaTime);

            transform.position = target;

            // Phình to hơn khi áp sát — mức độ đe dọa nhìn thấy được (nhẹ — không che tàu)
            float closeness = Mathf.InverseLerp(baseDistance, closeDistance, _currentDistance);
            transform.localScale = Vector3.one * Mathf.Lerp(baseScale, closeScale, closeness);

            // Safety net: khoảng cách z thực tế dưới ngưỡng → enemy nuốt player (cơ chế chết chắc chắn
            // chạy kể cả khi collider chưa kịp overlap do player đổi lane)
            if (Mathf.Abs(transform.position.z - player.position.z) < swallowDistance)
            {
                GameEvents.RaiseGameOver();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_catching) return; // đang bắt — coroutine đã chịu trách nhiệm Game Over
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (other.GetComponent<PlayerController>() != null)
            {
                GameEvents.RaiseGameOver();
            }
        }
    }

    /// <summary>
    /// Catalog enemy duy nhất (Flying Beetle) — self-heal (R4.18): nếu EnemyChase chưa được gán
    /// prefab qua tool Setup Enemy, tự tải qua AssetDatabase (chỉ editor). Giống ShipCatalog cho tàu.
    /// </summary>
    public static class EnemyCatalog
    {
        private const string EnemyPrefabPath = "Assets/Flying Beetle/prefab/Flying beetle.prefab";

        /// <summary>Load prefab Flying Beetle. Rỗng nếu không tìm thấy (build không editor).</summary>
        public static GameObject Load()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
#else
            return null;
#endif
        }
    }
}
