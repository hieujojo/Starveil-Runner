using UnityEngine;
using VoidRunner.Core.Interfaces;

namespace VoidRunner.Core.World.Strategies
{
    /// <summary>
    /// STRATEGY PATTERN — Hành vi Patroller (PatrollerDrone).
    /// Tách từ PatrollerDrone.cs: logic lắc ngang giữa các lane.
    ///
    /// Drone bám trước player ở aheadDistance, lắc qua lại giữa
    /// patrolMinLane ↔ patrolMaxLane theo hàm cos (mượt, predictable).
    /// </summary>
    public class PatrollerStrategy : IEnemyStrategy
    {
        public string StrategyName => "Patroller";

        [Header("Tuần tra")]
        private int _patrolMinLane = 0;
        private int _patrolMaxLane = 2;
        private float _patrolPeriod = 3.2f;
        private float _aheadDistance = 16f;

        [Header("Track")]
        private int _laneCount = 3;
        private float _laneWidth = 2f;

        // State
        private float _phase;
        private Vector3 _startPos;

        public void Setup(Transform player)
        {
            _phase = Random.value * Mathf.PI * 2f;
        }

        public void ResetState()
        {
            _phase = Random.value * Mathf.PI * 2f;
        }

        /// <summary>Update movement — gọi từ PatrollerDrone.Update().</summary>
        public void Execute(Transform self, Transform player, float deltaTime)
        {
            if (player == null) return;

            // Lưu start pos lần đầu
            if (_startPos == Vector3.zero)
                _startPos = self.position;

            // Bám trước mặt player (trục Z)
            Vector3 pos = self.position;
            pos.z = player.position.z + _aheadDistance;

            // Lắc ngang: cos giữa lane min/max
            float t = (Mathf.Cos(Time.time * (2f * Mathf.PI / _patrolPeriod) + _phase) + 1f) * 0.5f;
            float minX = (_patrolMinLane - (_laneCount - 1) * 0.5f) * _laneWidth;
            float maxX = (_patrolMaxLane - (_laneCount - 1) * 0.5f) * _laneWidth;
            pos.x = Mathf.Lerp(minX, maxX, t);

            self.position = pos;
        }

        /// <summary>Reset về vị trí ban đầu.</summary>
        public void ResetPosition(Transform self)
        {
            self.position = _startPos;
        }

        public void SetStartPos(Vector3 pos) => _startPos = pos;
    }
}
