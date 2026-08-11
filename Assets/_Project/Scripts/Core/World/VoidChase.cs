using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Systems.VFX;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// "Hư Không" (The Void) — kẻ đuổi theo player theo cơ chế 2 NẤC CỐ ĐỊNH
    /// (kiểu Subway Surfers / Temple Run — R0.4):
    ///
    ///   NẤC 0 (nền):  giữ baseDistance (9m) sau lưng player — chỉ bám theo lane,
    ///                 KHÔNG tự tăng tốc theo thời gian.
    ///   Đụng vật cản lần 1 → NẤC 1: Void tiến sát còn closeDistance (5m) + mở cửa sổ
    ///                 relaxWindow (12s). Void phình to hơn (đe dọa).
    ///   Né sạch 12s   → Void nới dần về baseDistance (reset NẤC 0).
    ///   Đụng lần 2 TRONG cửa sổ → Void nuốt player → Game Over.
    ///
    /// KHÔNG dùng NavMeshAgent (fix 2026-08-11): track là VÔ TẬN (tile recycle),
    /// NavMesh bake chỉ phủ 1 vùng cố định → player chạy xa là Void đứng yên,
    /// không bao giờ thấy. Void bám player trực tiếp theo vị trí.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VoidChase : MonoBehaviour
    {
        [Header("Khoảng cách")]
        [SerializeField] private Transform player;

        [SerializeField, Tooltip("Khoảng cách nền (nấc 0) sau lưng player — trong tầm camera offset -10")]
        private float baseDistance = 9f;

        [SerializeField, Tooltip("Khoảng cách khi đụng vật cản lần 1 (nấc 1) — áp sát nhưng chưa chết")]
        private float closeDistance = 5f;

        [SerializeField, Tooltip("Dưới ngưỡng này (khoảng cách z thật) = Void nuốt player — safety net")]
        private float swallowDistance = 1.6f;

        [SerializeField, Tooltip("Cửa sổ né sạch (giây) để Void nới lại về nấc 0 — 10–15s")]
        private float relaxWindow = 12f;

        [SerializeField, Tooltip("Tốc độ co/nới khoảng cách (m/s) — Void trượt mượt giữa 2 nấc")]
        private float distanceLerpSpeed = 3f;

        [SerializeField, Tooltip("Tốc độ bám ngang theo lane của player (m/s)")]
        private float lateralFollow = 4f;

        [Header("Hình dạng")]
        [SerializeField, Tooltip("Scale ở nấc 0 (nền)")]
        private float baseScale = 1f;

        [SerializeField, Tooltip("Scale ở nấc 1 (áp sát) — Void phình to đe dọa")]
        private float closeScale = 1.6f;

        private Vector3 _startPos;
        private int _stage;                 // 0 = nền, 1 = áp sát (đã đụng 1 lần)
        private float _currentDistance;
        private float _relaxTimer;

        // Visual hố đen (build bằng code — thay quả banh tím cũ): lõi đen + đĩa bồi tụ quay + hạt bị hút
        private Transform _disk;

        public void Setup(Transform playerRef) => player = playerRef;

        private void Awake()
        {
            _startPos = transform.position;
            _currentDistance = baseDistance;

            // Collider là trigger — player đi vào là bị nuốt (không đẩy vật lý)
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // R0.2: Void = HỐ ĐEN thực thụ (không phải banh tím) — dựng visual bằng code, idempotent
            BuildBlackHoleVisual();
        }

        /// <summary>
        /// Dựng visual hố đen: lõi đen (nuốt ánh sáng) + đĩa bồi tụ phát sáng tím quay + hạt bị hút vào tâm.
        /// Ẩn mesh renderer quả banh tím cũ trên root. Idempotent — chạy lại không nhân đôi.
        /// </summary>
        private void BuildBlackHoleVisual()
        {
            Transform existing = transform.Find("BlackHole");
            if (existing != null)
            {
                _disk = existing.Find("AccretionDisk");
                return;
            }

            // Ẩn quả banh tím cũ (mesh renderer gốc) — hố đen thay thế hình ảnh
            MeshRenderer rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            var bh = new GameObject("BlackHole");
            bh.transform.SetParent(transform, false);

            // Lõi đen — sphere đen tuyệt đối (nuốt ánh sáng), nhỏ hơn collider trigger
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(bh.transform, false);
            core.transform.localScale = Vector3.one * 0.45f;
            Collider coreCol = core.GetComponent<Collider>();
            if (coreCol != null) Destroy(coreCol);
            MeshRenderer coreMr = core.GetComponent<MeshRenderer>();
            coreMr.sharedMaterial = CreateBlackHoleMaterial();
            coreMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreMr.receiveShadows = false;

            // Đĩa bồi tụ — cylinder dẹt phát sáng tím neon, nghiêng nhẹ, quay chậm (xem như vành sáng hút vật chất)
            GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = "AccretionDisk";
            disk.transform.SetParent(bh.transform, false);
            disk.transform.localPosition = Vector3.zero;
            disk.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            disk.transform.localScale = new Vector3(1.15f, 0.03f, 1.15f);
            Collider diskCol = disk.GetComponent<Collider>();
            if (diskCol != null) Destroy(diskCol);
            MeshRenderer diskMr = disk.GetComponent<MeshRenderer>();
            diskMr.sharedMaterial = CreateAccretionMaterial();
            diskMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            diskMr.receiveShadows = false;
            _disk = disk.transform;

            // Hạt bị hút vào tâm — cảm giác hố đen đang "nuốt"
            CreateSuckParticles(bh.transform);
        }

        private static Material CreateBlackHoleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            mat.color = new Color(0.01f, 0.005f, 0.02f, 1f); // đen tím gần tuyệt đối
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.2f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.05f, 0.02f, 0.12f, 1f)); // hơi phát tím
            return mat;
        }

        private static Material CreateAccretionMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            mat.color = new Color(0.65f, 0.35f, 0.95f, 1f); // tím sáng
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.7f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.75f, 0.4f, 1f, 1f)); // tím neon
            return mat;
        }

        /// <summary>Hạt nhỏ bị hút vào tâm hố đen (velocityOverLifetime radial âm = hút vào).</summary>
        private static void CreateSuckParticles(Transform parent)
        {
            var go = new GameObject("SuckParticles");
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.5f;
            main.startSpeed = 0f; // chỉ dựa vào radial âm kéo về tâm — không phóng hạt ra ngoài lúc sinh
            main.startSize = 0.08f;
            main.startColor = new Color(0.7f, 0.4f, 1f, 0.6f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 60f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.9f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.radial = -1.5f; // âm = kéo về tâm

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = VFXManager.CreateSoftParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += ResetVoid;
            GameEvents.OnRestart += ResetVoid;
            GameEvents.OnObstacleHit += HandleObstacleHit;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= ResetVoid;
            GameEvents.OnRestart -= ResetVoid;
            GameEvents.OnObstacleHit -= HandleObstacleHit;
        }

        private void ResetVoid()
        {
            _stage = 0;
            _relaxTimer = 0f;
            _currentDistance = baseDistance;
            transform.position = _startPos;
            transform.localScale = Vector3.one * baseScale;
        }

        /// <summary>
        /// Player đụng vật cản (R0.4):
        /// - NẤC 0 → NẤC 1: Void tiến sát + mở cửa sổ né sạch.
        /// - Đã NẤC 1 (đang trong cửa sổ) → Void nuốt → Game Over.
        /// </summary>
        private void HandleObstacleHit()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (_stage == 0)
            {
                _stage = 1;
                _relaxTimer = relaxWindow;
            }
            else
            {
                GameEvents.RaiseGameOver();
            }
        }

        private void Update()
        {
            if (player == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // NẤC 1: đếm ngược cửa sổ né sạch — hết hạn (player không đụng gì nữa) → nới về NẤC 0
            if (_stage == 1)
            {
                _relaxTimer -= Time.deltaTime;
                if (_relaxTimer <= 0f)
                {
                    _stage = 0;
                }
            }

            float targetDistance = _stage == 1 ? closeDistance : baseDistance;
            _currentDistance = Mathf.MoveTowards(_currentDistance, targetDistance, distanceLerpSpeed * Time.deltaTime);

            // Mục tiêu: sau lưng player đúng `_currentDistance` (trục Z = hướng chạy), y giữ nguyên
            Vector3 target = player.position - Vector3.forward * _currentDistance;

            // Bám ngang theo lane player từ từ (không teleport ngang)
            target.x = Mathf.MoveTowards(transform.position.x, player.position.x, lateralFollow * Time.deltaTime);

            transform.position = target;

            // Phình to hơn khi áp sát — mức độ đe dọa nhìn thấy được
            float closeness = Mathf.InverseLerp(baseDistance, closeDistance, _currentDistance);
            transform.localScale = Vector3.one * Mathf.Lerp(baseScale, closeScale, closeness);

            // Đĩa bồi tụ quay chậm — hiệu ứng hố đen hút vật chất
            if (_disk != null)
            {
                _disk.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);
            }

            // Safety net: khoảng cách z thực tế dưới ngưỡng → Void nuốt player (cơ chế chết chắc chắn
            // chạy kể cả khi collider chưa kịp overlap do player đổi lane)
            if (Mathf.Abs(transform.position.z - player.position.z) < swallowDistance)
            {
                GameEvents.RaiseGameOver();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (other.GetComponent<PlayerController>() != null)
            {
                GameEvents.RaiseGameOver();
            }
        }
    }
}
