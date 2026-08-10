using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// G3 - Material/Lighting: đồng bộ tông "hư không" (tím/đen phát sáng neon).
    /// Chạy: Tools → Void Runner → Setup Material & Lighting (Open Scene).
    /// Idempotent — chạy lại an toàn, chạy cho cả Game + MainMenu.
    /// </summary>
    public static class MaterialLightingSetupTool
    {
        private const string MaterialsFolder = "Assets/_Project/Art/Materials";

        [MenuItem("Tools/Void Runner/Setup Material & Lighting (Open Scene)")]
        public static void SetupMaterialAndLighting()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("[VoidRunner] Vui lòng lưu scene trước (Ctrl+S) rồi chạy lại tool.");
                return;
            }

            SetupMaterials();
            SetupLighting();

            Debug.Log($"[VoidRunner] Material & Lighting sẵn sàng trong scene '{scene.name}': " +
                      "5 material tông hư không + Directional Light lạnh + ambient/fog tím.\n" +
                      "Nhớ Ctrl+S lưu scene.");
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Chỉnh 5 material theo tông "hư không": nền tím đen, nhân vật phát sáng neon.</summary>
        private static void SetupMaterials()
        {
            SetMaterial("Background.mat",
                new Color(0.04f, 0.03f, 0.09f, 1f), // tím đen rất tối
                Color.black, 0.1f, 0.2f);            // không phát sáng, mờ

            SetMaterial("Player.mat",
                new Color(0f, 0.86f, 1f, 1f),        // cyan neon
                new Color(0f, 0.35f, 0.5f, 1f),      // phát sáng cyan nhẹ
                0.1f, 0.7f);

            SetMaterial("Enemy.mat",
                new Color(0.65f, 0.1f, 0.8f, 1f),    // tím hồng (đậm hơn — chống chói)
                new Color(0.35f, 0.05f, 0.5f, 1f),   // phát sáng tím nhẹ
                0.1f, 0.6f);

            SetMaterial("PickUp.mat",
                new Color(1f, 0.78f, 0f, 1f),        // vàng coin
                new Color(0.5f, 0.4f, 0f, 1f),       // phát sáng vàng nhẹ
                0.2f, 0.6f);

            SetMaterial("Dynamic Obstacle.mat",
                new Color(1f, 0.3f, 0f, 1f),         // cam cảnh báo
                new Color(0.4f, 0.12f, 0f, 1f),      // phát sáng cam nhẹ
                0.2f, 0.5f);
        }

        /// <summary>Load material theo tên và set màu/emission/metallic/smoothness (URP Lit).</summary>
        private static void SetMaterial(string fileName, Color baseColor, Color emission, float metallic, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{fileName}");
            if (mat == null)
            {
                Debug.LogWarning($"[VoidRunner] Không tìm thấy material {fileName} — bỏ qua.");
                return;
            }

            // URP Lit properties
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            // Emission: kích hoạt keyword + màu (Bloom sẽ làm phát sáng)
            bool hasEmission = emission.maxColorComponent > 0.001f;
            if (hasEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
        }

        /// <summary>Chỉnh Directional Light (trắng lạnh) + ambient + fog tím cho scene đang mở.</summary>
        private static void SetupLighting()
        {
            // 1. Directional Light — trắng hơi lạnh, đủ sáng nhưng không chói
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                Debug.LogWarning("[VoidRunner] Không tìm thấy Directional Light — bỏ qua bước ánh sáng.");
            }
            else
            {
                sun.color = new Color(0.85f, 0.9f, 1f, 1f);
                sun.intensity = 1.1f;
                sun.shadows = LightShadows.Soft;
            }

            // 2. Ambient — tối tím nhẹ (không dùng skybox làm ambient vì muốn tông tối hơn)
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.1f, 0.2f, 1f);

            // 3. Fog — tím tối nhẹ, tạo chiều sâu "hư không"
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.12f, 1f);
        }
    }
}
