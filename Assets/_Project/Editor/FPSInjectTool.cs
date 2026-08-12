using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidRunner.Utils;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Gắn FPS counter vào scene đang mở (idempotent — chạy lại an toàn).
    /// - Nếu scene đã có GameObject "FPSCounter" + component FPSCounter → chỉ bật lại, không tạo mới.
    /// - Nếu có GameObject "Managers" → gắn vào đó (tránh thêm object rác).
    /// - Không có gì → tạo GameObject "FPSCounter" mới.
    /// </summary>
    public static class FPSInjectTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Add FPS Counter (Open Scene)")]
        public static void AddFpsCounter()
        {
            // 1. Đã có rồi thì chỉ bật + chọn.
            FPSCounter existing = Object.FindAnyObjectByType<FPSCounter>();
            if (existing != null)
            {
                existing.visibleOnStart = true;
                EditorUtility.SetDirty(existing.gameObject);
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[FPSInjectTool] FPS Counter đã có trên '" + existing.gameObject.name + "' — giữ nguyên (idempotent).");
                return;
            }

            // 2. Tìm "Managers" để gắn vào cho gọn scene.
            GameObject host = GameObject.Find("Managers");
            if (host == null)
            {
                host = new GameObject("FPSCounter");
            }

            host.AddComponent<FPSCounter>();
            EditorSceneManager.MarkSceneDirty(host.scene);
            Selection.activeGameObject = host;
            Debug.Log("[FPSInjectTool] Đã gắn FPSCounter vào '" + host.name + "'. (bấm F3 khi PLAY để ẩn/hiện)");
        }
    }
}
