using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Data;
using VoidRunner.Systems.Save;

namespace VoidRunner.Systems.Audio
{
    /// <summary>
    /// Quản lý âm thanh toàn cục: 1 AudioSource BGM (loop) + 1 AudioSource SFX (one-shot).
    /// Tồn tại xuyên scene (DontDestroyOnLoad), volume đọc/ghi qua SaveSystem.
    /// Không coupling: chỉ lắng nghe GameEvents — hệ thống khác chỉ cần Raise event.
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Nguồn phát (tự tạo nếu để trống)")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Âm thanh — kéo file .ogg/.wav vào")]
        [SerializeField] private AudioClip bgmClip;          // Nhạc nền loop (cần tải — folder Audio/Music)
        [SerializeField] private AudioClip coinSfx;          // Nhặt coin
        [SerializeField] private AudioClip deathSfx;         // Đụng obstacle / chết
        [SerializeField] private AudioClip powerUpSfx;       // Ăn power-up
        [SerializeField] private AudioClip laneSwitchSfx;    // Chuyển lane
        [SerializeField] private AudioClip gameStartSfx;     // Bắt đầu chạy

        [Header("Cài đặt")]
        [SerializeField, Range(0f, 1f)] private float sfxPitchRandom = 0.1f; // Biến thiên pitch nhẹ cho game feel

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
        }

        private void OnEnable()
        {
            GameEvents.OnCoinCollected += PlayCoinSfx;
            GameEvents.OnObstacleHit += PlayDeathSfx;
            GameEvents.OnPowerUpActivated += PlayPowerUpSfx;
            GameEvents.OnLaneChanged += PlayLaneSwitchSfx;
            GameEvents.OnGameStarted += PlayGameStartSfx;
        }

        private void OnDisable()
        {
            GameEvents.OnCoinCollected -= PlayCoinSfx;
            GameEvents.OnObstacleHit -= PlayDeathSfx;
            GameEvents.OnPowerUpActivated -= PlayPowerUpSfx;
            GameEvents.OnLaneChanged -= PlayLaneSwitchSfx;
            GameEvents.OnGameStarted -= PlayGameStartSfx;
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            ApplyVolume(SaveSystem.Volume);
            PlayBgm();
        }

        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        /// <summary>Phát SFX một lần (one-shot) — gọi được từ mọi nơi, clip null thì bỏ qua.</summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = 1f + Random.Range(-sfxPitchRandom, sfxPitchRandom);
            sfxSource.PlayOneShot(clip, volumeScale);
        }

        /// <summary>Đổi volume tổng (0..1) — lưu qua SaveSystem + áp ngay.</summary>
        public void SetVolume(float volume)
        {
            SaveSystem.Volume = volume;
            ApplyVolume(volume);
        }

        private void ApplyVolume(float volume)
        {
            if (bgmSource != null) bgmSource.volume = volume;
            if (sfxSource != null) sfxSource.volume = volume;
        }

        private void PlayBgm()
        {
            if (bgmClip == null || bgmSource == null) return;
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }

        // ---- Event handlers (đổi tên hàm thay lambda để unsubscribe cân bằng) ----

        private void PlayCoinSfx(int _) => PlaySfx(coinSfx, 0.8f);
        private void PlayDeathSfx() => PlaySfx(deathSfx);
        private void PlayPowerUpSfx(PowerUpType _) => PlaySfx(powerUpSfx);
        private void PlayLaneSwitchSfx(int _) => PlaySfx(laneSwitchSfx, 0.5f);
        private void PlayGameStartSfx() => PlaySfx(gameStartSfx, 0.9f);
    }
}
