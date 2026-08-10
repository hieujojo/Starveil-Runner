using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool Giai đoạn 2.5 — Refactor Gameplay (R0.x, chạy sau khi code đã compile):
    ///   Game scene     : Ground kéo dài 400m → 6000m (hết bug "đường chạy hết") + texts tiếng Anh
    ///   MainMenu scene : texts tiếng Anh (HowToPlay / BestScore / Sound) + SoundButton layout thoáng
    /// Chạy: Tools → Void Runner → Refactor: ...
    /// </summary>
    public static class RefactorGameplayTool
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        private const string HowToPlayEnglish =
            "Dodge obstacles with A/D or arrow keys.\n\n" +
            "Collect coins to earn points (higher combo = more points).\n\n" +
            "Power-ups: Shield (immune 3s), Magnet (attracts coins), Slow-mo (slows time).\n\n" +
            "Run far to score big!";

        [MenuItem("Tools/Void Runner/Refactor: Both Scenes (Track + English UI)")]
        public static void RefactorBothScenes()
        {
            FixGameScene();
            FixMainMenuScene();
        }

        [MenuItem("Tools/Void Runner/Refactor: Game Scene (Infinite Track + English)")]
        public static void FixGameSceneMenu()
        {
            FixGameScene();
        }

        [MenuItem("Tools/Void Runner/Refactor: Main Menu (English + Sound Layout)")]
        public static void FixMainMenuSceneMenu()
        {
            FixMainMenuScene();
        }

        private static void FixGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            // 1. Ground tĩnh 400m chỉ đủ chơi ~15-30s rồi "đường hết" → kéo dài 6000m
            // (track thật là tile recycle vô tận — Ground chỉ là nền dưới, không được giới hạn)
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.transform.localPosition = new Vector3(0f, -0.5f, 100f);
                ground.transform.localScale = new Vector3(10f, 1f, 6000f);
                Debug.Log("[Refactor] Ground: 400m → 6000m — track không còn 'chạy hết'.");
            }
            else
            {
                Debug.LogWarning("[Refactor] Không tìm thấy Ground trong Game scene — bỏ qua track.");
            }

            // 2. R0.5: English texts trong gameplay
            RewriteTexts(text =>
            {
                if (text.StartsWith("CHƠI")) return "RETRY";
                if (text.StartsWith("ĐIỂM")) return "SCORE: 0";
                if (text.StartsWith("CAO NHẤT")) return "BEST: 0";
                return null;
            });

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Refactor] Game scene xong — nhớ Ctrl+S.");
        }

        private static void FixMainMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            // 1. R0.5: English texts trong menu
            RewriteTexts(text =>
            {
                if (text.Contains("Né obstacle")) return HowToPlayEnglish;
                if (text.StartsWith("ĐIỂM CAO NHẤT")) return "BEST SCORE: 0";
                if (text.StartsWith("Âm thanh")) return "SOUND: ON";
                return null;
            });

            // 2. R3-6 / R0.8: SoundButton — rộng hơn + text có padding (hết bị thụt vào viền)
            FixSoundButtonLayout();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Refactor] MainMenu scene xong — nhớ Ctrl+S.");
        }

        private static void FixSoundButtonLayout()
        {
            GameObject btn = GameObject.Find("SoundButton");
            if (btn == null)
            {
                Debug.LogWarning("[Refactor] Không tìm thấy SoundButton — bỏ qua layout.");
                return;
            }

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(340f, 76f);
            }

            // Text con: stretch full nút + padding trong — thoáng, không chạm viền
            for (int i = 0; i < btn.transform.childCount; i++)
            {
                RectTransform child = btn.transform.GetChild(i).GetComponent<RectTransform>();
                if (child == null) continue;

                child.anchorMin = new Vector2(0f, 0f);
                child.anchorMax = new Vector2(1f, 1f);
                child.offsetMin = new Vector2(18f, 6f);
                child.offsetMax = new Vector2(-18f, -6f);

                TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 32f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            Debug.Log("[Refactor] SoundButton: 300x66 → 340x76 + text padding 18px.");
        }

        private static void RewriteTexts(Func<string, string> mapper)
        {
            int changed = 0;
            // ⚠️ BẮT BUỘC FindObjectsInactive.Include — GameOverPanel / HowToPlayPanel đang ẩn
            // (m_IsActive: 0) nên FindObjectsByType mặc định (Exclude) BỎ QUA → "Đổi 0 text" (bug 2026-08-11).
            TextMeshProUGUI[] all = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TextMeshProUGUI tmp in all)
            {
                if (tmp == null) continue;
                string next = mapper(tmp.text);
                if (next == null || next == tmp.text) continue;
                tmp.text = next;
                EditorUtility.SetDirty(tmp);
                changed++;
            }
            Debug.Log($"[Refactor] Đổi {changed} text sang tiếng Anh.");
        }
    }
}
