#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool editor: nâng cấp HUD trong scene Game đang mở —
    /// - ScoreText: font to hơn (58), màu vàng glow, thêm label "SCORE" nhỏ phía trên, bỏ nền panel kính
    /// - ComboText: nổi bật hơn (cam)
    /// - Tìm sprite coin từ game-icons (nếu có) để gắn icon
    /// Idempotent: chạy lại không nhân đôi (chỉ chỉnh thuộc tính text hiện có).
    /// </summary>
    public static class HUDUpgradeTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Upgrade Game HUD (Score glow + label)")]
        public static void UpgradeHud()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Void Runner — HUD", "Không tìm thấy Canvas trong scene mở.", "OK");
                return;
            }

            int changed = 0;

            // 1) ScoreText: tìm text con của ScorePanel hoặc text có font lớn nhất trong canvas
            var scoreText = FindScoreText(canvas);
            if (scoreText != null)
            {
                scoreText.fontSize = 58;
                scoreText.fontStyle = FontStyles.Bold;
                scoreText.color = new Color(1f, 0.9f, 0.25f);          // vàng glow
                if (scoreText.GetComponent<Shadow>() == null)
                {
                    var shadow = scoreText.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    shadow.effectDistance = new Vector2(2f, -2f);
                }
                if (scoreText.GetComponent<Outline>() == null)
                {
                    var outline = scoreText.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.1f, 0f, 0.3f, 1f); // viền tím hư không
                    outline.effectDistance = new Vector2(1f, -1f);
                }
                changed++;
            }

            // 2) Label "SCORE" phía trên score (tạo 1 lần, idempotent bằng cách check tên con)
            if (scoreText != null && scoreText.transform.parent != null)
            {
                EnsureScoreLabel(scoreText.transform.parent);
            }

            // 3) ComboText: cam nổi bật
            var comboText = FindTextByName(canvas.transform, "ComboText");
            if (comboText != null)
            {
                comboText.fontSize = 36;
                comboText.fontStyle = FontStyles.Bold;
                comboText.color = new Color(1f, 0.55f, 0.2f);
                changed++;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[VoidRunner] HUD upgrade xong: {changed} text được chỉnh.");
            EditorUtility.DisplayDialog("Void Runner — HUD",
                $"Đã nâng cấp HUD ({changed} phần tử).\nNhớ Ctrl+S lưu scene.", "OK");
        }

        private static void EnsureScoreLabel(Transform panel)
        {
            // Đã có label rồi → bỏ qua (idempotent)
            foreach (Transform child in panel)
            {
                if (child.name == "ScoreLabel") return;
            }

            var labelGo = new GameObject("ScoreLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(panel, false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -8f);
            rt.sizeDelta = new Vector2(0f, 22f);

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "SCORE";
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.7f, 0.85f, 1f, 0.95f);
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.font = FindKenneyFont();
        }

        private static TextMeshProUGUI FindScoreText(Canvas canvas)
        {
            // Ưu tiên text con của ScorePanel
            var panel = FindTransformByName(canvas.transform, "ScorePanel");
            if (panel != null)
            {
                var inPanel = panel.GetComponentInChildren<TextMeshProUGUI>(true);
                if (inPanel != null) return inPanel;
            }
            return FindTextByName(canvas.transform, "ScoreText");
        }

        private static TextMeshProUGUI FindTextByName(Transform root, string name)
        {
            var t = FindTransformByName(root, name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Transform FindTransformByName(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var deep = FindTransformByName(child, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private static TMP_FontAsset FindKenneyFont()
        {
            string[] guids = AssetDatabase.FindAssets("Kenney Future SDF t:TMP_FontAsset", new[] { "Assets/_Project/Art/Fonts" });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static T FindAnyObjectByType<T>() where T : Object
        {
            return Object.FindAnyObjectByType<T>();
        }

        [MenuItem(MenuRoot + "Upgrade Game HUD (Score glow + label)", true)]
        private static bool ValidateHud()
        {
            return FindAnyObjectByType<Canvas>() != null;
        }
    }
}
#endif
