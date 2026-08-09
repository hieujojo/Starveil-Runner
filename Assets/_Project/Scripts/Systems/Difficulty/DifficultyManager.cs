using System;
using UnityEngine;
using VoidRunner.Core;

namespace VoidRunner.Systems.Difficulty
{
    /// <summary>
    /// Nguồn sự thật về độ khó: tốc độ player + mật độ obstacle tăng dần theo thời gian chơi.
    /// Dùng AnimationCurve (chỉnh trong Inspector) để kiểm soát nhịp tăng — có giới hạn tối đa (fair play).
    /// Event-driven: phát <see cref="OnDifficultyChanged"/> — Player/ObstacleManager subscribe, không coupling trực tiếp.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Ramp theo thời gian chơi")]
        [SerializeField, Tooltip("t = 0..1 (thời gian/rampDuration); value = mức độ khó 0..1")]
        private AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Tooltip("Số giây để đạt độ khó tối đa")]
        private float rampDuration = 60f;

        [Header("Tốc độ player")]
        [SerializeField, Tooltip("Tốc độ khởi đầu (nên khớp forwardSpeed của PlayerController)")]
        private float startSpeed = 10f;
        [SerializeField, Tooltip("Giới hạn tốc độ tối đa — fair play, tránh quá khó")]
        private float maxSpeed = 20f;

        [Header("Mật độ obstacle")]
        [SerializeField, Tooltip("Xác suất obstacle/tile lúc khởi đầu")]
        private float startSpawnChance = 0.45f;
        [SerializeField, Tooltip("Xác suất tối đa — vẫn luôn chừa ≥1 lane an toàn nhờ ObstacleManager")]
        private float maxSpawnChance = 0.75f;

        /// <summary>Thay đổi khi độ khó cập nhật (mỗi frame khi đang chơi).</summary>
        public static event Action<float, float> OnDifficultyChanged; // (speed, spawnChance)

        private float _runTime;

        public float CurrentSpeed { get; private set; }
        public float CurrentSpawnChance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Khởi tạo sớm trong Awake — GameManager.Start chạy StartTrack (spawn tile đầu tiên)
            // trong cùng pha Start, nên giá trị phải sẵn sàng trước khi tile đầu tiên hỏi.
            CurrentSpeed = startSpeed;
            CurrentSpawnChance = startSpawnChance;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetRamp;
            GameEvents.OnRestart += ResetRamp;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetRamp;
            GameEvents.OnRestart -= ResetRamp;
            if (Instance == this) Instance = null;
        }

        private void ResetRamp()
        {
            _runTime = 0f;
            // Reset ngay giá trị + báo consumer — tránh 1 frame dùng mật độ/tốc độ cũ (cao nhất) sau Restart
            CurrentSpeed = startSpeed;
            CurrentSpawnChance = startSpawnChance;
            OnDifficultyChanged?.Invoke(CurrentSpeed, CurrentSpawnChance);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            _runTime += Time.deltaTime;
            float t = Mathf.Clamp01(_runTime / rampDuration);
            float level = difficultyCurve.Evaluate(t);

            float speed = Mathf.Lerp(startSpeed, maxSpeed, level);
            float chance = Mathf.Lerp(startSpawnChance, maxSpawnChance, level);

            // Chỉ phát event khi giá trị thực sự đổi (tránh spam)
            if (Math.Abs(speed - CurrentSpeed) > 0.001f || Math.Abs(chance - CurrentSpawnChance) > 0.001f)
            {
                CurrentSpeed = speed;
                CurrentSpawnChance = chance;
                OnDifficultyChanged?.Invoke(CurrentSpeed, CurrentSpawnChance);
            }
        }
    }
}
