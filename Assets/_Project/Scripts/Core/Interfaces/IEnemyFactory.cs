using UnityEngine;

namespace VoidRunner.Core.Interfaces
{
    /// <summary>
    /// FACTORY PATTERN — Interface cho việc tạo enemy.
    /// Thêm enemy mới = thêm class implement + đăng ký trong Factory.
    /// TileSpawner không cần biết cách tạo enemy, chỉ gọi factory.Create().
    /// </summary>
    public interface IEnemyFactory
    {
        /// <summary>Tạo enemy với strategy tương ứng.</summary>
        GameObject Create(EnemyType type, Transform parent);

        /// <summary>Các loại enemy hỗ trợ.</summary>
        EnemyType[] SupportedTypes { get; }
    }

    /// <summary>Các loại enemy trong game.</summary>
    public enum EnemyType
    {
        Chase,      // Flying Beetle — đuổi theo player
        Patroller   // PatrollerDrone — lắc ngang lane
    }
}
