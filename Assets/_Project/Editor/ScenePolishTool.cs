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
                light.intensity = 0.65f;
                changed++;
            }

            // 3) Bloom: threshold 1.0 + intensity 0.22 (không thổi FBX/skybox nữa)
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null)
            {
                var so = new SerializedObject(profile);
                var components = so.FindProperty("components");
                if (components != null)
                {
                    for (int i = 0; i < components.arraySize; i++)
                    {
                        var comp = components.GetArrayElementAtIndex(i);
                        var script = comp.FindPropertyRelative("m_Script");
                        if (script == null || script.objectReferenceValue == null) continue;
                        string typeName = script.objectReferenceValue.name;

                        if (typeName == "Bloom")
                        {
                            var threshold = comp.FindPropertyRelative("threshold.m_Value");
                            if (threshold != null) { threshold.floatValue = 1.0f; }
                            var intensity = comp.FindPropertyRelative("intensity.m_Value");
                            if (intensity != null) { intensity.floatValue = 0.22f; }
                            changed++;
                        }
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[VoidRunner] Polish scene xong ({changed} mục chỉnh): nền tím đen, FOV 60, light 0.65, Bloom nhẹ.");
            EditorUtility.DisplayDialog("Void Runner — Polish",
                $"Đã polish scene ({changed} mục):\n• Nền hư không tím đen (bỏ skybox chói)\n• FOV 60 — đường nhìn rộng\n• Ánh sáng dịu 0.65\n• Bloom nhẹ — không thổi FBX\n\nNhớ Ctrl+S lưu scene!", "OK");
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
