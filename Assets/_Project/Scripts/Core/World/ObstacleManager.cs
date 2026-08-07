using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Data;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Gắn obstacle ngẫu nhiên lên tile khi tile được spawn.
    /// Luôn chừa ít nhất 1 lane an toàn để người chơi có đường né.
    /// </summary>
    public class ObstacleManager : MonoBehaviour
    {
        [Header("Cấu hình")]
        [SerializeField] private ObstacleData[] obstacleTypes;
        [SerializeField] private float spawnChance = 0.45f;
        [SerializeField] private int laneCount = 3;
        [SerializeField] private float laneWidth = 2f;

        public void TrySpawn(Tile tile)
        {
            if (obstacleTypes == null || obstacleTypes.Length == 0) return;
            if (Random.value > spawnChance) return;

            ObstacleData data = PickRandomType();
            if (data == null || data.prefab == null) return;

            // Chặn ngẫu nhiên 1..laneCount-1 lane → luôn còn ≥1 lane trống
            int blockedLanes = Random.Range(1, laneCount);
            HashSet<int> chosen = new HashSet<int>();
            while (chosen.Count < blockedLanes)
            {
                chosen.Add(Random.Range(0, laneCount));
            }

            foreach (int lane in chosen)
            {
                float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;
                SpawnOnTile(data, tile, x);
            }
        }

        private void SpawnOnTile(ObstacleData data, Tile tile, float x)
        {
            GameObject obstacle = Instantiate(data.prefab, tile.transform);
            obstacle.transform.localPosition = new Vector3(x, 0f, Random.Range(0f, tile.Length * 0.6f));
            if (obstacle.GetComponent<Obstacle>() == null)
            {
                obstacle.AddComponent<Obstacle>();
            }
        }

        private ObstacleData PickRandomType()
        {
            float total = 0f;
            foreach (ObstacleData d in obstacleTypes)
            {
                if (d != null) total += Mathf.Max(0f, d.spawnWeight);
            }
            if (total <= 0f)
            {
                return obstacleTypes[Random.Range(0, obstacleTypes.Length)];
            }

            float r = Random.value * total;
            foreach (ObstacleData d in obstacleTypes)
            {
                if (d == null) continue;
                r -= Mathf.Max(0f, d.spawnWeight);
                if (r <= 0f) return d;
            }
            return obstacleTypes[obstacleTypes.Length - 1];
        }
    }
}
