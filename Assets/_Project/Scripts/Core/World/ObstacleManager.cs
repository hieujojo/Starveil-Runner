using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Data;
using VoidRunner.Systems.Difficulty;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Gắn obstacle ngẫu nhiên lên tile khi tile được spawn.
    /// Luôn chừa ít nhất 1 lane an toàn để người chơi có đường né.
    /// Mật độ (spawnChance) tăng dần theo DifficultyManager.
    /// </summary>
    public class ObstacleManager : MonoBehaviour
    {
        [Header("Cấu hình")]
        [SerializeField] private ObstacleData[] obstacleTypes;
        [SerializeField, Tooltip("Xác suất cơ bản — DifficultyManager có thể tăng dần")]
        private float spawnChance = 0.45f;
        [SerializeField] private int laneCount = 3;
        [SerializeField] private float laneWidth = 2f;

        /// <summary>Xác suất hiện tại: ưu tiên từ DifficultyManager, fallback về giá trị cấu hình.</summary>
        private float CurrentSpawnChance
        {
            get
            {
                if (DifficultyManager.Instance != null)
                {
                    return DifficultyManager.Instance.CurrentSpawnChance;
                }
                return spawnChance;
            }
        }

        private float _lastDiagLog; // tạm — chẩn đoán không có vật cản (2026-08-11)

        /// <summary>
        /// Các lane đã chặn ở tile gần nhất (reset mỗi TrySpawn) — PickupSpawner đọc để đặt hàng coin
        /// sang lane KHÁC, tránh obstacle đè lên xu (fix 2026-08-11).
        /// </summary>
        private readonly HashSet<int> _blockedLanes = new HashSet<int>();
        public IReadOnlyCollection<int> BlockedLanes => _blockedLanes;

        public void TrySpawn(Tile tile)
        {
            _blockedLanes.Clear();
            // [TẠM] chẩn đoán — log trạng thái mỗi 2s
            if (Time.time - _lastDiagLog > 2f)
            {
                _lastDiagLog = Time.time;
                Debug.Log($"[DiagObstacle] types={(obstacleTypes == null ? 0 : obstacleTypes.Length)} chance={CurrentSpawnChance}");
            }

            if (obstacleTypes == null || obstacleTypes.Length == 0) return;
            if (Random.value > CurrentSpawnChance) return;

            ObstacleData data = PickRandomType();
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning($"[DiagObstacle] data rỗng hoặc prefab null (data={(data == null ? "null" : data.name)})");
                return;
            }

            // Chặn ngẫu nhiên 1..laneCount-1 lane → luôn còn ≥1 lane trống
            int blockedLanes = Random.Range(1, laneCount);
            HashSet<int> chosen = new HashSet<int>();
            while (chosen.Count < blockedLanes)
            {
                chosen.Add(Random.Range(0, laneCount));
            }

            foreach (int lane in chosen)
            {
                _blockedLanes.Add(lane);
                float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;
                SpawnOnTile(data, tile, x);
            }
        }

        private void SpawnOnTile(ObstacleData data, Tile tile, float x)
        {
            // [TẠM] chẩn đoán — log WORLD position + tile rotation (để biết tile có bị xoay không)
            Debug.Log($"[DiagObstacle] TẠO {data.name} lane={x} tileRot={tile.transform.eulerAngles} tilePos={tile.transform.position}");
            GameObject obstacle = Instantiate(data.prefab, tile.transform);
            // y=0.5: obstacle nằm trên mặt road (road surface ở y≈0.05) — trigger nên không cần vật lý đặt xuống
            obstacle.transform.localPosition = new Vector3(x, 0.5f, Random.Range(0f, tile.Length * 0.6f));
            Debug.Log($"[DiagObstacle] WORLD {obstacle.transform.position} local={obstacle.transform.localPosition} parent={obstacle.transform.parent.name} renderer={obstacle.GetComponent<Renderer>()?.enabled}");

            Obstacle comp = obstacle.GetComponent<Obstacle>();
            if (comp == null)
            {
                comp = obstacle.AddComponent<Obstacle>();
            }
            comp.SetData(data); // gán data đã chọn — component biết mình thuộc loại nào
        }

        private ObstacleData PickRandomType()
        {
            float total = 0f;
            foreach (ObstacleData d in obstacleTypes)
            {
                if (d != null) total += Mathf.Max(0f, d.spawnWeight);
            }
            if (total <= 0f)
            {
                return obstacleTypes[Random.Range(0, obstacleTypes.Length)];
            }

            float r = Random.value * total;
            foreach (ObstacleData d in obstacleTypes)
            {
                if (d == null) continue;
                r -= Mathf.Max(0f, d.spawnWeight);
                if (r <= 0f) return d;
            }
            return obstacleTypes[obstacleTypes.Length - 1];
        }
    }
}
