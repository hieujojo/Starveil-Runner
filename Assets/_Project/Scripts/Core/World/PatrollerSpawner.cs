using UnityEngine;
using VoidRunner.Core.Factories;
using VoidRunner.Core.Interfaces;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// UPGRADE_PLAN Mục 6 — spawn PatrollerDrone (enemy mới: drone tuần tra ngang lane) lên tile.
    /// Pattern y hệt PickupSpawner: TileSpawner gọi TrySpawn mỗi tile → spawn với xác suất thấp.
    ///
    /// Patroller KHÔNG chặn lane (nó lắc qua lại) nên không thêm vào BlockedLanes — nhưng để
    /// gameplay công bằng: patroller chỉ spawn trên tile CÓ obstacle (mật độ cao — đã qua safe zone)
    /// và với xác suất riêng thấp — không bao giờ 2 tile liên tiếp có patroller.
    ///
    /// Gắn component này vào cùng GameObject với ObstacleManager (hoặc Managers) trong scene Game.
    /// </summary>
    public class PatrollerSpawner : MonoBehaviour
    {
        [Header("Cấu hình")]
        [Tooltip("Prefab PatrollerDrone (tạo bằng tool Setup → Patroller Drone)")]
        [SerializeField] private GameObject patrollerPrefab;

        [Tooltip("Xác suất 1 tile có patroller — thấp (enemy đặc biệt, không phải obstacle thường)")]
        [Range(0f, 1f)]
        [SerializeField] private float spawnChance = 0.06f;

        [Tooltip("Tối đa patroller đồng thời trên track (tránh dày đặc)")]
        [SerializeField] private int maxConcurrent = 1;

        [Tooltip("Số lane trên track (khớp ObstacleManager)")]
        [SerializeField] private int laneCount = 3;
        [Tooltip("Bề rộng 1 lane (khớp ObstacleManager)")]
        [SerializeField] private float laneWidth = 2f;

        // FACTORY PATTERN — dùng IEnemyFactory thay vì Instantiate trực tiếp
        private IEnemyFactory _enemyFactory;

        private ObstacleManager _obstacleManager;
        private Transform _player;
        private int _activeCount;
        private int _tilesSinceLast; // chống 2 patroller liên tiếp

        /// <summary>TileSpawner gọi lúc Initialize — biết lane obstacle + player.</summary>
        public void Bind(ObstacleManager om, Transform playerRef)
        {
            _obstacleManager = om;
            _player = playerRef;

            // FACTORY PATTERN — khởi tạo enemy factory (nếu chưa có)
            if (_enemyFactory == null)
                _enemyFactory = new EnemyFactory(null); // null obstacleTypes = dùng catalog fallback
        }

        /// <summary>Gọi từ TileSpawner khi tile được spawn (sau TrySpawn obstacle).</summary>
        public void TrySpawn(Tile tile)
        {
            if (tile == null || patrollerPrefab == null || _player == null) return;
            if (_activeCount >= maxConcurrent) return;
            if (_tilesSinceLast < 3) { _tilesSinceLast++; return; } // tối thiểu 3 tile cách nhau
            if (Random.value > spawnChance) return;

            // Patroller lắc giữa mọi lane — chỉ spawn khi tile này CÓ obstacle (mật độ cao) để
            // tổng khó không vượt quá; nhưng KHÔNG đặt đè lên obstacle tĩnh (lệch z).
            if (_obstacleManager == null || _obstacleManager.BlockedLanes.Count == 0) return;

            int lane = Random.Range(0, laneCount);
            float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

            // FACTORY PATTERN — dùng factory thay vì Instantiate trực tiếp
            GameObject drone = patrollerPrefab != null
                ? Instantiate(patrollerPrefab, tile.transform)
                : _enemyFactory?.Create(EnemyType.Patroller, tile.transform);
            // z ở NỬA SAU tile — xa khỏi obstacle tĩnh (nửa trước), tránh chồng đè
            drone.transform.localPosition = new Vector3(x, 0.5f, tile.Length * 0.7f);

            var patrol = drone.GetComponent<PatrollerDrone>();
            if (patrol != null) patrol.Setup(_player);

            _activeCount++;
            _tilesSinceLast = 0;
        }

        /// <summary>Patroller bị recycle (tile sau lưng) → trừ đếm — PatrollerDrone tự hủy theo tile.</summary>
        public void NotifyRecycled() => _activeCount = Mathf.Max(0, _activeCount - 1);

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetSpawner;
            GameEvents.OnRestart += ResetSpawner;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetSpawner;
            GameEvents.OnRestart -= ResetSpawner;
        }

        private void ResetSpawner()
        {
            _activeCount = 0;
            _tilesSinceLast = 0;
        }
    }
}
