using System;

namespace VoidRunner.Systems.Leaderboard
{
    /// <summary>
    /// Một dòng điểm trên leaderboard (đồng bộ JSON với Supabase bảng `leaderboard`).
    /// `name` = 3 ký tự kiểu arcade · `score` = điểm khi Game Over.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;

        public LeaderboardEntry() { }

        public LeaderboardEntry(string playerName, int playerScore)
        {
            name = playerName;
            score = playerScore;
        }
    }
}
