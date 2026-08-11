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

        /// <summary>
        /// Tự tạo TMP font từ Kenney Future.ttf nếu chưa có, HOẶC nếu font bị hỏng
        /// (thiếu atlas texture — bài học m_AtlasTextures: texture/material phải AddObjectToAsset).
        /// </summary>
        public static TMP_FontAsset CreateFontAssetIfMissing(string fontAssetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            // ⚠️ Check ĐỦ ký tự, không chỉ atlasTexture: font atlas 1024 cũ chỉ chứa ~30-41/95 ký tự ASCII
            // (thiếu 'x', '2', chữ thường) → atlas tồn tại nhưng font VẪN HỎNG → phải tái tạo (bug 2026-08-11).
            if (existing != null && existing.atlasTexture != null
                && existing.characterTable != null && existing.characterTable.Count >= 80)
            {
                return existing;
            }

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
                // ⚠️ Giữ NGUYÊN guid khi tái tạo — nếu không, text trong scene mất font (rơi về mặc định).
                string oldGuid = ReadGuid(fontAssetPath);
                AssetDatabase.DeleteAsset(fontAssetPath);
                AssetDatabase.Refresh();

                TMP_FontAsset created = CreateFontAssetCore(font, fontAssetPath);
                RestoreGuid(fontAssetPath, oldGuid);
                return created;
            }

            return CreateFontAssetCore(font, fontAssetPath);
        }

        /// <summary>
        /// Tạo TMP font + LƯU CẢ texture/material làm sub-asset — dùng chung cho mọi tool UI.
        /// (Bài học m_AtlasTextures: CreateFontAsset tạo texture/material trong memory, không tự lưu;
        /// thiếu AddObjectToAsset → file .asset ghi fileID 0 → mở lại Unity là font rỗng + exception.)
        /// ⚠️ Atlas BẮT BUỘC 2048x2048 (không được 1024): sampling 128 + padding 9 → 1024 chỉ chứa ~40/95 ký tự
        /// ASCII → thiếu 'x', '2', chữ thường → combo "x2" / HowToPlay chữ thường hiện lỗi (bug 2026-08-11).
        /// </summary>
        public static TMP_FontAsset CreateFontAssetCore(Font font, string fontAssetPath)
        {
            // ⚠️ GỐC RỄ font chỉ có 8 ký tự (bug 2026-08-11 — combo "x2" hiện "HS"):
            // Kenney Future.ttf importer để fontTextureCase = Dynamic (mặc định, không có field trong .meta)
            // → Unity chỉ extract ký tự ĐANG ĐƯỢC DÙNG trong scene → characterInfo gần rỗng →
            // CreateFontAsset chỉ tạo ~8 glyph. Fix: ép importer extract toàn bộ Latin trước khi tạo.
            // ⚠️ Unity 6: KHÔNG còn FontImporter/characterSet/FontImporterCharacterSet (CS0246) —
            // thay bằng TrueTypeFontImporter.fontTextureCase + enum FontTextureCase (không có ASCIIPrintableSet).
            var ttfImporter = AssetImporter.GetAtPath(KenneyFontPath) as TrueTypeFontImporter;
            if (ttfImporter != null && ttfImporter.fontTextureCase != FontTextureCase.Unicode)
            {
                ttfImporter.fontTextureCase = FontTextureCase.Unicode;
                ttfImporter.SaveAndReimport();
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 128, 9, GlyphRenderMode.SDFAA, 2048, 2048);

            // Belt-and-suspenders: ép pack TOÀN BỘ ASCII (32..126) vào atlas 2048 — đủ x/2/chữ thường.
            // ⚠️ API TMP bản này: chỉ có TryAddCharacters(string) / TryAddCharacters(uint[]) —
            // KHÔNG có overload IEnumerable<char> / out bool (CS1503/CS1615 nếu dùng List<char> + out).
            var sb = new System.Text.StringBuilder();
            for (int c = 32; c < 127; c++) sb.Append((char)c);
            if (!fontAsset.TryAddCharacters(sb.ToString()))
            {
                Debug.LogWarning($"[VoidRunner] Font không pack đủ toàn bộ ASCII — atlas 2048 có thể đầy ({fontAsset.characterTable?.Count ?? 0} ký tự). Kiểm tra lại.");
            }

            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

            // BẮT BUỘC: lưu atlas texture + material làm sub-asset
            if (fontAsset.atlasTexture != null)
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // Log số ký tự để phát hiện sớm nếu font lại bị thiếu glyph (bug 2026-08-11: chỉ 30/95 ký tự)
            int charCount = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
            Debug.Log($"[VoidRunner] Đã tự tạo TMP font: {fontAssetPath} ({charCount} ký tự).");
            return fontAsset;
        }

        /// <summary>Đọc guid từ file .meta của asset (null nếu chưa có .meta).</summary>
        public static string ReadGuid(string assetPath)
        {
            string meta = assetPath + ".meta";
            if (!File.Exists(meta)) return null;
            foreach (string line in File.ReadAllLines(meta))
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("guid: "))
                {
                    string guid = trimmed.Substring("guid: ".Length).Trim();
                    return guid.Length == 32 ? guid : null;
                }
            }
            return null;
        }

        /// <summary>
        /// Ghi đè guid trong .meta vừa sinh bằng guid cũ — dùng sau khi DeleteAsset + CreateAsset
        /// (Unity tạo guid MỚI khi recreate; mọi tham chiếu trong scene theo guid cũ sẽ gãy nếu không restore).
        /// </summary>
        public static void RestoreGuid(string assetPath, string oldGuid)
        {
            if (string.IsNullOrEmpty(oldGuid) || oldGuid.Length != 32) return;
            string meta = assetPath + ".meta";
            if (!File.Exists(meta)) return;

            string text = File.ReadAllText(meta);
            if (text.Contains(oldGuid)) return; // guid vẫn giữ nguyên — không cần làm gì

            // ⚠️ KHÔNG dùng Regex.Replace(text, pattern, replacement, count): overload 4 tham số với
            // 'count' KHÔNG có trong BCL của Unity 6 → error CS1503 (Argument 3: string → int).
            // Dùng string ops thuần: .meta luôn có ĐÚNG 1 dòng 'guid: ' ở đầu → an toàn mọi phiên bản.
            const string marker = "guid: ";
            int idx = text.IndexOf(marker, System.StringComparison.Ordinal);
            if (idx < 0) return;
            int start = idx + marker.Length;
            if (text.Length < start + 32) return;

            string updated = text.Substring(0, start) + oldGuid + text.Substring(start + 32);
            if (updated != text)
            {
                File.WriteAllText(meta, updated);
                AssetDatabase.Refresh();
                // BẮT BUỘC: ForceUpdate để asset database nạp lại asset theo guid mới (một số version cache guid trong memory)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[VoidRunner] Đã khôi phục guid cũ ({oldGuid}) cho {assetPath}.");
            }
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
