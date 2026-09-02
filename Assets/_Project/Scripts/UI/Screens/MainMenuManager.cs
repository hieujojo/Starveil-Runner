using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        [SerializeField, Tooltip("2026-08-12: chỉ dùng để ẨN đi — thay bằng slider âm lượng (VolumeSliderBuilder)")]
        private Button soundButton;
        [SerializeField] private Button creditsButton; // tạo bằng code (EnsureCreditsButton)

        [Header("Panel")]
        [SerializeField] private GameObject howToPlayPanel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI pressStartText; // M2: hướng dẫn "PRESS SPACE TO START"

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
            if (pressStartText != null) pressStartText.gameObject.SetActive(false); // M2: mặc định ẩn hint
            EnsureCloseButton(); // nút CLOSE trên panel — đóng popup rõ ràng
            EnsureCredits();    // nút CREDITS + panel credits (tạo bằng code, idempotent)
            EnsureShipSelect(); // Task D: panel chọn ship (preview 3D, lưu SaveSystem.SelectedShip)
            EnsureVolumeSlider(); // 2026-08-12: thay nút bật/tắt âm thanh bằng slider kéo
            SetupMainMenuPositions(); // FIX M1: cân bằng spacing giữa các nút
            ShowPressStartHint(); // M2: hiển thị hint bắt đầu

            if (playButton != null) playButton.onClick.AddListener(PlayGame);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(ToggleHowToPlay);
            if (creditsButton != null) creditsButton.onClick.AddListener(ToggleCredits);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(PlayGame);
            if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(ToggleHowToPlay);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(ToggleCredits);
        }

        private void PlayGame()
        {
            // M2: Ẩn hint "PRESS SPACE TO START" khi bấm Play
            pressStartText?.gameObject.SetActive(false);
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

                // ⚠️ FIX 2026-08-12 (bug "popup đè lộ VOID RUNNER phía sau"): dimmer phải nằm TRÊN
                // menu nhưng DƯỚI panel. SetAsFirstSibling (cũ) chìm dimmer XUỐNG DƯỚI menu → các nút
                // menu vẽ lên trên → vẫn sáng rõ phía sau panel. Đúng: dimmer SetAsLastSibling TRƯỚC,
                // rồi panel SetAsLastSibling SAU → dimmer che menu, panel che dimmer.
                if (_dimmer != null) _dimmer.transform.SetAsLastSibling();
                howToPlayPanel.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// Tạo nút đóng dấu X nhỏ góc trên phải panel HowToPlay (fix 2026-08-11 — user: "HowToPlay
        /// ổn nhưng không có nút để tắt"). 2026-08-12: đổi nút CLOSE TO (150×56 che chữ trong panel)
        /// → nút X vuông nhỏ 40×40, nằm lệch ra ngoài viền (không che text).
        /// Tạo bằng code, idempotent (transform.Find kiểm tra trước khi tạo).
        /// </summary>
        private void EnsureCloseButton()
        {
            if (howToPlayPanel == null) return;
            if (howToPlayPanel.transform.Find("CloseButton") != null) return;

            // Nút nền — vuông nhỏ, đặt lệch ra ngoài mép trên phải (không đè chữ bên trong)
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(howToPlayPanel.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-8f, -8f);
            rt.sizeDelta = new Vector2(40f, 40f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.48f, 0.29f, 1f, 1f); // tím — tông nút phụ (khớp HowToPlayButton)

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(ToggleHowToPlay);
            btn.transition = Selectable.Transition.ColorTint;

            // Label con — chữ X đậm (font chỉ pack ASCII — ✕ U+2715 ra ô vuông □, R5.2)
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);

            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontSize = 28;
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
            creditsButton = CreditsPanelBuilder.EnsureButton(canvas.transform, "CreditsButton", new Vector2(160f, -210f), new Vector2(260f, 50f)); // M1: cân bằng spacing — trên ship (-280), dưới volume (-60)

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
            if (_dimmer != null) _dimmer.SetActive(show);
            if (show)
            {
                // Cùng fix dimmer như ToggleHowToPlay: dimmer trên menu, panel trên dimmer
                _dimmer.transform.SetAsLastSibling();
                panel.SetAsLastSibling();
            }
        }

        /// <summary>Tạo 1 Image đen alpha 0.93 phủ toàn màn hình, nằm TRÊN menu nhưng DƯỚI panel (idempotent).</summary>
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
            // 0.85 → 0.93 (fix 2026-08-12): menu phía sau gần như khuất hẳn, không lộ chữ VOID RUNNER
            img.color = new Color(0f, 0f, 0f, 0.93f);
            img.raycastTarget = true; // chặn click xuyên xuống nút menu phía sau

            // Click vào vùng tối (ngoài popup) = đóng popup ĐANG MỞ — dimmer dùng chung cho cả
            // HowToPlay + Credits nên không thể hardcode ToggleHowToPlay (bug 2026-08-12: click
            // dimmer khi Credits mở sẽ BẬT HowToPlay). Đóng popup nào đang active.
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(CloseActivePopup);
            btn.transition = Selectable.Transition.None;

            go.SetActive(false);
            _dimmer = go;
        }

        /// <summary>Đóng popup đang mở (HowToPlay hoặc Credits) — dùng cho click dimmer (dùng chung).</summary>
        private void CloseActivePopup()
        {
            if (howToPlayPanel != null && howToPlayPanel.activeSelf)
            {
                ToggleHowToPlay();
                return;
            }
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform credits = canvas.transform.Find("CreditsPanel");
                if (credits != null && credits.gameObject.activeSelf)
                {
                    ToggleCredits();
                    return;
                }
            }
            // Không popup nào mở → tự tắt dimmer (an toàn)
            if (_dimmer != null) _dimmer.SetActive(false);
        }

        /// <summary>
        /// 2026-08-12 (user: "âm thanh chỉ có 1 nút bật tắt, muốn 1 thanh slide kéo được"):
        /// ẩn nút SoundButton cũ + dựng slider âm lượng ĐÚNG vị trí nút cũ (0, -60).
        /// ⚠️ FIX overlap (góp ý reviewer): ban đầu đặt (0,-130) nhưng BestScoreText ở (0,-160)
        /// (rect 600×50 → top -135) → đè 18px. (0,-60) nằm giữa HowToPlay (y=24) và Best (y=-160)
        /// — không chạm gì (HowTo bottom -14 vs slider top -37; slider bottom -83 vs Best top -135).
        /// Idempotent.
        /// </summary>
        private void EnsureVolumeSlider()
        {
            if (soundButton != null) soundButton.gameObject.SetActive(false);
            if (soundButton == null) return;

            Canvas canvas = soundButton.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (canvas.transform.Find("VolumeSlider") != null) return;

            VolumeSliderBuilder.Build(
                canvas.transform, "VolumeSlider",
                new Vector2(0f, -60f), new Vector2(440f, 76f), // v3: nới rộng 360→440 (user: "hơi chật") — slider thoáng hơn
                new Color(0.2f, 0.75f, 1f, 1f)); // cyan — khớp tông nút chính
        }

        /// <summary>FIX M1: Cân bằng spacing giữa các nút Main Menu.</summary>
        /// Cũ: PLAY y=60, HOW TO PLAY y=24, SHIP y=-245, CREDITS y=-245 (không đều).
        /// Mới: PLAY y=80, HOW TO PLAY y=0, SHIP y=-80, CREDITS y=-160 (khoảng cách 80px đều).
        private void SetupMainMenuPositions()
        {
            // PLAY: y=80
            if (playButton != null)
            {
                var rt = playButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 80f);
                }
            }

            // HOW TO PLAY: y=0
            if (howToPlayButton != null)
            {
                var rt = howToPlayButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 0f);
                }
            }

            // CREDITS: y=-160 (cùng hàng với ShipSelect theo Task D, ship ở y=-80)
            if (creditsButton != null)
            {
                var rt = creditsButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, -160f);
                }
            }

            // Ship select button không có field trực tiếp trong MainMenuManager —
            // được tạo bởi EnsureShipSelect() -> ShipSelectManager, vị trí của ship
            // phụ thuộc vào ShipSelectManager setup. Nhận định: ship nên ở y=-80
            // để cân bằng với Credits y=-160 (cách 80px).
            // Nếu cần điều chỉnh: sửa trong ShipSelectManager hoặc EnsureShipSelect.
        }

        /// <summary>M2: Hiển thị/text nhấp nháy "PRESS SPACE TO START" dưới nút PLAY.</summary>
        private void ShowPressStartHint()
        {
            if (pressStartText == null) return;
            // Vị trí: dưới Play button, cách khoảng 20px
            if (playButton != null)
            {
                var playRt = playButton.GetComponent<RectTransform>();
                var startRt = pressStartText.rectTransform;
                if (playRt != null && startRt != null)
                {
                    // Đặt text ngay dưới Play button, căn giữa horizont
                    startRt.anchoredPosition = new Vector2(playRt.anchoredPosition.x, playRt.anchoredPosition.y - 30f);
                    startRt.sizeDelta = new Vector2(300f, 40f); // vừa đủ text
                }
            }
            pressStartText.color = new Color(0.0f, 0.8f, 1f, 1f); // cyan khớp tông nút
            // Font sẽ được gán qua Inspector (giống bestScoreText)
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

    }
}
