using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Interfaces;
using VoidRunner.Core.World;
using VoidRunner.Systems.Difficulty;
using VoidRunner.Systems.Input;
using VoidRunner.Systems.PowerUp;
using VoidRunner.Systems.Save;
using VoidRunner.Systems.VFX;
using VoidRunner.Utils;

namespace VoidRunner.Core.Player
{
    /// <summary>
    /// Tàu vũ trụ nhỏ (R0.1) tự bay về phía trước (forwardSpeed) và chuyển lane trái/phải trên 3 lane.
    /// Điều khiển qua Rigidbody linearVelocity — mượt, không teleport.
    ///
    /// R0.1: player = TÀU VŨ TRỤ (dựng từ primitive trong Awake — idempotent, tông cyan neon).
    /// R0.4: đụng obstacle KHÔNG chết — chỉ RaiseObstacleHit (Enemy tiến sát).
    ///       Đụng lần 2 trong cửa sổ 10–15s = Enemy nuốt (EnemyChase xử lý → RaiseGameOver).
    /// Tốc độ chạy do DifficultyManager điều khiển (event-driven) — forwardSpeed là tốc độ nền.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Di chuyển")]
        [SerializeField, Tooltip("Tốc độ nền — DifficultyManager có thể tăng dần lên tới maxSpeed")]
        private float forwardSpeed = 10f;
        [SerializeField] private float laneWidth = 2f;
        [SerializeField, Tooltip("Tốc độ trượt tới vị trí lane đích (m/s) — cao = phản hồi tức thì")]
        private float laneChangeSpeed = 16f;
        [SerializeField, Tooltip("Tốc độ trượt ngang khi ĐÈ GIỮ phím (m/s) — đè lâu băng qua nhiều lane")]
        private float sweepSpeed = 9f;
        [SerializeField] private int laneCount = 3;

        [Header("Tàu vũ trụ (visual)")]
        [SerializeField, Tooltip("Góc nghiêng tối đa khi đổi lane (độ) — banking mượt")]
        private float bankAngle = 14f;
        [SerializeField, Tooltip("Tốc độ nghiêng về cân bằng (càng cao càng nhanh)")]
        private float bankSmooth = 10f;

        [Header("Tàu MODEL (Task D — chọn ở MainMenu)")]
        [Tooltip("2 prefab tàu (SF Fighter / Sparrow) — tool Setup Ship Select tự gán. Rỗng = tàu primitive cũ.")]
        [SerializeField] private GameObject[] shipPrefabs;
        [SerializeField, Tooltip("Chiều cao tàu (đơn vị) — đo bounds thật rồi ép scale; 2026-08-12 v3 user: tàu to thêm ~10px (1.1 → 1.2) + Point Light bám tàu cho nổi bật")]
        private float shipTargetHeight = 1.2f;
        [Tooltip("Xoay thêm quanh Y (độ) nếu model quay mặt sai hướng (0 = model forward +Z = hướng chạy).")]
        [SerializeField] private float shipYaw = 0f;

        private Rigidbody _rb;
        private Vector3 _startPos;
        private int _currentLane;
        private float _targetX;
        private bool _isDead;
        private float _currentSpeed;
        private Transform _ship;
        private InputReader _input;
        private float _lastInputX; // phát hiện CẠNH LÊN của phím (0→±1) để nhảy 1 lane ngay lập tức

        // COMMAND PATTERN — lịch sử lệnh (cho undo/redo nếu cần)
        private readonly Stack<ICommand> _commandHistory = new Stack<ICommand>();

        // Đuôi tàu — lửa tên lửa (v3f.9): ánh sáng cam lập lòe + vệt lửa MƯỢT (TrailRenderer).
        // Bỏ hẳn hạt exhaust (v3f.8 vẫn bị user báo "hạt vuông cam" — chùm hạt dày + Bloom → dải
        // blocky) → TrailRenderer cùng material mềm như vệt khói con bọ (đã chứng minh mượt, không
        // bao giờ vuông trong project này).
        private Light _flameLight;
        private float _flameBaseIntensity = 1.5f;
        private TrailRenderer _flameTrail;

        // Material dùng chung cho tàu (tạo 1 lần, tông cyan neon — không phụ thuộc asset)
        private static Material _bodyMat;
        private static Material _wingMat;
        private static Material _cockpitMat;
        private static Material _engineMat;

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

            // v3f.10.2 (user: "con bọ cứ rung rung khó nhìn"): bật Interpolation cho rigidbody
            // player — mặc định None = vị trí chỉ cập nhật ở fixed timestep (50Hz) → tàu + con bọ
            // (bám player.position) + camera đều giật bậc thang → rung. Interpolate = vị trí mượt
            // giữa các fixed step (visual), không đổi vật lý collision.
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

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

