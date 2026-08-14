using UnityEngine;

namespace VoidRunner.Systems.Leaderboard
{
    /// <summary>
    /// Cấu hình leaderboard online (ScriptableObject) — tách data khỏi logic, theo style ObstacleData.
    /// Tạo asset: Assets → Create → VoidRunner → Leaderboard Config.
    /// Đặt URL + anon key từ Supabase (Settings → API) → game gửi/nhận điểm thật.
    /// Nếu CHƯA có asset hoặc URL rỗng → LeaderboardService chạy OFFLINE (game vẫn chơi bình thường,
    /// chỉ không có top 10 online).
    /// </summary>
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "VoidRunner/Leaderboard Config")]
    public class LeaderboardConfig : ScriptableObject
    {
        [Header("Supabase (Settings → API)")]
        [Tooltip("Project URL, ví dụ https://abcd.supabase.co — để trống = OFFLINE")]
        public string url;

        [Tooltip("anon public key — chỉ dùng public read/insert (Row Level Security đã mở)")]
        public string anonKey;

        [Header("Bảng")]
        [Tooltip("Tên bảng trong Supabase (mặc định leaderboard)")]
        public string tableName = "leaderboard";

        [Tooltip("Số dòng top hiển thị")]
        [Range(3, 50)]
        public int maxEntries = 10;

        /// <summary>Có cấu hình online hợp lệ không?</summary>
        public bool IsOnline => !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(anonKey);
    }
}
