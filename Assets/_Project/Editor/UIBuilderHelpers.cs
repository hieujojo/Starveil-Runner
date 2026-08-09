#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Helper dùng chung cho các Editor tool dựng UI (MainMenuUIBuilder, HUDUIBuilder, ...).
    /// Tập trung một chỗ → tránh lặp code gây drift (bài học: 2 lần thiếu using vì copy code).
    /// </summary>
    public static class UIBuilderHelpers
    {
        public const string KenneyFontPath = "Assets/_Project/Art/kenney_ui-pack/Font/Kenney Future.ttf";
        public const string FontsFolder = "Assets/_Project/Art/Fonts";

        /// <summary>Tự tạo TMP font từ Kenney Future.ttf nếu chưa có (idempotent).</summary>
        public static TMP_FontAsset CreateFontAssetIfMissing(string fontAssetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(KenneyFontPath);
            if (font == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    "Không tìm thấy font 'Kenney Future.ttf' trong gói kenney_ui-pack.\n\nKiểm tra Assets/_Project/Art/kenney_ui-pack/Font/", "OK");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(FontsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Fonts");
            }

            if (File.Exists(fontAssetPath))
            {
                AssetDatabase.DeleteAsset(fontAssetPath);
                AssetDatabase.Refresh();
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 128, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VoidRunner] Đã tự tạo TMP font: {fontAssetPath}");
            return fontAsset;
        }

        public static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
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

        public static void ClearChildren(Canvas canvas)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>Load sprite theo tên, ưu tiên /Blue/ rồi /Grey/.</summary>
        public static Sprite LoadSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
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

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool fullRect = false)
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

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, TMP_FontAsset font,
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, float size,
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
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(tmp.rectTransform);

            return btn;
        }

        public static void SetAnchors(Component comp, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            var rt = comp.transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
