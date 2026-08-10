#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool editor: polish visual cho scene đang mở —
    /// - Camera: bỏ Procedural Skybox sáng (nguồn chói + mặt trời bị Bloom thổi) → Solid Color tím đen \"hư không\"
    /// - FOV camera: tăng lên 60 (đường nhìn rộng hơn, bớt cảm giác hẹp)
    /// - Directional Light: giảm 1.1 → 0.65 (bớt chói)
    /// - Bloom: threshold 0.8 → 1.0, intensity 0.35 → 0.22 (chỉ glow vật THẬT sáng như coin/player, không thổi FBX)
    /// Idempotent: chạy lại chỉ đảm bảo giá trị đúng.
    /// </summary>
    public static class ScenePolishTool
    {
        private const string MenuRoot = "Tools/Void Runner/";
        private const string ProfilePath = "Assets/_Project/Settings/PostProcessing/VoidRunnerProfile.asset";

        [MenuItem(MenuRoot + "Polish Scene (Camera + Sky + Light)")]
        public static void PolishScene()
        {
            int changed = 0;

            // 1) Camera: Solid Color tím đen thay vì skybox sáng
            var cam = FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.012f, 0.05f, 1f); // tím đen sâu
                cam.fieldOfView = 60f; // nhìn rộng hơn — road bớt hẹp
                changed++;
            }

            // 1b) Cinemachine lens đồng bộ FOV
            var vcam = FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Lens.FieldOfView = 60f;
                changed++;
            }

            // 2) Directional Light: giảm cường độ (bớt chói)
            var light = FindAnyObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.intensity = 0.4f;
                changed++;
            }

            // 3) Bloom: threshold 1.15 + intensity 0.12 (tối thiểu — chỉ glow vật cực sáng như coin/player)
            // Dùng TryGet<T> (API runtime) — SerializedObject KHÔNG sửa được sub-asset VolumeComponent.
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null)
            {
                if (profile.TryGet<Bloom>(out var bloom))
                {
                    bloom.threshold.Override(1.15f);
                    bloom.intensity.Override(0.12f);
                    bloom.scatter.Override(0.3f);
                    bloom.tint.value = Color.white;
                    changed++;
                }
                if (profile.TryGet<ColorAdjustments>(out var colorAdj))
                {
                    colorAdj.contrast.Override(12f);
                    colorAdj.saturation.Override(5f);
                    changed++;
                }
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[VoidRunner] Polish scene xong ({changed} mục chỉnh): nền tím đen, FOV 60, light 0.4, Bloom tối thiểu.");
            EditorUtility.DisplayDialog("Void Runner — Polish",
                $"Đã polish scene ({changed} mục):\n• Nền hư không tím đen (bỏ skybox chói)\n• FOV 60 — đường nhìn rộng\n• Ánh sáng dịu 0.4 (tối)\n• Bloom tối thiểu 0.12 — chỉ coin/player glow\n\nNhớ Ctrl+S lưu scene!", "OK");
        }

        [MenuItem(MenuRoot + "Polish Scene (Camera + Sky + Light)", true)]
        private static bool ValidatePolish()
        {
            return FindAnyObjectByType<Camera>() != null;
        }

        private static T FindAnyObjectByType<T>() where T : Object
        {
            return Object.FindAnyObjectByType<T>();
        }
    }
}
#endif
