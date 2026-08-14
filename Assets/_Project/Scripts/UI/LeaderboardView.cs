using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoidRunner.Systems.Leaderboard;

namespace VoidRunner.UI
{
    /// <summary>
    /// Builder cho UI LEADERBOARD (top 10 online) — hiện trong Game Over panel.
    /// Tạo bằng code, idempotent (transform.Find trước khi tạo), theo style CreditsPanelBuilder:
    /// nền tím đen đục + viền cyan neon + tiêu đề vàng.
    ///
    /// Flow: Game Over → panel hiện → tự tải top 10 (Loading... → danh sách).
    /// Người chơi nhập tên 3 ký tự (arcade) → bấm SUBMIT → gửi điểm lên server → refresh top.
    /// Offline/lỗi server → không crash, chỉ không có top online (LeaderboardService tự xử lý).
    /// </summary>
    public static class LeaderboardView
    {
        private const string RootName = "LeaderboardPanel";
        private const string NameKey = "StarveilRunner_ArcadeName";

        /// <summary>
        /// Đảm bảo panel leaderboard tồn tại (con của Game Over panel — ẩn cùng panel).
        /// Trả về null nếu parent không hợp lệ.
        /// </summary>
        public static GameObject Ensure(Transform parent)
        {
            if (parent == null) return null;
            Transform existing = parent.Find(RootName);
            if (existing != null) return existing.gameObject;

            var panel = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var prt = (RectTransform)panel.transform;
            // Đỉnh panel Game Over (anchor giữa 680×560): từ y=-230 xuống -530 — dưới FinalScore,
            // không đè Best/Retry/Menu (y=-140)/Credits (y=-245, ngoài panel)
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -230f);
            prt.sizeDelta = new Vector2(560f, 300f);

            var pimg = panel.GetComponent<Image>();
            pimg.color = new Color(0.06f, 0.04f, 0.12f, 1f); // tím đen — đục hoàn toàn (R0.9)
            AddBorder(panel.transform, new Vector2(560f, 300f));

            // Tiêu đề
            var title = CreateLabel(panel.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(500f, 32f), "LEADERBOARD", 28,
                new Color(1f, 0.85f, 0.3f, 1f), TextAlignmentOptions.Center);

            // Danh sách top — text nhiều dòng, đọc được
            var list = CreateLabel(panel.transform, "List", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), "", 22, new Color(0.9f, 0.9f, 1f, 1f), TextAlignmentOptions.TopLeft);
            var lrt = (RectTransform)list.transform;
            lrt.offsetMin = new Vector2(28f, 60f);
            lrt.offsetMax = new Vector2(-28f, -44f);
            list.text = "Loading...";

            // Hàng dưới: input tên + nút SUBMIT
            var input = EnsureNameInput(panel.transform);
            var submit = EnsureSubmitButton(panel.transform);

