using System;
using UnityEngine;
using VoidRunner.Systems.Difficulty;

namespace VoidRunner.Systems.VFX
{
    /// <summary>
    /// Task B (2026-08-11): càng chơi sâu càng "đi vào hư không" — skybox đổi dần qua 4 nebula
    /// (Nebula_01 → 04) theo mức độ khó. Lắng nghe DifficultyManager.OnDifficultyChanged
    /// (speed, spawnChance): level = (speed - start)/(max - start) → chọn material nebula theo
    /// ngưỡng. Event-driven, không poll. Nếu chưa gán 4 material (tool chưa chạy) → vô hiệu im lặng.
    /// </summary>
    public class NebulaChanger : MonoBehaviour
    {
        [Header("4 nebula theo độ khó (tool Setup Nebula tự gán)")]
        [Tooltip("Nebula[0] = nhẹ nhất (đầu game) → Nebula[3] = đậm nhất (khó nhất).")]
        [SerializeField] private Material[] nebulaMaterials = new Material[4];

        [Header("Ngưỡng đổi theo difficulty")]
        [Tooltip("level = (CurrentSpeed - startSpeed)/(maxSpeed - startSpeed), 0..1")]
        [SerializeField] private float startSpeed = 10f;
        [SerializeField] private float maxSpeed = 20f;

        private int _currentIndex = -1;

        private void OnEnable()
        {
            DifficultyManager.OnDifficultyChanged += HandleDifficultyChanged;
            // Góp ý reviewer: áp ngay skybox bậc 1 khi Start nếu bỏ lỡ event đầu
            // (OnDifficultyChanged chỉ fire khi giá trị thay đổi >0.001 — đầu game có thể chưa kịp)
            if (DifficultyManager.Instance != null)
            {
                HandleDifficultyChanged(DifficultyManager.Instance.CurrentSpeed, DifficultyManager.Instance.CurrentSpawnChance);
            }
        }

        private void OnDisable()
        {
            DifficultyManager.OnDifficultyChanged -= HandleDifficultyChanged;
        }

        private void HandleDifficultyChanged(float speed, float spawnChance)
        {
            if (nebulaMaterials == null || nebulaMaterials.Length == 0) return;

            float range = Mathf.Max(0.001f, maxSpeed - startSpeed);
            float level = Mathf.Clamp01((speed - startSpeed) / range);

            // 4 mức: 0-0.25 → nebula[0], 0.25-0.5 → [1], ...
            int index = Mathf.Clamp(Mathf.FloorToInt(level * nebulaMaterials.Length), 0, nebulaMaterials.Length - 1);
            if (index == _currentIndex) return;

            var mat = nebulaMaterials[index];
            if (mat == null) return;

            _currentIndex = index;
            RenderSettings.skybox = mat;
        }
    }
}
