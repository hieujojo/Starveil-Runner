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
    /// Tự dựng lại UI MainMenu với sprite Kenney UI (tông Blue) + font Kenney Future:
    /// background, title, 3 nút (Play / How to play / Sound), best score, panel hướng dẫn.
    /// Sau đó TỰ GÁN tất cả field vào MainMenuManager trong scene — không cần kéo thả tay.
    /// Chạy: Tools → Void Runner → Build MainMenu UI.
    /// Yêu cầu: đã chạy Convert Sprites (font tự tạo nếu chưa có).
    /// </summary>
    public static class MainMenuUIBuilder
    {
        private const string MenuRoot = "Tools/Void Runner/";
        private const string FontAssetPath = "Assets/_Project/Art/Fonts/Kenney Future SDF.asset";

        [MenuItem(MenuRoot + "Build MainMenu UI (sprite Blue + Kenney Future)")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "MainMenu")
            {
                if (!EditorUtility.DisplayDialog("Void Runner",
                        $"Scene đang mở là '{scene.name}' — tool chỉ chạy trên scene MainMenu.\n\nMở scene MainMenu rồi chạy lại nhé?", "OK"))
                    return;
                return;
            }

            // 1. Font asset — tự tạo nếu chưa có (không cần chạy tool font riêng)
            var font = UIBuilderHelpers.CreateFontAssetIfMissing(FontAssetPath);
            if (font == null) return;

            // 2. Tìm/nhận diện các object gốc
            var canvas = UIBuilderHelpers.FindOrCreateCanvas();
            var manager = FindMainMenuManager();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    "Không tìm thấy 'MainMenuManager' trong scene. Hãy tạo GameObject rỗng 'MainMenuManager' + gắn component MainMenuManager, rồi chạy lại.", "OK");
                return;
            }

            // 3. Xóa UI cũ trong Canvas (giữ Canvas, EventSystem, AudioManager nằm ngoài canvas)
            UIBuilderHelpers.ClearChildren(canvas);

            // 4. Dựng UI mới
            var bg = UIBuilderHelpers.CreateImage(canvas.transform, "Background",
                UIBuilderHelpers.LoadSprite("panel_glass"), new Color(0.02f, 0.04f, 0.12f, 1f), true);
            UIBuilderHelpers.Stretch(bg.rectTransform);

            var title = UIBuilderHelpers.CreateText(canvas.transform, "TitleText", "VOID RUNNER", font, 110, Color.white, FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(900f, 140f));

            var titleGlow = UIBuilderHelpers.CreateText(canvas.transform, "TitleGlow", "VOID RUNNER", font, 110,
                new Color(0.2f, 0.6f, 1f, 0.35f), FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(titleGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(4f, 256f), new Vector2(900f, 140f));
            titleGlow.transform.SetSiblingIndex(title.transform.GetSiblingIndex()); // glow sau title

            var playBtn = UIBuilderHelpers.CreateButton(canvas.transform, "PlayButton", "PLAY", font, 46,
                UIBuilderHelpers.LoadSprite("button_rectangle_gloss"), new Color(0.15f, 0.55f, 1f, 1f), Color.white);
            UIBuilderHelpers.SetAnchors(playBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(420f, 100f));

            var howBtn = UIBuilderHelpers.CreateButton(canvas.transform, "HowToPlayButton", "HOW TO PLAY", font, 34,
                UIBuilderHelpers.LoadSprite("button_rectangle_flat"), new Color(0.12f, 0.45f, 0.9f, 1f), Color.white);
            UIBuilderHelpers.SetAnchors(howBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(340f, 76f));

            var soundBtn = UIBuilderHelpers.CreateButton(canvas.transform, "SoundButton", "Âm thanh: BẬT", font, 30,
                UIBuilderHelpers.LoadSprite("button_rectangle_flat"), new Color(0.1f, 0.35f, 0.75f, 1f), Color.white);
            UIBuilderHelpers.SetAnchors(soundBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(300f, 66f));

            var bestScore = UIBuilderHelpers.CreateText(canvas.transform, "BestScoreText", "ĐIỂM CAO NHẤT: 0", font, 32,
                new Color(1f, 0.85f, 0.3f, 1f), FontStyles.Bold);
            UIBuilderHelpers.SetAnchors(bestScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(600f, 50f));

            var panel = UIBuilderHelpers.CreateImage(canvas.transform, "HowToPlayPanel",
                UIBuilderHelpers.LoadSprite("panel_glass"), new Color(0.05f, 0.09f, 0.22f, 0.96f), true);
            UIBuilderHelpers.SetAnchors(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(720f, 480f));
            panel.gameObject.SetActive(false);

            var panelText = UIBuilderHelpers.CreateText(panel.transform, "HowToPlayText",
                "Né obstacle bằng A/D hoặc mũi tên trái/phải.\n\nNhặt coin để cộng điểm (combo càng cao càng tốt).\n\nPower-up: Shield (miễn nhiễm 3s), Magnet (hút coin), Slow-mo (chậm thời gian).\n\nChạy càng xa, điểm càng cao!",
                font, 30, new Color(0.9f, 0.93f, 1f, 1f), FontStyles.Normal);
            UIBuilderHelpers.SetAnchors(panelText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(640f, 400f));

            // 5. Gán field vào MainMenuManager
            AssignFields(manager, playBtn, howBtn, soundBtn, panel.gameObject, bestScore, soundBtn.GetComponentInChildren<TextMeshProUGUI>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("Void Runner",
                "Đã dựng xong UI MainMenu!\n\n- Đã tự tạo font (nếu chưa có)\n- Đã gán 6 field vào MainMenuManager\n\nNhớ Ctrl+S lưu scene, rồi bấm ▶ Play để test.", "OK");
        }

        private static MainMenuManager FindMainMenuManager()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var m = root.GetComponentInChildren<MainMenuManager>(true);
                if (m != null) return m;
            }
            return null;
        }

        private static void AssignFields(MainMenuManager manager, Button play, Button how, Button sound,
            GameObject panel, TextMeshProUGUI bestScore, TextMeshProUGUI soundText)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("howToPlayButton").objectReferenceValue = how;
            so.FindProperty("soundButton").objectReferenceValue = sound;
            so.FindProperty("howToPlayPanel").objectReferenceValue = panel;
            so.FindProperty("bestScoreText").objectReferenceValue = bestScore;
            so.FindProperty("soundButtonText").objectReferenceValue = soundText;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }
    }
}
#endif
