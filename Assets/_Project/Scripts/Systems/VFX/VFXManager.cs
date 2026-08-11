using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;
using VoidRunner.Data;
using VoidRunner.Systems.Score;

namespace VoidRunner.Systems.VFX
{
    /// <summary>
    /// Hiệu ứng hình ảnh (VFX) — event-driven, không coupling:
    /// - Nhặt coin → burst hạt vàng tại vị trí coin + popup điểm "+10" bay lên
    /// - Ăn power-up → burst hạt màu theo loại (Shield=xanh, Magnet=đỏ, SlowMo=tím)
    /// - Va chạm obstacle → screen shake (Cinemachine Impulse)
    /// - Void luôn có vệt khói tối (TrailRenderer tạo bằng code)
    /// Particle được tạo 100% bằng code (không cần prefab/material asset) — chỉ cần
    /// gắn component này vào scene là chạy. Không GC spike: burst dùng Emit() có giới hạn,
    /// popup dùng object pool, không Instantiate/Destroy giữa chừng.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Coin burst")]
        [SerializeField] private int coinBurstCount = 14;
        [SerializeField] private float coinBurstSpeed = 5f;
        [SerializeField] private Color coinColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Header("Power-up burst")]
        [SerializeField] private int powerUpBurstCount = 22;
        [SerializeField] private float powerUpBurstSpeed = 6f;
        [SerializeField] private Color shieldColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private Color magnetColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color slowMoColor = new Color(0.7f, 0.4f, 1f, 1f);

        [Header("Screen shake")]
        [SerializeField] private float shakeForce = 3f;
        [SerializeField] private float shakeDuration = 0.18f;

        [Header("Score popup")]
        [Tooltip("Font cho popup điểm — tool Setup VFX tự gán Kenney Future.")]
        [SerializeField] private TMP_FontAsset popupFont;
        [Tooltip("Số điểm hiển thị mỗi popup (nên khớp ScoreSystem.coinScore = 10).")]
        [SerializeField] private int popupScore = 10;
        [SerializeField] private int popupPoolSize = 8;
        [SerializeField] private float popupFloatDistance = 70f;
        [SerializeField] private float popupDuration = 0.7f;
        [SerializeField] private Color popupColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Header("Void trail")]
        [SerializeField] private float trailTime = 0.6f;
        [SerializeField] private float trailStartWidth = 1.4f;
        [SerializeField] private Color trailColor = new Color(0.05f, 0.05f, 0.1f, 0.55f);

        private Transform _player;
        private ParticleSystem _coinBurst;
        private ParticleSystem _powerUpBurst;
        private CinemachineImpulseSource _impulseSource;

        private Canvas _canvas;
        private TextMeshProUGUI[] _popups;
        private int _popupIndex;
        private TrailRenderer _voidTrail;
        private Transform _voidTransform;
        private Camera _cam;
        private ScoreSystem _scoreSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnCoinCollectedAt += HandleCoinCollectedAt;
            GameEvents.OnPowerUpActivated += HandlePowerUpActivated;
            GameEvents.OnObstacleHit += HandleObstacleHit;
            GameEvents.OnRestart += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnCoinCollectedAt -= HandleCoinCollectedAt;
            GameEvents.OnPowerUpActivated -= HandlePowerUpActivated;
            GameEvents.OnObstacleHit -= HandleObstacleHit;
            GameEvents.OnRestart -= HandleRestart;

            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) _player = pc.transform;

            _cam = Camera.main;
            _scoreSystem = FindAnyObjectByType<ScoreSystem>(); // chỉ để đọc Multiplier cho popup

            // Burst system dùng chung một material mềm (tạo runtime, không cần asset)
            Material softMat = CreateSoftParticleMaterial();

            _coinBurst = CreateBurstSystem("CoinBurst", coinBurstCount, coinBurstSpeed, coinColor, softMat);
            _powerUpBurst = CreateBurstSystem("PowerUpBurst", powerUpBurstCount, powerUpBurstSpeed, Color.white, softMat);

            SetupScreenShake();
            SetupPopups();
            SetupVoidTrail();
        }

        private void Update()
        {
            // Vệt khói tự nở rộng theo kích thước Void (scale tăng dần theo độ khó)
            if (_voidTrail != null && _voidTransform != null)
            {
                _voidTrail.startWidth = trailStartWidth * _voidTransform.localScale.x;
            }
        }

        // ---------- Handlers ----------

        private void HandleCoinCollectedAt(Vector3 worldPos)
        {
            // Burst và popup độc lập — cái này lỗi không kéo cái kia
            if (_coinBurst != null)
            {
                _coinBurst.transform.position = worldPos;
                _coinBurst.Emit(coinBurstCount);
            }

            // Điểm popup nhân theo combo (khớp ScoreSystem: coinScore × multiplier)
            int multiplier = _scoreSystem != null ? _scoreSystem.Multiplier : 1;
            ShowPopup(worldPos, $"+{popupScore * multiplier}");
        }

        private void HandlePowerUpActivated(PowerUpType type)
        {
            if (_player == null || _powerUpBurst == null) return;

            Color color = type switch
            {
                PowerUpType.Shield => shieldColor,
                PowerUpType.Magnet => magnetColor,
                PowerUpType.SlowMo => slowMoColor,
                _ => Color.white,
            };

            _powerUpBurst.transform.position = _player.position + Vector3.up * 0.6f;
            var main = _powerUpBurst.main;
            main.startColor = color;
            _powerUpBurst.Emit(powerUpBurstCount);
        }

        private void HandleObstacleHit()
        {
            if (_impulseSource == null) return;
            _impulseSource.ImpulseDefinition.ImpulseDuration = shakeDuration;
            _impulseSource.GenerateImpulseWithVelocity(new Vector3(0.4f, 0.15f, 0f) * shakeForce);
        }

        private void HandleRestart()
        {
            // Void teleport về vị trí ban đầu khi restart — xóa vệt cũ tránh kéo dài xuyên map
            if (_voidTrail != null) _voidTrail.Clear();
        }

        // ---------- Setup ----------

        private void SetupScreenShake()
        {
            // CinemachineImpulseSource đặt ngay trên VFXManager
            _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();

            // CinemachineImpulseListener là extension gắn trên CinemachineCamera (Unity.Cinemachine)
            var cam = FindAnyObjectByType<CinemachineCamera>();
            if (cam != null && cam.GetComponent<CinemachineImpulseListener>() == null)
            {
                cam.gameObject.AddComponent<CinemachineImpulseListener>();
            }
        }

        /// <summary>Tạo ParticleSystem burst 1 lần (tự hủy khi phát xong).</summary>
        private ParticleSystem CreateBurstSystem(string name, int maxParticles, float speed, Color color, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = speed;
            main.startSize = 0.35f;
            main.startColor = color;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Không cấu hình emission/duration — dùng Emit() trực tiếp cho one-shot (không dead config)

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            return ps;
        }

        /// <summary>Tạo pool popup điểm (TMP text) nằm trên Canvas — không Instantiate/Destroy giữa chừng.</summary>
        private void SetupPopups()
        {
            _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            if (popupFont == null)
            {
                // Fallback: dùng font của text TMP đầu tiên trong scene (thường là Kenney Future từ HUD)
                var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();
                popupFont = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
                if (popupFont == null)
                {
                    Debug.LogWarning("VFXManager: không có font cho popup điểm — text sẽ không hiện. Chạy lại tool 'Setup VFX in Game Scene' để tự gán Kenney Future.");
                }
            }

            _popups = new TextMeshProUGUI[popupPoolSize];
            for (int i = 0; i < popupPoolSize; i++)
            {
                var go = new GameObject($"ScorePopup_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(_canvas.transform, false);
                go.SetActive(false);

                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.font = popupFont;
                tmp.fontSize = 46;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = popupColor;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                _popups[i] = tmp;
            }
        }

        /// <summary>Hiện popup "+10" tại vị trí coin trên màn hình — bay lên rồi mờ dần (DOTween, pool).</summary>
        private void ShowPopup(Vector3 worldPos, string text)
        {
            if (_popups == null || _popups.Length == 0 || _canvas == null) return;
            if (_cam == null) return;

            var tmp = _popups[_popupIndex];
            _popupIndex = (_popupIndex + 1) % _popups.Length;

            tmp.gameObject.SetActive(true);
            tmp.text = text;
            tmp.alpha = 1f;
            tmp.rectTransform.localScale = Vector3.one;

            // World → screen: popup hiện đúng chỗ coin bị nhặt (góc trên vì coin nằm phía trước)
            Vector3 screen = _cam.WorldToScreenPoint(worldPos + Vector3.up * 0.5f);
            tmp.rectTransform.position = screen;

            // Kill tween cũ nếu popup này đang được tái sử dụng
            DOTween.Kill(tmp);
            DOTween.Kill(tmp.rectTransform);

            // Bay lên + bounce scale + mờ dần, xong tắt lại để dùng pool
            var seq = DOTween.Sequence();
            seq.Append(tmp.rectTransform.DOMove(screen + Vector3.up * popupFloatDistance, popupDuration)
                .SetEase(Ease.OutCubic));
            seq.Join(tmp.rectTransform.DOScale(1.15f, 0.08f).SetEase(Ease.OutBack));
            seq.Join(tmp.DOFade(0f, popupDuration).SetDelay(popupDuration * 0.5f));
            seq.OnComplete(() => tmp.gameObject.SetActive(false));
        }

        /// <summary>Tạo vệt khói tối cho Void — TrailRenderer tạo bằng code, nở rộng theo scale Void.</summary>
        private void SetupVoidTrail()
        {
            var voidChase = FindAnyObjectByType<VoidChase>();
            if (voidChase == null) return;

            _voidTransform = voidChase.transform;
            if (_voidTransform.GetComponent<TrailRenderer>() == null)
            {
                _voidTransform.gameObject.AddComponent<TrailRenderer>();
            }

            _voidTrail = _voidTransform.GetComponent<TrailRenderer>();
            _voidTrail.time = trailTime;
            _voidTrail.startWidth = trailStartWidth * _voidTransform.localScale.x;
            _voidTrail.endWidth = 0.05f;
            _voidTrail.minVertexDistance = 0.2f;
            _voidTrail.numCornerVertices = 4;
            _voidTrail.numCapVertices = 4;
            _voidTrail.startColor = trailColor;
            _voidTrail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            // Dùng chung material mềm (Particles/Unlit + texture radial) — đã hoạt động với burst;
            // URP/Unlit mặc định KHÔNG sample vertex color → trail sẽ hiện trắng, nên không dùng.
            _voidTrail.material = CreateSoftParticleMaterial();
        }

        /// <summary>
        /// Texture tròn mềm (radial alpha) + material Unlit — không cần asset ngoài.
        /// internal static để các hệ thống khác (PlayerController exhaust...) tái sử dụng, không duplicate.
        /// </summary>
        internal static Material CreateSoftParticleMaterial()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha; // mềm hơn ở mép
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (mat == null || mat.shader == null)
            {
                // Fallback nếu không tìm thấy shader URP (hiếm khi xảy ra)
                mat = new Material(Shader.Find("Sprites/Default"));
            }
            mat.mainTexture = tex;
            return mat;
        }
    }
}