        // COMMAND PATTERN — wrapper methods tạo command object
        public void MoveLeft()
        {
            var cmd = new Commands.MoveLeftCommand(this);
            cmd.Execute();
            _commandHistory.Push(cmd);
        }

        public void MoveRight()
        {
            var cmd = new Commands.MoveRightCommand(this);
            cmd.Execute();
            _commandHistory.Push(cmd);
        }

        /// <summary>COMMAND PATTERN — thực hiện command bên ngoài (từ InputReader).</summary>
        public void ExecuteCommand(ICommand command)
        {
            if (command == null) return;
            command.Execute();
            _commandHistory.Push(command);
        }

        /// <summary>COMMAND PATTERN — undo lệnh cuối cùng.</summary>
        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var cmd = _commandHistory.Pop();
                cmd.Undo();
            }
        }

        private void MoveToLane(int lane)
        {
            if (_isDead) return;
            _currentLane = Mathf.Clamp(lane, 0, laneCount - 1);
            _targetX = (_currentLane - (laneCount - 1) * 0.5f) * laneWidth;
            GameEvents.RaiseLaneChanged(_currentLane);
        }

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

            // FIX 2026-08-12 (góp ý reviewer): gate theo State — đồng bộ với EnemyChase/Difficulty.
            // Khi Paused: FixedUpdate vẫn chạy dù timeScale=0 → đè A/D sẽ đổi _targetX (nhảy 1 lane
            // khi resume). Gate Playing = không nhận input khi pause, không nhảy lane bất ngờ.
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            // ---- Cơ chế (fix 2026-08-11 vòng 2, user: "bấm/đè phải phản hồi ngay"):
            //   • CẠNH LÊN (vừa bấm)   → nhảy NGAY 1 lane theo hướng phím (tap = di chuyển 1 tí)
            //   • ĐÈ GIỮ              → trượt liên tục qua nhiều lane (đè càng lâu càng rẽ sang)
            //   • Nhả phím            → snap về lane gần nhất (không lơ lửng giữa 2 lane)
            // Trước đây chỉ sweep 6 m/s (0.5s/lane) → bấm-nhả nhanh gần như không đi đâu
            // (phải bấm 2 lần), đè giữ thì quá chậm. ----
            float maxX = (laneCount - 1) * 0.5f * laneWidth;
            float inputX = _input != null ? _input.MoveInput.x : 0f;

            bool risingEdge = Mathf.Abs(inputX) > 0.1f && Mathf.Abs(_lastInputX) <= 0.1f;
            _lastInputX = inputX;

            if (risingEdge)
            {
                // Bấm mới: nhảy đúng 1 lane theo hướng (phản hồi TỨC THÌ)
                _targetX = Mathf.Clamp(_targetX + Mathf.Sign(inputX) * laneWidth, -maxX, maxX);
                // Đồng bộ _currentLane NGAY (tránh stale — MoveLeft/MoveRight/test đọc từ _currentLane)
                _currentLane = Mathf.Clamp(Mathf.RoundToInt(_targetX / laneWidth + (laneCount - 1) * 0.5f), 0, laneCount - 1);
            }
            else if (Mathf.Abs(inputX) > 0.1f)
            {
                // Đè giữ: trượt liên tục theo hướng phím — đè lâu = băng qua nhiều lane
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
            // Ánh sáng lửa đuôi lập lòe (PerlinNoise) — tắt hẳn khi chết
            if (_flameLight == null && _flameTrail == null) return;
            if (_isDead)
            {
                if (_flameLight != null) _flameLight.intensity = 0f;
                if (_flameTrail != null) _flameTrail.emitting = false;
                return;
            }

            if (_flameLight != null)
            {
                float f = 0.7f + 0.3f * Mathf.PerlinNoise(Time.time * 22f, 0f);
                _flameLight.intensity = _flameBaseIntensity * f;
            }
            if (_flameTrail != null && !_flameTrail.emitting) _flameTrail.emitting = true;
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

                // R0.4: đụng obstacle KHÔNG chết — chỉ báo lỗi để Enemy tiến sát.
                // Đụng lần 2 trong cửa sổ = Enemy nuốt (EnemyChase.HandleObstacleHit → RaiseGameOver).
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

        private void HandleRestart() => ResetToStart();

        /// <summary>
        /// Đưa player về ĐIỂM BẮT ĐẦU CỐ ĐỊNH (_startPos = vị trí scene) — set cả transform + rigidbody
        /// (fix 2026-08-11: restart trước đây chỉ set _rb.position nhưng thứ tự event làm player vẫn
        /// đứng nguyên ở z=148 → mỗi lần chơi lại vị trí khác nhau). GameManager gọi TRỰC TIẾP method này.
        /// </summary>
        public void ResetToStart()
        {
            _isDead = false;
            _currentLane = laneCount / 2;
            _targetX = 0f;
            _currentSpeed = forwardSpeed; // DifficultyManager sẽ gửi lại tốc độ mới qua event
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = _startPos; // teleport transform — rigidbody không có interpolation nên không lag
            _rb.position = _startPos;
            SetShipRenderersVisible(true); // tàu phải hiện đầy đủ khi chơi lại (có thể đang ẩn do blink dở)
            if (_ship != null) _ship.localRotation = Quaternion.identity;
            if (_flameLight != null) _flameLight.intensity = _flameBaseIntensity; // bật lại lửa
            if (_flameTrail != null)
            {
                _flameTrail.emitting = true;
                _flameTrail.Clear(); // teleport về start — xóa vệt cũ tránh kéo dài xuyên map
            }
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
                // v3f.8 self-heal: tàu cũ có thể mang "Thruster" cube + "Exhaust" material cũ →
                // xóa và dựng lại hiệu ứng lửa MỚI (idempotent — chạy lại an toàn)
                Transform oldThruster = existing.Find("Thruster");
                if (oldThruster != null && oldThruster.GetComponent<Light>() == null)
                {
                    // DestroyImmediate (Awake — runtime setup, object vừa dựng) — tránh 1 frame
                    // tồn tại 2 object cùng tên "Thruster"/"Exhaust" khi Destroy deferred
                    DestroyImmediate(oldThruster.gameObject);
                }
                Transform oldExhaust = existing.Find("Exhaust");
                if (oldExhaust != null) DestroyImmediate(oldExhaust.gameObject);
                _flameLight = CreateFlameLight(existing, new Vector3(0f, 0.12f, -0.85f));
                _flameTrail = CreateFlameTrail(existing, new Vector3(0f, 0.12f, -1.05f));
                BlobShadow.Attach(transform); // v3f.10: bóng mềm cho tàu (idempotent — đã có thì thôi)
                return;
            }

            // Task D (2026-08-11): ưu tiên model tàu đã chọn ở MainMenu (SaveSystem.SelectedShip)
            int idx = SaveSystem.SelectedShip;
            GameObject prefab = null;
            if (shipPrefabs != null && idx >= 0 && idx < shipPrefabs.Length) prefab = shipPrefabs[idx];
            // Self-heal (R4.18): chưa gán prefab trong scene (tool chưa chạy) → tự tải qua ShipCatalog
            if (prefab == null) prefab = ShipCatalog.Load(idx);
            if (prefab != null)
            {
                BuildModelShip(prefab);
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

            // Ngọn lửa đẩy sau đuôi (v3f.9): ánh sáng cam lập lòe + vệt lửa TRƠN (TrailRenderer)
            _flameLight = CreateFlameLight(ship.transform, new Vector3(0f, 0.12f, -0.85f));
            _flameTrail = CreateFlameTrail(ship.transform, new Vector3(0f, 0.12f, -1.05f));

            // FIX 2026-08-12 v3: Point Light bám tàu (nhánh primitive — giống model ship)
            EnsureShipLight(ship.transform);

            _ship = ship.transform;

            // v3f.10 (user: "con bọ có bóng, làm bóng với tàu luôn"): bóng mềm — primitive parts
            // đã shadowCastingMode Off sẵn, thêm quad đen mờ dưới tàu trên track.
            BlobShadow.Attach(transform);

            // Cache renderer của toàn bộ thân tàu (cho hiệu ứng nhấp nháy khi đụng obstacle)
            _shipRenderers = _ship.GetComponentsInChildren<MeshRenderer>();
        }

        /// <summary>
        /// FIX 2026-08-12 v3 (user: "tàu bị mờ"): tạo Point Light cyan bám theo tàu (con của Ship)
        /// để tàu sáng + nổi bật nhất trên track tối. Idempotent — đã có "ShipLight" thì thôi.
        /// </summary>
        private static void EnsureShipLight(Transform ship)
        {
            if (ship.Find("ShipLight") != null) return;

            var lightGo = new GameObject("ShipLight");
            lightGo.transform.SetParent(ship, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.45f, 0.85f, 1f, 1f); // cyan nhạt
            light.intensity = 2.2f;
            light.range = 7f;
            light.shadows = LightShadows.None; // nhẹ — không tốn shadow map
        }

        /// <summary>
        /// Task D: dựng tàu từ MODEL (prefab FBX — SF Fighter / Sparrow) thay vì primitive.
        /// Scale chuẩn theo chiều cao thật, vô hiệu hóa collider con (chỉ root sphere collider
        /// quản lý va chạm), gắn flame + exhaust ngay sau đuôi model (đo bounds).
        /// </summary>
        private void BuildModelShip(GameObject prefab)
        {
            // Ẩn trái banh cũ
            MeshRenderer ball = GetComponent<MeshRenderer>();
            if (ball != null) ball.enabled = false;

            EnsureMaterials(); // flame/exhaust vẫn dùng material neon code

            GameObject ship = Instantiate(prefab, transform);
            ship.name = "Ship";

            // Fix material Built-in → URP (model 3rd-party thường dùng shader Standard → hiện TÍM trong URP)
            MaterialFixer.EnsureURPMaterials(ship);

            // Vô hiệu hóa collider con — không đụng vật lý, chỉ render
            foreach (var col in ship.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Chuẩn hóa scale theo chiều cao thật
            Bounds b = GetRenderBounds(ship);
            if (b.size.y > 0.001f)
            {
                ship.transform.localScale = Vector3.one * (shipTargetHeight / b.size.y);
            }
            // Quay mặt về hướng chạy (+Z) — model forward +Z là chuẩn; sai hướng thì chỉnh shipYaw
            ship.transform.localRotation = Quaternion.Euler(0f, shipYaw, 0f);

            // Flame + exhaust gắn SAU đuôi model (tính lại bounds sau scale)
            Bounds scaled = GetRenderBounds(ship);
            float rearZ = -scaled.size.z * 0.5f - 0.15f;
            float liftY = scaled.size.y * 0.35f;

            _flameLight = CreateFlameLight(ship.transform, new Vector3(0f, liftY, rearZ));
            _flameTrail = CreateFlameTrail(ship.transform, new Vector3(0f, liftY, rearZ - 0.1f));

            // FIX 2026-08-12 v3 (user: "tàu bị mờ, muốn nó là thứ nổi bật nhất"): Point Light cyan
            // bám theo tàu — track tối đen, không có UI đè; tàu mờ vì THIẾU ÁNH SÁNG. Light này
            // chiếu sáng thân tàu + halo quanh, tàu nổi bật hơn mọi thứ xung quanh.
            EnsureShipLight(ship.transform);

            // v3f.10.1 (user: "làm bóng với tàu"): tắt shadow thật của model tàu (quá nhỏ/mờ)
            // → thay bằng blob đen mờ ĐẶT ĐÚNG TRÊN MẶT ROAD (road cube top y=0.05 — offset 0.93
            // từ root y=1 → blob y=0.07, không còn chìm như v3f.10).
            foreach (var r in ship.GetComponentsInChildren<Renderer>())
            {
                if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            BlobShadow.Attach(transform);

            _ship = ship.transform;
            _shipRenderers = _ship.GetComponentsInChildren<MeshRenderer>();
        }

        private static Bounds GetRenderBounds(GameObject go)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool has = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (has) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; has = true; }
            }
            return has ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        /// <summary>
        /// Vệt lửa tên lửa (v3f.9 — effect MỚI): TrailRenderer ngắn sau đuôi, gradient cam
        /// (sáng vàng → cam → trong suốt), cùng material mềm như vệt khói con bọ — render như
        /// 1 dải ribbon MƯỢT, về mặt kỹ thuật không thể hiện ra "ô vuông".
        /// </summary>
        private static TrailRenderer CreateFlameTrail(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Exhaust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.22f;             // v3f.9.5: user "lửa dài ra 1 tí xíu" 0.18→0.22
            trail.startWidth = 0.45f;       // v3f.9.4: user "lửa to thêm 1 tí" 0.34→0.45
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.08f;
            trail.numCornerVertices = 4;
            trail.numCapVertices = 2;
            trail.autodestruct = false;

            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.8f, 0.35f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.45f),
                    new GradientColorKey(new Color(0.55f, 0.2f, 0.05f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.55f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            trail.colorGradient = grad;

            // Tái sử dụng material mềm của VFXManager (cùng loại vệt khói con bọ — đã mượt)
            trail.material = VFXManager.CreateSoftParticleMaterial();
            return trail;
        }

        /// <summary>Ánh sáng lửa cam lập lòe sau đuôi (thay cube Thruster cũ — v3f.8).</summary>
        private static Light CreateFlameLight(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Thruster");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.15f, 1f);
            light.intensity = 1.5f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;
            return light;
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
