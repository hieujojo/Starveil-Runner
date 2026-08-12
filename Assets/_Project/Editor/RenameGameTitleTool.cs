using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Đổi tiêu đề game trong scene đang mở: "VOID RUNNER" / "Void Runner" → "STARVEIL RUNNER".
    /// (2026-08-12: user đổi tên game từ Void Runner sang Starveil Runner — CHỈ đổi text hiển thị,
    /// KHÔNG đổi namespace/code để tránh rủi ro compile.)
    /// Idempotent — chạy lại an toàn. Nhớ Ctrl+S lưu scene.
    /// </summary>
    public static class RenameGameTitleTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Rename Game Title (Starveil Runner)")]
        public static void Rename()
        {
            var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            int changed = 0;
            foreach (var t in texts)
            {
                if (t.text == null)
                {
                    continue;
                }

                string newText = t.text;
                if (newText.Contains("Void Runner"))
                {
                    newText = newText.Replace("Void Runner", "STARVEIL RUNNER");
                }
                else if (newText.Contains("VOID RUNNER"))
                {
                    newText = newText.Replace("VOID RUNNER", "STARVEIL RUNNER");
                }

                if (newText != t.text)
                {
                    t.text = newText;
                    EditorUtility.SetDirty(t);
                    changed++;
                }
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            Debug.Log($"[RenameGameTitle] Đã đổi {changed} text title → STARVEIL RUNNER. Nhớ Ctrl+S lưu scene.");
        }
    }
}
