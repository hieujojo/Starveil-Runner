using UnityEngine;
using UnityEngine.UI;

namespace VoidRunner.UI
{
    /// <summary>
    /// Theme UI dùng chung cho các builder dựng bằng code (MainMenu/Pause/Credits/ShipSelect) —
    /// tách từ CreditsPanelBuilder.AddBorder (2026-08-12) để tái dùng cho slider volume + nút Ship/Credits.
    /// Phong cách: nền tím đen + viền neon mỏng 4 cạnh + vạch accent dọc sát mép (chi tiết ngoài lề
    /// — user: "chủ yếu các chi tiết ngoài lề như cạnh viền thôi").
    /// </summary>
    public static class UITheme
    {
        // ---- Bảng màu tông (khớp các popup có sẵn) ----
        public static readonly Color PanelBg   = new Color(0.06f, 0.04f, 0.12f, 1f);      // tím đen đục
        public static readonly Color ButtonBg  = new Color(0.05f, 0.04f, 0.12f, 0.95f);    // nền nút tối
        public static readonly Color Cyan      = new Color(0.2f, 0.75f, 1f, 1f);           // cyan — tông chính
        public static readonly Color CyanFaint = new Color(0.35f, 0.85f, 1f, 0.35f);       // cyan mờ (panel)
        public static readonly Color Purple    = new Color(0.48f, 0.29f, 1f, 1f);          // tím — tông phụ
        public static readonly Color Gold      = new Color(1f, 0.85f, 0.3f, 1f);           // vàng — điểm nhấn

        /// <summary>Viền 4 cạnh mỏng (kiểu HUD) quanh một RectTransform — y hệt CreditsPanelBuilder.AddBorder cũ.</summary>
        public static void AddBorder(Transform parent, Vector2 size, float thickness, Color color)
        {
            CreateStrip(parent, "BorderTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -thickness * 0.5f), new Vector2(size.x, thickness), color);
            CreateStrip(parent, "BorderBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness * 0.5f), new Vector2(size.x, thickness), color);
            CreateStrip(parent, "BorderLeft", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(thickness * 0.5f, 0f), new Vector2(thickness, size.y), color);
            CreateStrip(parent, "BorderRight", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-thickness * 0.5f, 0f), new Vector2(thickness, size.y), color);
        }

        /// <summary>Vạch accent dọc nhỏ sát mép trái/phải của nút — chi tiết ngoài lề (idempotent theo tên).</summary>
        public static void AddEdgeAccent(Transform parent, string name, bool right, Color color)
        {
            if (parent.Find(name) != null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (right)
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-10f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(10f, 0f);
            }
            rt.sizeDelta = new Vector2(3f, 30f);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void CreateStrip(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
