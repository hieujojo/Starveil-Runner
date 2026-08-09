using UnityEngine;

namespace VoidRunner.Data
{
    /// <summary>Các loại power-up trong game.</summary>
    public enum PowerUpType
    {
        Shield,  // miễn nhiễm 1 lần va chạm obstacle
        Magnet,  // hút coin quanh player
        SlowMo   // làm chậm thời gian (Time.timeScale) — Void chậm lại
    }

    /// <summary>
    /// Cấu hình một loại power-up (ScriptableObject) — tách data khỏi logic.
    /// Tạo asset: Assets → Create → VoidRunner → PowerUp Data.
    /// </summary>
    [CreateAssetMenu(fileName = "PowerUpData", menuName = "VoidRunner/PowerUp Data")]
    public class PowerUpData : ScriptableObject
    {
        [Header("Thông tin")]
        public PowerUpType powerUpType;
        public string powerUpName;

        [Tooltip("Prefab pickup (cần collider trigger + component PowerUpPickup)")]
        public GameObject prefab;

        [Header("Hiệu ứng")]
        [Tooltip("Thời gian hiệu lực (giây)")]
        public float duration = 3f;

        [Header("Magnet")]
        [Tooltip("Bán kính hút coin quanh player (chỉ dùng cho Magnet)")]
        public float magnetRadius = 6f;

        [Header("Slow-mo")]
        [Range(0.1f, 0.9f)]
        [Tooltip("Time.timeScale khi kích hoạt (chỉ dùng cho SlowMo)")]
        public float slowMoScale = 0.5f;

        [Header("Spawn")]
        [Range(0f, 1f)]
        [Tooltip("Trọng số xuất hiện so với các loại khác")]
        public float spawnWeight = 0.5f;
    }
}
