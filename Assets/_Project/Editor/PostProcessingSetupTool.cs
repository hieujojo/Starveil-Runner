using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// G3 - Post-processing: tự dựng Global Volume (Bloom, Vignette, Color Adjustments)
    /// + bật renderPostProcessing trên camera của scene đang mở.
    /// Idempotent — chạy lại an toàn.
    /// </summary>
    public static class PostProcessingSetupTool
    {
        private const string ProfilePath = "Assets/_Project/Settings/PostProcessing/VoidRunnerProfile.asset";

        [MenuItem("Tools/Void Runner/Setup Post-Processing in Open Scene")]
        public static void SetupPostProcessing()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("[VoidRunner] Vui lòng lưu scene trước (Ctrl+S) rồi chạy lại tool.");
                return;
            }

            var volumeGo = EnsureGlobalVolume();
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[VoidRunner] Không tìm thấy Main Camera trong scene này.");
                return;
            }

            EnsurePostProcessingOnCamera(camera);

            Debug.Log($"[VoidRunner] Post-processing sẵn sàng trong scene '{scene.name}': " +
                      $"Global Volume '{volumeGo.name}' + renderPostProcessing = true trên '{camera.name}'.\n" +
                      "Nhớ Ctrl+S lưu scene.");
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Đảm bảo tồn tại GameObject Global Volume (isGlobal) có profile với 3 override.</summary>
        private static GameObject EnsureGlobalVolume()
        {
            // 1. Tìm volume có sẵn trong scene
            var existing = Object.FindAnyObjectByType<Volume>();
            if (existing != null && existing.isGlobal && existing.sharedProfile != null)
            {
                Debug.Log($"[VoidRunner] Đã có Global Volume '{existing.gameObject.name}' — giữ nguyên.");
                return existing.gameObject;
            }

            // 2. Tạo hoặc load profile asset
            var profile = LoadOrCreateProfile();

            // 3. Tạo GameObject Volume
            var go = new GameObject("Global Volume");
            Undo.RegisterCreatedObjectUndo(go, "Create Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            Debug.Log($"[VoidRunner] Đã tạo Global Volume '{go.name}' với profile '{ProfilePath}'.");
            return go;
        }

        private static VolumeProfile LoadOrCreateProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (existing != null)
            {
                EnsureOverrides(existing);
                Debug.Log($"[VoidRunner] Profile '{ProfilePath}' đã có — đảm bảo đủ override.");
                return existing;
            }

            var dir = Path.GetDirectoryName(ProfilePath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            EnsureOverrides(profile);
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VoidRunner] Đã tạo profile '{ProfilePath}' (Bloom + Vignette + Color Adjustments).");
            return profile;
        }

        /// <summary>Đảm bảo profile có đủ 3 override — nếu thiếu thì thêm lại (idempotent).</summary>
        private static void EnsureOverrides(VolumeProfile profile)
        {
            // Bloom — phát sáng nhẹ cho coin/power-up
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.8f;
                bloom.intensity.value = 0.35f;
                bloom.scatter.value = 0.6f;
                bloom.tint.value = new Color(0.6f, 0.8f, 1f, 1f); // tông xanh nhẹ
            }

            // Vignette — tối viền tạo cảm giác "hư không"
            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
                vignette.color.value = new Color(0.02f, 0.02f, 0.08f, 1f);
                vignette.intensity.value = 0.25f;
                vignette.smoothness.value = 0.35f;
            }

            // Color Adjustments — màu điện ảnh, hơi lạnh hơn
            if (!profile.TryGet<ColorAdjustments>(out var color))
            {
                color = profile.Add<ColorAdjustments>(true);
                color.postExposure.value = 0.15f;
                color.contrast.value = 8f;
                color.saturation.value = 6f;
                color.colorFilter.value = new Color(0.85f, 0.92f, 1f, 1f);
            }
        }

        private static void EnsurePostProcessingOnCamera(Camera cam)
        {
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;
            camData.volumeTrigger = cam.transform;
            camData.volumeLayerMask = 1; // Default layer
        }
    }
}
