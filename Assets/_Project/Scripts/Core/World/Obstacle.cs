using UnityEngine;
using VoidRunner.Data;

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
        }
    }
}
