using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.World;
using VoidRunner.Systems.Difficulty;
using VoidRunner.Systems.Input;
using VoidRunner.Systems.PowerUp;
using VoidRunner.Systems.VFX;

namespace VoidRunner.Core.Player
{
    /// <summary>
    /// Tàu vũ trụ nhỏ (R0.1) tự bay về phía trước (forwardSpeed) và chuyển lane trái/phải trên 3 lane.
    /// Điều khiển qua Rigidbody linearVelocity — mượt, không teleport.
    ///
    /// R0.1: player = TÀU VŨ TRỤ (dựng từ primitive trong Awake — idempotent, tông cyan neon).
    /// R0.4: đụng obstacle KHÔNG chết — chỉ RaiseObstacleHit (Void tiến sát).
    ///       Đụng lần 2 trong cửa sổ 10–15s = Void nuốt (VoidChase xử lý → RaiseGameOver).
    /// Tốc độ chạy do DifficultyManager điều khiển (event-driven) — forwardSpeed là tốc độ nền.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Di chuyển")]
        [SerializeField, Tooltip("Tốc độ nền — DifficultyManager có thể tăng dần lên tới maxSpeed")]
        private float forwardSpeed = 10f;
        [SerializeField] private float laneWidth = 2f;
        [SerializeField] private float laneChangeSpeed = 8f;
        [SerializeField, Tooltip("Tốc độ trượt ngang khi ĐÈ GIỮ phím (m/s) — đè lâu băng qua nhiều lane")]
        private float sweepSpeed = 6f;
        [SerializeField] private int laneCount = 3;

        [Header("Tàu vũ trụ (visual)")]
        [SerializeField, Tooltip("Góc nghiêng tối đa khi đổi lane (độ) — banking mượt")]
        private float bankAngle = 14f;
        [SerializeField, Tooltip("Tốc độ nghiêng về cân bằng (càng cao càng nhanh)")]
        private float bankSmooth = 10f;

        private Rigidbody _rb;
        private Vector3 _startPos;
        private int _currentLane;
        private float _targetX;
        private bool _isDead;
        private float _currentSpeed;
        private Transform _ship;
        private InputReader _input;

        // Đuôi tàu — ngọn lửa đẩy lập lòe + hạt exhaust (hiệu ứng cảm giác di chuyển)
        private Transform _flame;
        private Vector3 _flameBaseScale;
        private ParticleSystem _exhaust;

        // Material dùng chung cho tàu (tạo 1 lần, tông cyan neon — không phụ thuộc asset)
        private static Material _bodyMat;
        private static Material _wingMat;
        private static Material _cockpitMat;
        private static Material _engineMat;
        private static Material _flameMat;

        // Hiệu ứng va chạm: nhấp nháy thân tàu khi đụng obstacle (R: "chạm vào là người nhấp nháy")
        private Renderer[] _shipRenderers;
        private Coroutine _blinkRoutine;

        public float ForwardSpeed => _currentSpeed;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _startPos = transform.position;
            _currentLane = laneCount / 2;
            _targetX = 0f;
            _currentSpeed = forwardSpeed;

            // R0.1 (fix 2026-08-11): tàu KHÔNG lăn — đóng băng xoay để khỏi bị vật lý
            // (sphere collider lăn trên Ground) lật tàu liên tục.
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.angularVelocity = Vector3.zero;

