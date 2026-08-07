using UnityEngine;
using UnityEngine.AI;
using VoidRunner.Core;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// "Hư Không" — AI đuổi theo player bằng NavMesh.
    /// Tốc độ và kích thước tăng dần theo thời gian chơi (Lerp tuyến tính).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Collider))]
    public class VoidChase : MonoBehaviour
    {
        [Header("Đuổi theo")]
        [SerializeField] private Transform player;

        [Header("Độ khó")]
        [SerializeField] private float startSpeedRatio = 0.85f;   // khởi đầu chậm hơn player
        [SerializeField] private float maxSpeedRatio = 1.05f;     // cuối game nhanh hơn player
        [SerializeField] private float rampDuration = 60f;        // thời gian tăng từ start → max (giây)

        [Header("Hình dạng")]
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float maxScale = 2.5f;

        private NavMeshAgent _agent;
        private Vector3 _startPos;
        private float _runTime;

        public void Setup(Transform playerRef) => player = playerRef;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _startPos = transform.position;

            // Collider là trigger — player đi vào là bị nuốt (không đẩy vật lý)
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleGameStarted;
            GameEvents.OnRestart += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleGameStarted;
            GameEvents.OnRestart -= HandleRestart;
        }

        private void HandleGameStarted() => _runTime = 0f;

        private void HandleRestart()
        {
            _runTime = 0f;
            transform.position = _startPos;
            transform.localScale = Vector3.one * startScale;
            if (_agent != null) _agent.ResetPath();
        }

        private void Update()
        {
            if (player == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            _runTime += Time.deltaTime;
            float t = Mathf.Clamp01(_runTime / rampDuration);

            float playerSpeed = 10f;
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) playerSpeed = pc.ForwardSpeed;

            _agent.speed = Mathf.Lerp(startSpeedRatio, maxSpeedRatio, t) * playerSpeed;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, maxScale, t);
            _agent.SetDestination(player.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                GameEvents.RaiseGameOver();
            }
        }
    }
}
