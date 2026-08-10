using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// "Hư Không" (The Void) — kẻ đuổi theo player theo cơ chế 2 NẤC CỐ ĐỊNH
    /// (kiểu Subway Surfers / Temple Run — R0.4):
    ///
    ///   NẤC 0 (nền):  giữ baseDistance (9m) sau lưng player — chỉ bám theo lane,
    ///                 KHÔNG tự tăng tốc theo thời gian.
    ///   Đụng vật cản lần 1 → NẤC 1: Void tiến sát còn closeDistance (5m) + mở cửa sổ
    ///                 relaxWindow (12s). Void phình to hơn (đe dọa).
    ///   Né sạch 12s   → Void nới dần về baseDistance (reset NẤC 0).
    ///   Đụng lần 2 TRONG cửa sổ → Void nuốt player → Game Over.
    ///
    /// KHÔNG dùng NavMeshAgent (fix 2026-08-11): track là VÔ TẬN (tile recycle),
    /// NavMesh bake chỉ phủ 1 vùng cố định → player chạy xa là Void đứng yên,
    /// không bao giờ thấy. Void bám player trực tiếp theo vị trí.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VoidChase : MonoBehaviour
    {
        [Header("Khoảng cách")]
        [SerializeField] private Transform player;

        [SerializeField, Tooltip("Khoảng cách nền (nấc 0) sau lưng player — trong tầm camera offset -10")]
        private float baseDistance = 9f;

        [SerializeField, Tooltip("Khoảng cách khi đụng vật cản lần 1 (nấc 1) — áp sát nhưng chưa chết")]
        private float closeDistance = 5f;

        [SerializeField, Tooltip("Dưới ngưỡng này (khoảng cách z thật) = Void nuốt player — safety net")]
        private float swallowDistance = 1.6f;

        [SerializeField, Tooltip("Cửa sổ né sạch (giây) để Void nới lại về nấc 0 — 10–15s")]
        private float relaxWindow = 12f;

        [SerializeField, Tooltip("Tốc độ co/nới khoảng cách (m/s) — Void trượt mượt giữa 2 nấc")]
        private float distanceLerpSpeed = 3f;

        [SerializeField, Tooltip("Tốc độ bám ngang theo lane của player (m/s)")]
        private float lateralFollow = 4f;

        [Header("Hình dạng")]
        [SerializeField, Tooltip("Scale ở nấc 0 (nền)")]
        private float baseScale = 1f;

        [SerializeField, Tooltip("Scale ở nấc 1 (áp sát) — Void phình to đe dọa")]
        private float closeScale = 1.6f;

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
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetVoid;
            GameEvents.OnRestart += ResetVoid;
            GameEvents.OnObstacleHit += HandleObstacleHit;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetVoid;
            GameEvents.OnRestart -= ResetVoid;
            GameEvents.OnObstacleHit -= HandleObstacleHit;
        }

        private void ResetVoid()
        {
            _stage = 0;
            _relaxTimer = 0f;
            _currentDistance = baseDistance;
            transform.position = _startPos;
            transform.localScale = Vector3.one * baseScale;
        }

        /// <summary>
        /// Player đụng vật cản (R0.4):
        /// - NẤC 0 → NẤC 1: Void tiến sát + mở cửa sổ né sạch.
        /// - Đã NẤC 1 (đang trong cửa sổ) → Void nuốt → Game Over.
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

            // Phình to hơn khi áp sát — mức độ đe dọa nhìn thấy được
            float closeness = Mathf.InverseLerp(baseDistance, closeDistance, _currentDistance);
            transform.localScale = Vector3.one * Mathf.Lerp(baseScale, closeScale, closeness);

            // Safety net: khoảng cách z thực tế dưới ngưỡng → Void nuốt player (cơ chế chết chắc chắn
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
}
