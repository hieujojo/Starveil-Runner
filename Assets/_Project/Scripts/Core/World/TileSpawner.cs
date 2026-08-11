using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Utils;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Quản lý track vô tận bằng Object Pool:
    /// tile được spawn phía trước player và recycle sau lưng — không Instantiate/Destroy trong lúc chơi.
    /// </summary>
    public class TileSpawner : MonoBehaviour
    {
        [Header("Track")]
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private float tileLength = 10f;
        [SerializeField] private int tileCountAhead = 6;
        [SerializeField] private int poolSize = 12;
        [SerializeField] private float recycleDistance = 15f;

        [Header("An toàn đầu game")]
        [Tooltip("Đoạn đầu sau khi (re)start KHÔNG có obstacle — player có thời gian làm quen, không chết ngay. (fix 2026-08-11)")]
        [SerializeField] private float safeZoneAhead = 20f;

        [Header("Tham chiếu (tự gán qua Initialize)")]
        [SerializeField] private Transform player;
        [SerializeField] private ObstacleManager obstacleManager;
        [SerializeField] private PickupSpawner pickupSpawner;

        private static int _instanceCount;
        private ObjectPool<Tile> _pool;
        private readonly List<Tile> _activeTiles = new List<Tile>();
        private float _nextSpawnZ;
        private bool _initialized;
        private float _lastDiagLog; // tạm — chẩn đoán không có vật cản/xu (2026-08-11)

        private void Awake()
        {
            _instanceCount++;
            Debug.Log($"[DiagTS] Awake instance={_instanceCount} parent={(transform.parent != null ? transform.parent.name : "(root)")} pos={transform.position}");

            if (tilePrefab == null)
            {
                Debug.LogError("TileSpawner thiếu tilePrefab.");
                return;
            }

            // Activation do Tile.Activate xử lý — pool chỉ quản lý vòng đời
            _pool = new ObjectPool<Tile>(
                factory: CreateTile,
                onRelease: tile => tile.Deactivate(),
                prewarmCount: poolSize);
        }

        private void OnDestroy()
        {
            _instanceCount--;
        }

        private void OnEnable()
        {
            Debug.Log($"[DiagTS] OnEnable instance={_instanceCount} — có TileSpawner khác chạy? count thực tế từ FindAny: {FindObjectsByType<TileSpawner>(FindObjectsSortMode.None).Length}");
        }

        private Tile CreateTile()
        {
            Tile tile = Instantiate(tilePrefab, transform);
            tile.name = "Tile";
            tile.gameObject.SetActive(false);
            return tile;
        }

        public void Initialize(Transform playerRef, ObstacleManager obstacles)
        {
            player = playerRef;
            obstacleManager = obstacles;
            if (pickupSpawner == null) pickupSpawner = FindAnyObjectByType<PickupSpawner>();
            // Coin phải chọn lane KHÁC obstacle (fix 2026-08-11: obstacle đè lên xu)
            if (pickupSpawner != null) pickupSpawner.BindObstacleManager(obstacles);
            _initialized = true;
        }

        public void StartTrack()
        {
            if (!_initialized || player == null)
            {
                Debug.LogWarning("TileSpawner chưa được Initialize — bỏ qua StartTrack.");
                return;
            }

            // Trả toàn bộ tile về pool
            for (int i = _activeTiles.Count - 1; i >= 0; i--)
            {
                _pool.Release(_activeTiles[i]);
            }
            _activeTiles.Clear();

            // Xếp lại track bắt đầu từ sau lưng player (fix 2026-08-11: player đã được GameManager
            // ResetToStart về điểm cố định TRƯỚC khi gọi đây → track luôn dựng từ cùng 1 chỗ)
            _nextSpawnZ = player.position.z - tileLength;
        }

        private void Update()
        {
            if (!_initialized || player == null) return;

            // Recycle tile đã vượt qua sau lưng player
            for (int i = _activeTiles.Count - 1; i >= 0; i--)
            {
                Tile tile = _activeTiles[i];
                if (tile.IsBehind(player, recycleDistance))
                {
                    _pool.Release(tile);
                    _activeTiles.RemoveAt(i);
                }
            }

            // Spawn bù tile phía trước player
            float targetZ = player.position.z + tileCountAhead * tileLength;
            while (_nextSpawnZ < targetZ)
            {
                SpawnTile(_nextSpawnZ);
                _nextSpawnZ += tileLength;
            }
        }

        private void SpawnTile(float z)
        {
            Tile tile = _pool.Get();
            tile.Activate(new Vector3(0f, 0f, z));
            _activeTiles.Add(tile);

            // Safe zone: tile trong đoạn đầu (so với vị trí player hiện tại) KHÔNG spawn obstacle
            // (coin vẫn có — vô hại). Trước đây obstacle spawn ngay tile đầu (z≈1.7) → chết tức thì.
            bool inSafeZone = z < player.position.z + safeZoneAhead;
            if (!inSafeZone) obstacleManager?.TrySpawn(tile);
            pickupSpawner?.TrySpawn(tile);

            // [TẠM] chẩn đoán — user báo không có vật cản/xu dù wiring đúng
            if (Time.time - _lastDiagLog > 2f)
            {
                _lastDiagLog = Time.time;
                Debug.Log($"[DiagSpawn] tile={_activeTiles.Count} playerZ={player.position.z:F1} nextZ={_nextSpawnZ:F1} pool={_pool != null}");
            }
        }
    }
}
