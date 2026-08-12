using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace VoidRunner.Utils
{
    /// <summary>
    /// FPS counter đơn giản để test hiệu năng khi PLAY.
    /// - Hiển thị trên màn hình: FPS · frame time (ms) · GC heap (MB) — bật/tắt bằng F3
    /// - Sống XUYÊN SCENE (DontDestroyOnLoad): gắn 1 lần ở MainMenu là thấy cả ở Game/GameOver
    ///   (kèm anti-duplicate: nếu đã có instance khác thì tự hủy)
    /// - Log ra Console mỗi [logInterval] giây (mặc định 10s): [FPS-LOG] FPS=.. ms=.. GC=..MB
    /// - Không phụ thuộc Canvas/TMP — dùng OnGUI nên sống được mọi nơi.
    /// - Tool: Tools/Void Runner/Add FPS Counter (Open Scene) để gắn vào scene.
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        [Tooltip("Hiện ngay khi vào Play hay phải bấm phím?")]
        public bool visibleOnStart = true;

        [Tooltip("Log FPS ra Console mỗi X giây (0 = tắt log)")]
        public float logInterval = 10f;

        private bool _visible;
        private readonly float[] _frameTimes = new float[60];
        private int _index;
        private float _fps;
        private float _logTimer;
        private readonly StringBuilder _sb = new StringBuilder(128);

        private void Awake()
        {
            // Anti-duplicate: object này (ví dụ bản scene Game) thấy bản cũ từ MainMenu đang
            // DontDestroyOnLoad → tự hủy, giữ đúng 1 counter duy nhất.
            FPSCounter[] all = FindObjectsByType<FPSCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            _visible = visibleOnStart;
        }

        private void Update()
        {
            // Bật/tắt bằng F3 (Input System — không dùng legacy Input vì project chỉ dùng new Input System).
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }

            // Trung bình 60 frame để FPS bớt nhấp nháy.
            _frameTimes[_index] = Time.unscaledDeltaTime;
            _index = (_index + 1) % _frameTimes.Length;

            float sum = 0f;
            foreach (float t in _frameTimes)
            {
                sum += t;
            }

            _fps = sum > 0f ? _frameTimes.Length / sum : 0f;

            // Log định kỳ ra Console để đọc số liệu dễ hơn (không cần nhìn Game view).
            if (logInterval > 0f)
            {
                _logTimer += Time.unscaledDeltaTime;
                if (_logTimer >= logInterval)
                {
                    _logTimer = 0f;
                    float ms = Time.unscaledDeltaTime * 1000f;
                    float gcMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                    Debug.Log($"[FPS-LOG] FPS={_fps:F0} ms={ms:F1} GC={gcMb:F0}MB scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            float ms = Time.unscaledDeltaTime * 1000f;
            float gcMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

            _sb.Clear();
            _sb.Append("FPS: ");
            _sb.Append(_fps.ToString("F0"));
            _sb.Append("  (");
            _sb.Append(ms.ToString("F1"));
            _sb.Append(" ms)");
            _sb.Append("  GC: ");
            _sb.Append(gcMb.ToString("F0"));
            _sb.Append(" MB");
            _sb.Append("   [F3]");

            // Nền bán trong suốt cho dễ đọc trên nền tối/trắng.
            GUI.Box(new Rect(8, 8, 260, 26), GUIContent.none);
            GUI.Label(new Rect(14, 12, 250, 20), _sb.ToString());
        }
    }
}
