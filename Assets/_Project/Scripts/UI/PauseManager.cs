using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidRunner.Core;

namespace VoidRunner.UI
{
    /// <summary>
    /// Màn hình PAUSE (2026-08-12 — user chọn phương án OVERLAY ngay trong scene Game thay vì tách
    /// scene riêng: runner vô tận sinh track/bọ/điểm procedural, tách scene sẽ mất toàn bộ trạng thái).
    ///
    /// Cách mở: bấm nút nhỏ "II" góc trên phải HUD HOẶC phím ESC. Cách đóng: ESC / nút RESUME.
    /// Cơ chế: Time.timeScale = 0 đóng băng gameplay (physics + Update đều dừng); nhớ giá trị
    /// timeScale CŨ để khôi phục khi resume — tương thích SlowMo (PowerUpSystem) đang chạy dở.
    /// EnemyChase/DifficultyManager đã gate theo GameManager.State == Playing nên tự đứng yên khi Paused.
    ///
    /// Overlay (tạo bằng code, idempotent): nền vũ trụ tối + panel "PAUSED" với
    /// RESUME · RESTART · slider VOLUME · MENU. Nút MENU phải khôi phục timeScale TRƯỚC khi
    /// LoadScene — nếu không scene MainMenu mới sẽ bị đóng băng theo.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Tên scene MainMenu (khớp tên file .unity)")]
        private string menuSceneName = "MainMenu";

        private Canvas _canvas;
        private GameObject _overlay;
        private Button _pauseButton;
        private bool _isPaused;
        private float _timeScaleBefore = 1f;

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnRestart += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnRestart -= HandleRestart;
            // An toàn: scene unload / object bị hủy khi đang pause → trả thời gian thật (không để
            // game đóng băng ở scene sau). Giống PowerUpSystem trả timeScale khi bị tắt.
            if (_isPaused) ForceResume();
        }

        private void Start()
        {
            _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas == null)
            {
                enabled = false;
                return;
            }
            EnsurePauseButton();
            EnsureOverlay();
        }

        private void Update()
        {
            // ESC = pause/resume (chỉ ở scene Game — PauseManager chỉ tồn tại ở đó qua GameManager.EnsurePause)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_isPaused) Resume();
            else Pause();
        }

        /// <summary>Pause game: chỉ khi đang Playing (không pause lúc Game Over / Menu).</summary>
        public void Pause()
        {
            if (_isPaused) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            _isPaused = true;
            _timeScaleBefore = Time.timeScale; // giữ SlowMo nếu đang chạy dở
            Time.timeScale = 0f;
            GameManager.Instance.SetPaused(true);

            if (_overlay != null)
            {
                _overlay.SetActive(true);
                _overlay.transform.SetAsLastSibling(); // trên HUD + GameOverPanel nếu lỡ cùng lúc
            }
        }

        public void Resume()
        {
            if (!_isPaused) return;
            ForceResume();
        }

        private void ForceResume()
        {
            _isPaused = false;
            Time.timeScale = _timeScaleBefore;
            if (_overlay != null) _overlay.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused)
            {
                GameManager.Instance.SetPaused(false);
            }
        }

        private void HandleGameOver()
        {
            // Bọ nuốt player — dọn mọi overlay pause đang mở + khôi phục thời gian (phòng hờ)
            if (_isPaused) ForceResume();
            if (_overlay != null) _overlay.SetActive(false);
            // Ẩn nút pause khi Game Over (bấm vô tác dụng — guard State; ẩn cho gọn UI)
            if (_pauseButton != null) _pauseButton.gameObject.SetActive(false);
        }

        private void HandleRestart()
        {
            // Chơi lại → nút pause hiện lại
            if (_pauseButton != null) _pauseButton.gameObject.SetActive(true);
        }

        private void RestartGame()
        {
            ForceResume(); // trả timeScale + state Playing TRƯỚC, rồi Restart (ResetToStart + dựng track)
            if (GameManager.Instance != null) GameManager.Instance.Restart();
        }

        private void GoToMenu()
        {
            ForceResume(); // bắt buộc — nếu timeScale vẫn = 0, MainMenu mới sẽ đóng băng theo
            SceneManager.LoadScene(menuSceneName);
        }

        // ---------------------------------------------------------------------
        // Build UI bằng code (idempotent — chạy lại không nhân đôi)
        // ---------------------------------------------------------------------

        /// <summary>Nút pause nhỏ góc trên phải HUD — "II" (font ASCII an toàn, R5.2).</summary>
        private void EnsurePauseButton()
        {
            Transform existing = _canvas.transform.Find("PauseButton");
            if (existing != null)
            {
                _pauseButton = existing.GetComponent<Button>(); // cache — HandleGameOver/Restart cần
                return;
            }

            var go = new GameObject("PauseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta = new Vector2(56f, 56f); // đủ to cho touch mobile

            var img = go.GetComponent<Image>();
            img.color = new Color(0.06f, 0.04f, 0.12f, 0.75f); // tím đen mờ — không che game

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(TogglePause);

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = "II";
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);

            _pauseButton = btn; // lưu ref — HandleGameOver ẩn / HandleRestart hiện lại
        }

        private void EnsureOverlay()
        {
            Transform existing = _canvas.transform.Find("PauseOverlay");
            if (existing != null)
            {
                _overlay = existing.gameObject;
                return;
            }

            var root = new GameObject("PauseOverlay", typeof(RectTransform));
            root.transform.SetParent(_canvas.transform, false);
            var rrt = (RectTransform)root.transform;
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            // Nền vũ trụ tối phủ toàn màn hình (chặn click xuyên xuống game/HUD)
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            var brt = (RectTransform)backdrop.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var bimg = backdrop.GetComponent<Image>();
            bimg.color = new Color(0.06f, 0.04f, 0.12f, 0.94f); // tông vũ trụ tím đen (khớp popup khác)
            bimg.raycastTarget = true;

            // Panel trung tâm
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(520f, 560f);
            var pimg = panel.GetComponent<Image>();
            pimg.color = new Color(0.06f, 0.04f, 0.12f, 0.98f);
            pimg.raycastTarget = true;

            CreateText(panel.transform, "Title", new Vector2(0f, 235f), "PAUSED", 52, new Color(1f, 0.85f, 0.3f, 1f));
            CreateButton(panel.transform, "ResumeButton", new Vector2(0f, 120f), "RESUME", new Color(0.2f, 0.75f, 1f, 1f), Resume);
            CreateButton(panel.transform, "RestartButton", new Vector2(0f, 45f), "RESTART", new Color(0.48f, 0.29f, 1f, 1f), RestartGame);
            VolumeSliderBuilder.Build(panel.transform, "VolumeSlider", new Vector2(0f, -40f), new Vector2(380f, 60f), new Color(0.2f, 0.75f, 1f, 1f));
            CreateButton(panel.transform, "MenuButton", new Vector2(0f, -130f), "MENU", new Color(0.48f, 0.29f, 1f, 1f), GoToMenu);

            _overlay = root;
            root.SetActive(false);
        }

        private static void CreateText(Transform parent, string name, Vector2 anchoredPos, string text, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(440f, 56f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);
        }

        private static void CreateButton(Transform parent, string name, Vector2 anchoredPos, string text, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(340f, 56f);
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(onClick);

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);
        }

        private static void AssignFallbackFont(TextMeshProUGUI tmp)
        {
            if (tmp.font != null) return;
            var anyTmp = Object.FindAnyObjectByType<TextMeshProUGUI>();
            tmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        }
    }
}
