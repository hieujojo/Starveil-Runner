using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace VoidRunner.Systems.Leaderboard
{
    /// <summary>
    /// Gửi/nhận điểm leaderboard qua Supabase REST API (UnityWebRequest — không cần package nào).
    /// THUẦN — không phụ thuộc UI; UI (LeaderboardView) chỉ gọi + nhận callback.
    ///
    /// Offline-first: chưa có config / không có mạng / server lỗi → callback false + Debug.Log 1 lần,
    /// game vẫn chơi bình thường (nguyên tắc: leaderboard KHÔNG bao giờ chặn gameplay).
    ///
    /// API:
    ///   POST {url}/rest/v1/{table}            — thêm 1 dòng {name, score}
    ///   GET  {url}/rest/v1/{table}?order=score.desc&limit={n} — top n
    /// Header: apikey + Authorization: Bearer (theo chuẩn Supabase anon key).
    /// </summary>
    public static class LeaderboardService
    {
        private static LeaderboardConfig _cachedConfig;
        private static bool _warnedOffline;

        /// <summary>
        /// Lấy config: ưu tiên asset trong Resources ("LeaderboardConfig").
        /// Nếu chưa tạo asset → tự tạo instance mặc định (URL rỗng = OFFLINE) — không crash, không đòi setup.
        /// </summary>
        public static LeaderboardConfig GetConfig()
        {
            if (_cachedConfig != null) return _cachedConfig;
            _cachedConfig = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
            if (_cachedConfig == null)
            {
                _cachedConfig = ScriptableObject.CreateInstance<LeaderboardConfig>();
                if (!_warnedOffline)
                {
                    _warnedOffline = true;
                    Debug.LogWarning("[Leaderboard] Chưa có asset LeaderboardConfig (tạo: Assets → Create → VoidRunner → Leaderboard Config, đặt trong thư mục Resources) — chạy OFFLINE, không hiện top 10 online.");
                }
            }
            return _cachedConfig;
        }

        /// <summary>Gửi điểm lên server. onComplete(true) = thành công.</summary>
        public static void SubmitScore(string playerName, int score, Action<bool> onComplete = null)
        {
            var config = GetConfig();
            if (!config.IsOnline) { onComplete?.Invoke(false); return; }

            string url = $"{config.url}/rest/v1/{config.tableName}";
            var entry = new LeaderboardEntry(SanitizeName(playerName), score);
            string json = JsonUtility.ToJson(entry);

            var req = UnityWebRequest.Post(url, json, "application/json");
            AddAuth(req, config);

            req.SendWebRequest().completed += _ =>
            {
                bool ok = req.result == UnityWebRequest.Result.Success;
                if (!ok) Debug.LogWarning($"[Leaderboard] Gửi điểm thất bại ({req.result}): {req.error}");
                req.Dispose();
                onComplete?.Invoke(ok);
            };
        }

        /// <summary>Lấy top n điểm. onComplete(entries) — rỗng nếu lỗi/offline.</summary>
        public static void FetchTopScores(Action<List<LeaderboardEntry>> onComplete)
        {
            var config = GetConfig();
            if (!config.IsOnline) { onComplete?.Invoke(new List<LeaderboardEntry>()); return; }

            string url = $"{config.url}/rest/v1/{config.tableName}?order=score.desc&limit={config.maxEntries}";
            var req = UnityWebRequest.Get(url);
            AddAuth(req, config);

            req.SendWebRequest().completed += _ =>
            {
                var list = new List<LeaderboardEntry>();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    list = ParseTopScores(req.downloadHandler != null ? req.downloadHandler.text : null);
                }
                else
                {
                    Debug.LogWarning($"[Leaderboard] Tải top 10 thất bại ({req.result}): {req.error}");
                }
                req.Dispose();
                onComplete?.Invoke(list);
            };
        }

        /// <summary>
        /// Parse JSON mảng từ Supabase (vd `[{"name":"ACE","score":120},...]`) thành danh sách.
        /// JsonUtility không parse top-level array → bọc {"data": [...]} rồi FromJson.
        /// Thuần — test được.
        /// </summary>
        [Serializable]
        private class ResponseWrapper { public List<LeaderboardEntry> data; }

        public static List<LeaderboardEntry> ParseTopScores(string json)
        {
            var result = new List<LeaderboardEntry>();
            if (string.IsNullOrWhiteSpace(json)) return result;

            try
            {
                var wrapper = JsonUtility.FromJson<ResponseWrapper>("{\"data\":" + json + "}");
                if (wrapper != null && wrapper.data != null) result = wrapper.data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Parse JSON lỗi: {e.Message}");
            }
            return result;
        }

        /// <summary>Chuẩn hóa tên arcade: in hoa, tối đa 3 ký tự, chỉ chữ số + chữ cái. Thuần — test được.</summary>
        public static string SanitizeName(string raw)
        {
            if (raw == null) return "AAA";
            var cleaned = new System.Text.StringBuilder();
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c)) cleaned.Append(char.ToUpperInvariant(c));
                if (cleaned.Length >= 3) break;
            }
            string result = cleaned.ToString();
            return result.Length > 0 ? result.PadRight(3, 'A') : "AAA";
        }

        private static void AddAuth(UnityWebRequest req, LeaderboardConfig config)
        {
            req.SetRequestHeader("apikey", config.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + config.anonKey);
            req.SetRequestHeader("Prefer", "return=minimal"); // không cần response body khi insert
        }
    }
}
