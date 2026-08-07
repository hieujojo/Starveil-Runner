using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.World;

namespace VoidRunner.Core.Player
{
    /// <summary>
    /// Bóng tự lăn về trước (forwardSpeed) và chuyển lane trái/phải.
    /// Điều khiển hoàn toàn qua Rigidbody velocity — mượt, không teleport.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Di chuyển")]
        [SerializeField] private float forwardSpeed = 10f;
        [SerializeField] private float laneWidth = 2f;
        [SerializeField] private float laneChangeSpeed = 8f;
        [SerializeField] private int laneCount = 3;

        private Rigidbody _rb;
        private Vector3 _startPos;
        private int _currentLane;
        private float _targetX;
        private bool _isDead;

        public float ForwardSpeed => forwardSpeed;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _startPos = transform.position;
            _currentLane = laneCount / 2;
            _targetX = 0f;
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnRestart += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnRestart -= HandleRestart;
        }

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

            _rb.linearVelocity = new Vector3(stepX / Time.fixedDeltaTime, _rb.linearVelocity.y, forwardSpeed);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;
            if (other.TryGetComponent<Obstacle>(out _))
            {
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
            _rb.linearVelocity = Vector3.zero;
            _rb.position = _startPos;
        }
    }
}
