using System.Collections.Generic;
using UnityEngine;

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

        [Tooltip("Khoảng cách 2 bên so với tâm track (mép road ở ±5, prop đặt sát ±6).")]
        [SerializeField] private float sideOffset = 6f;

        [Tooltip("Khoảng cách giữa 2 prop liên tiếp trên cùng 1 bên.")]
        [SerializeField] private float spacing = 9f;

        [Tooltip("Số prop mỗi bên (tổng prop = 2 × count).")]
        [SerializeField] private int countPerSide = 10;

        [Tooltip("Khi prop lùi sau player quá khoảng này thì dịch lên phía trước.")]
        [SerializeField] private float recycleDistance = 18f;

        [Tooltip("Tỉ lệ prop được đặt lệch vị trí ngẫu nhiên (0 = đều tăm tắp).")]
        [SerializeField, Range(0f, 1f)] private float jitter = 0.4f;

        [Tooltip("Scale ngẫu nhiên của prop (0 = đồng đều).")]
        [SerializeField, Range(0f, 1f)] private float scaleVariation = 0.3f;

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

        /// <summary>Dựng lại toàn bộ prop 2 bên (gọi khi tool chạy / restart).</summary>
        public void BuildProps()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            _props.Clear();

            if (propPrefabs.Count == 0 || player == null) return;

            float startZ = player.position.z - countPerSide * spacing * 0.5f;

            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -sideOffset : sideOffset;
                for (int i = 0; i < countPerSide; i++)
                {
                    GameObject prefab = propPrefabs[Random.Range(0, propPrefabs.Count)];

                    float z = startZ + i * spacing;
                    float jitterZ = jitter > 0f ? Random.Range(-spacing * jitter, spacing * jitter) : 0f;
                    float rotY = Random.Range(0f, 360f);
                    float scale = 1f + (scaleVariation > 0f ? Random.Range(-scaleVariation, scaleVariation) : 0f);

                    GameObject prop = Instantiate(prefab, transform);
                    prop.name = $"{prefab.name} ({(side == 0 ? "L" : "R")}{i})";
                    prop.transform.position = new Vector3(x, 0f, z + jitterZ);
                    prop.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
                    prop.transform.localScale = Vector3.one * scale;

                    // FBX Kenney có material trắng sáng → đổi sang material tối tím để không chói mắt
                    // + không bị Bloom thổi phồng (bài học: FBX trắng + Bloom = chói)
                    ApplyDarkMaterial(prop);

                    _props.Add(prop.transform);
                }
            }
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
