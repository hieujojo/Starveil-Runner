using UnityEngine;
using VoidRunner.Core.Interfaces;
using VoidRunner.Core.Player;
using VoidRunner.Core.World.Strategies;

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

        // STRATEGY PATTERN — PatrollerStrategy xử lý patrol logic
        private PatrollerStrategy _strategy;

        private Transform _player;
        private bool _active;

        private void Awake()
        {
            // STRATEGY PATTERN — khởi tạo patroller strategy
            _strategy = new PatrollerStrategy();
            _strategy.SetStartPos(transform.position);
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
        public void Setup(Transform playerRef)
        {
            _player = playerRef;
            _strategy.Setup(playerRef);
        }

        private void ResetDrone()
        {
            // STRATEGY PATTERN — reset strategy
            _strategy.ResetState();
            _strategy.ResetPosition(transform);
            _active = true;
        }

        private void Update()
        {
            if (_player == null || !_active) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // STRATEGY PATTERN — delegate patrol movement to strategy
            _strategy.Execute(transform, _player, Time.deltaTime);
        }
    }
}
