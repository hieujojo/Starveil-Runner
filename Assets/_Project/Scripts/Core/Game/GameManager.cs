using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;
using VoidRunner.Systems.VFX;
using VoidRunner.UI;

namespace VoidRunner.Core
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    /// <summary>
    /// State machine và orchestrator của game:
    /// khởi tạo run (track, void), xử lý Game Over / Restart, nối input → player.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Tham chiếu (kéo thả trong Inspector)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private TileSpawner tileSpawner;
        [SerializeField] private EnemyChase enemy; // 2026-08-12: đổi tên từ voidChase (Void → Enemy)

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            ResolveReferences();
            EnsureCameraRig();
            EnsureSpaceFX();
            EnsurePause(); // 2026-08-12: màn hình Pause (ESC + nút II, overlay trong scene) — idempotent
            StartRun();
        }

        private void ResolveReferences()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (tileSpawner == null) tileSpawner = FindAnyObjectByType<TileSpawner>();
            if (enemy == null) enemy = FindAnyObjectByType<EnemyChase>();
            // Ghi chú 2026-08-11: PlayerController tự đọc InputReader.MoveInput trực tiếp
            // (đè giữ = trượt liên tục) — không còn wiring lane event rời rạc ở đây.
        }

        /// <summary>
        /// Camera KHÔNG được trôi ngang theo tàu (bug 2026-08-11: "cảnh vật di chuyển theo"):
        /// tạo CameraRig (khóa X=0) giữa camera và player, CinemachineCamera theo dõi rig thay vì player.
        /// Idempotent — chạy lại không nhân đôi.
        /// </summary>
        private void EnsureCameraRig()
        {
            if (player == null) return;

            var cam = FindAnyObjectByType<CinemachineCamera>();
            if (cam == null) return;

            var rig = FindAnyObjectByType<CameraRig>();
            if (rig == null)
            {
                var go = new GameObject("CameraRig");
                rig = go.AddComponent<CameraRig>();
            }
            rig.target = player.transform;

            // Chỉ gán một lần nếu camera đang bám thẳng vào player
            if (cam.Follow == null || cam.Follow == player.transform)
            {
                cam.Follow = rig.transform;
            }
        }

        /// <summary>
        /// Tạo hiệu ứng vệt sao 2 bên (SpeedLines) lúc Start nếu chưa có — idempotent.
        /// Thay thế props lề đã ẩn (2026-08-11): skybox vũ trụ rồi thì prop lơ lửng trông giả,
        /// còn hạt sao vụt ngang tạo cảm giác tốc độ (subway-surfers style).
        /// </summary>
        private void EnsureSpaceFX()
        {
            if (transform.Find("SpaceFX") != null) return;

            var go = new GameObject("SpaceFX");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpeedLines>();
        }

        public void StartRun()
        {
            if (player == null || tileSpawner == null)
            {
                Debug.LogError("GameManager thiếu tham chiếu player/tileSpawner.");
                return;
            }

            State = GameState.Playing;
            ObstacleManager obstacles = FindAnyObjectByType<ObstacleManager>();
            tileSpawner.Initialize(player.transform, obstacles);
            tileSpawner.StartTrack();
            enemy?.Setup(player.transform);
            GameEvents.RaiseGameStarted();
        }

        /// <summary>
        /// Chuyển trạng thái Paused (PauseManager điều khiển Time.timeScale + overlay UI).
        /// Chỉ cho phép Playing→Paused và Paused→Playing — không đụng Menu/GameOver.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused && State == GameState.Playing) State = GameState.Paused;
            else if (!paused && State == GameState.Paused) State = GameState.Playing;
        }

        /// <summary>
        /// Gắn PauseManager (ESC + nút II + overlay) — idempotent, chạy lại không nhân đôi.
        /// </summary>
        private void EnsurePause()
        {
            if (transform.Find("PauseManager") != null) return;
            var go = new GameObject("PauseManager");
            go.transform.SetParent(transform, false);
            go.AddComponent<PauseManager>();
        }

        public void Restart()
        {
            if (tileSpawner == null) return;
            State = GameState.Playing;

            // Fix 2026-08-11: reset player VỀ ĐIỂM BẮT ĐẦU CỐ ĐỊNH TRƯỚC (không phụ thuộc thứ tự
            // subscriber event) — trước đây chỉ dựa RaiseRestart, player vẫn đứng nguyên z=148
            // → mỗi lần chơi lại vị trí khác nhau + track dựng quanh vị trí cũ.
            if (player != null) player.ResetToStart();

            GameEvents.RaiseRestart();   // các hệ thống khác reset qua event
            tileSpawner.StartTrack();    // reset track — giờ đọc player.position = vị trí bắt đầu cố định
        }

        private void Update()
        {
            // G1: phím R restart (tạm — G2 thay bằng nút UI). 2026-08-12: guard Playing —
            // không restart khi đang Pause (tránh dựng track khi timeScale=0) hay GameOver/Menu.
            if (State != GameState.Playing) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        private void HandleGameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
        }
    }
}
