using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// "Hư Không" — kẻ thù đuổi theo player phía sau lưng.
    ///
    /// KHÔNG dùng NavMeshAgent nữa (fix 2026-08-11): track là VÔ TẬN (tile recycle),
    /// NavMesh bake chỉ phủ 1 vùng cố định quanh gốc → player chạy xa là NavMesh hết vùng,
    /// Void mất đường đi, đứng yên → tụt lại sau màn hình vĩnh viễn → người chơi không bao giờ thấy.
    ///
    /// Cách mới: Void bám theo player trực tiếp — luôn giữ sau lưng player một khoảng cách
    /// (startDistance → minDistance co dần theo thời gian = áp lực tăng), bám ngang theo lane
    /// player từ từ. Trigger vẫn nuốt player khi chạm → Game Over.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VoidChase : MonoBehaviour
    {
        [Header("Đuổi theo")]
        [SerializeField] private Transform player;

        [Header("Độ khó")]
        [SerializeField, Tooltip("Khoảng cách ban đầu sau lưng player (m) — <10 để camera (offset -10) nhìn thấy")]
        private float startDistance = 9f;
        [SerializeField, Tooltip("Khoảng cách tối thiểu cuối game — Void áp sát tới mức nuốt được player")]
        private float minDistance = 1.5f;
        [SerializeField] private float rampDuration = 60f;   // co dần trong 60s chơi
        [SerializeField, Tooltip("Khoảng cách dưới ngưỡng này = Void nuốt player (safety net)")]
        private float swallowDistance = 1.6f;
        [SerializeField, Tooltip("Tốc độ bám ngang theo lane của player (m/s)")]
        private float lateralFollow = 4f;

        [Header("Hình dạng")]
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float maxScale = 2.5f;

        private Vector3 _startPos;
        private float _runTime;

        public void Setup(Transform playerRef) => player = playerRef;

        private void Awake()
        {
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
        }

        private void Update()
        {
            if (player == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            _runTime += Time.deltaTime;
            float t = Mathf.Clamp01(_runTime / rampDuration);
            float distance = Mathf.Lerp(startDistance, minDistance, t);

            // Mục tiêu: sau lưng player đúng `distance` (trục Z = hướng chạy), y giữ nguyên
            Vector3 target = player.position - Vector3.forward * distance;

            // Bám ngang theo lane player từ từ (không teleport ngang)
            target.x = Mathf.MoveTowards(transform.position.x, player.position.x, lateralFollow * Time.deltaTime);

            transform.position = target;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, maxScale, t);

            // Safety net: khoảng cách thực tế dưới ngưỡng → Void nuốt player (cơ chế chết chắc chắn chạy,
            // kể cả khi collider chưa kịp overlap do player đổi lane)
            if (Mathf.Abs(transform.position.z - player.position.z) < swallowDistance)
            {
                GameEvents.RaiseGameOver();
            }
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
