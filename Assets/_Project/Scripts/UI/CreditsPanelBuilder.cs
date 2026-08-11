using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoidRunner.UI
{
    /// <summary>
    /// Builder cho UI CREDITS — dùng chung ở MainMenu + Game Over (tránh duplicate code).
    /// Tạo bằng code, idempotent (transform.Find trước khi tạo). Panel: nền tím đen đục
    /// (R0.9) + viền cyan neon + tiêu đề + danh sách third-party assets (khớp agent/CREDITS.md).
    /// Caller (MainMenuManager/UIManager) quản lý toggle + dimmer — builder chỉ dựng UI tĩnh.
    /// </summary>
    public static class CreditsPanelBuilder
    {
        private const string PanelName = "CreditsPanel";
        private const string ButtonName = "CreditsButton";
        private const string CloseName = "CreditsClose";

        /// <summary>Tạo nút CREDITS (tím — nút phụ) tại vị trí chỉ định, idempotent. Trả về Button.</summary>
        /// <param name="parent">Nơi gắn nút — canvas root (MainMenu) HOẶC gameOverPanel (Game Over, ẩn cùng panel).</param>
        public static Button EnsureButton(Transform parent, string buttonName, Vector2 anchoredPos, Vector2 size)
        {
            if (parent == null) return null;
            Transform existing = parent.Find(buttonName);
            if (existing != null) return existing.GetComponent<Button>();

            var go = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.48f, 0.29f, 1f, 1f); // tím — tông nút phụ

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10f, 4f);
            lrt.offsetMax = new Vector2(-10f, -4f);

            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = "CREDITS";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);

            return btn;
        }

        /// <summary>Tạo panel credits (ẩn sẵn) + nút CLOSE, idempotent. Trả về GameObject panel.</summary>
        public static GameObject EnsurePanel(Canvas canvas)
        {
            if (canvas == null) return null;
            Transform existingPanel = canvas.transform.Find(PanelName);
            if (existingPanel != null) return existingPanel.gameObject;

            var panel = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(720f, 560f);

            var pimg = panel.GetComponent<Image>();
            pimg.color = new Color(0.06f, 0.04f, 0.12f, 1f); // tím đen — đục hoàn toàn (R0.9)
            // Viền cyan neon mờ — tông game
            AddBorder(panel.transform, new Vector2(720f, 560f));

            // Tiêu đề
            var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            title.transform.SetParent(panel.transform, false);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -18f);
            trt.sizeDelta = new Vector2(640f, 56f);
            var ttmp = title.GetComponent<TextMeshProUGUI>();
            ttmp.text = "CREDITS";
            ttmp.fontSize = 44;
            ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = new Color(1f, 0.85f, 0.3f, 1f); // vàng — tông điểm nhấn
            ttmp.alignment = TextAlignmentOptions.Center;
            ttmp.raycastTarget = false;
            AssignFallbackFont(ttmp);

            // Nội dung
            var textGo = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panel.transform, false);
            var brt = (RectTransform)textGo.transform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(36f, 36f);
            brt.offsetMax = new Vector2(-36f, -88f); // chừa chỗ tiêu đề + nút CLOSE

            var body = textGo.GetComponent<TextMeshProUGUI>();
            body.text = BuildCreditsText();
            body.fontSize = 20;
            body.color = new Color(0.82f, 0.8f, 1f, 1f);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.raycastTarget = false;
            body.textWrappingMode = TextWrappingModes.Normal;
            AssignFallbackFont(body);

            // Nút CLOSE (do caller gán onClick — builder chỉ dựng hình)
            var closeGo = new GameObject(CloseName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            var crt = (RectTransform)closeGo.transform;
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-20f, -20f);
            crt.sizeDelta = new Vector2(130f, 48f);
            var cimg = closeGo.GetComponent<Image>();
            cimg.color = new Color(0.48f, 0.29f, 1f, 1f);
            var cbtn = closeGo.GetComponent<Button>();
            cbtn.transition = Selectable.Transition.ColorTint;

            var closeLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            closeLabel.transform.SetParent(closeGo.transform, false);
            var clrt = (RectTransform)closeLabel.transform;
            clrt.anchorMin = Vector2.zero;
            clrt.anchorMax = Vector2.one;
            clrt.offsetMin = Vector2.zero;
            clrt.offsetMax = Vector2.zero;
            var ctmp = closeLabel.GetComponent<TextMeshProUGUI>();
            ctmp.text = "CLOSE";
            ctmp.fontSize = 24;
            ctmp.fontStyle = FontStyles.Bold;
            ctmp.color = Color.white;
            ctmp.alignment = TextAlignmentOptions.Center;
            ctmp.raycastTarget = false;
            ctmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(ctmp);

            panel.SetActive(false);
            return panel;
        }

        /// <summary>Viền cyan neon mờ quanh panel (2 Image mỏng trái/phải hoặc khung).</summary>
        private static void AddBorder(Transform parent, Vector2 panelSize)
        {
            // Khung: 4 cạnh mỏng màu cyan — đơn giản là 2 sọc ngang đỉnh/đáy + 2 sọc dọc
            float t = 3f;
            CreateBorderStrip(parent, "BorderTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -t * 0.5f), new Vector2(panelSize.x, t));
            CreateBorderStrip(parent, "BorderBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t * 0.5f), new Vector2(panelSize.x, t));
            CreateBorderStrip(parent, "BorderLeft", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(t * 0.5f, 0f), new Vector2(t, panelSize.y));
            CreateBorderStrip(parent, "BorderRight", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-t * 0.5f, 0f), new Vector2(t, panelSize.y));
        }

        private static void CreateBorderStrip(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
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

        /// <summary>Nội dung credits — danh sách third-party assets (khớp agent/CREDITS.md).</summary>
        public static string BuildCreditsText()
        {
            return "THIRD-PARTY ASSETS\n" +
                   "────────────────────\n\n" +
                   "Kenney — UI Pack, Space Kit, Particle Pack,\n" +
                   "   Game Icons, Space Station Kit, Fonts, Audio\n" +
                   "   (CC0 Public Domain) — kenney.nl\n\n" +
                   "Nebula Skyboxes — Cubemap skybox\n" +
                   "   (Unity Asset Store EULA)\n\n" +
                   "SpaceSkies Free by PULSAR BYTES — Skybox\n" +
                   "   (Unity Asset Store EULA)\n\n" +
                   "Free SF Fighter by CGPitbull — Space Fighter\n" +
                   "   (Unity Asset Store EULA)\n\n" +
                   "Star Sparrow Modular by Ebal Studios — Fighter\n" +
                   "   (Unity Asset Store EULA)\n\n" +
                   "Monster / Flying Beetle / Fantasy Spider — Creatures\n" +
                   "   (Unity Asset Store EULA)\n\n" +
                   "────────────────────\n" +
                   "Developed with Unity Engine — © Unity Technologies";
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
