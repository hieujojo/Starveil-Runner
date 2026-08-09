#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidRunner.UI;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tự dựng lại Game HUD + Game Over panel (tông Blue + font Kenney Future):
    /// score góc trên trái (có icon coin), combo x2..., Game Over panel với nút Retry + Menu.
    /// Sau đó TỰ GÁN toàn bộ field vào UIManager — không cần kéo thả tay.
    /// Chạy: Tools → Void Runner → Build Game HUD UI.
    /// Yêu cầu: scene Game đang mở + đã chạy Convert Sprites (font tự tạo nếu chưa có).
    /// </summary>
    public static class HUDUIBuilder
    {
        private const string MenuRoot = "Tools/Void Runner/";
        private const string FontAssetPath = "Assets/_Project/Art/Fonts/Kenney Future SDF.asset";

        [MenuItem(MenuRoot + "Build Game HUD UI (sprite Blue + Kenney Future)")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Void Runner",
                    $"Scene đang mở là '{scene.name}' — tool chỉ chạy trên scene Game.\n\nMở scene Game rồi chạy lại nhé?", "OK");
                return;
            }

            // 1. Font — tự tạo nếu chưa có
            var font = UIBuilderHelpers.CreateFontAssetIfMissing(FontAssetPath);
            if (font == null) return;

            // 2. Canvas + UIManager
            var canvas = UIBuilderHelpers.FindOrCreateCanvas();
            var uiManager = FindUIManager();
            if (uiManager == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    "Không tìm thấy 'UIManager' trong scene. Hãy tạo GameObject rỗng (vd 'Managers') + gắn component UIManager, rồi chạy lại.", "OK");
                return;
            }

            // 3. Xóa UI cũ trong Canvas (giữ Canvas + các object ngoài canvas)
            UIBuilderHelpers.ClearChildren(canvas);

            // 4. HUD — score + combo góc trên trái
            var scorePanel = UIBuilderHelpers.CreateImage(canvas.transform, "ScorePanel",
                UIBuilderHelpers.LoadSprite("panel_glass"), new Color(0.05f, 0.12f, 0.28f, 0.85f), true);
            UIBuilderHelpers.SetAnchors(scorePanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(260f, 90f));

            var coinIcon = UIBuilderHelpers.CreateImage(scorePanel.transform, "CoinIcon",
                UIBuilderHelpers.LoadSprite("star"), new Color(1f, 0.85f, 0.2f, 1f), false);
            UIBuilderHelpers.SetAnchors(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));

            var scoreText = UIBuilderHelpers.CreateText(scorePanel.transform, "ScoreText", "0", font, 52, Color.white, FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(scoreText, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(42f, 0f), new Vector2(-80f, 0f));
            scoreText.alignment = TextAlignmentOptions.Left;

            var comboText = UIBuilderHelpers.CreateText(canvas.transform, "ComboText", "x2", font, 34,
                new Color(0.4f, 0.9f, 1f, 1f), FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(comboText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -150f), new Vector2(220f, 50f));
            comboText.gameObject.SetActive(false); // UIManager tự bật khi combo > 1

            // 5. Game Over panel (ẩn sẵn) + nút Retry/Menu
            var panel = UIBuilderHelpers.CreateImage(canvas.transform, "GameOverPanel",
                UIBuilderHelpers.LoadSprite("panel_glass"), new Color(0.04f, 0.06f, 0.18f, 0.95f), true);
            UIBuilderHelpers.SetAnchors(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(680f, 560f));
            panel.gameObject.SetActive(false);

            var title = UIBuilderHelpers.CreateText(panel.transform, "TitleText", "GAME OVER", font, 72,
                new Color(1f, 0.35f, 0.35f, 1f), FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(600f, 90f));

            var finalScore = UIBuilderHelpers.CreateText(panel.transform, "FinalScoreText", "ĐIỂM: 0", font, 42, Color.white, FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(600f, 60f));

            var bestScore = UIBuilderHelpers.CreateText(panel.transform, "BestScoreText", "CAO NHẤT: 0", font, 34,
                new Color(1f, 0.85f, 0.3f, 1f), FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(bestScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(600f, 50f));

            var retryBtn = UIBuilderHelpers.CreateButton(panel.transform, "RetryButton", "CHƠI LẠI", font, 36,
                UIBuilderHelpers.LoadSprite("button_rectangle_gloss"), new Color(0.15f, 0.55f, 1f, 1f), Color.white);
            UIBuilderHelpers.SetAnchors(retryBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, -140f), new Vector2(260f, 80f));

            var menuBtn = UIBuilderHelpers.CreateButton(panel.transform, "MenuButton", "MENU", font, 34,
                UIBuilderHelpers.LoadSprite("button_rectangle_flat"), new Color(0.12f, 0.4f, 0.85f, 1f), Color.white);
            UIBuilderHelpers.SetAnchors(menuBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, -140f), new Vector2(220f, 80f));

            // 6. Gán field vào UIManager
            AssignFields(uiManager, scoreText, comboText, panel.gameObject, finalScore, bestScore, retryBtn, menuBtn);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("Void Runner",
                "Đã dựng xong Game HUD!\n\n- ScorePanel + coin icon + ScoreText + ComboText\n- GameOverPanel (GAME OVER + điểm + cao nhất + nút CHƠI LẠI/MENU)\n- Đã gán đủ field vào UIManager\n\nNhớ Ctrl+S lưu scene, rồi bấm ▶ Play để test.",
                "OK");
        }

        private static UIManager FindUIManager()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var m = root.GetComponentInChildren<UIManager>(true);
                if (m != null) return m;
            }
            return null;
        }

        private static void AssignFields(UIManager manager, TextMeshProUGUI score, TextMeshProUGUI combo,
            GameObject panel, TextMeshProUGUI finalScore, TextMeshProUGUI bestScore, Button retry, Button menu)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("comboText").objectReferenceValue = combo;
            so.FindProperty("gameOverPanel").objectReferenceValue = panel;
            so.FindProperty("finalScoreText").objectReferenceValue = finalScore;
            so.FindProperty("bestScoreText").objectReferenceValue = bestScore;
            so.FindProperty("retryButton").objectReferenceValue = retry;
            so.FindProperty("menuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }
    }
}
#endif
