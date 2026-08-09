using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.World;
using VoidRunner.Systems.Difficulty;
using VoidRunner.Systems.PowerUp;

namespace VoidRunner.Core.Player
{
    /// <summary>
    /// Bóng tự lăn về trước (forwardSpeed) và chuyển lane trái/phải.
    /// Điều khiển hoàn toàn qua Rigidbody velocity — mượt, không teleport.
    /// Tốc độ chạy do DifficultyManager điều khiển (event-driven) — giữ forwardSpeed làm tốc độ nền.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Di chuyển")]
        [SerializeField, Tooltip("Tốc độ nền — DifficultyManager có thể tăng dần lên tới maxSpeed")]
        private float forwardSpeed = 10f;
        [SerializeField] private float laneWidth = 2f;
        [SerializeField] private float laneChangeSpeed = 8f;
        [SerializeField] private int laneCount = 3;

        private Rigidbody _rb;
        private Vector3 _startPos;
        private int _currentLane;
        private float _targetX;
        private bool _isDead;
        private float _currentSpeed;

        public float ForwardSpeed => _currentSpeed;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _startPos = transform.position;
            _currentLane = laneCount / 2;
            _targetX = 0f;
            _currentSpeed = forwardSpeed;
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnRestart += HandleRestart;
            DifficultyManager.OnDifficultyChanged += HandleDifficultyChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnRestart -= HandleRestart;
            DifficultyManager.OnDifficultyChanged -= HandleDifficultyChanged;
        }

        /// <summary>Nhận tốc độ mới từ DifficultyManager (khi game đang chơi).</summary>
        private void HandleDifficultyChanged(float speed, float _) => _currentSpeed = speed;

        public void MoveLeft() => MoveToLane(_currentLane - 1);
        public void MoveRight() => MoveToLane(_currentLane + 1);

        private void MoveToLane(int lane)
        {
            if (_isDead) return;
            _currentLane = Mathf.Clamp(lane, 0, laneCount - 1);
            _targetX = (_currentLane - (laneCount - 1) * 0.5f) * laneWidth;
            GameEvents.RaiseLaneChanged(_currentLane);
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            // Tiến về trước + lerp ngang mượt giữa các lane (không overshoot)
            float dx = _targetX - _rb.position.x;
            float maxStep = laneChangeSpeed * Time.fixedDeltaTime;
            float stepX = Mathf.Clamp(dx, -maxStep, maxStep);

            _rb.linearVelocity = new Vector3(stepX / Time.fixedDeltaTime, _rb.linearVelocity.y, _currentSpeed);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;
            if (other.TryGetComponent<Obstacle>(out _))
            {
                // Shield: miễn nhiễm va chạm 1 lần (PowerUpSystem tiêu hao qua timer, không ăn ngay)
                if (PowerUpSystem.Instance != null && PowerUpSystem.Instance.IsShieldActive)
                {
                    return;
                }
                Die();
            }
        }

        private void Die()
        {
            _isDead = true;
            _rb.linearVelocity = Vector3.zero;
            GameEvents.RaiseObstacleHit();
            GameEvents.RaiseGameOver();
        }

        private void HandleGameOver()
        {
            _isDead = true;
            _rb.linearVelocity = Vector3.zero;
        }

        private void HandleRestart()
        {
            _isDead = false;
            _currentLane = laneCount / 2;
            _targetX = 0f;
            _currentSpeed = forwardSpeed; // DifficultyManager sẽ gửi lại tốc độ mới qua event
            _rb.linearVelocity = Vector3.zero;
            _rb.position = _startPos;
        }
    }
}
