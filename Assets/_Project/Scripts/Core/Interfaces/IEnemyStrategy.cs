using UnityEngine;

namespace VoidRunner.Core.Interfaces
{
    /// <summary>
    /// STRATEGY PATTERN — Interface cho hành vi enemy.
    /// Mỗi loại enemy implement strategy riêng: ChaseStrategy (Flying Beetle),
    /// PatrollerStrategy (PatrollerDrone). Thêm enemy mới = thêm class implement,
    /// KHÔNG sửa code cũ (Open/Closed Principle).
    ///
    /// Dùng trong: EnemyFactory.Create(), EnemyBase.SetStrategy().
    /// </summary>
    public interface IEnemyStrategy
    {
        /// <summary>Tên strategy (debug/log).</summary>
        string StrategyName { get; }

        /// <summary>Gọi mỗi frame khi game đang Playing.</summary>
        void Execute(Transform self, Transform player, float deltaTime);

        /// <summary>Reset về trạng thái ban đầu (khi restart game).</summary>
        void ResetState();

        /// <summary>Setup ban đầu (gán player reference).</summary>
        void Setup(Transform player);
    }
}
