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
        // Road rộng (fix 2026-08-11 — user: "đường quá nhỏ"): half 7 → 9 (cả đường 18 đơn vị),
        // khớp Ground scale x=18 + laneWidth=3. Đồng bộ mọi nơi: AmbientScroller roadHalfWidth=9.
        [SerializeField] private float roadHalfWidth = 9f;

        /// <summary>Material dùng chung cho mọi lane marker (tạo 1 lần, tông cyan neon).</summary>
        private static Material _laneMat;

        /// <summary>Material road tối (fallback).</summary>
        private static Material _roadMat;

        public float Length => length;

        private void Awake()
        {
            // ⚠️ KHÔNG scale ROOT tile (bug 2026-08-11 — gốc rễ "không thấy vật cản/xu"):
            // Unity nhân scale của parent vào VỊ TRÍ lẫn KÍCH THƯỚC của mọi con → obstacle/coin spawn
            // tại lane x=3 thực tế ở world x=42 (xa ngoài đường ±7) và bị dẹt cao 0.1 → vô hình.
            // Root luôn scale (1,1,1); road visual phải là CHILD "Road" có scale riêng.
            transform.localScale = Vector3.one;
            EnsureRoadVisual();
            BuildLaneMarkers();
        }

        /// <summary>
        /// Road visual nằm trên child "Road" (cube roadHalfWidth*2 × 0.1 × length).
        /// Di chuyển mesh/material từ root (prefab cũ scale z=0 vô hình) xuống child;
        /// bỏ collider root (1×1×1 solid gây bump khi tile recycle). Idempotent.
        /// </summary>
        private void EnsureRoadVisual()
        {
            Transform road = transform.Find("Road");

            if (road == null)
            {
                var rootMf = GetComponent<MeshFilter>();
                var rootMr = GetComponent<MeshRenderer>();

                if (rootMf != null && rootMr != null)
                {
                    var go = new GameObject("Road");
                    go.transform.SetParent(transform, false);
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = rootMf.sharedMesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = rootMr.sharedMaterial;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    go.transform.localScale = new Vector3(roadHalfWidth * 2f, 0.1f, length);
                }
                else
                {
                    // Fallback: primitive cube + material tối (không phụ thuộc prefab)
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Road";
                    go.transform.SetParent(transform, false);
                    go.transform.localScale = new Vector3(roadHalfWidth * 2f, 0.1f, length);
                    var col = go.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    go.GetComponent<MeshRenderer>().sharedMaterial = CreateRoadMaterial();
                }
            }
            else
            {
                road.localScale = new Vector3(roadHalfWidth * 2f, 0.1f, length);
            }

            // Dọn root: bỏ mesh cube cũ (scale z=0 vô hình) + collider solid 1×1×1
            MeshFilter rmf = GetComponent<MeshFilter>();
            if (rmf != null) Destroy(rmf);
            MeshRenderer rmr = GetComponent<MeshRenderer>();
            if (rmr != null) Destroy(rmr);
            Collider rcol = GetComponent<Collider>();
            if (rcol != null) Destroy(rcol);
        }

        /// <summary>Material road tối (fallback khi prefab không có material).</summary>
        private static Material CreateRoadMaterial()
        {
            if (_roadMat != null) return _roadMat;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            _roadMat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            _roadMat.color = new Color(0.1f, 0.08f, 0.16f, 1f);
            _roadMat.SetFloat("_Metallic", 0f);
            _roadMat.SetFloat("_Smoothness", 0.4f);
            return _roadMat;
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
            // Dọn obstacle/pickup con về pool — GIỮ lane markers + ROAD visual (road là child scale riêng,
            // không được xóa khi recycle — bug 2026-08-11 nếu xóa thì tile hết mặt đường sau vòng đầu)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "LaneMarker" || child.name == "Road") continue;
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
