using UnityEngine;
using VoidRunner.Core.Interfaces;
using VoidRunner.Data;

namespace VoidRunner.Core.Factories
{
    /// <summary>
    /// FACTORY PATTERN — Tạo enemy theo type.
    /// ObstacleManager/TileSpawner gọi factory.Create(type) thay vì hardcode Instantiate.
    /// Thêm enemy mới = thêm enum + case trong switch, KHÔNG sửa caller.
    /// </summary>
    public class EnemyFactory : IEnemyFactory
    {
        private readonly ObstacleData[] _obstacleTypes;

        public EnemyType[] SupportedTypes => new[] { EnemyType.Chase, EnemyType.Patroller };

        public EnemyFactory(ObstacleData[] obstacleTypes)
        {
            _obstacleTypes = obstacleTypes;
        }

        public GameObject Create(EnemyType type, Transform parent)
        {
            switch (type)
            {
                case EnemyType.Chase:
                    return CreateChaseEnemy(parent);
                case EnemyType.Patroller:
                    return CreatePatrollerEnemy(parent);
                default:
                    Debug.LogWarning($"[EnemyFactory] Unknown enemy type: {type}");
                    return null;
            }
        }

        private GameObject CreateChaseEnemy(Transform parent)
        {
            // Flying Beetle — load từ catalog hoặc prefab đầu tiên trong obstacleTypes
            GameObject prefab = null;
            if (_obstacleTypes != null && _obstacleTypes.Length > 0 && _obstacleTypes[0] != null)
                prefab = _obstacleTypes[0].prefab;

            if (prefab == null)
            {
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Flying Beetle/prefab/Flying beetle.prefab");
#endif
            }

            if (prefab == null) return null;

            return Object.Instantiate(prefab, parent);
        }

        private GameObject CreatePatrollerEnemy(Transform parent)
        {
            // PatrollerDrone — load từ catalog
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Obstacles/PatrollerDrone.prefab");
#endif

            if (prefab == null) return null;

            return Object.Instantiate(prefab, parent);
        }
    }
}
