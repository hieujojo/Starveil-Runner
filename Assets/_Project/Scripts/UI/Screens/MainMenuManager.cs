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
            EnsureShipSelect(); // Task D: panel chọn ship (preview 3D, lưu SaveSystem.SelectedShip)

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
        /// Tạo nút CREDITS + panel credits (thiết kế đẹp — xem CreditsPanelBuilder).
        /// Idempotent, tạo bằng code — không cần kéo thả scene.
        /// </summary>
        private void EnsureCredits()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Nút CREDITS — CÙNG HÀNG với nút SHIP (y=-280): CREDITS bên PHẢI (160), SHIP bên TRÁI (-160)
            // (tránh chồng nhau — user thêm chọn ship Task D). KHÔNG subscribe onClick ở đây —
            // Start() đã subscribe (subscribe 2 lần = 1 click toggle 2 lần = nhìn như hỏng — góp ý reviewer).
            creditsButton = CreditsPanelBuilder.EnsureButton(canvas.transform, "CreditsButton", new Vector2(160f, -280f), new Vector2(300f, 56f));

            // Panel credits + nút CLOSE
            GameObject panel = CreditsPanelBuilder.EnsurePanel(canvas);
            if (panel != null)
            {
                Transform close = panel.transform.Find("CreditsClose");
                if (close != null)
                {
                    var closeBtn = close.GetComponent<Button>();
                    if (closeBtn != null) closeBtn.onClick.AddListener(ToggleCredits);
                }
            }
        }

        /// <summary>Task D: gắn ShipSelectManager (tạo panel chọn ship) — idempotent.</summary>
        private void EnsureShipSelect()
        {
            if (FindAnyObjectByType<ShipSelectManager>() != null) return;
            gameObject.AddComponent<ShipSelectManager>();
        }

        /// <summary>Bật/tắt panel credits (có dimmer giống HowToPlay).</summary>
        private void ToggleCredits()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform panel = canvas.transform.Find("CreditsPanel");
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
