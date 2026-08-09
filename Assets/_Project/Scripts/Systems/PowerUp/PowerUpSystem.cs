using System;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;
using VoidRunner.Data;

namespace VoidRunner.Systems.PowerUp
{
    /// <summary>
    /// Quản lý trạng thái power-up đang hoạt động.
    /// - Shield: player miễn nhiễm va chạm (PlayerController kiểm tra IsShieldActive trước khi Die)
    /// - Magnet: hút coin trong bán kính về phía player (mỗi frame)
    /// - SlowMo: Time.timeScale giảm tạm thời → Void + mọi thứ chậm lại
    /// Event-driven: OnPowerUpActivated / OnPowerUpExpired — UI subscribe, không coupling.
    /// </summary>
    public class PowerUpSystem : MonoBehaviour
    {
        public static PowerUpSystem Instance { get; private set; }

        [Header("Tham chiếu (auto-resolve nếu để trống)")]
        [SerializeField] private Transform player;

        public event Action<PowerUpType> OnPowerUpActivated;
        public event Action<PowerUpType> OnPowerUpExpired;

        public bool IsShieldActive { get; private set; }
        public bool IsMagnetActive { get; private set; }
        public bool IsSlowMoActive { get; private set; }
        public float SlowMoScale { get; private set; } = 1f;

        private float _shieldTimer;
        private float _magnetTimer;
        private float _slowMoTimer;
        private float _magnetRadius = 6f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (player == null)
            {
                PlayerController controller = FindAnyObjectByType<PlayerController>();
                if (controller != null) player = controller.transform;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnRestart += ResetAll;
            GameEvents.OnGameOver += ResetAll;
        }

        private void OnDisable()
        {
            GameEvents.OnRestart -= ResetAll;
            GameEvents.OnGameOver -= ResetAll;

            // Phải trả timeScale về 1 — nếu SlowMo đang chạy mà component bị tắt/scene unload,
            // không restore sẽ khiến game chậm vĩnh viễn.
            if (IsSlowMoActive) Time.timeScale = 1f;

            if (Instance == this) Instance = null;
        }

        /// <summary>Kích hoạt power-up (gọi từ PowerUpPickup khi player chạm).</summary>
        public void Activate(PowerUpData data)
        {
            if (data == null) return;

            switch (data.powerUpType)
            {
                case PowerUpType.Shield:
                    IsShieldActive = true;
                    _shieldTimer = data.duration;
                    break;
                case PowerUpType.Magnet:
                    IsMagnetActive = true;
                    _magnetTimer = data.duration;
                    _magnetRadius = data.magnetRadius;
                    break;
                case PowerUpType.SlowMo:
                    IsSlowMoActive = true;
                    _slowMoTimer = data.duration;
                    SlowMoScale = data.slowMoScale;
                    Time.timeScale = data.slowMoScale;
                    break;
            }

            OnPowerUpActivated?.Invoke(data.powerUpType);
            GameEvents.RaisePowerUpActivated(data.powerUpType);
        }

        private void Update()
        {
            if (IsShieldActive)
            {
                _shieldTimer -= Time.deltaTime;
                if (_shieldTimer <= 0f) EndPowerUp(PowerUpType.Shield);
            }

            if (IsMagnetActive)
            {
                _magnetTimer -= Time.deltaTime;
                if (_magnetTimer <= 0f) EndPowerUp(PowerUpType.Magnet);
                else PullCoins();
            }

            if (IsSlowMoActive)
            {
                _slowMoTimer -= Time.deltaTime;
                if (_slowMoTimer <= 0f) EndPowerUp(PowerUpType.SlowMo);
            }
        }

        /// <summary>Hút coin trong bán kính về phía player — coin tự hủy khi chạm player.</summary>
        private void PullCoins()
        {
            if (player == null) return;

            // Duyệt registry tĩnh (Coin tự đăng ký khi active) — tránh FindObjectsByType mỗi frame (GC)
            for (int i = Coin.Active.Count - 1; i >= 0; i--)
            {
                Coin coin = Coin.Active[i];
                if (coin == null) continue;
                Vector3 toCoin = coin.transform.position - player.position;
                if (toCoin.magnitude <= _magnetRadius)
                {
                    coin.PullToward(player.position, Time.deltaTime);
                }
            }
        }

        private void EndPowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Shield:
                    IsShieldActive = false;
                    break;
                case PowerUpType.Magnet:
                    IsMagnetActive = false;
                    break;
                case PowerUpType.SlowMo:
                    IsSlowMoActive = false;
                    Time.timeScale = 1f;   // trả lại thời gian thực
                    SlowMoScale = 1f;
                    break;
            }

            OnPowerUpExpired?.Invoke(type);
        }

        private void ResetAll()
        {
            if (IsSlowMoActive) Time.timeScale = 1f;
            IsShieldActive = false;
            IsMagnetActive = false;
            IsSlowMoActive = false;
            SlowMoScale = 1f;
            _shieldTimer = _magnetTimer = _slowMoTimer = 0f;
        }
    }
}
