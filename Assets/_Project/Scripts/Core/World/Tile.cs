using UnityEngine;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Một đoạn track — được tái sử dụng qua ObjectPool (không Instantiate/Destroy giữa chừng).
    ///
    /// Fix 2026-08-11: (1) tile prefab có scale z=0 → khối cube bị dẹt vô hình → chỉ còn Ground
    /// tĩnh bên dưới → KHÔNG có vật gì trượt qua → mất cảm giác chuyển động. Tile giờ tự ép
    /// scale z = length trong Awake để luôn là khối 10×0.1×10 nhìn thấy được.
    /// (2) thêm Lane Markers (vạch neon dọc + vạch đứt giữa) là con của tile → trượt theo tile
    /// khi recycle → người chơi cảm nhận tốc độ rõ rệt.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [SerializeField] private float length = 10f;
        // Road rộng hơn (fix 2026-08-11 — user: "đường quá nhỏ"): half 5 → 7 (cả đường 14 đơn vị),
        // khớp Ground scale x=14 + laneWidth=3 mà tool Refactor set trong scene.
        [SerializeField] private float roadHalfWidth = 7f;

        /// <summary>Material dùng chung cho mọi lane marker (tạo 1 lần, tông cyan neon).</summary>
        private static Material _laneMat;

        public float Length => length;

        private void Awake()
        {
            // Ép đúng chiều dài — prefab cũ scale z=0 (tile vô hình) là bug gây mất cảm giác chuyển động
            transform.localScale = new Vector3(roadHalfWidth * 2f, 0.1f, length);
            BuildLaneMarkers();
        }

        /// <summary>Tạo 2 vạch neon 2 mép + vạch đứt đoạn giữa đường (con của tile → trượt cùng tile).</summary>
        private void BuildLaneMarkers()
        {
            EnsureMaterial();

            // 2 vạch liền 2 mép (cách mép 0.4) — khung đường sáng
            CreateMarker(new Vector3(-roadHalfWidth + 0.4f, 0.06f, 0f), new Vector3(0.25f, 0.05f, length));
            CreateMarker(new Vector3(roadHalfWidth - 0.4f, 0.06f, 0f), new Vector3(0.25f, 0.05f, length));

            // Vạch đứt đoạn giữa đường — đoạn dài 2.5, nghỉ 2.5 → trượt rõ rệt
            for (float z = -length / 2f; z < length / 2f; z += 5f)
            {
                CreateMarker(new Vector3(0f, 0.06f, z + 1.25f), new Vector3(0.3f, 0.05f, 2.5f));
            }
        }

        private void CreateMarker(Vector3 localPos, Vector3 localScale)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "LaneMarker";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = localPos;
            marker.transform.localScale = localScale;

            // Bỏ collider — chỉ render, không va chạm
            var col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = marker.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _laneMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static void EnsureMaterial()
        {
            if (_laneMat != null) return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            _laneMat = shader != null ? new Material(shader) : new Material(Shader.Find("Unlit/Color"));
            _laneMat.color = new Color(0.2f, 0.8f, 1f, 1f); // cyan neon nổi trên nền tối
        }

        public void Activate(Vector3 position)
        {
            transform.position = position;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            // Dọn obstacle/pickup con về pool — GIỮ lane markers (cảm giác chuyển động liên tục)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "LaneMarker") continue;
                Destroy(child.gameObject);
            }
            gameObject.SetActive(false);
        }

        public bool IsBehind(Transform reference, float distance)
        {
            return transform.position.z < reference.position.z - distance;
        }
    }
}
