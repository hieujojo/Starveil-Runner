using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidRunner.Systems.Audio;
using VoidRunner.Systems.Save;

namespace VoidRunner.UI
{
    /// <summary>
    /// Màn hình MainMenu (scene riêng): nút Play → load scene Game,
    /// How to play (toggle panel), hiển thị best score, nút âm thanh bật/tắt.
    /// Không phụ thuộc gameplay — chỉ là entry point + đọc SaveSystem.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Nút (kéo thả)")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button creditsButton; // tạo bằng code (EnsureCreditsButton)

        [Header("Panel")]
        [SerializeField] private GameObject howToPlayPanel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI soundButtonText;

        [Header("Scene")]
        [SerializeField, Tooltip("Tên scene Game (khớp tên file .unity)")]
        private string gameSceneName = "Game";

        // Dimmer toàn màn hình — làm tối main menu khi mở HowToPlay (fix 2026-08-11:
        // trước đây panel hiện đè lên menu sáng → rất khó đọc)
        private GameObject _dimmer;

        private void Start()
        {
            howToPlayPanel?.SetActive(false);
            RefreshBestScore();
            RefreshSoundLabel();
            EnsureCloseButton(); // nút CLOSE trên panel — đóng popup rõ ràng (không chỉ click dimmer)
            EnsureCredits();    // nút CREDITS + panel credits (tạo bằng code, idempotent)

            if (playButton != null) playButton.onClick.AddListener(PlayGame);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(ToggleHowToPlay);
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            if (creditsButton != null) creditsButton.onClick.AddListener(ToggleCredits);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(PlayGame);
            if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(ToggleHowToPlay);
            if (soundButton != null) soundButton.onClick.RemoveListener(ToggleSound);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(ToggleCredits);
        }

        private void PlayGame()
        {
            if (string.IsNullOrEmpty(gameSceneName)) return;
            SceneManager.LoadScene(gameSceneName);
        }

