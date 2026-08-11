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

            if (playButton != null) playButton.onClick.AddListener(PlayGame);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(ToggleHowToPlay);
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(PlayGame);
            if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(ToggleHowToPlay);
            if (soundButton != null) soundButton.onClick.RemoveListener(ToggleSound);
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

            // Panel luôn vẽ TRÊN dimmer (SetAsLastSibling) — menu phía sau tối lại, text đọc rõ
            if (show)
            {
                howToPlayPanel.transform.SetAsLastSibling();
                if (_dimmer != null) _dimmer.transform.SetAsFirstSibling();
            }
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
            img.color = new Color(0f, 0f, 0f, 0.72f);
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
