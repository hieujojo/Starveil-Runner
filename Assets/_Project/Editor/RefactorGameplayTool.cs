using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;

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
            // Road cũng RỘNG HƠN: scale x 10 → 14 (fix "đường quá nhỏ" 2026-08-11)
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.transform.localPosition = new Vector3(0f, -0.5f, 100f);
                ground.transform.localScale = new Vector3(14f, 1f, 6000f);
                Debug.Log("[Refactor] Ground: 400m → 6000m, rộng 10 → 14 — track không còn 'chạy hết' + road rộng.");
            }
            else
            {
                Debug.LogWarning("[Refactor] Không tìm thấy Ground trong Game scene — bỏ qua track.");
            }

            // 1b. Road rộng hơn → laneWidth 2 → 3 cho Player / Obstacle / Pickup (khớp road ±7)
            // + đẩy ambient 2 bên ra ngoài mép road (sideOffset 7 → 9.5)
            WidenRoadAndMoveAmbientOut();

            // 2. R0.5: English texts trong gameplay
            RewriteTexts(text =>
            {
                if (text.StartsWith("CHƠI")) return "RETRY";
                if (text.StartsWith("ĐIỂM")) return "SCORE: 0";
                if (text.StartsWith("CAO NHẤT")) return "BEST: 0";
                return null;
            });

            // 3. HUD layout (fix 2026-08-11 — user test): điểm tràn khung + combo dạt góc trái
            FixHudLayout();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Refactor] Game scene xong — nhớ Ctrl+S.");
        }

        private static void FixHudLayout()
        {
            // ScorePanel: rộng hơn (300 → 360) để chứa số điểm 6 chữ số không tràn
            RectTransform panel = FindRectTransform("ScorePanel");
            if (panel != null) panel.sizeDelta = new Vector2(360f, 90f);

            // ScoreLabel "SCORE": đưa lên sát đỉnh panel, căn giữa, font nhỏ — TÁCH khỏi số điểm
            // (fix 2026-08-11: label đang nằm ngay trên số → "SCORE và số quá sát nhau")
            TextMeshProUGUI label = FindText("ScoreLabel");
            if (label != null)
            {
                RectTransform lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0.5f, 1f);
                lrt.anchorMax = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, -4f);
                lrt.sizeDelta = new Vector2(220f, 24f);
                label.fontSize = 20f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
            }

            // ScoreText: nửa DƯỚI panel (cách label thoải mái), font 40
            TextMeshProUGUI score = FindText("ScoreText");
            if (score != null)
            {
                RectTransform srt = score.rectTransform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(1f, 0.72f);
                srt.anchoredPosition = Vector2.zero;
                srt.sizeDelta = Vector2.zero;
                score.fontSize = 40f;
                score.fontSizeMin = 18f;
                score.alignment = TextAlignmentOptions.Center;
            }

            // ComboText "x2": từ góc trái (0,1)@(34,-150) → xuống DƯỚI panel điểm, căn giữa màn hình
            RectTransform combo = FindRectTransform("ComboText");
            if (combo != null)
            {
                combo.anchorMin = new Vector2(0.5f, 1f);
                combo.anchorMax = new Vector2(0.5f, 1f);
                combo.anchoredPosition = new Vector2(0f, -110f);
                combo.sizeDelta = new Vector2(220f, 50f);
            }
            TextMeshProUGUI comboText = FindText("ComboText");
            if (comboText != null) comboText.fontSize = 36f;

            Debug.Log("[Refactor] HUD: ScorePanel 360x90, label SCORE trên (y=-4) + số dưới (tách rõ), ComboText dưới điểm (0,-110).");
        }

        /// <summary>
        /// Road rộng hơn: laneWidth 2 → 3 (Player/Obstacle/Pickup — khớp road ±7) + ambient đẩy ra
        /// ngoài mép road (sideOffset 7 → 9.5) để prop không nằm trên đường.
        /// </summary>
        private static void WidenRoadAndMoveAmbientOut()
        {
            int changed = 0;

            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (SetSerializedFloat(pc, "laneWidth", 3f)) changed++;

            var om = UnityEngine.Object.FindAnyObjectByType<ObstacleManager>();
            if (SetSerializedFloat(om, "laneWidth", 3f)) changed++;

            var ps = UnityEngine.Object.FindAnyObjectByType<PickupSpawner>();
            if (SetSerializedFloat(ps, "laneWidth", 3f)) changed++;

            var ambient = UnityEngine.Object.FindAnyObjectByType<AmbientScroller>();
            if (SetSerializedFloat(ambient, "sideOffset", 9.5f)) changed++;

            Debug.Log($"[Refactor] Road rộng: laneWidth 2→3 ({changed} component) + ambient sideOffset→9.5.");
        }

        /// <summary>Set field float trên component qua SerializedObject (không phá prefab).</summary>
        private static bool SetSerializedFloat<T>(T comp, string field, float value) where T : Component
        {
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) return false;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            return true;
        }

        private static RectTransform FindRectTransform(string name)
        {
            foreach (RectTransform rt in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rt.name == name) return rt;
            }
            return null;
        }

        private static TextMeshProUGUI FindText(string name)
        {
            foreach (TextMeshProUGUI t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name) return t;
            }
            return null;
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
