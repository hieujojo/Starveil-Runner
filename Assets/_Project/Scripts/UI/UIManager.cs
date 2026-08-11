using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidRunner.Core;
using VoidRunner.Systems.Save;
using VoidRunner.Systems.Score;

namespace VoidRunner.UI
{
    /// <summary>
    /// Quản lý toàn bộ UI: HUD (score, combo multiplier) + Game Over panel (score, best score, fade).
    /// Event-driven: subscribe ScoreSystem.OnScoreChanged/OnComboChanged + GameEvents — không coupling trực tiếp.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button creditsButton; // tạo bằng code (CreditsPanelBuilder)

        [Header("Hiệu ứng")]
        [SerializeField] private float fadeDuration = 0.4f;

        private ScoreSystem _scoreSystem;
        private CanvasGroup _panelGroup;

        private void OnEnable()
        {
            GameEvents.OnGameOver += ShowGameOver;
            GameEvents.OnRestart += HideGameOver;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= ShowGameOver;
            GameEvents.OnRestart -= HideGameOver;

            if (_scoreSystem != null)
            {
                _scoreSystem.OnScoreChanged -= HandleScoreChanged;
                _scoreSystem.OnComboChanged -= HandleComboChanged;
            }
        }

        private void Start()
        {
            gameOverPanel?.SetActive(false);

            if (retryButton != null) retryButton.onClick.AddListener(RestartGame);
            if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);

            EnsureCreditsButton(); // nút CREDITS trên Game Over panel (tạo bằng code)
            EnsureCreditsPanel();  // panel credits (dùng chung CreditsPanelBuilder)

            // Chuẩn bị CanvasGroup cho fade TRƯỚC — panel phải luôn hiện được dù ScoreSystem lỗi
            _panelGroup = gameOverPanel != null ? gameOverPanel.GetComponent<CanvasGroup>() : null;
            if (gameOverPanel != null && _panelGroup == null)
            {
                _panelGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }

            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
            if (_scoreSystem == null)
            {
                Debug.LogWarning("UIManager không tìm thấy ScoreSystem — HUD sẽ không cập nhật.");
                return;
            }

            _scoreSystem.OnScoreChanged += HandleScoreChanged;
            _scoreSystem.OnComboChanged += HandleComboChanged;
            HandleComboChanged(_scoreSystem.Multiplier); // trạng thái combo ban đầu (ẩn x1)
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString("N0");
        }

        private void HandleComboChanged(int multiplier)
        {
            if (comboText == null) return;
            comboText.gameObject.SetActive(multiplier > 1);
            comboText.text = $"x{multiplier}";
        }

        private void ShowGameOver()
        {
            // R3-4: panel BẮT BUỘC hiện khi game kết thúc — chỉ early-return khi panel không tồn tại
            if (gameOverPanel == null) return;

            // Lưu best score độc lập với ScoreSystem (player đã chơi là phải ghi nhận)
            if (_scoreSystem != null) SaveSystem.BestScore = _scoreSystem.Score;

            // R0.5: toàn bộ UI gameplay = tiếng Anh
            if (_scoreSystem != null && finalScoreText != null) finalScoreText.text = $"SCORE: {_scoreSystem.Score:N0}";
            if (bestScoreText != null) bestScoreText.text = $"BEST: {SaveSystem.BestScore:N0}";

            gameOverPanel.SetActive(true);
            if (_panelGroup != null)
            {
                _panelGroup.alpha = 0f;
                _panelGroup.DOFade(1f, fadeDuration).SetUpdate(true);
            }
        }

        private void HideGameOver()
        {
            gameOverPanel?.SetActive(false);
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(RestartGame);
            if (menuButton != null) menuButton.onClick.RemoveListener(GoToMenu);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(ToggleCredits);
        }

        private void RestartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Restart();
            }
        }

        /// <summary>Nút CREDITS nhỏ dưới Game Over panel — mở panel credits (idempotent).</summary>
        private void EnsureCreditsButton()
        {
            if (gameOverPanel == null) return;
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // ⚠️ Gắn làm CON của gameOverPanel (không phải canvas root) — nếu không nút sẽ HIỆN LỘ
            // khi panel ẩn (SetActive(false) chỉ ẩn con, không ẩn nút nằm ngoài panel).
            // y=-240: dưới RetryButton (-140)/MenuButton (-140) — tránh đè lề dưới 2 nút (góp ý reviewer)
            creditsButton = CreditsPanelBuilder.EnsureButton(gameOverPanel.transform, "GameOverCreditsButton", new Vector2(0f, -240f), new Vector2(220f, 48f));
            if (creditsButton != null) creditsButton.onClick.AddListener(ToggleCredits);
        }

        private void EnsureCreditsPanel()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

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

        private void ToggleCredits()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform panel = canvas.transform.Find("CreditsPanel");
            if (panel == null) return;

            bool show = !panel.gameObject.activeSelf;
            panel.gameObject.SetActive(show);
            if (show) panel.SetAsLastSibling();
        }

        private void GoToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
