#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core;
using VoidRunner.Systems.VFX;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tự gắn VFXManager vào scene Game (tìm GameObject có GameManager để đặt chung)
    /// + tự gán font Kenney Future cho popup điểm. Idempotent: chạy lại an toàn
    /// (chỉ bổ sung font nếu chưa gán, không tạo component trùng).
    /// Chạy: Tools → Void Runner → Setup VFX in Game Scene.
    /// Yêu cầu: scene Game đang mở.
    /// </summary>
    public static class VFXSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Setup VFX in Game Scene")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Void Runner",
                    $"Scene đang mở là '{scene.name}' — tool chỉ chạy trên scene Game.\n\nMở scene Game rồi chạy lại nhé?", "OK");
                return;
            }

            // Tìm GameObject đang chứa GameManager để đặt VFXManager chung (thường là "Managers")
            GameObject host = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<GameManager>(true) != null)
                {
                    host = root;
                    break;
                }
            }

            if (host == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    "Không tìm thấy GameObject nào chứa GameManager trong scene.\n\nTạo GameObject rỗng 'Managers' + gắn GameManager trước, rồi chạy lại.", "OK");
                return;
            }

            var vfx = host.GetComponent<VFXManager>();
            if (vfx == null)
            {
                vfx = host.AddComponent<VFXManager>();
            }

            // Tự gán font Kenney Future cho popup điểm (nếu asset đã được tạo)
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Art/Fonts/Kenney Future SDF.asset");
            if (font != null)
            {
                var so = new SerializedObject(vfx);
                so.FindProperty("popupFont").objectReferenceValue = font;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(vfx);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("Void Runner",
                $"Đã gắn VFXManager vào '{host.name}' (popup font: {(font != null ? "Kenney Future ✅" : "fallback TMP")}).\n\nKhi chạy game, VFXManager tự:\n- particle burst (coin vàng, power-up màu)\n- popup điểm \"+10\" khi nhặt coin (DOTween bounce)\n- vệt khói tối theo Void\n- Cinemachine Impulse + gắn ImpulseListener vào CinemachineCamera\n\nNhớ Ctrl+S lưu scene.",
                "OK");
        }
    }
}
#endif