        private void ToggleHowToPlay()
        {
            if (howToPlayPanel == null) return;
            bool show = !howToPlayPanel.activeSelf;

            if (show) EnsureDimmer();
            howToPlayPanel.SetActive(show);
            if (_dimmer != null) _dimmer.SetActive(show);

            if (show)
            {
                // Panel phải ĐỤC HOÀN TOÀN (fix vòng 2 2026-08-11): alpha 0.92 vẫn để các nút menu
                // phía sau (PlayButton y=60, BestScore y=-230 nằm TRONG vùng panel 720×480) lộ xuyên
                // qua → đọc rối. Ép alpha=1 để panel che kín mọi thứ đằng sau.
                var panelImg = howToPlayPanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    var c = panelImg.color;
                    c.a = 1f;
                    panelImg.color = c;
                }

                // Panel luôn vẽ TRÊN dimmer (SetAsLastSibling) — menu phía sau tối lại, text đọc rõ
                howToPlayPanel.transform.SetAsLastSibling();
                if (_dimmer != null) _dimmer.transform.SetAsFirstSibling();
            }
        }

        /// <summary>
        /// Tạo nút "CLOSE" góc phải panel HowToPlay (fix 2026-08-11 — user: "HowToPlay ổn nhưng
        /// không có nút để tắt"). Trước đây chỉ có cách click vùng tối (dimmer) — user không rõ.
        /// Tạo bằng code, idempotent (transform.Find kiểm tra trước khi tạo).
        /// </summary>
        private void EnsureCloseButton()
        {
            if (howToPlayPanel == null) return;
            if (howToPlayPanel.transform.Find("CloseButton") != null) return;

            // Nút nền
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(howToPlayPanel.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -24f);
            rt.sizeDelta = new Vector2(150f, 56f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.48f, 0.29f, 1f, 1f); // tím — tông nút phụ (khớp HowToPlayButton)

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(ToggleHowToPlay);
            btn.transition = Selectable.Transition.ColorTint;

            // Label con
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);

            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10f, 4f);
            lrt.offsetMax = new Vector2(-10f, -4f);

            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = "CLOSE";
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            // Fallback font: dùng font của text TMP bất kỳ trong scene (thường Kenney Future)
            if (tmp.font == null)
            {
                var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();
                tmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
            }
        }

        /// <summary>
        /// Tạo nút CREDITS (góc dưới — y=-250, nhỏ) + panel credits hiển thị third-party assets
        /// (dữ liệu khớp agent/CREDITS.md). Tạo bằng code idempotent — không cần kéo thả scene.
        /// </summary>
        private void EnsureCredits()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // --- Nút CREDITS (chỉ tạo nếu chưa có) ---
            Transform existingBtn = canvas.transform.Find("CreditsButton");
            if (existingBtn != null)
            {
                creditsButton = existingBtn.GetComponent<Button>();
            }
            else
            {
                var go = new GameObject("CreditsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(canvas.transform, false);

                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -280f); // dưới BestScore (-230) — an toàn màn hình nhỏ (góp ý reviewer: -320 có thể cắt dưới)
                rt.sizeDelta = new Vector2(280f, 56f);

                var img = go.GetComponent<Image>();
                img.color = new Color(0.48f, 0.29f, 1f, 1f); // tím — tông nút phụ

                var btn = go.GetComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                btn.onClick.AddListener(ToggleCredits);

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
                if (tmp.font == null)
                {
                    var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();
                    tmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
                }

                creditsButton = btn;
            }

            // --- Panel credits (ẩn sẵn) ---
            if (canvas.transform.Find("CreditsPanel") != null) return;

            var panel = new GameObject("CreditsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(760f, 560f);
            var pimg = panel.GetComponent<Image>();
            pimg.color = new Color(0.07f, 0.05f, 0.14f, 1f); // tím đen đục hoàn toàn (R0.9)

            // Text nội dung credits
            var textGo = new GameObject("CreditsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panel.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(30f, 30f);
            trt.offsetMax = new Vector2(-30f, -80f); // chừa chỗ nút CLOSE trên

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = BuildCreditsText();
            text.fontSize = 22;
            text.color = new Color(0.85f, 0.82f, 1f, 1f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            if (text.font == null)
            {
                var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();
                text.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
            }

            // Nút CLOSE trên panel credits
            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            var crt = (RectTransform)closeGo.transform;
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-20f, -20f);
            crt.sizeDelta = new Vector2(140f, 52f);
            var cimg = closeGo.GetComponent<Image>();
            cimg.color = new Color(0.48f, 0.29f, 1f, 1f);
            var cbtn = closeGo.GetComponent<Button>();
            cbtn.transition = Selectable.Transition.ColorTint;
            cbtn.onClick.AddListener(ToggleCredits);

            var closeLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            closeLabel.transform.SetParent(closeGo.transform, false);
            var clrt = (RectTransform)closeLabel.transform;
            clrt.anchorMin = Vector2.zero;
            clrt.anchorMax = Vector2.one;
            clrt.offsetMin = Vector2.zero;
            clrt.offsetMax = Vector2.zero;
            var ctmp = closeLabel.GetComponent<TextMeshProUGUI>();
            ctmp.text = "CLOSE";
            ctmp.fontSize = 26;
            ctmp.fontStyle = FontStyles.Bold;
            ctmp.color = Color.white;
            ctmp.alignment = TextAlignmentOptions.Center;
            ctmp.raycastTarget = false;
            ctmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (ctmp.font == null)
            {
                var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();
                ctmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
            }

            panel.SetActive(false);
        }

        /// <summary>Nội dung credits — danh sách third-party assets (khớp agent/CREDITS.md).</summary>
        private static string BuildCreditsText()
        {
            return "THIRD-PARTY ASSETS\n\n" +
                   "Kenney — UI Pack, Space Kit, Particle Pack, Game Icons,\n" +
                   "   Space Station Kit, Fonts, Audio (CC0 Public Domain)\n" +
                   "   https://kenney.nl\n\n" +
                   "Nebula Skyboxes — Skybox cubemaps (Unity Asset Store EULA)\n" +
                   "SpaceSkies Free by PULSAR BYTES — Skybox (Unity Asset Store EULA)\n" +
                   "   https://assetstore.unity.com\n\n" +
                   "Game code, design and gameplay — Void Runner project.\n" +
                   "Developed with Unity Engine (c) Unity Technologies.\n";
        }

        /// <summary>Bật/tắt panel credits (có dimmer giống HowToPlay).</summary>
        private void ToggleCredits()
        {
            var panel = FindAnyObjectByType<Canvas>()?.transform.Find("CreditsPanel");
            if (panel == null) return;

            bool show = !panel.gameObject.activeSelf;

            if (show) EnsureDimmer();
            panel.gameObject.SetActive(show);
            if (_dimmer != null)
            {
                _dimmer.SetActive(show);
                if (show) _dimmer.transform.SetAsFirstSibling();
            }
            if (show) panel.SetAsLastSibling();
        }

        /// <summary>Tạo 1 Image đen alpha 0.72 phủ toàn màn hình, nằm DƯỚI panel (idempotent).</summary>
        private void EnsureDimmer()
        {
            if (_dimmer != null) return;
            if (howToPlayPanel == null) return;

            Canvas canvas = howToPlayPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("HowToPlayDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            // 0.72 → 0.85 (fix vòng 2): menu phía sau tối sâu hơn, popup nổi bật hơn hẳn
            img.color = new Color(0f, 0f, 0f, 0.85f);
            img.raycastTarget = true; // chặn click xuyên xuống nút menu phía sau

            // Click vào vùng tối (ngoài popup) = đóng popup — dimmer phải là Button, nếu không
            // user bị kẹt (nút HowToPlay phía sau bị dimmer chặn, không đóng được — góp ý reviewer)
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(ToggleHowToPlay);
            btn.transition = Selectable.Transition.None;

            go.SetActive(false);
            _dimmer = go;
        }

        /// <summary>Bật/tắt âm thanh (0 hoặc 1) — lưu qua SaveSystem, áp ngay qua AudioManager.</summary>
        private void ToggleSound()
        {
            float next = SaveSystem.Volume > 0.01f ? 0f : 1f;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVolume(next);
            }
            else
            {
                SaveSystem.Volume = next; // AudioManager chưa có (scene menu) — tự ghi để khi vào Game đọc đúng
            }
            RefreshSoundLabel();
        }

        private void RefreshBestScore()
        {
            if (bestScoreText == null) return;

            // R0.6: best score chỉ hiển thị khi có điểm thật (> 0) — lần đầu chơi = 0 là vô nghĩa
            bool hasBest = SaveSystem.BestScore > 0;
            bestScoreText.gameObject.SetActive(hasBest);
            if (hasBest)
            {
                bestScoreText.text = $"BEST SCORE: {SaveSystem.BestScore:N0}";
            }
        }

        private void RefreshSoundLabel()
        {
            if (soundButtonText != null)
            {
                soundButtonText.text = SaveSystem.Volume > 0.01f ? "SOUND: ON" : "SOUND: OFF";
            }
        }
    }
}
