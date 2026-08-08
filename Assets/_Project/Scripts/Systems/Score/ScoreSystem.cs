using System;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;

namespace VoidRunner.Systems.Score
{
    /// <summary>
    /// Điểm số: khoảng cách chạy (deltaZ × multiplier) + coin.
    /// Combo multiplier tăng dần khi sống liên tục không va chạm, reset khi dính obstacle.
    /// Event-driven — UI chỉ cần subscribe OnScoreChanged / OnComboChanged, không coupling trực tiếp.
    /// </summary>
    public class ScoreSystem : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [Tooltip("Dùng vị trí player để đo khoảng cách đã chạy (auto-resolve nếu để trống).")]
        [SerializeField] private Transform player;

        [Header("Cấu hình")]
        [Tooltip("Điểm mỗi coin nhặt được (nhân với multiplier hiện tại).")]
        [SerializeField] private int coinScore = 10;

        [Tooltip("Số giây sống liên tục để tăng 1 bậc combo.")]
        [SerializeField] private float comboInterval = 5f;

        [Tooltip("Multiplier tối đa.")]
        [SerializeField] private int maxCombo = 5;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnComboChanged;

        public int Score { get; private set; }
        public int Multiplier { get; private set; } = 1;

        private Vector3 _lastPos;
        private float _comboTimer;
        private bool _active;

        private void Awake()
        {
            if (player == null)
            {
                PlayerController controller = FindAnyObjectByType<PlayerController>();
                if (controller != null) player = controller.transform;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnCoinCollected += HandleCoinCollected;
            GameEvents.OnObstacleHit += HandleObstacleHit;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnRestart += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnCoinCollected -= HandleCoinCollected;
            GameEvents.OnObstacleHit -= HandleObstacleHit;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnRestart -= HandleRestart;
        }

        private void Start()
        {
            ResetRun();
        }

        private void Update()
        {
            if (!_active || player == null) return;

            // Score theo khoảng cách — đo deltaZ thực tế, độc lập với tốc độ tăng dần
            float deltaZ = player.position.z - _lastPos.z;
            _lastPos = player.position;
            if (deltaZ > 0f)
            {
                AddScore(Mathf.RoundToInt(deltaZ * 10f));
            }

            // Combo: sống liên tục → tăng multiplier theo chu kỳ
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f && Multiplier < maxCombo)
            {
                SetMultiplier(Multiplier + 1);
                _comboTimer = comboInterval;
            }
        }

        /// <summary>Cộng điểm (raw) đã nhân multiplier. AI/UI không gọi trực tiếp — dùng event.</summary>
        public void AddScore(int raw)
        {
            if (raw <= 0) return;
            Score += raw * Multiplier;
            OnScoreChanged?.Invoke(Score);
        }

        private void SetMultiplier(int value)
        {
            Multiplier = Mathf.Clamp(value, 1, maxCombo);
            OnComboChanged?.Invoke(Multiplier);
        }

        private void ResetCombo()
        {
            SetMultiplier(1);
            _comboTimer = comboInterval;
        }

        private void ResetRun()
        {
            Score = 0;
            Multiplier = 1;
            _comboTimer = comboInterval;
            _active = true;
            if (player != null) _lastPos = player.position;
            OnScoreChanged?.Invoke(Score);
            OnComboChanged?.Invoke(Multiplier);
        }

        private void HandleCoinCollected(int coins) => AddScore(coinScore);

        private void HandleObstacleHit() => ResetCombo();

        private void HandleGameOver() => _active = false;

        private void HandleRestart() => ResetRun();
    }
}
