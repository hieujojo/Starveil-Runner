#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool gắn SKYBOX cho scene đang mở — dùng 1 trong 2 gói user đã import:
    ///   • "Nebula Skyboxes" (4 cubemap .exr, import sẵn dạng Cube) — tạo material Skybox/Cubemap
    ///   • "SpaceSkies Free" (Purple 2K có material sẵn) — fallback / lựa chọn nhẹ
    ///
    /// Lý do tồn tại: Game scene đang camera `ClearFlags = Solid Color` (m_ClearFlags: 2)
    /// → có gán skybox vào RenderSettings cũng KHÔNG hiện (skybox chỉ vẽ khi camera clear = Skybox).
    /// Tool tự: tạo/load material skybox → gán RenderSettings.skyboxMaterial → ép camera
    /// ClearFlags = Skybox. Idempotent — chạy lại không nhân đôi, chạy cho cả Game + MainMenu.
    ///
    /// Chạy: Tools → Void Runner → Setup Skybox (Nebula) / Setup Skybox (SpaceSkies Purple).
    /// </summary>
    public static class SkyboxSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        private const string NebulaTexPath = "Assets/Nebula Skyboxes/Nebula_02_Cubemap.exr"; // tím đỏ hư không
        private const string NebulaMatPath = "Assets/_Project/Materials/Skybox/NebulaSkybox.mat";
        private const string SpaceSkiesMatPath = "Assets/SpaceSkies Free/Skybox_3/Purple_2K_Resolution.mat";
        private const string FallbackSpaceskies = "Assets/SpaceSkies Free/Skybox_2/Green_2K_Resoution.mat";

        [MenuItem(MenuRoot + "Setup Skybox (Nebula — tinh vân hư không)")]
        public static void SetupNebula() => Setup(useNebula: true);

        [MenuItem(MenuRoot + "Setup Skybox (SpaceSkies Purple — nhẹ hơn)")]
        public static void SetupSpaceSkies() => Setup(useNebula: false);

        private static void Setup(bool useNebula)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game" && scene.name != "MainMenu")
            {
                EditorUtility.DisplayDialog("Void Runner — Skybox",
                    $"Scene đang mở là '{scene.name}'. Mở scene Game hoặc MainMenu rồi chạy lại.", "OK");
                return;
            }

            Material skybox = useNebula ? GetOrCreateNebulaMaterial() : LoadSpaceSkiesMaterial();

            if (skybox == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Skybox",
                    "Không tạo được material skybox (thiếu gói asset?). Kiểm tra 'Nebula Skyboxes' / 'SpaceSkies Free' đã import.", "OK");
                return;
            }

            // Gán skybox vào RenderSettings của scene
            RenderSettings.skybox = skybox;

            // ⚠️ BẮT BUỘC: camera phải ClearFlags = Skybox (m_ClearFlags: 1) — nếu đang Solid Color (2)
            // thì skybox không bao giờ được vẽ (bug 2026-08-11: gán xong không thấy).
            int cameras = 0;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.clearFlags != CameraClearFlags.Skybox)
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                }
                cameras++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[VoidRunner] Skybox: '{skybox.name}' gán cho scene '{scene.name}' ({cameras} camera → Skybox).");
            EditorUtility.DisplayDialog("Void Runner — Skybox",
                $"✓ Skybox '{skybox.name}' đã gán cho scene '{scene.name}'.\n\n" +
                $"Lặp lại cho scene còn lại (Game + MainMenu).\nNhớ Ctrl+S lưu cả 2 scene.", "OK");
        }

        /// <summary>Tạo material Skybox/Cubemap từ Nebula exr (nếu chưa có trên đĩa — idempotent).</summary>
        private static Material GetOrCreateNebulaMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(NebulaMatPath);
            if (existing != null) return existing;

            if (!File.Exists(NebulaTexPath))
            {
                Debug.LogWarning("[Skybox] Không thấy Nebula exr — fallback SpaceSkies.");
                return LoadSpaceSkiesMaterial();
            }

            // Load cubemap đã import (textureShape: Cube, maxTextureSize 2048)
            var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(NebulaTexPath);
            if (cubemap == null)
            {
                Debug.LogWarning("[Skybox] Nebula exr chưa import dạng Cubemap — fallback SpaceSkies.");
                return LoadSpaceSkiesMaterial();
            }

            var shader = Shader.Find("Skybox/Cubemap");
            if (shader == null)
            {
                Debug.LogError("[Skybox] Không tìm thấy shader 'Skybox/Cubemap'.");
                return LoadSpaceSkiesMaterial();
            }

            string dir = Path.GetDirectoryName(NebulaMatPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh(); // ⚠️ bắt buộc — nếu không AssetDatabase chưa biết folder → CreateAsset fail (góp ý reviewer)
            }

            var mat = new Material(shader) { name = "NebulaSkybox" };
            mat.SetTexture("_Tex", cubemap);
            AssetDatabase.CreateAsset(mat, NebulaMatPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        /// <summary>Load material SpaceSkies có sẵn (Purple 2K — tông tím khớp game).</summary>
        private static Material LoadSpaceSkiesMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SpaceSkiesMatPath);
            if (mat == null)
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>(FallbackSpaceskies);
            }
            return mat;
        }

        [MenuItem(MenuRoot + "Setup Skybox (Nebula — tinh vân hư không)", true)]
        [MenuItem(MenuRoot + "Setup Skybox (SpaceSkies Purple — nhẹ hơn)", true)]
        private static bool Validate()
        {
            var scene = SceneManager.GetActiveScene();
            return scene.name == "Game" || scene.name == "MainMenu";
        }
    }
}
#endif
