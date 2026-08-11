using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidRunner.Data;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Spawn coin + power-up ngẫu nhiên lên tile khi tile được spawn (song song với ObstacleManager).
    /// Coin xếp theo hàng trên lane; power-up xuất hiện hiếm, có weight riêng.
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [Header("Cấu hình")]

        [Tooltip("Prefab coin (cần collider trigger + component Coin)")]
        [SerializeField] private GameObject coinPrefab;

        [Tooltip("Các loại power-up — chọn theo spawnWeight")]
        [SerializeField] private PowerUpData[] powerUpTypes;

        [Tooltip("Xác suất 1 tile có coin")]
        [Range(0f, 1f)]
        [SerializeField] private float coinChance = 0.5f;

        [Tooltip("Xác suất 1 tile có power-up (thấp — hiếm gặp)")]
        [Range(0f, 1f)]
        [SerializeField] private float powerUpChance = 0.08f;

        [Tooltip("Số coin mỗi hàng (trên cùng lane)")]
        [SerializeField] private int coinRowCount = 4;

        [Tooltip("Khoảng cách giữa 2 coin trong hàng")]
        [SerializeField] private float coinSpacing = 1.2f;

        [Tooltip("Số lane trên track (khớp ObstacleManager)")]
        [SerializeField] private int laneCount = 3;

        [Tooltip("Bề rộng 1 lane (khớp ObstacleManager)")]
        [SerializeField] private float laneWidth = 2f;

        private ObstacleManager _obstacleManager; // để biết lane nào đã bị obstacle chặn ở tile này

        /// <summary>TileSpawner truyền vào lúc Initialize — coin chọn lane KHÔNG trùng obstacle (fix 2026-08-11).</summary>
        public void BindObstacleManager(ObstacleManager om) => _obstacleManager = om;

        /// <summary>Gọi từ TileSpawner khi tile được spawn.</summary>
        public void TrySpawn(Tile tile)
        {
            if (tile == null) return;

            if (coinPrefab != null && Random.value <= coinChance)
            {
                SpawnCoinRow(tile);
            }

            if (powerUpTypes != null && powerUpTypes.Length > 0 && Random.value <= powerUpChance)
            {
                SpawnPowerUp(tile);
            }
        }

        private void SpawnCoinRow(Tile tile)
        {
            // Chọn lane coin KHÁC các lane obstacle đã chặn trên tile này (fix 2026-08-11:
            // trước đây chọn ngẫu nhiên độc lập → obstacle đè lên hàng xu). Fallback ngẫu nhiên nếu kẹt.
            int lane = PickLaneAvoidingObstacles();
            float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

            // Hàng coin nằm ở 1 nửa tile, tránh chồng lên obstacle ở đầu tile
            float startZ = tile.Length * 0.25f;
            for (int i = 0; i < coinRowCount; i++)
            {
                GameObject coin = Instantiate(coinPrefab, tile.transform);
                coin.transform.localPosition = new Vector3(x, 0.8f, startZ + i * coinSpacing);
            }

            // [DIAG-TẠM] user yêu cầu log vị trí coin (2026-08-12 v3f.3) — XÓA SAU KHI XÁC NHẬN
            Debug.Log($"[DIAG-Coin] lane={x:F2} startZ={startZ:F1} spacing={coinSpacing} count={coinRowCount} first={new Vector3(x, 0.8f, startZ).ToString("F1")}");
        }

        private void SpawnPowerUp(Tile tile)
        {
            PowerUpData data = PickRandomType();
            if (data == null || data.prefab == null) return;

            int lane = PickLaneAvoidingObstacles();
            float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

            GameObject pickup = Instantiate(data.prefab, tile.transform);
            pickup.transform.localPosition = new Vector3(x, 0.8f, tile.Length * 0.5f);

            PowerUpPickup comp = pickup.GetComponent<PowerUpPickup>();
            if (comp != null) comp.SetData(data);
        }

        /// <summary>Chọn lane ngẫu nhiên KHÔNG nằm trong danh sách lane obstacle đã chặn của tile này.</summary>
        private int PickLaneAvoidingObstacles()
        {
            if (_obstacleManager == null || _obstacleManager.BlockedLanes.Count == 0)
            {
                return Random.Range(0, laneCount);
            }

            List<int> free = new List<int>();
            for (int i = 0; i < laneCount; i++)
            {
                if (!_obstacleManager.BlockedLanes.Contains(i)) free.Add(i);
            }
            return free.Count > 0 ? free[Random.Range(0, free.Count)] : Random.Range(0, laneCount);
        }

        private PowerUpData PickRandomType()
        {
            float total = 0f;
            foreach (PowerUpData d in powerUpTypes)
            {
                if (d != null) total += Mathf.Max(0f, d.spawnWeight);
            }
            if (total <= 0f)
            {
                return powerUpTypes[Random.Range(0, powerUpTypes.Length)];
            }

            float r = Random.value * total;
            foreach (PowerUpData d in powerUpTypes)
            {
                if (d == null) continue;
                r -= Mathf.Max(0f, d.spawnWeight);
                if (r <= 0f) return d;
            }
            return powerUpTypes[powerUpTypes.Length - 1];
        }
    }
}
