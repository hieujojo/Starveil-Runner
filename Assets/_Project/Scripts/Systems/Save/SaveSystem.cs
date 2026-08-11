using UnityEngine;

namespace VoidRunner.Systems.Save
{
    /// <summary>
    /// Lưu/load dữ liệu bền vững qua PlayerPrefs — best score + volume.
    /// Static class: gọi trực tiếp SaveSystem.BestScore / SaveSystem.Volume, không cần instance.
    /// </summary>
    public static class SaveSystem
    {
        private const string BestScoreKey = "VoidRunner_BestScore";
        private const string VolumeKey = "VoidRunner_Volume";
        private const string SelectedShipKey = "VoidRunner_SelectedShip";

        /// <summary>Best score đã đạt (chỉ ghi khi cao hơn — tự động lưu).</summary>
        public static int BestScore
        {
            get => PlayerPrefs.GetInt(BestScoreKey, 0);
            set
            {
                if (value > BestScore)
                {
                    PlayerPrefs.SetInt(BestScoreKey, value);
                    PlayerPrefs.Save();
                }
            }
        }

        /// <summary>Volume tổng (0..1) — AudioManager sẽ đọc khi được xây dựng.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Tàu đã chọn (index vào PlayerController.shipPrefabs — 0 = SF Fighter, 1 = Sparrow).</summary>
        public static int SelectedShip
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(SelectedShipKey, 0), 0, 3);
            set
            {
                PlayerPrefs.SetInt(SelectedShipKey, Mathf.Clamp(value, 0, 3));
                PlayerPrefs.Save();
            }
        }
    }
}
