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
            // Chọn ngẫu nhiên 1 lane có coin (không cần biết obstacle ở lane nào — obstacle thường rải lane khác)
            int lane = Random.Range(0, laneCount);
            float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

            // Hàng coin nằm ở 1 nửa tile, tránh chồng lên obstacle ở đầu tile
            float startZ = tile.Length * 0.25f;
            for (int i = 0; i < coinRowCount; i++)
            {
                GameObject coin = Instantiate(coinPrefab, tile.transform);
                coin.transform.localPosition = new Vector3(x, 0.8f, startZ + i * coinSpacing);
            }
        }

        private void SpawnPowerUp(Tile tile)
        {
            PowerUpData data = PickRandomType();
            if (data == null || data.prefab == null) return;

            int lane = Random.Range(0, laneCount);
            float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

            GameObject pickup = Instantiate(data.prefab, tile.transform);
            pickup.transform.localPosition = new Vector3(x, 0.8f, tile.Length * 0.5f);

            PowerUpPickup comp = pickup.GetComponent<PowerUpPickup>();
            if (comp != null) comp.SetData(data);
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
