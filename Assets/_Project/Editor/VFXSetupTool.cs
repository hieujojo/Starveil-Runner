#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core;
using VoidRunner.Systems.VFX;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tự gắn VFXManager vào scene Game (tìm GameObject có GameManager để đặt chung).
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

            if (host.GetComponent<VFXManager>() == null)
            {
                host.AddComponent<VFXManager>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("Void Runner",
                $"Đã gắn VFXManager vào '{host.name}'.\n\nKhi chạy game, VFXManager tự:\n- tạo particle burst (coin vàng, power-up màu)\n- tạo Cinemachine Impulse + gắn ImpulseListener vào CinemachineCamera\n\nNhớ Ctrl+S lưu scene.",
                "OK");
        }
    }
}
#endif
