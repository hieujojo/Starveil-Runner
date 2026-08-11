using UnityEngine;
using VoidRunner.Systems.Difficulty;

namespace VoidRunner.Systems.VFX
{
    /// <summary>
    /// Vệt sao 2 bên đường (speed-lines) — thay thế "props lề" (đã ẩn 2026-08-11): khi đã có
    /// skybox vũ trụ thật, prop đứng lơ lửng 2 bên trông giả tạo; còn tốc độ thì cần hiệu ứng
    /// lao nhanh. Hạt sao sinh dọc 2 bên track (ngoài road ±9), bay ngược -Z với tốc độ theo
    /// DifficultyManager.CurrentSpeed + renderer Stretch → thành vệt sáng kéo dài lướt qua,
    /// cảm giác tốc độ tăng khi độ khó tăng (giống Subway Surfers / Star Wars hyperspace).
    ///
    /// Được gắn tự động bởi GameManager.EnsureSpaceFX() lúc Start (idempotent) — không cần
    /// kéo thả trong scene. Material mềm tái sử dụng VFXManager.CreateSoftParticleMaterial.
    /// </summary>
    public class SpeedLines : MonoBehaviour
    {
        [Header("Vị trí 2 bên")]
        [Tooltip("Khoảng cách 2 bên so với tâm track — road rộng 18 (±9) nên đặt ngoài road; 11.5 là mép rìa vẫn thấy rõ với FOV 68 (nửa bề ngang thấy được ~11.9).")]
        [SerializeField] private float sideOffset = 11.5f;

        [Tooltip("Bán chiều dài dải hạt phủ quanh player (theo trục Z).")]
        [SerializeField] private float bandHalfLength = 24f;

        [Header("Hạt")]
        [SerializeField] private float startSize = 0.06f;
        [SerializeField] private float startLifetime = 0.55f;
        [SerializeField] private float emissionRate = 90f;
        [SerializeField] private int maxParticles = 240;
        [Tooltip("Hệ số nhân tốc độ hạt so với tốc độ player (>1 = vệt lướt qua rõ hơn).")]
        [SerializeField] private float speedFactor = 1.15f;
        [SerializeField] private Color lineColor = new Color(0.7f, 0.85f, 1f, 0.55f);

        private Transform _player;
        private ParticleSystem _left;
        private ParticleSystem _right;

        private void Start()
        {
            if (_player == null)
            {
                var pc = FindAnyObjectByType<VoidRunner.Core.Player.PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            Material softMat = VFXManager.CreateSoftParticleMaterial();
            _left = BuildSide(-1, softMat);
            _right = BuildSide(1, softMat);
        }

        private ParticleSystem BuildSide(int side, Material mat)
        {
            var go = new GameObject(side < 0 ? "SpeedLinesL" : "SpeedLinesR");
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = startLifetime;
            main.startSpeed = -1f; // ghi đè mỗi frame theo difficulty
            main.startSize = startSize;
            main.startColor = lineColor;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = emissionRate;

            // Dải sinh dọc theo track — hạt xuất hiện quanh player, bay ngược -Z thành vệt
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.1f, 0.1f, bandHalfLength * 2f);

            // Stretch theo vận tốc → vệt sáng kéo dài (speed-line)
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.14f;
            renderer.lengthScale = 1.1f;
            renderer.material = mat;

            return ps;
        }

        private void Update()
        {
            // Re-fetch nếu Start miss (thứ tự scene edge case) — giá rẻ, chỉ chạy khi null
            if (_player == null)
            {
                var pc = FindAnyObjectByType<VoidRunner.Core.Player.PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            if (_player == null || _left == null || _right == null) return;

            // Tốc độ hạt = tốc độ player (theo difficulty) — chậm đầu game, vụt nhanh khi tăng độ khó
            float speed = 14f;
            if (DifficultyManager.Instance != null)
            {
                speed = DifficultyManager.Instance.CurrentSpeed;
            }

            float z = _player.position.z;
            float y = _player.position.y + 0.5f; // ngang tầm tàu, trên mặt road

            PositionAndSpeed(_left, -sideOffset, y, z, speed);
            PositionAndSpeed(_right, sideOffset, y, z, speed);
        }

        private void PositionAndSpeed(ParticleSystem ps, float x, float y, float z, float speed)
        {
            Vector3 pos = ps.transform.position;
            pos.x = x;
            pos.y = y;
            pos.z = z;
            ps.transform.position = pos;

            var main = ps.main;
            main.startSpeed = -speed * speedFactor;
        }
    }
}
