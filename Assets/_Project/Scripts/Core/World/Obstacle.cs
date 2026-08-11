using UnityEngine;
using VoidRunner.Data;
using VoidRunner.Utils;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Marker component cho obstacle — Player phát hiện bằng TryGetComponent (không phụ thuộc tag string).
    /// Giữ tham chiếu ObstacleData để truy vấn cấu hình (loại, prefab...) từ component.
    /// Tự bật isTrigger cho collider để Player dùng OnTriggerEnter.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleData _data;
        public ObstacleData Data => _data;

        /// <summary>Gán data khi spawn từ ObstacleManager (runtime) — data cũng có thể gán trực tiếp trên prefab.</summary>
        public void SetData(ObstacleData data) => _data = data;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // FIX 2026-08-12 (user: "vật thể màu tím chứ ko phải màu nguyên bản — tưởng tuân rule rồi"):
            // material 3rd-party (OlegWER asteroid) dùng shader Standard Built-in → TÍM trong URP.
            // Trước đây MaterialFixer chỉ áp dụng cho tàu (PlayerController/ShipSelectManager) — sót OBSTACLE.
            // Self-heal R3.16/R4.18: mọi obstacle spawn ra tự ép material URP/Lit giữ màu gốc.
            MaterialFixer.EnsureURPMaterials(gameObject);
        }
    }
}
