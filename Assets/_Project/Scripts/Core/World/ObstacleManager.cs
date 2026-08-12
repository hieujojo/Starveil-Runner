using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Data;
using VoidRunner.Systems.Difficulty;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Gắn obstacle lên tile khi tile được spawn.
    /// Luôn chừa ít nhất 1 lane an toàn để người chơi có đường né.
    ///
    /// v3f.10.3 (user: "spawn hợp lý theo từng độ khó, tránh trống trải hoặc dày đặc, mọi thứ
    /// đồng đều"): thay Random.value thuần bằng LINEAR SCHEDULING — mỗi tile tích lũy
    /// CurrentSpawnChance (tăng theo DifficultyManager), đạt ≥1 thì spawn tile này (trừ 1).
    /// Mật độ trung bình ĐÚNG bằng xác suất yêu cầu nhưng phân bố ĐỀU: không bao giờ nhiều
    /// tile trống liên tiếp (cảm giác trống trải) cũng không bao giờ nhiều tile dày liên tiếp
    /// (cảm giác dày đặc). Random trước đây = cụm ngẫu nhiên (variance cao).
    /// </summary>
    public class ObstacleManager : MonoBehaviour
    {
        [Header("Cấu hình")]
        [SerializeField] private ObstacleData[] obstacleTypes;
        [SerializeField, Tooltip("Xác suất cơ bản — DifficultyManager có thể tăng dần")]
        private float spawnChance = 0.45f;
        [SerializeField] private int laneCount = 3;
        [SerializeField] private float laneWidth = 2f;

        /// <summary>Bộ tích lũy linear scheduling — spawn đều theo xác suất (v3f.10.3).</summary>
        private float _spawnAccumulator;

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

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetSchedule;
            GameEvents.OnRestart += ResetSchedule;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetSchedule;
            GameEvents.OnRestart -= ResetSchedule;
        }

        /// <summary>Bắt đầu ván mới → bộ tích lũy về 0 (mật độ đều từ đầu, không giữ dư nợ ván cũ).</summary>
        private void ResetSchedule() => _spawnAccumulator = 0f;

        /// <summary>
        /// Các lane đã chặn ở tile gần nhất (reset mỗi TrySpawn) — PickupSpawner đọc để đặt hàng coin
        /// sang lane KHÁC, tránh obstacle đè lên xu (fix 2026-08-11).
        /// </summary>
        private readonly HashSet<int> _blockedLanes = new HashSet<int>();
        public IReadOnlyCollection<int> BlockedLanes => _blockedLanes;

        /// <summary>Xóa trạng thái lane chặn của tile trước (TileSpawner gọi khi tile thuộc safe zone —
        /// không spawn obstacle nên không gọi TrySpawn → phải clear tay, không thì coin tránh lane cũ vô cớ).</summary>
        public void ClearBlockedLanes() => _blockedLanes.Clear();

        public void TrySpawn(Tile tile)
        {
            _blockedLanes.Clear();
            if (obstacleTypes == null || obstacleTypes.Length == 0) return;

            // v3f.10.3 — linear scheduling (xem class doc): spawn đều theo mật độ yêu cầu
            _spawnAccumulator += CurrentSpawnChance;
            if (_spawnAccumulator < 1f) return;
            _spawnAccumulator -= 1f;

            ObstacleData data = PickRandomType();
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning($"[ObstacleManager] data rỗng hoặc prefab null (data={(data == null ? "null" : data.name)})");
                return;
            }

            // v3f.10.3 (góp ý reviewer): số lane chặn THEO ĐỘ KHÓ — đầu game chủ yếu 1 drone
            // (dễ né), càng về cuối (spawnChance càng cao) càng hay chặn 2 lane (khó dần) —
            // vẫn luôn ≥1 lane trống (2 = laneCount-1 tối đa). 0.45→0%, 0.75→60% chặn 2 lane.
            float twoLaneChance = Mathf.InverseLerp(0.45f, 0.75f, CurrentSpawnChance) * 0.6f;
            int blockedLanes = Random.value < twoLaneChance ? 2 : 1;
            HashSet<int> chosen = new HashSet<int>();
            while (chosen.Count < blockedLanes)
            {
                chosen.Add(Random.Range(0, laneCount));
            }

            // v3f.10.3 — spacing z giữa các obstacle CÙNG tile: lane đầu ở [0, len*0.35], lane thứ 2
            // ở [len*0.4, len*0.65] → luôn cách nhau ≥ len*0.05 (0.5m) + dàn đều không cụm 1 chỗ
            int index = 0;
            foreach (int lane in chosen)
            {
                _blockedLanes.Add(lane);
                float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;
                float z = index == 0
                    ? Random.Range(0f, tile.Length * 0.35f)
                    : Random.Range(tile.Length * 0.4f, tile.Length * 0.65f);
                SpawnOnTile(data, tile, x, z);
                index++;
            }
        }

        private void SpawnOnTile(ObstacleData data, Tile tile, float x, float z)
        {
            GameObject obstacle = Instantiate(data.prefab, tile.transform);
            // y=0.5: obstacle nằm trên mặt road (road surface ở y≈0.05) — trigger nên không cần vật lý đặt xuống
            obstacle.transform.localPosition = new Vector3(x, 0.5f, z);

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
