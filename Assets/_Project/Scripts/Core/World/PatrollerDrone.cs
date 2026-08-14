using UnityEngine;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// LOẠI ENEMY MỚI (UPGRADE_PLAN Mục 6) — PatrollerDrone: drone tuần tra NGANG giữa 3 lane
    /// phía TRƯỚC player (khác obstacle tĩnh — phải né theo TIMING).
    ///
    /// Cách hoạt động:
    /// - Bám theo player ở khoảng cách cố định phía trước (aheadDistance) — không rơi khỏi màn hình.
    /// - Lắc lư qua lại giữa 2 lane (patrolMinLane ↔ patrolMaxLane) theo hàm cos — mượt, predictable.
    /// - Phase lắc được RANDOM hóa mỗi lần spawn → nhiều drone không bao giờ lắc đồng bộ (khó chịu).
    /// - Vẫn là obstacle: gắn kèm component <see cref="Obstacle"/> → player đụng = RaiseObstacleHit
    ///   (Enemy bọ tiến sát — CÙNG luật với obstacle tĩnh, không phá thiết kế 2 nấc).
    ///
    /// Prefab: tạo bằng tool `Tools → Starveil Runner → Setup → Patroller Drone` — tự dựng từ
    /// DroneObstacle.prefab (Robot_Guardian) + thêm component này. Idempotent.
    /// </summary>
    [RequireComponent(typeof(Obstacle))]
    public class PatrollerDrone : MonoBehaviour
    {
        [Header("Tuần tra")]
        [SerializeField, Tooltip("Lane trái nhất khi lắc (0..2)")]
        private int patrolMinLane = 0;
        [SerializeField, Tooltip("Lane phải nhất khi lắc (0..2)")]
        private int patrolMaxLane = 2;
        [SerializeField, Tooltip("Thời gian lắc hết 1 chu kỳ qua lại (giây) — nhỏ = nhanh")]
        private float patrolPeriod = 3.2f;
        [SerializeField, Tooltip("Khoảng cách phía trước player (m) — drone luôn ở trước mặt, không tụt sau")]
        private float aheadDistance = 16f;

        [Header("Cấu hình track (khớp PlayerController/ObstacleManager)")]
        [SerializeField, Tooltip("Số lane — mặc định 3")]
        private int laneCount = 3;
        [SerializeField, Tooltip("Bề rộng 1 lane (m) — mặc định 2 (road 18m / 3 lane)")]
        private float laneWidth = 2f;

        private Transform _player;
        private Vector3 _startPos;
        private float _phase;
        private bool _active;

        private void Awake()
        {
            _startPos = transform.position;
            _phase = Random.value * Mathf.PI * 2f; // phase ngẫu nhiên — không đồng bộ với drone khác
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetDrone;
            GameEvents.OnRestart += ResetDrone;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetDrone;
            GameEvents.OnRestart -= ResetDrone;
        }

        /// <summary>Gán player (GameManager gọi lúc StartRun — giống EnemyChase.Setup).</summary>
        public void Setup(Transform playerRef) => _player = playerRef;

        private void ResetDrone()
        {
            transform.position = _startPos;
            _active = true;
        }

        private void Update()
        {
            if (_player == null || !_active) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // Bám trước mặt player (trục Z) — vị trí X do lắc kiểm soát, Y giữ nguyên
            Vector3 pos = transform.position;
            pos.z = _player.position.z + aheadDistance;

            // Lắc ngang: cos giữa lane min/max — mượt, predictable (player đoán được quỹ đạo)
            float t = (Mathf.Cos(Time.time * (2f * Mathf.PI / patrolPeriod) + _phase) + 1f) * 0.5f; // 0..1
            float minX = (patrolMinLane - (laneCount - 1) * 0.5f) * laneWidth;
            float maxX = (patrolMaxLane - (laneCount - 1) * 0.5f) * laneWidth;
            pos.x = Mathf.Lerp(minX, maxX, t);

            transform.position = pos;
        }
    }
}
