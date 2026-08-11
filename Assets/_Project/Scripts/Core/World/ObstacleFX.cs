using UnityEngine;
using VoidRunner.Systems.VFX;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Hiệu ứng ambient cho obstacle — drone Robot_Guardian (v3f.5, user: "chỉ drone + vài hiệu ứng
    /// là đủ cho trò chơi vũ trụ"): đèn cảnh báo đỏ + hạt năng lượng cam + lơ lửng/xoay chậm
    /// (cảm giác drone bay tuần tra, không đứng yên như tượng).
    ///
    /// ⚠️ TẠO RUNTIME (gắn trong Obstacle.Awake) — KHÔNG nướng vào prefab: mọi material/hiệu ứng
    /// tạo bằng code lúc EDIT-TIME mà được SaveAsPrefabAsset tham chiếu sẽ ghi objectReference
    /// {fileID: 0} → null → màu TÍM (R3.1). Runtime = không serialize = an toàn.
    /// </summary>
    public class ObstacleFX : MonoBehaviour
    {
        private Transform _model;
        private Vector3 _baseLocal;
        private float _phase;

        private void Awake()
        {
            _model = transform.Find("Model"); // con do SciFiObstacleSetupTool đặt tên "Model"
            _phase = Random.value * Mathf.PI * 2f;

            EnsureWarningLight();
            EnsureEnergyParticles();

            if (_model != null) _baseLocal = _model.localPosition;
        }

        private void EnsureWarningLight()
        {
            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.25f, 0.1f, 1f); // đỏ cảnh báo — nổi trên nền tối
            light.intensity = 1.6f; // FIX v3f.5 (reviewer): 2→1.6 — nhiều drone đồng thời → giảm chói (user từng phàn nàn ánh sáng quá chói)
            light.range = 5f;       // 6→5 — phạm vi nhỏ, bớt chồng sáng giữa các drone
            light.shadows = LightShadows.None; // nhẹ — không tốn shadow map
        }

        private void EnsureEnergyParticles()
        {
            var go = new GameObject("EnergyFX");
            go.transform.SetParent(_model != null ? _model : transform, false);
            go.transform.localPosition = Vector3.up * 0.6f; // quanh thân drone

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 0.4f;
            main.startSize = 0.12f;
            main.startColor = new Color(1f, 0.55f, 0.15f, 0.9f); // cam năng lượng
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            // Tái sử dụng material mềm của VFXManager (không duplicate — bài học code reuse)
            renderer.material = VFXManager.CreateSoftParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private void Update()
        {
            if (_model == null) return;

            // Drone lơ lửng (bob nhẹ) — Robot_Guardian không Animator nên an toàn xoay/xê dịch
            float t = Time.time;
            _model.localPosition = new Vector3(
                _baseLocal.x,
                _baseLocal.y + Mathf.Sin(t * 2f + _phase) * 0.08f,
                _baseLocal.z);

            // Xoay chậm quanh Y — cảm giác tuần tra
            _model.localRotation *= Quaternion.Euler(0f, 25f * Time.deltaTime, 0f);
        }
    }
}
