using UnityEngine;
using UnityEngine.InputSystem;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;
using VoidRunner.Systems.Input;

namespace VoidRunner.Core
{
    public enum GameState { Menu, Playing, GameOver }

    /// <summary>
    /// State machine và orchestrator của game:
    /// khởi tạo run (track, void), xử lý Game Over / Restart, nối input → player.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Tham chiếu (kéo thả trong Inspector)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private TileSpawner tileSpawner;
        [SerializeField] private VoidChase voidChase;
        [SerializeField] private InputReader input;

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
            WireInput();
            StartRun();
        }

        private void ResolveReferences()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (tileSpawner == null) tileSpawner = FindAnyObjectByType<TileSpawner>();
            if (voidChase == null) voidChase = FindAnyObjectByType<VoidChase>();
            if (input == null) input = FindAnyObjectByType<InputReader>();
        }

        private void WireInput()
        {
            if (input == null || player == null) return;
            input.LaneLeft += player.MoveLeft;
            input.LaneRight += player.MoveRight;
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
            voidChase?.Setup(player.transform);
            GameEvents.RaiseGameStarted();
        }

        public void Restart()
        {
            if (tileSpawner == null) return;
            State = GameState.Playing;
            GameEvents.RaiseRestart();   // player + void tự reset qua event
            tileSpawner.StartTrack();    // reset track
        }

        private void Update()
        {
            // G1: phím R restart (tạm — G2 thay bằng nút UI)
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        private void HandleGameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            Debug.Log("Game Over — nhấn R để chơi lại.");
        }
    }
}