            BuildSpaceship();
        }

        private void Start()
        {
            // Đọc input trực tiếp (không qua GameManager wiring) — ĐÈ GIỮ = trượt liên tục
            _input = FindAnyObjectByType<InputReader>();
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnRestart += HandleRestart;
            GameEvents.OnObstacleHit += HandleObstacleHitBlink;
            DifficultyManager.OnDifficultyChanged += HandleDifficultyChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnRestart -= HandleRestart;
            GameEvents.OnObstacleHit -= HandleObstacleHitBlink;
            DifficultyManager.OnDifficultyChanged -= HandleDifficultyChanged;
        }

        /// <summary>Đụng obstacle → thân tàu nhấp nháy vài nhịp (hiệu ứng va chạm rõ ràng).</summary>
        private void HandleObstacleHitBlink()
        {
            if (_isDead || _shipRenderers == null || _shipRenderers.Length == 0) return;
            if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
            _blinkRoutine = StartCoroutine(BlinkShip());
        }

        private System.Collections.IEnumerator BlinkShip()
        {
            const float blinkStep = 0.09f;
            const int blinks = 4; // 4 lần tắt/bật = nhấp nháy rõ mà không quá lâu
            for (int i = 0; i < blinks * 2; i++)
            {
                bool visible = i % 2 == 0;
                SetShipRenderersVisible(visible);
                yield return new WaitForSeconds(blinkStep);
            }
            SetShipRenderersVisible(true);
            _blinkRoutine = null;
        }

        private void SetShipRenderersVisible(bool visible)
        {
            for (int i = 0; i < _shipRenderers.Length; i++)
            {
                if (_shipRenderers[i] != null) _shipRenderers[i].enabled = visible;
            }
        }

        /// <summary>Nhận tốc độ mới từ DifficultyManager (khi game đang chơi).</summary>
        private void HandleDifficultyChanged(float speed, float _) => _currentSpeed = speed;

        public void MoveLeft() => MoveToLane(_currentLane - 1);
        public void MoveRight() => MoveToLane(_currentLane + 1);

        private void MoveToLane(int lane)
        {
            if (_isDead) return;
            _currentLane = Mathf.Clamp(lane, 0, laneCount - 1);
            _targetX = (_currentLane - (laneCount - 1) * 0.5f) * laneWidth;
            GameEvents.RaiseLaneChanged(_currentLane);
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            // ---- ĐÈ GIỮ phím = trượt liên tục (Subway Surfers); nhả phím = snap về lane gần nhất ----
            float maxX = (laneCount - 1) * 0.5f * laneWidth;
            float inputX = _input != null ? _input.MoveInput.x : 0f;

            if (Mathf.Abs(inputX) > 0.1f)
            {
                // Trượt liên tục theo hướng phím — đè lâu = băng qua nhiều lane (không dừng ở lane kế)
                _targetX = Mathf.Clamp(_targetX + Mathf.Sign(inputX) * sweepSpeed * Time.fixedDeltaTime, -maxX, maxX);
            }
            else
            {
                // Không đè → tự về giữa lane gần nhất (không lơ lửng giữa 2 lane)
                _targetX = Mathf.Clamp(Mathf.Round(_targetX / laneWidth) * laneWidth, -maxX, maxX);
                // Đồng bộ _currentLane theo vị trí (tránh state cũ lệch — chỉ còn dùng cho MoveLeft/Right & test)
                _currentLane = Mathf.Clamp(Mathf.RoundToInt(_targetX / laneWidth + (laneCount - 1) * 0.5f), 0, laneCount - 1);
            }

            // Lerp ngang mượt về target (không overshoot)
            float dx = _targetX - _rb.position.x;
            float maxStep = laneChangeSpeed * Time.fixedDeltaTime;
            float stepX = Mathf.Clamp(dx, -maxStep, maxStep);

            _rb.linearVelocity = new Vector3(stepX / Time.fixedDeltaTime, _rb.linearVelocity.y, _currentSpeed);

            // Banking: nghiêng nhẹ theo hướng đổi lane (visual chỉ — collider giữ nguyên)
            if (_ship != null)
            {
                float t = maxStep > 0.0001f ? Mathf.Clamp(stepX / maxStep, -1f, 1f) : 0f;
                Quaternion target = Quaternion.Euler(0f, 0f, -t * bankAngle);
                _ship.localRotation = Quaternion.Slerp(_ship.localRotation, target, bankSmooth * Time.fixedDeltaTime);
            }
        }

        private void Update()
        {
            // Ngọn lửa đuôi lập lòe (PerlinNoise) — tắt hẳn khi chết
            if (_flame == null) return;
            if (_isDead)
            {
                _flame.localScale = Vector3.zero;
                if (_exhaust != null && _exhaust.isPlaying) _exhaust.Stop();
                return;
            }

            float f = 0.7f + 0.3f * Mathf.PerlinNoise(Time.time * 22f, 0f);
            _flame.localScale = new Vector3(
                _flameBaseScale.x * (1.4f - f * 0.6f),
                _flameBaseScale.y * (1.4f - f * 0.6f),
                _flameBaseScale.z * f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;
            if (other.TryGetComponent<Obstacle>(out _))
            {
                // Shield: miễn nhiễm va chạm 1 lần (không phát ObstacleHit → Void không tiến sát)
                if (PowerUpSystem.Instance != null && PowerUpSystem.Instance.IsShieldActive)
                {
                    return;
                }

                // R0.4: đụng obstacle KHÔNG chết — chỉ báo lỗi để Void tiến sát.
                // Đụng lần 2 trong cửa sổ = Void nuốt (VoidChase.HandleObstacleHit → RaiseGameOver).
                GameEvents.RaiseObstacleHit();
            }
        }

        private void HandleGameOver()
        {
            _isDead = true;
            _rb.linearVelocity = Vector3.zero;
            // Nếu đang blink dở (tàu đang ẩn) mà chết → ép hiện lại, không để tàu biến mất ở game over
            SetShipRenderersVisible(true);
        }

        private void HandleRestart()
        {
            _isDead = false;
            _currentLane = laneCount / 2;
            _targetX = 0f;
            _currentSpeed = forwardSpeed; // DifficultyManager sẽ gửi lại tốc độ mới qua event
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = _startPos;
            SetShipRenderersVisible(true); // tàu phải hiện đầy đủ khi chơi lại (có thể đang ẩn do blink dở)
            if (_ship != null) _ship.localRotation = Quaternion.identity;
            if (_flame != null) _flame.localScale = _flameBaseScale; // bật lại lửa
            _exhaust?.Play();
        }

        // ---------------------------------------------------------------------
        // Tàu vũ trụ — dựng từ primitive, idempotent (chạy lại không nhân đôi)
        // ---------------------------------------------------------------------

        private void BuildSpaceship()
        {
            Transform existing = transform.Find("Ship");
            if (existing != null)
            {
                _ship = existing;
                _shipRenderers = _ship.GetComponentsInChildren<MeshRenderer>();
                return;
            }

            // Ẩn trái banh cũ (MeshRenderer trên root) — tàu thay thế hình ảnh
            MeshRenderer ball = GetComponent<MeshRenderer>();
            if (ball != null) ball.enabled = false;

            EnsureMaterials();

            var ship = new GameObject("Ship");
            ship.transform.SetParent(transform, false);

            // Thân chính (hướng +Z = tiến)
            CreatePart(ship.transform, "Body", new Vector3(0f, 0.15f, 0f), new Vector3(0.6f, 0.26f, 1.2f), _bodyMat);
            // Cánh trái / phải
            CreatePart(ship.transform, "WingL", new Vector3(-0.65f, 0.12f, -0.15f), new Vector3(0.9f, 0.05f, 0.6f), _wingMat);
            CreatePart(ship.transform, "WingR", new Vector3(0.65f, 0.12f, -0.15f), new Vector3(0.9f, 0.05f, 0.6f), _wingMat);
            // Buồng lái (kính sáng)
            CreatePart(ship.transform, "Cockpit", new Vector3(0f, 0.34f, 0.18f), new Vector3(0.3f, 0.14f, 0.5f), _cockpitMat);
            // Động cơ sau đuôi (phát sáng cam)
            CreatePart(ship.transform, "Engine", new Vector3(0f, 0.12f, -0.62f), new Vector3(0.3f, 0.12f, 0.2f), _engineMat);

            // Ngọn lửa đẩy sau đuôi — lập lòe theo thời gian (cảm giác đang bay)
            _flame = CreatePart(ship.transform, "Thruster", new Vector3(0f, 0.12f, -0.85f), new Vector3(0.18f, 0.18f, 0.55f), _flameMat);
            _flameBaseScale = _flame.localScale;

            // Hạt exhaust bay ngược (-Z) từ đuôi
            _exhaust = CreateExhaustSystem(ship.transform);

            _ship = ship.transform;

            // Cache renderer của toàn bộ thân tàu (cho hiệu ứng nhấp nháy khi đụng obstacle)
            _shipRenderers = _ship.GetComponentsInChildren<MeshRenderer>();
        }

        /// <summary>Hệ hạt exhaust liên tục — hạt cam mềm bay về sau đuôi (không cần asset).</summary>
        private static ParticleSystem CreateExhaustSystem(Transform parent)
        {
            var go = new GameObject("Exhaust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.12f, -1.05f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.35f;
            main.startSpeed = -7f; // hướng về sau (-Z)
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.55f, 0.12f, 0.85f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 45f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            // Tái sử dụng material mềm của VFXManager (không duplicate — bài học code reuse)
            renderer.material = VFXManager.CreateSoftParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private static Transform CreatePart(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;

            // Bỏ collider — chỉ render (va chạm do collider root của Player quản lý)
            Collider col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = part.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            return part.transform;
        }

        private static void EnsureMaterials()
        {
            if (_bodyMat != null) return;
            _bodyMat = CreateNeonMaterial(new Color(0.1f, 0.75f, 1f), new Color(0.05f, 0.45f, 0.8f));
            _wingMat = CreateNeonMaterial(new Color(0.05f, 0.4f, 0.65f), Color.black);
            _cockpitMat = CreateNeonMaterial(new Color(0.85f, 0.97f, 1f), new Color(0.4f, 0.9f, 1f));
            _engineMat = CreateNeonMaterial(new Color(1f, 0.5f, 0.15f), new Color(1f, 0.4f, 0.1f));
            _flameMat = CreateNeonMaterial(new Color(1f, 0.6f, 0.15f), new Color(1f, 0.4f, 0f));
        }

        private static Material CreateNeonMaterial(Color baseColor, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.6f);
            if (emission != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
            return mat;
        }
    }
}
