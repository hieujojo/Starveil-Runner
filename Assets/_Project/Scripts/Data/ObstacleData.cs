using UnityEngine;

namespace VoidRunner.Data
{
    /// <summary>
    /// Cấu hình một loại obstacle (ScriptableObject) — tách data khỏi logic.
    /// Tạo asset: Assets → Create → VoidRunner → Obstacle Data.
    /// </summary>
    public enum ObstacleType
    {
        Pillar,    // cố định, phải né sang lane khác
        Ramp,      // thấp, có thể lăn qua
        Dynamic    // di động (xê dịch) — xử lý chi tiết ở G2
    }

    [CreateAssetMenu(fileName = "ObstacleData", menuName = "VoidRunner/Obstacle Data")]
    public class ObstacleData : ScriptableObject
    {
        [Header("Thông tin")]
        public ObstacleType obstacleType;
        public string obstacleName;

        [Tooltip("Prefab của obstacle (cần có collider + component Obstacle)")]
        public GameObject prefab;

        [Header("Spawn")]
        [Range(0f, 1f)]
        [Tooltip("Trọng số xuất hiện so với các loại khác")]
        public float spawnWeight = 0.5f;

        [Tooltip("Obstacle di động (xê dịch qua lại) — bật xử lý ở G2")]
        public bool isDynamic;
    }
}
