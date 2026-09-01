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

        // ── Bảng màu neon arcade ──
        private static readonly Color PanelBg = new Color(0.05f, 0.03f, 0.10f, 0.95f);
        private static readonly Color BorderCyan = new Color(0.3f, 0.85f, 1f, 0.5f);
        private static readonly Color TitleGold = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentCyan = new Color(0.2f, 0.75f, 1f, 1f);
        private static readonly Color CloseRed = new Color(1f, 0.35f, 0.35f, 1f);

        /// <summary>
        /// Đảm bảo panel leaderboard tồn tại (con của Game Over panel — ẩn cùng panel).
        /// Trả về null nếu parent không hợp lệ.
        /// </summary>
        public static GameObject Ensure(Transform parent)
        {
            if (parent == null) return null;
            Transform existing = parent.Find(RootName);
            if (existing != null) return existing.gameObject;

            float W = 520f;  // chiều rộng panel
            float H = 380f;  // chiều cao panel (tăng từ 300 → 380 cho thoáng)

            // ── Panel chính ──
            var panel = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0f, -40f);  // giữa Game Over panel, dịch xuống 40px
            prt.sizeDelta = new Vector2(W, H);

            var pimg = panel.GetComponent<Image>();
            pimg.color = PanelBg;

            // Viền neon cyan (4 cạnh)
            AddBorder(panel.transform, new Vector2(W, H), BorderCyan);

            // ── Hàng ngang trên cùng: Title + Close button ──
            // Tiêu đề "LEADERBOARD" — bên trái, có padding
            CreateLabel(panel.transform, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(30f, -8f), new Vector2(-50f, 30f), "🏆  LEADERBOARD", 24,
                TitleGold, TextAlignmentOptions.Left);

            // Nút ✕ Close — góc phải trên
            EnsureCloseButton(panel.transform, W);

            // ── Đường kẻ ngang phân tách ──
            CreateStrip(panel.transform, "Divider", new Vector2(0.1f, 1f), new Vector2(0.9f, 1f),
                new Vector2(0f, -42f), new Vector2(W * 0.8f, 2f));

            // ── Danh sách top 10 ──
            var list = CreateLabel(panel.transform, "List", new Vector2(0f, 1f), new Vector2(1f, 0.35f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), "", 18, TextWhite, TextAlignmentOptions.TopLeft);
            var lrt = (RectTransform)list.transform;
            lrt.offsetMin = new Vector2(30f, 0f);
            lrt.offsetMax = new Vector2(-30f, -50f);
            list.text = "Loading...";
            list.lineSpacing = 8f;  // giãn dòng cho thoáng

            // ── Hàng dưới cùng: Name Input + SUBMIT ──
            EnsureNameInput(panel.transform);
            EnsureSubmitButton(panel.transform);

            // ── Thông báo trạng thái ──
            var status = CreateLabel(panel.transform, "Status", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(400f, 22f), "", 14,
                new Color(1f, 0.5f, 0.5f, 1f), TextAlignmentOptions.Center);
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
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 38f);  // góc trái dưới, có padding
            rt.sizeDelta = new Vector2(160f, 42f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.07f, 0.18f, 1f);

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
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 38f);  // góc phải dưới, có padding
            rt.sizeDelta = new Vector2(140f, 42f);

            var img = go.GetComponent<Image>();
            img.color = AccentCyan;

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var label = CreateLabel(go.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), "SUBMIT", 20, Color.white, TextAlignmentOptions.Center);
            var lrt = (RectTransform)label.transform;
            lrt.offsetMin = new Vector2(8f, 0f);
            lrt.offsetMax = new Vector2(-8f, 0f);

            return btn;
        }

        /// <summary>Nút ✕ Close — góc phải trên,ấn để ẩn leaderboard.</summary>
        private static void EnsureCloseButton(Transform parent, float panelWidth)
        {
            Transform existing = parent.Find("CloseButton");
            if (existing != null) return;

            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-8f, -6f);  // góc phải trên
            rt.sizeDelta = new Vector2(32f, 32f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.1f, 0.25f, 0.8f);

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() => Hide(parent.gameObject));

            var label = CreateLabel(go.transform, "X", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), "✕", 20, CloseRed, TextAlignmentOptions.Center);
            var lrt = (RectTransform)label.transform;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
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

        private static void AddBorder(Transform parent, Vector2 size, Color color)
        {
            float t = 2f;
            CreateStrip(parent, "BorderTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -t * 0.5f), new Vector2(size.x, t), color);
            CreateStrip(parent, "BorderBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t * 0.5f), new Vector2(size.x, t), color);
            CreateStrip(parent, "BorderLeft", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(t * 0.5f, 0f), new Vector2(t, size.y), color);
            CreateStrip(parent, "BorderRight", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-t * 0.5f, 0f), new Vector2(t, size.y), color);
        }

        private static void CreateStrip(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color ?? BorderCyan;
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
