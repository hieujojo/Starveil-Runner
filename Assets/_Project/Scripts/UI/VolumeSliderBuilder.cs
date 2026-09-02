using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoidRunner.Systems.Audio;
using VoidRunner.Systems.Save;

namespace VoidRunner.UI
{
    /// <summary>
    /// Builder slider âm lượng dùng chung (MainMenu + Pause overlay) — tạo bằng code, idempotent.
    /// Gồm 1 hàng ngang: label "VOLUME" (trái) + slider kéo 0..1 (phải).
    /// Đổi giá trị → AudioManager.SetVolume (lưu qua SaveSystem); AudioManager chưa có thì tự ghi
    /// SaveSystem để khi vào Game đọc đúng (giống pattern ToggleSound cũ).
    /// 2026-08-12: thay nút bật/tắt âm thanh bằng slider — user yêu cầu "1 thanh slide ngắn kéo được".
    /// </summary>
    public static class VolumeSliderBuilder
    {
        /// <summary>Dựng slider âm lượng dưới parent. Trả về Slider để gọi tiếp nếu cần.</summary>
        public static Slider Build(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color accent)
        {
            // 2026-08-12 v2 (user reject style viền mảnh): container = HỘP CHỨA ĐẶC giống hệt nút
            // Play/HowToPlay (solid màu BtnDark tím đậm — kế thừa vị trí + kích thước nút Sound cũ,
            // hàng nút menu đồng bộ). KHÔNG dùng viền mảnh nữa.
            var container = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            container.transform.SetParent(parent, false);
            var crt = (RectTransform)container.transform;
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = anchoredPos;
            crt.sizeDelta = size;

            var containerBg = container.GetComponent<Image>();
            containerBg.sprite = GetSprite("Assets/Space_Exploration_GUI_Kit/Settings_&_Menu_Components/Extra Large/sound-bar-container-extra-large.png");
            if (containerBg.sprite != null)
            {
                containerBg.type = Image.Type.Sliced;
                containerBg.color = Color.white;
            }
            else
            {
                containerBg.color = new Color(0.29f, 0.17f, 0.54f, 1f);
            }

            // Label VOLUME (trái)
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(container.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(14f, 0f); // cách mép trái 14px
            lrt.sizeDelta = new Vector2(90f, 36f);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = "VOLUME";
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);

            // 2026-08-12 v3.2 (user: "text trong nút Play/HowToPlay không bao giờ sát mép — muốn volume
            // cũng vậy; giờ label + slider chạm full width, không chừa khoảng trống 2 bên"):
            // label cách mép trái 16px, slider cách mép phải 16px (ContentPad) — như padding nút khác.
            const float ContentPad = 16f;
            const float LabelWidth = 110f;
            const float LabelSliderGap = 10f;
            float sliderWidth = size.x - ContentPad * 2f - LabelWidth - LabelSliderGap;

            // Slider (neo PHẢI — cách mép phải ContentPad, không chạm viền hộp)
            var go = new GameObject("Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            go.transform.SetParent(container.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-ContentPad, 0f);
            rt.sizeDelta = new Vector2(sliderWidth, 36f); // taller slider cho dễ kéo

            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.25f);      // track nền sáng hơn — dễ thấy hơn

            var slider = go.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Fill Area + Fill
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fart = (RectTransform)fillArea.transform;
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.offsetMin = new Vector2(4f, 3f);
            fart.offsetMax = new Vector2(-4f, -3f);
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = accent;
            slider.fillRect = frt;

            // Handle Slide Area + Handle
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var hart = (RectTransform)handleArea.transform;
            hart.anchorMin = Vector2.zero;
            hart.anchorMax = Vector2.one;
            hart.offsetMin = new Vector2(4f, 3f);
            hart.offsetMax = new Vector2(-4f, -3f);
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleGo.transform.SetParent(handleArea.transform, false);
            var hrt = (RectTransform)handleGo.transform;
            hrt.sizeDelta = new Vector2(28f, 28f); // handle to hơn — dễ kéo trên mobile
            var hImg = handleGo.GetComponent<Image>();
            hImg.color = Color.white;
            slider.handleRect = hrt;
            slider.targetGraphic = hImg;

            // Set giá trị hiện tại TRƯỚC khi subscribe (tránh ghi đè không cần thiết lúc build)
            slider.SetValueWithoutNotify(SaveSystem.Volume);
            slider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.SetVolume(v);
                else SaveSystem.Volume = v; // AudioManager chưa có (scene menu) — tự ghi để vào Game đọc đúng
            });

            return slider;
        }

        private static Sprite GetSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignFallbackFont(TextMeshProUGUI tmp)
        {
            if (tmp.font != null) return;
            var anyTmp = Object.FindAnyObjectByType<TextMeshProUGUI>();
            tmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        }
    }
}
