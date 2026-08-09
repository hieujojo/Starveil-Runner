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

            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
            if (_scoreSystem == null)
            {
                Debug.LogWarning("UIManager không tìm thấy ScoreSystem — HUD sẽ không cập nhật.");
                return;
            }

            _scoreSystem.OnScoreChanged += HandleScoreChanged;
            _scoreSystem.OnComboChanged += HandleComboChanged;
            HandleComboChanged(_scoreSystem.Multiplier); // trạng thái combo ban đầu (ẩn x1)

            _panelGroup = gameOverPanel != null ? gameOverPanel.GetComponent<CanvasGroup>() : null;
            if (gameOverPanel != null && _panelGroup == null)
            {
                _panelGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }
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
            if (gameOverPanel == null || _scoreSystem == null) return;

            SaveSystem.BestScore = _scoreSystem.Score;

            if (finalScoreText != null) finalScoreText.text = $"ĐIỂM: {_scoreSystem.Score:N0}";
            if (bestScoreText != null) bestScoreText.text = $"CAO NHẤT: {SaveSystem.BestScore:N0}";

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
