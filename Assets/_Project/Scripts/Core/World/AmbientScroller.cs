using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Trang trí 2 bên đường (ambient): cột trụ / trạm vũ trụ từ Kenney Space Kit.
    /// Hoạt động như Object Pool mini: các prop được đặt dọc 2 bên track và tái sử dụng
    /// (dịch lên phía trước khi lùi sau player) — không Instantiate/Destroy trong lúc chơi.
    /// Editor tool `Setup Ambient in Game Scene` tự dựng + gán propPrefabs.
    /// </summary>
    public class AmbientScroller : MonoBehaviour
    {
        [Header("Cấu hình ambient")]
        [Tooltip("Các mô hình FBX (Space Kit) — tool Setup Ambient tự gán.")]
        [SerializeField] private List<GameObject> propPrefabs = new List<GameObject>();

        [Tooltip("Khoảng cách 2 bên so với tâm track — phải lớn hơn nửa chiều rộng prop.")]
        [SerializeField] private float sideOffset = 11f;

        [Tooltip("Chiều cao chuẩn hóa của prop (đơn vị) — nhỏ = prop nhỏ, không đè lên road.")]
        [SerializeField] private float targetHeight = 3.2f;

        [Tooltip("Bề ngang tối đa chuẩn hóa của prop (đơn vị) — chặn model bề ngang khổng lồ (gate/pipe) đè lên road.")]
        [SerializeField] private float targetWidth = 3.5f;

        [Tooltip("Khoảng cách giữa 2 prop liên tiếp trên cùng 1 bên.")]
        [SerializeField] private float spacing = 9f;

        [Tooltip("Số prop mỗi bên (tổng prop = 2 × count).")]
        [SerializeField] private int countPerSide = 10;

        [Tooltip("Khi prop lùi sau player quá khoảng này thì dịch lên phía trước.")]
        [SerializeField] private float recycleDistance = 18f;

        [Tooltip("Tỉ lệ prop được đặt lệch vị trí ngẫu nhiên (0 = đều tăm tắp).")]
        [SerializeField, Range(0f, 1f)] private float jitter = 0.15f;

        [Tooltip("Độ lệch xoay Y ngẫu nhiên tối đa (độ) — nhỏ để prop đứng ngay ngắn.")]
        [SerializeField, Range(0f, 90f)] private float maxRotY = 20f;

        [Tooltip("Scale ngẫu nhiên của prop (0 = đồng đều).")]
        [SerializeField, Range(0f, 1f)] private float scaleVariation = 0.1f;

        [Header("Tham chiếu (tự gán)")]
        [SerializeField] private Transform player;

        private readonly List<Transform> _props = new List<Transform>();

        public void Initialize(Transform playerRef, IReadOnlyList<GameObject> prefabs)
        {
            player = playerRef;
            if (prefabs != null)
            {
                propPrefabs.Clear();
                propPrefabs.AddRange(prefabs);
            }
            BuildProps();
        }

        private void Start()
        {
            // GameManager KHÔNG gọi Initialize ở runtime (chỉ Editor tool) → props được tool dựng cứng
            // trong scene KHÔNG nằm trong _props → Update recycle return sớm → hết props sau 1 quãng
            // đường (triệu chứng "chỉ phần đầu có cảnh vật"). Tự heal:
            // 1) nạp các con hiện có vào pool; 2) ép lại scale/vị trí chuẩn (chặn bề ngang + ngoài road).
            if (player == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }

            if (_props.Count == 0 && transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.GetComponentInChildren<Renderer>() == null) continue;
                    HealProp(child);
                    _props.Add(child);
                }
                Debug.Log($"[DiagProp] Start-heal: nạp {_props.Count} prop có sẵn trong scene (scale + vị trí chuẩn)");
            }
        }

        /// <summary>
        /// Ép 1 prop (đã dựng cứng trong scene bằng tool) về đúng chuẩn:
        /// scale chặn cả chiều cao lẫn bề ngang + đặt x ngoài road theo bounds THỰC.
        /// (Props bản cũ dùng code chỉ chuẩn chiều cao → bề ngang khổng lồ đè lên road.)
        /// </summary>
        private void HealProp(Transform prop)
        {
            const float roadHalfWidth = 7f;  // road rộng 14 (bán kính = 7)
            const float roadMargin = 1.5f;   // mép prop cách mép road tối thiểu 1.5m

            float scale = NormalizeScale(prop.gameObject);
            prop.localScale = Vector3.one * scale;

            Vector3 size = GetRenderBoundsSize(prop.gameObject);
            float halfWidth = Mathf.Max(size.x, 1f) * 0.5f;
            float side = prop.position.x >= 0f ? 1f : -1f;
            float x = side * Mathf.Max(sideOffset, roadHalfWidth + halfWidth + roadMargin);

            Vector3 pos = prop.position;
            pos.x = x;
            prop.position = pos;
        }

        /// <summary>Dựng lại toàn bộ prop 2 bên (gọi khi tool chạy / restart).</summary>
        public void BuildProps()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            _props.Clear();

            if (propPrefabs.Count == 0 || player == null) return;

            const float roadHalfWidth = 7f;   // road rộng 14 (bán kính = 7)
            const float roadMargin = 1.5f;    // mép prop cách mép road tối thiểu 1.5m

            float startZ = player.position.z - countPerSide * spacing * 0.5f;

            for (int side = 0; side < 2; side++)
            {
                for (int i = 0; i < countPerSide; i++)
                {
                    GameObject prefab = propPrefabs[Random.Range(0, propPrefabs.Count)];

                    float z = startZ + i * spacing;
                    float jitterZ = jitter > 0f ? Random.Range(-spacing * jitter, spacing * jitter) : 0f;
                    // Xoay nhẹ quanh Y (model hướng về trước) — không xoay lung tung 360°
                    float rotY = Random.Range(-maxRotY, maxRotY);

                    GameObject prop = Instantiate(prefab, transform);
                    prop.name = $"{prefab.name} ({(side == 0 ? "L" : "R")}{i})";
                    prop.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

                    // Scale: chặn CẢ chiều cao lẫn bề ngang (model bề ngang khổng lồ bị thu nhỏ
                    // — gốc rễ "prop đè lên road": NormalizeScale cũ chỉ chuẩn theo chiều cao)
                    float scale = NormalizeScale(prop);
                    scale *= 1f + (scaleVariation > 0f ? Random.Range(-scaleVariation, scaleVariation) : 0f);
                    prop.transform.localScale = Vector3.one * scale;

                    // Đo bề ngang THỰC sau scale → đặt x sao cho prop nằm hẳn NGOÀI road
                    Vector3 size = GetRenderBoundsSize(prop);
                    float halfWidth = Mathf.Max(size.x, 1f) * 0.5f;
                    float x = side == 0
                        ? -(roadHalfWidth + halfWidth + roadMargin)
                        : (roadHalfWidth + halfWidth + roadMargin);
                    // Prop hẹp thì giữ mức sideOffset quen thuộc — không đẩy xa hơn cần thiết
                    x = side == 0 ? Mathf.Min(x, -sideOffset) : Mathf.Max(x, sideOffset);

                    prop.transform.position = new Vector3(x, 0f, z + jitterZ);

                    // FBX Kenney có material trắng sáng → đổi sang material tối tím để không chói mắt
                    // + không bị Bloom thổi phồng (bài học: FBX trắng + Bloom = chói)
                    ApplyDarkMaterial(prop);

                    // [TẠM] chẩn đoán — xác nhận prop không đè lên road (bỏ khi đã chuẩn)
                    float innerEdge = side == 0 ? x + halfWidth : x - halfWidth;
                    Debug.Log($"[DiagProp] {prop.name} side={(side == 0 ? "L" : "R")} x={x:F2} scale={scale:F2} size=({size.x:F2},{size.y:F2},{size.z:F2}) halfW={halfWidth:F2} innerEdge={innerEdge:F2} roadEdge=±{roadHalfWidth}");

                    _props.Add(prop.transform);
                }
            }
        }

        /// <summary>
        /// Tính scale để model có chiều cao ~targetHeight VÀ bề ngang ≤ targetWidth
        /// (đồng nhất kích thước giữa các prop + chặn model bề ngang khổng lồ đè lên road).
        /// </summary>
        private float NormalizeScale(GameObject prop)
        {
            Vector3 size = GetRenderBoundsSize(prop);
            if (size.sqrMagnitude <= 0.001f) return 1f;
            float heightScale = targetHeight / Mathf.Max(size.y, 0.001f);
            float widthScale = targetWidth / Mathf.Max(size.x, 0.001f);
            return Mathf.Min(heightScale, widthScale);
        }

        /// <summary>Bounds world-space gộp mọi renderer của prop (world == local vì AmbientScroller ở gốc, không scale).</summary>
        private Bounds GetRenderBounds(GameObject prop)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool hasBounds = false;
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            {
                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }
            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private Vector3 GetRenderBoundsSize(GameObject prop)
        {
            return GetRenderBounds(prop).size;
        }

        /// <summary>Đổi toàn bộ renderer của prop sang material tối tím (URP Lit, không phát sáng).</summary>
        private void ApplyDarkMaterial(GameObject prop)
        {
            if (_darkMaterial == null)
            {
                _darkMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (_darkMaterial != null)
                {
                    _darkMaterial.color = new Color(0.13f, 0.1f, 0.22f, 1f); // tím đen hư không
                    _darkMaterial.SetFloat("_Smoothness", 0.15f);
                    _darkMaterial.SetFloat("_Metallic", 0f);
                    _darkMaterial.DisableKeyword("_EMISSION");
                }
            }
            if (_darkMaterial == null) return;

            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = _darkMaterial;
            }
        }

        private Material _darkMaterial;

        private void Update()
        {
            if (player == null || _props.Count == 0) return;

            // Recycle: prop nào lùi sau player quá xa → dịch lên phía trước đúng 1 chu kỳ
            float totalLength = countPerSide * spacing;
            for (int i = _props.Count - 1; i >= 0; i--)
            {
                Transform prop = _props[i];
                if (prop.position.z < player.position.z - recycleDistance)
                {
                    prop.position += Vector3.forward * totalLength;
                }
            }
        }
    }
}
