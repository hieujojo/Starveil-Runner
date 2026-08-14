using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidRunner.Core;
using VoidRunner.Systems.Save;
using VoidRunner.Systems.Score;
using VoidRunner.Systems.Leaderboard;

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

        [Header("Hiệu ứng")]
        [SerializeField] private float fadeDuration = 0.4f;

        private ScoreSystem _scoreSystem;
        private CanvasGroup _panelGroup;
        private GameObject _leaderboardPanel;

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

            // Chuẩn bị CanvasGroup cho fade TRƯỚC — panel phải luôn hiện được dù ScoreSystem lỗi
            _panelGroup = gameOverPanel != null ? gameOverPanel.GetComponent<CanvasGroup>() : null;
            if (gameOverPanel != null && _panelGroup == null)
            {
                _panelGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }

            // Leaderboard online (Mục 2 UPGRADE_PLAN) — dựng panel + nút SUBMIT bằng code, idempotent
            _leaderboardPanel = LeaderboardView.Ensure(gameOverPanel != null ? gameOverPanel.transform : null);
            Transform submit = _leaderboardPanel != null ? _leaderboardPanel.transform.Find("SubmitButton") : null;
            if (submit != null)
            {
                var btn = submit.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(SubmitLeaderboardScore);
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

            // Tải top 10 + hiện panel leaderboard (ẩn cùng panel Game Over)
            LeaderboardView.Show(_leaderboardPanel);
        }

        private void HideGameOver()
        {
            gameOverPanel?.SetActive(false);
            LeaderboardView.Hide(_leaderboardPanel);
        }

        /// <summary>Gửi điểm hiện tại lên leaderboard (nút SUBMIT trên panel Game Over).</summary>
        private void SubmitLeaderboardScore()
        {
            if (_leaderboardPanel == null || _scoreSystem == null) return;
            TMP_InputField input = _leaderboardPanel.transform.Find("NameInput")?.GetComponent<TMP_InputField>();
            string name = input != null ? input.text : "AAA";
            LeaderboardView.Submit(_leaderboardPanel, name, _scoreSystem.Score);
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(RestartGame);
            if (menuButton != null) menuButton.onClick.RemoveListener(GoToMenu);
        }

        private void RestartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Restart();
            }
        }

        private void GoToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
