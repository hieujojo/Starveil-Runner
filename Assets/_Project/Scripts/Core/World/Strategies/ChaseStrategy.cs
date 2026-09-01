using UnityEngine;
using VoidRunner.Core.Interfaces;

namespace VoidRunner.Core.World.Strategies
{
    /// <summary>
    /// STRATEGY PATTERN — Hành vi Chase (Flying Beetle).
    /// Tách từ EnemyChase.cs: logic đuổi theo player 2 nấc cố định.
    ///
    /// Nấc 0: giữ baseDistance sau lưng player.
    /// Nấc 1: tiến sát closeDistance + mở cửa sổ relaxWindow.
    /// Đụng lần 2 trong cửa sổ → Game Over.
    /// </summary>
    public class ChaseStrategy : IEnemyStrategy
    {
        public string StrategyName => "Chase";

        [Header("Khoảng cách")]
        private float _baseDistance = 7f;
        private float _closeDistance = 5.5f;
        private float _swallowDistance = 1.6f;
        private float _relaxWindow = 12f;
        private float _distanceLerpSpeed = 3f;
        private float _lateralFollow = 20f;

        [Header("Hình dạng")]
        private float _baseScale = 1f;
        private float _closeScale = 1.2f;

        // State
        private int _stage;
        private float _currentDistance;
        private float _relaxTimer;
        private bool _catching;

        public bool IsCatching => _catching;
        public int Stage => _stage;

        /// <summary>Configure từ EnemyChase fields (để test có thể set giá trị).</summary>
        public void Configure(float baseDist, float closeDist, float relaxWin, float lerpSpeed, float lateral)
        {
            _baseDistance = baseDist;
            _closeDistance = closeDist;
            _relaxWindow = relaxWin;
            _distanceLerpSpeed = lerpSpeed;
            _lateralFollow = lateral;
        }

        public void Setup(Transform player)
        {
            _currentDistance = _baseDistance;
        }

        public void ResetState()
        {
            _stage = 0;
            _relaxTimer = 0f;
            _currentDistance = _baseDistance;
            _catching = false;
        }

        /// <summary>Enemy tiến sát khi player đụng obstacle lần 1.</summary>
        public void OnObstacleHit()
        {
            if (_catching) return;

            if (_stage == 0)
            {
                _stage = 1;
                _relaxTimer = _relaxWindow;
            }
            else
            {
                _catching = true; // trigger catch sequence trong MonoBehaviour
            }
        }

        /// <summary>Update movement — gọi từ EnemyChase.LateUpdate().</summary>
        public void Execute(Transform self, Transform player, float deltaTime)
        {
            if (player == null) return;

            // Nấc 1: đếm ngược cửa sổ né sạch
            if (_stage == 1)
            {
                _relaxTimer -= deltaTime;
                if (_relaxTimer <= 0f)
                {
                    _stage = 0;
                }
            }

            float targetDistance = _stage == 1 ? _closeDistance : _baseDistance;
            _currentDistance = Mathf.MoveTowards(_currentDistance, targetDistance, _distanceLerpSpeed * deltaTime);

            // Mục tiêu: sau lưng player
            Vector3 target = player.position - Vector3.forward * _currentDistance;

            // Bám ngang theo lane
            target.x = Mathf.MoveTowards(self.position.x, player.position.x, _lateralFollow * deltaTime);

            self.position = target;

            // Phình to khi áp sát
            float closeness = Mathf.InverseLerp(_baseDistance, _closeDistance, _currentDistance);
            self.localScale = Vector3.one * Mathf.Lerp(_baseScale, _closeScale, closeness);

            // Safety net: nuốt player
            if (Mathf.Abs(self.position.z - player.position.z) < _swallowDistance)
            {
                GameEvents.RaiseGameOver();
            }
        }
    }
}
