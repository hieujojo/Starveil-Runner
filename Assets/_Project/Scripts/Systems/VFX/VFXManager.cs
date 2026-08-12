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
    /// - Ăn power-up → burst hạt màu theo loại (Shield=xanh, Magnet=đỏ, SlowMo=tím)        /// - Va chạm obstacle → screen shake (Cinemachine Impulse)
        /// - Enemy luôn có vệt khói tối (TrailRenderer tạo bằng code)
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

        [Header("Enemy trail")]
        // FIX 2026-08-12 v3f.10 (user: "có vài khoảnh khắc thấy 2 con bọ đè lên nhau"): vệt khói
        // cũ quá dài (0.6s) + quá rộng (1.4) + quá đặc (alpha 0.55) → khi bọ đổi lane nhanh,
        // vệt uốn cong ĐÈ LÊN thân bọ → nhìn như con bọ thứ 2. Giảm: ngắn + mảnh + mờ hơn
        // (vẫn giữ hiệu ứng khói đuổi theo nhưng không còn ảo giác 2 con).
        [SerializeField] private float trailTime = 0.35f;
        [SerializeField] private float trailStartWidth = 0.9f;
        [SerializeField] private Color trailColor = new Color(0.05f, 0.05f, 0.1f, 0.28f);

        // NOTE (v3f.7): LỬA TÊN LỬA nằm ở PlayerController (Thruster + hạt exhaust cam) —
        // KHÔNG tạo vệt trail ở VFXManager nữa (vệt cũ dùng shader Additive không tồn tại trong
        // URP → error shader TÍM + quá dài che mất flame — user phàn nàn).

        [Header("Space drift (sao trôi ngang — chiều sâu vũ trụ)")]
        [SerializeField] private float driftRate = 50f;
        [SerializeField] private float driftSpeed = -10f; // ngược hướng chạy (phía sau)
        [SerializeField] private float driftLifetime = 1.8f;
        [SerializeField] private float driftSizeMin = 0.15f;
        [SerializeField] private float driftSizeMax = 0.35f;
        [SerializeField] private Color driftColor = new Color(0.9f, 0.95f, 1f, 1f);

        private Transform _player;
        private ParticleSystem _coinBurst;
        private ParticleSystem _powerUpBurst;
        private CinemachineImpulseSource _impulseSource;

        private Canvas _canvas;
        private TextMeshProUGUI[] _popups;
        private int _popupIndex;
        private TrailRenderer _enemyTrail;
        private Transform _enemyTransform;
        private ParticleSystem _spaceDrift;
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

            _scoreSystem = FindAnyObjectByType<ScoreSystem>(); // chỉ để đọc Multiplier cho popup

            // Burst system dùng chung một material mềm (tạo runtime, không cần asset)
            Material softMat = CreateSoftParticleMaterial();

            _coinBurst = CreateBurstSystem("CoinBurst", coinBurstCount, coinBurstSpeed, coinColor, softMat);
            _powerUpBurst = CreateBurstSystem("PowerUpBurst", powerUpBurstCount, powerUpBurstSpeed, Color.white, softMat);

            SetupScreenShake();
            SetupPopups();
            SetupEnemyTrail();
            SetupSpaceDrift();
        }

        private void Update()
        {
            // Vệt khói tự nở rộng theo kích thước enemy (scale tăng dần theo độ khó)
            if (_enemyTrail != null && _enemyTransform != null)
            {
                _enemyTrail.startWidth = trailStartWidth * _enemyTransform.localScale.x;
            }

            // Hệ sao trôi bám theo player (box bao quanh player) — sao trôi ngược ra sau = chiều sâu + tốc độ
            if (_spaceDrift != null && _player != null)
            {
                _spaceDrift.transform.position = _player.position + new Vector3(0f, 1.5f, 0f);
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
            // worldPos vẫn dùng cho burst hạt — popup điểm thì nằm VỊ TRÍ CỐ ĐỊNH ngoài đường
            ShowPopup($"+{popupScore * multiplier}");
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
            // Enemy teleport về vị trí ban đầu khi restart — xóa vệt cũ tránh kéo dài xuyên map
            if (_enemyTrail != null) _enemyTrail.Clear();
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

        /// <summary>
        /// Hiện popup điểm ở VỊ TRÍ CỐ ĐỊNH NGOÀI ĐƯỜNG (fix 2026-08-11 — user: "+10 chèn thẳng
        /// vào UI che obstacle/coin, đặt ra khỏi trục đường chơi"). Trước đây popup hiện ngay tại
        /// vị trí coin qua WorldToScreenPoint → chữ điểm nằm TRÊN đường, che vật cản phía trước
        /// → người chơi không thấy kịp để né. Giờ popup nằm lệch về bên phải cạnh ScorePanel
        /// (khu vực không có gameplay) — vẫn dễ đọc nhưng không bao giờ che đường.
        /// </summary>
        private void ShowPopup(string text)
        {
            if (_popups == null || _popups.Length == 0 || _canvas == null) return;

            var tmp = _popups[_popupIndex];
            _popupIndex = (_popupIndex + 1) % _popups.Length;

            tmp.gameObject.SetActive(true);
            tmp.text = text;
            tmp.alpha = 1f;
            tmp.rectTransform.localScale = Vector3.one;

            // Vị trí cố định: BÊN PHẢI ScorePanel (anchor 0.5,1 @ (240,-60)) — ngoài vùng panel
            // (panel trải x ±180) + ngoài trục đường chơi, không bao giờ che obstacle/coin
            // (v3f.9.3: 260→240; v3f.9.4: user "cho gần thêm 1 tí nữa, chỉ 1 tí" 240→228)
            tmp.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            tmp.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tmp.rectTransform.pivot = new Vector2(0.5f, 1f);
            Vector2 basePos = new Vector2(228f, -60f);
            tmp.rectTransform.anchoredPosition = basePos;

            // Kill tween cũ nếu popup này đang được tái sử dụng
            DOTween.Kill(tmp);
            DOTween.Kill(tmp.rectTransform);

            // Bay lên + bounce scale + mờ dần, xong tắt lại để dùng pool
            var seq = DOTween.Sequence();
            seq.Append(tmp.rectTransform.DOAnchorPos(basePos + Vector2.up * popupFloatDistance, popupDuration)
                .SetEase(Ease.OutCubic));
            seq.Join(tmp.rectTransform.DOScale(1.15f, 0.08f).SetEase(Ease.OutBack));
            seq.Join(tmp.DOFade(0f, popupDuration).SetDelay(popupDuration * 0.5f));
            seq.OnComplete(() => tmp.gameObject.SetActive(false));
        }

        /// <summary>
        /// Hệ sao trôi quanh player (ParticleSystem World) — chấm sáng trôi ngược hướng chạy.
        /// v3f.7: TO hơn (0.35–0.8, trước 0.06–0.18 quá nhỏ không thấy) + box BAO QUANH player
        /// (trước đặt cách 14m phía trước) → sao lướt ngang qua 2 bên tàu rõ ràng.
        /// </summary>
        private void SetupSpaceDrift()
        {
            var go = new GameObject("SpaceDrift");
            go.transform.SetParent(transform, false);

            _spaceDrift = go.AddComponent<ParticleSystem>();
            var main = _spaceDrift.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = driftLifetime;
            main.startSpeed = driftSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(driftSizeMin, driftSizeMax);
            main.startColor = driftColor;
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _spaceDrift.emission;
            emission.rateOverTime = driftRate;

            var shape = _spaceDrift.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 14f, 26f); // bao quanh player ±13 z + rộng hơn road ±9

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateSoftParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        /// <summary>Tạo vệt khói tối cho Enemy — TrailRenderer tạo bằng code, nở rộng theo scale enemy.</summary>
        private void SetupEnemyTrail()
        {
            var enemyChase = FindAnyObjectByType<EnemyChase>();
            if (enemyChase == null) return;

            _enemyTransform = enemyChase.transform;
            if (_enemyTransform.GetComponent<TrailRenderer>() == null)
            {
                _enemyTransform.gameObject.AddComponent<TrailRenderer>();
            }

            _enemyTrail = _enemyTransform.GetComponent<TrailRenderer>();
            _enemyTrail.time = trailTime;
            _enemyTrail.startWidth = trailStartWidth * _enemyTransform.localScale.x;
            _enemyTrail.endWidth = 0.05f;
            _enemyTrail.minVertexDistance = 0.2f;
            _enemyTrail.numCornerVertices = 4;
            _enemyTrail.numCapVertices = 4;
            _enemyTrail.startColor = trailColor;
            _enemyTrail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            // Dùng chung material mềm (Particles/Unlit + texture radial) — đã hoạt động với burst;
            // URP/Unlit mặc định KHÔNG sample vertex color → trail sẽ hiện trắng, nên không dùng.
            _enemyTrail.material = CreateSoftParticleMaterial();
        }

        /// <summary>
        /// Texture tròn mềm (radial alpha) dùng chung cho mọi material mềm — không cần asset ngoài.
        /// </summary>
        private static Texture2D BuildSoftTexture()
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
            return tex;
        }

        /// <summary>
        /// Material mềm alpha blend — dùng cho burst, hạt, khói, sao trôi.
        /// internal static để các hệ thống khác (PlayerController exhaust...) tái sử dụng, không duplicate.
        ///
        /// R3.1 lesson (v3f.7): SHADER "Universal Render Pipeline/Particles/Additive" KHÔNG TỒN TẠI trong
        /// URP → Shader.Find null → new Material(null) = error shader MÀU TÍM. Check Shader TRƯỚC.
        ///
        /// v3f.7.2 (user: "vẫn hình vuông"): ĐỌC SHADER THẬT URP 17.4 (ParticlesUnlit.shader) phát hiện:
        ///   - _SrcBlend/_DstBlend default = One/Zero → new Material mặc định OPAQUE;
        ///   - KHÔNG có keyword _ALPHABLEND_ON (v3f.7.1 bật keyword không tồn tại = vô tác dụng);
        ///   - KHÔNG có lệnh Blend [...] trong pass → set property gì cũng vẫn OPAQUE → hạt LUÔN VUÔNG.
        /// ⇒ BỎ HẲN URP Particles — dùng Sprites/Default: Blend One OneMinusSrcAlpha CỐ ĐỊNH + nhân vertex
        /// color (startColor) + [PerRendererData] _MainTex → KHÔNG cần cấu hình, chắc chắn TRÒN.
        /// </summary>
        // Cache static (v3f.8 — reviewer): 1 texture + 1 material dùng chung cho mọi hệ hạt,
        // không tạo 32×32 Texture2D mới mỗi lần gọi (trước đây 5 lần lúc start = 5 texture).
        private static Material _cachedSoftMaterial;

        internal static Material CreateSoftParticleMaterial()
        {
            if (_cachedSoftMaterial == null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.mainTexture = BuildSoftTexture();
                _cachedSoftMaterial = mat;
            }
            return _cachedSoftMaterial;
        }
    }
}
