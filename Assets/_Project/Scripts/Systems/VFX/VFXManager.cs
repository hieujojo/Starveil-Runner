using Unity.Cinemachine;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Data;

namespace VoidRunner.Systems.VFX
{
    /// <summary>
    /// Hiệu ứng hình ảnh (VFX) — event-driven, không coupling:
    /// - Nhặt coin → burst hạt vàng tại player
    /// - Ăn power-up → burst hạt màu theo loại (Shield=xanh, Magnet=đỏ, SlowMo=tím)
    /// - Va chạm obstacle → screen shake (Cinemachine Impulse)
    /// Particle được tạo 100% bằng code (không cần prefab/material asset) — chỉ cần
    /// gắn component này vào scene là chạy. Không GC spike: burst dùng Emit() có giới hạn,
    /// không Instantiate/Destroy giữa chừng.
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

        private Transform _player;
        private ParticleSystem _coinBurst;
        private ParticleSystem _powerUpBurst;
        private CinemachineImpulseSource _impulseSource;

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
            GameEvents.OnCoinCollected += HandleCoinCollected;
            GameEvents.OnPowerUpActivated += HandlePowerUpActivated;
            GameEvents.OnObstacleHit += HandleObstacleHit;
        }

        private void OnDisable()
        {
            GameEvents.OnCoinCollected -= HandleCoinCollected;
            GameEvents.OnPowerUpActivated -= HandlePowerUpActivated;
            GameEvents.OnObstacleHit -= HandleObstacleHit;

            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) _player = pc.transform;

            // Burst system dùng chung một material mềm (tạo runtime, không cần asset)
            Material softMat = CreateSoftParticleMaterial();

            _coinBurst = CreateBurstSystem("CoinBurst", coinBurstCount, coinBurstSpeed, coinColor, softMat);
            _powerUpBurst = CreateBurstSystem("PowerUpBurst", powerUpBurstCount, powerUpBurstSpeed, Color.white, softMat);

            SetupScreenShake();
        }

        // ---------- Handlers ----------

        private void HandleCoinCollected(int _)
        {
            if (_player == null || _coinBurst == null) return;
            _coinBurst.transform.position = _player.position + Vector3.up * 0.5f;
            _coinBurst.Emit(coinBurstCount);
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
            _impulseSource.GenerateImpulseWithVelocity(new Vector3(0.4f, 0.15f, 0f) * shakeForce);
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

        /// <summary>Texture tròn mềm (radial alpha) + material Unlit — không cần asset ngoài.</summary>
        private Material CreateSoftParticleMaterial()
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