            // Thông báo trạng thái (offline/lỗi) — nhỏ, dưới list
            var status = CreateLabel(panel.transform, "Status", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 14f), new Vector2(500f, 24f), "", 16, new Color(1f, 0.5f, 0.5f, 1f), TextAlignmentOptions.Center);
            status.transform.SetAsLastSibling();

            panel.SetActive(false);
            return panel;
        }

        /// <summary>Hiện panel + tải top 10. Gọi khi Game Over.</summary>
        public static void Show(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(true);

            TMP_InputField input = panel.transform.Find("NameInput")?.GetComponent<TMP_InputField>();
            if (input != null)
            {
                input.text = PlayerPrefs.GetString(NameKey, "AAA");
                // Chọn toàn bộ text để gõ đè nhanh
                input.caretPosition = input.text.Length;
            }

            RefreshList(panel);
        }

        /// <summary>Ẩn panel (khi restart).</summary>
        public static void Hide(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>Tải top 10 từ server và cập nhật text list.</summary>
        public static void RefreshList(GameObject panel)
        {
            if (panel == null) return;
            TextMeshProUGUI list = panel.transform.Find("List")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI status = panel.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();
            if (list == null) return;

            list.text = "Loading...";
            if (status != null) status.text = "";

            LeaderboardService.FetchTopScores(entries =>
            {
                if (list == null) return;
                if (entries == null || entries.Count == 0)
                {
                    list.text = "No scores yet — be the first!";
                    if (status != null) status.text = "";
                    return;
                }

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < entries.Count; i++)
                {
                    sb.Append((i + 1).ToString("00")).Append(".  ");
                    sb.Append(entries[i].name.PadRight(4));
                    sb.Append(entries[i].score.ToString("N0"));
                    sb.Append('\n');
                }
                list.text = sb.ToString();
            });
        }

        /// <summary>Gửi điểm + refresh. Gọi từ nút SUBMIT.</summary>
        public static void Submit(GameObject panel, string playerName, int score)
        {
            if (panel == null) return;
            TextMeshProUGUI status = panel.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();

            string name = LeaderboardService.SanitizeName(playerName);
            PlayerPrefs.SetString(NameKey, name);

            // Lưu tên vào input để lần sau hiện lại
            TMP_InputField input = panel.transform.Find("NameInput")?.GetComponent<TMP_InputField>();
            if (input != null) input.text = name;

            LeaderboardService.SubmitScore(name, score, ok =>
            {
                if (status == null) return;
                status.text = ok ? "Saved!" : "Offline — score kept locally only";
                status.color = ok ? new Color(0.4f, 1f, 0.6f, 1f) : new Color(1f, 0.5f, 0.5f, 1f);
                RefreshList(panel);
            });
        }

        // ─────────────────────────── helpers ───────────────────────────

        private static TMP_InputField EnsureNameInput(Transform parent)
        {
            Transform existing = parent.Find("NameInput");
            if (existing != null) return existing.GetComponent<TMP_InputField>();

            var go = new GameObject("NameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(-90f, 62f);
            rt.sizeDelta = new Vector2(180f, 44f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.08f, 0.2f, 1f);

            var field = go.GetComponent<TMP_InputField>();
            field.characterLimit = 3; // tên arcade 3 ký tự
            // Dùng onValidateInput (không cần CustomValidator — tránh yêu cầu inputValidator)

            // Text hiển thị
            var text = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            text.transform.SetParent(go.transform, false);
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 0f);
            trt.offsetMax = new Vector2(-12f, 0f);
            var tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.text = "AAA";
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            AssignFallbackFont(tmp);

            // Placeholder
            var ph = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            ph.transform.SetParent(go.transform, false);
            var prt = (RectTransform)ph.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(12f, 0f);
            prt.offsetMax = new Vector2(-12f, 0f);
            var ptmp = ph.GetComponent<TextMeshProUGUI>();
            ptmp.text = "AAA";
            ptmp.fontSize = 26;
            ptmp.fontStyle = FontStyles.Bold;
            ptmp.color = new Color(0.6f, 0.6f, 0.7f, 0.6f);
            ptmp.alignment = TextAlignmentOptions.Center;
            ptmp.raycastTarget = false;
            AssignFallbackFont(ptmp);

            field.textComponent = tmp;
            field.placeholder = ptmp;

            // Hạn chế ký tự: chỉ chữ + số, in hoa — cùng luật SanitizeName
            field.onValidateInput = (text, index, ch) =>
            {
                if (char.IsLetterOrDigit(ch)) return char.ToUpperInvariant(ch);
                return '\0';
            };

            return field;
        }

        private static Button EnsureSubmitButton(Transform parent)
        {
            Transform existing = parent.Find("SubmitButton");
            if (existing != null) return existing.GetComponent<Button>();

            var go = new GameObject("SubmitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(110f, 62f);
            rt.sizeDelta = new Vector2(160f, 44f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.75f, 1f, 1f); // cyan — tông nút chính

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var label = CreateLabel(go.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), "SUBMIT", 24, Color.white, TextAlignmentOptions.Center);
            var lrt = (RectTransform)label.transform;
            lrt.offsetMin = new Vector2(10f, 0f);
            lrt.offsetMax = new Vector2(-10f, 0f);

            return btn;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Vector2 pos, Vector2 size, string text, float fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);
            return tmp;
        }

        private static void AddBorder(Transform parent, Vector2 size)
        {
            float t = 3f;
            CreateStrip(parent, "BorderTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -t * 0.5f), new Vector2(size.x, t));
            CreateStrip(parent, "BorderBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t * 0.5f), new Vector2(size.x, t));
            CreateStrip(parent, "BorderLeft", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(t * 0.5f, 0f), new Vector2(t, size.y));
            CreateStrip(parent, "BorderRight", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-t * 0.5f, 0f), new Vector2(t, size.y));
        }

        private static void CreateStrip(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.35f, 0.85f, 1f, 0.35f); // cyan mờ
            img.raycastTarget = false;
        }

        /// <summary>Fallback font: dùng font TMP bất kỳ có trong scene (thường Kenney Future).</summary>
        private static void AssignFallbackFont(TextMeshProUGUI tmp)
        {
            if (tmp.font != null) return;
            var anyTmp = Object.FindAnyObjectByType<TextMeshProUGUI>();
            tmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        }
    }
}
