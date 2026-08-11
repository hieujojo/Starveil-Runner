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
            // material 3rd-party dùng shader Standard Built-in → TÍM trong URP.
            // Self-heal R3.16/R4.18: mọi obstacle spawn ra tự ép material URP/Lit giữ màu gốc.
            MaterialFixer.EnsureURPMaterials(gameObject);

            // FIX 2026-08-12 v3f.5 (user: "đọc log — drone chưa đúng, lệch +1.5m sang phải"): model
            // 3rd-party có PIVOT LỆCH (Robot_Guardian mesh lệch ~+1.5 dù tool đã bù +1.45 — chênh 0.05).
            // Self-heal: căn giữa model con theo renderer bounds THẬT lúc spawn (không phụ thuộc prefab).
            CenterModelOnLane();

            // FIX 2026-08-12 v3f.5 (user: "chỉ drone + vài hiệu ứng là đủ cho game vũ trụ"): hiệu ứng
            // ambient — đèn cảnh báo đỏ + hạt năng lượng + lơ lửng/xoay. Tạo RUNTIME (không nướng vào
            // prefab — tránh R3.1: material runtime không serialize → fileID 0 → màu tím).
            if (GetComponent<ObstacleFX>() == null) gameObject.AddComponent<ObstacleFX>();
        }

        /// <summary>
        /// Căn giữa model con ("Model") theo bounds thật trên trục X/Z — mesh nằm ĐÚNG TÂM lane
        /// dù pivot model lệch. Chỉ dịch X/Z (vị trí lane/tile), Y do prefab kiểm soát. Idempotent.
        /// </summary>
        private void CenterModelOnLane()
        {
            Transform model = transform.Find("Model");
            if (model == null) return;

            Bounds b = GetRenderBounds(model.gameObject);
            if (!b.IsValid()) return; // không có renderer → bỏ qua

            // Độ lệch giữa tâm mesh (world) và vị trí root (lane/tile) — dịch ngược để về tâm
            Vector3 offset = b.center - transform.position;
            if (Mathf.Abs(offset.x) < 0.05f && Mathf.Abs(offset.z) < 0.05f) return; // đã đúng tâm
            model.localPosition -= new Vector3(offset.x, 0f, offset.z);
        }

        private static Bounds GetRenderBounds(GameObject go)
        {
            Bounds bounds = default; // kích thước 0 → IsValid() == false khi không có renderer (góp ý reviewer v3f.5)
            bool has = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (has) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; has = true; }
            }
            return bounds;
        }
    }
}
