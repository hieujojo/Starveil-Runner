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
    /// Yêu cầu: đã chạy 2 tool trước (Convert Sprites + Create TMP Font).
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

            // 1. Font asset
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    $"Chưa có font TMP tại:\n{FontAssetPath}\n\nChạy tool 'Create TMP Font (Kenney Future)' trước.", "OK");
                return;
            }

            // 2. Tìm/nhận diện các object gốc
            var canvas = FindOrCreateCanvas();
            var manager = FindMainMenuManager();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    "Không tìm thấy 'MainMenuManager' trong scene. Hãy tạo GameObject rỗng 'MainMenuManager' + gắn component MainMenuManager, rồi chạy lại.", "OK");
                return;
            }

            // 3. Xóa UI cũ trong Canvas (giữ Canvas, EventSystem, AudioManager nằm ngoài canvas)
            ClearChildren(canvas);

            // 4. Dựng UI mới
            var bg = CreateImage(canvas.transform, "Background", LoadSprite("panel_glass"), new Color(0.02f, 0.04f, 0.12f, 1f), true);
            Stretch(bg.rectTransform);

            var title = CreateText(canvas.transform, "TitleText", "VOID RUNNER", font, 110, Color.white, FontStyles.Bold);
            SetAnchors(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(900f, 140f));

            var titleGlow = CreateText(canvas.transform, "TitleGlow", "VOID RUNNER", font, 110, new Color(0.2f, 0.6f, 1f, 0.35f), FontStyles.Bold);
            SetAnchors(titleGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(4f, 256f), new Vector2(900f, 140f));
            titleGlow.transform.SetSiblingIndex(title.transform.GetSiblingIndex()); // glow sau title

            var playBtn = CreateButton(canvas.transform, "PlayButton", "PLAY", font, 46,
                LoadSprite("button_rectangle_gloss"), new Color(0.15f, 0.55f, 1f, 1f), Color.white);
            SetAnchors(playBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(420f, 100f));

            var howBtn = CreateButton(canvas.transform, "HowToPlayButton", "HOW TO PLAY", font, 34,
                LoadSprite("button_rectangle_flat"), new Color(0.12f, 0.45f, 0.9f, 1f), Color.white);
            SetAnchors(howBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(340f, 76f));

            var soundBtn = CreateButton(canvas.transform, "SoundButton", "Âm thanh: BẬT", font, 30,
                LoadSprite("button_rectangle_flat"), new Color(0.1f, 0.35f, 0.75f, 1f), Color.white);
            SetAnchors(soundBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(300f, 66f));

            var bestScore = CreateText(canvas.transform, "BestScoreText", "ĐIỂM CAO NHẤT: 0", font, 32, new Color(1f, 0.85f, 0.3f, 1f), FontStyles.Bold);
            SetAnchors(bestScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(600f, 50f));

            var panel = CreateImage(canvas.transform, "HowToPlayPanel", LoadSprite("panel_glass"), new Color(0.05f, 0.09f, 0.22f, 0.96f), true);
            SetAnchors(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(720f, 480f));
            panel.gameObject.SetActive(false);

            var panelText = CreateText(panel.transform, "HowToPlayText",
                "Né obstacle bằng A/D hoặc mũi tên trái/phải.\n\nNhặt coin để cộng điểm (combo càng cao càng tốt).\n\nPower-up: Shield (miễn nhiễm 3s), Magnet (hút coin), Slow-mo (chậm thời gian).\n\nChạy càng xa, điểm càng cao!",
                font, 30, new Color(0.9f, 0.93f, 1f, 1f), FontStyles.Normal);
            SetAnchors(panelText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(640f, 400f));

            // 5. Gán field vào MainMenuManager
            AssignFields(manager, playBtn, howBtn, soundBtn, panel, bestScore, soundBtn.GetComponentInChildren<TextMeshProUGUI>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("Void Runner",
                "Đã dựng xong UI MainMenu!\n\nĐã tự gán 6 field vào MainMenuManager.\nBấm ▶ Play để test.", "OK");
        }

        // ---------- Helpers ----------

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
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

        private static void ClearChildren(Canvas canvas)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
            }
        }

        private static Sprite LoadSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Ưu tiên Blue trước
                if (path.Contains("/Blue/")) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Grey/")) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool fullRect = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (fullRect && sprite != null)
            {
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, TMP_FontAsset font,
            float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.text = content;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, float size,
            Sprite sprite, Color bgColor, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = bgColor;
            if (sprite != null) img.type = Image.Type.Sliced;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            Stretch(tmp.rectTransform);

            return btn;
        }

        private static void SetAnchors(Component comp, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            var rt = comp.transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
