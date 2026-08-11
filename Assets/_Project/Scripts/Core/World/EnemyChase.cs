using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Systems.VFX;

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

        [SerializeField, Tooltip("Khoảng cách nền (nấc 0) sau lưng player — trong tầm camera offset -10")]
        private float baseDistance = 9f;

        [SerializeField, Tooltip("Khoảng cách khi đụng vật cản lần 1 (nấc 1) — áp sát nhưng KHÔNG che tàu (fix 2026-08-12: 5m che mất tàu)")]
        private float closeDistance = 7.5f;

        [SerializeField, Tooltip("Dưới ngưỡng này (khoảng cách z thật) = enemy nuốt player — safety net")]
        private float swallowDistance = 1.6f;

        [SerializeField, Tooltip("Cửa sổ né sạch (giây) để enemy nới lại về nấc 0 — 10–15s")]
        private float relaxWindow = 12f;

        [SerializeField, Tooltip("Tốc độ co/nới khoảng cách (m/s) — enemy trượt mượt giữa 2 nấc")]
        private float distanceLerpSpeed = 3f;

        [SerializeField, Tooltip("Tốc độ bám ngang theo lane của player (m/s)")]
        private float lateralFollow = 4f;

        [Header("Hình dạng")]
        [SerializeField, Tooltip("Scale ở nấc 0 (nền)")]
        private float baseScale = 1f;

        [SerializeField, Tooltip("Scale ở nấc 1 (áp sát) — phình nhẹ đe dọa nhưng KHÔNG che tàu (fix 2026-08-12: 1.6 quá to)")]
        private float closeScale = 1.2f;

        [Header("Enemy model (duy nhất)")]
        [Tooltip("Flying Beetle (Assets/Flying Beetle/prefab) — có Animator, instantiate là bay. Tool Setup Enemy tự gán.")]
        [SerializeField] private GameObject enemyPrefab;

        [Tooltip("Chiều cao chuẩn hóa của enemy (đơn vị) — nhỏ hơn nữa để không che tàu player (~1.1).")]
        [SerializeField] private float enemyTargetHeight = 1.8f;

        [Tooltip("Xoay thêm quanh Y (độ) nếu model quay mặt sai hướng (0 = model forward +Z về phía player).")]
        [SerializeField] private float enemyYaw = 0f;

        private Vector3 _startPos;
        private int _stage;                 // 0 = nền, 1 = áp sát (đã đụng 1 lần)
        private float _currentDistance;
        private float _relaxTimer;

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

            Transform existing = transform.Find("Enemy");
            if (existing != null) return; // idempotent — đã dựng rồi

            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.name = "Enemy";

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

            // Quay mặt về hướng player (player luôn phía +Z so với enemy) — set 1 LẦN khi build.
            // ⚠️ KHÔNG ép mỗi frame (R4.17): enemy có Animator — ghi đè localRotation mỗi frame
            // sẽ đánh nhau với root motion của animation.
            enemy.transform.localRotation = Quaternion.Euler(0f, enemyYaw, 0f);

            Debug.Log($"[Enemy] Model: {enemyPrefab.name} (scale={enemy.transform.localScale.x:F2}, bounds={b.size})");
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
        }

        /// <summary>
        /// Player đụng vật cản (R0.4):
        /// - NẤC 0 → NẤC 1: enemy tiến sát + mở cửa sổ né sạch.
        /// - Đã NẤC 1 (đang trong cửa sổ) → enemy nuốt → Game Over.
        /// </summary>
        private void HandleObstacleHit()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (_stage == 0)
            {
                _stage = 1;
                _relaxTimer = relaxWindow;
            }
            else
            {
                GameEvents.RaiseGameOver();
            }
        }

        private void Update()
        {
            if (player == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

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
