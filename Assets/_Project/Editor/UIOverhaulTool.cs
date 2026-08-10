#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// TOOL TỔNG — ép chuẩn toàn bộ UI theo tông "hư không" (tím đen + cyan + vàng glow),
    /// đồng bộ cả 2 scene. Chạy lại an toàn (idempotent — chỉ set giá trị chuẩn, không tạo mới).
    ///
    /// Tông chuẩn:
    ///   Nền panel  : tím đen #0E0620
    ///   Nút chính  : cyan #3EC1FF (Play / Retry)
    ///   Nút phụ    : tím #7B4BFF (Menu / HowToPlay) — nút nhỏ tím đậm (Sound)
    ///   Score      : vàng glow #FFE640 · Combo: cam #FF8C42 · Label: cyan nhạt
    ///
    /// Lý do tồn tại: 2 builder cũ (HUDUIBuilder/MainMenuUIBuilder) tạo tông BLUE cũ +
    /// chạy sai thứ tự → scene giữ màu trắng/xanh dương, lệch tông tím hư không hiện tại.
    /// </summary>
    public static class UIOverhaulTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // ===== Tông màu chuẩn =====
        private static readonly Color PanelBg = new Color(0.055f, 0.024f, 0.125f, 0.92f);      // #0E0620
        private static readonly Color PanelBgDeep = new Color(0.04f, 0.018f, 0.1f, 0.96f);     // GameOver đậm hơn
        private static readonly Color BtnPrimary = new Color(0.24f, 0.76f, 1f, 1f);            // cyan #3EC1FF
        private static readonly Color BtnSecondary = new Color(0.48f, 0.29f, 1f, 1f);          // tím #7B4BFF
        private static readonly Color BtnDark = new Color(0.29f, 0.17f, 0.54f, 1f);            // tím đậm (Sound)
        private static readonly Color ScoreGold = new Color(1f, 0.9f, 0.25f, 1f);              // #FFE640
        private static readonly Color ComboOrange = new Color(1f, 0.55f, 0.26f, 1f);           // #FF8C42
        private static readonly Color LabelCyan = new Color(0.7f, 0.85f, 1f, 0.95f);
        private static readonly Color BestGold = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color DangerRed = new Color(1f, 0.38f, 0.38f, 1f);             // GAME OVER
        private static readonly Color OutlineVoid = new Color(0.1f, 0f, 0.3f, 1f);             // viền tím hư không

        [MenuItem(MenuRoot + "Overhaul UI (ép chuẩn tông hư không — 2 scene)")]
        public static void Overhaul()
        {
            var scene = SceneManager.GetActiveScene();
            int total = 0;

            if (scene.name == "Game")
            {
                total = OverhaulGame(scene);
            }
            else if (scene.name == "MainMenu")
            {
                total = OverhaulMainMenu(scene);
            }
            else
            {
                EditorUtility.DisplayDialog("Void Runner — UI Overhaul",
                    "Tool chạy trên scene 'Game' hoặc 'MainMenu'.\nMở 1 trong 2 scene rồi chạy lại.", "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[VoidRunner] UI Overhaul xong ({scene.name}): {total} phần tử ép chuẩn.");
            EditorUtility.DisplayDialog("Void Runner — UI Overhaul",
                $"Đã ép chuẩn {total} phần tử trên scene '{scene.name}'.\n\n" +
                "Lặp lại cho scene còn lại (mở scene khác → chạy tool).\nNhớ Ctrl+S lưu cả 2 scene.", "OK");
        }

        // ==================================================================
        // GAME SCENE
        // ==================================================================
        private static int OverhaulGame(Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas == null) continue;

                // --- ScorePanel: nền tím đen, đưa lên GIỮA-ĐỈNH màn hình + vẽ trên cùng ---
                // (fix 2026-08-11: trước đây ở góc trái bị các element khác che → người chơi không thấy điểm)
                var scorePanel = FindTransform(canvas.transform, "ScorePanel");
                if (scorePanel != null)
                {
                    scorePanel.SetAsLastSibling(); // vẽ trên cùng — không element nào che được
                    var prt = scorePanel.GetComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0.5f, 1f);
                    prt.anchorMax = new Vector2(0.5f, 1f);
                    prt.pivot = new Vector2(0.5f, 0.5f);
                    prt.anchoredPosition = new Vector2(0f, -45f);
                    prt.sizeDelta = new Vector2(300f, 90f);
                    if (TryGetImage(scorePanel, out var spImg))
                    {
                        spImg.color = PanelBg;
                        n++;
                    }
                }

                // --- ScoreText: căn giữa + vàng glow + viền tím ---
                var scoreText = FindTransform(canvas.transform, "ScoreText");
                if (scoreText != null && scoreText.TryGetComponent<TextMeshProUGUI>(out var st))
                {
                    st.fontSize = 58;
                    st.fontStyle = FontStyles.Bold;
                    st.color = ScoreGold;
                    st.alignment = TextAlignmentOptions.Center;
                    var rt = scoreText.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    EnsureShadow(scoreText.gameObject, new Vector2(2f, -2f));
                    EnsureOutline(scoreText.gameObject, OutlineVoid);
                    n++;
                }

                // --- CoinIcon: vàng (giữ) ---
                // --- ComboText: cam nổi bật ---
                var combo = FindTransform(canvas.transform, "ComboText");
                if (combo != null && combo.TryGetComponent<TextMeshProUGUI>(out var ct))
                {
                    ct.fontSize = 40;
                    ct.fontStyle = FontStyles.Bold;
                    ct.color = ComboOrange;
                    n++;
                }

                // --- GameOverPanel: nền tím đen đậm ---
                var goPanel = FindTransform(canvas.transform, "GameOverPanel");
                if (goPanel != null && TryGetImage(goPanel, out var goImg))
                {
                    goImg.color = PanelBgDeep;
                    n++;
                }

                // --- Title "GAME OVER": đỏ phát sáng + viền ---
                var title = FindTransform(goPanel, "TitleText");
                if (title != null && title.TryGetComponent<TextMeshProUGUI>(out var tt))
                {
                    tt.fontSize = 76;
                    tt.color = DangerRed;
                    EnsureShadow(title.gameObject, new Vector2(3f, -3f));
                    EnsureOutline(title.gameObject, OutlineVoid);
                    n++;
                }

                // --- FinalScore: trắng to rõ ---
                var finalScore = FindTransform(goPanel, "FinalScoreText");
                if (finalScore != null && finalScore.TryGetComponent<TextMeshProUGUI>(out var ft))
                {
                    ft.fontSize = 46;
                    ft.fontStyle = FontStyles.Bold;
                    ft.color = Color.white;
                    EnsureShadow(finalScore.gameObject, new Vector2(2f, -2f));
                    n++;
                }

                // --- BestScore: vàng ---
                var best = FindTransform(goPanel, "BestScoreText");
                if (best != null && best.TryGetComponent<TextMeshProUGUI>(out var bt))
                {
                    bt.color = BestGold;
                    n++;
                }

                // --- RetryButton: cyan (chính) ---
                var retry = FindTransform(goPanel, "RetryButton");
                if (retry != null && TryGetImage(retry, out var retryImg))
                {
                    retryImg.color = BtnPrimary;
                    n++;
                }

                // --- MenuButton: tím (phụ) ---
                var menu = FindTransform(goPanel, "MenuButton");
                if (menu != null && TryGetImage(menu, out var menuImg))
                {
                    menuImg.color = BtnSecondary;
                    n++;
                }
            }
            return n;
        }

        // ==================================================================
        // MAIN MENU SCENE
        // ==================================================================
        private static int OverhaulMainMenu(Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas == null) continue;

                // --- Background: tím đen đậm ---
                var bg = FindTransform(canvas.transform, "Background");
                if (bg != null && TryGetImage(bg, out var bgImg))
                {
                    bgImg.color = new Color(0.03f, 0.014f, 0.075f, 1f);
                    n++;
                }

                // --- TitleText: trắng sáng + shadow (chính) ---
                var title = FindTransform(canvas.transform, "TitleText");
                if (title != null && title.TryGetComponent<TextMeshProUGUI>(out var tt))
                {
                    tt.color = Color.white;
                    tt.fontStyle = FontStyles.Bold;
                    EnsureShadow(title.gameObject, new Vector2(4f, -4f));
                    n++;
                }

                // --- TitleGlow: cyan mờ phía sau (hiệu ứng glow, đè lệch nhẹ) ---
                var glow = FindTransform(canvas.transform, "TitleGlow");
                if (glow != null && glow.TryGetComponent<TextMeshProUGUI>(out var gt))
                {
                    gt.color = new Color(0.2f, 0.6f, 1f, 0.35f);
                    n++;
                }

                // --- PlayButton: cyan (chính) ---
                var play = FindTransform(canvas.transform, "PlayButton");
                if (play != null && TryGetImage(play, out var playImg))
                {
                    playImg.color = BtnPrimary;
                    n++;
                }

                // --- HowToPlayButton: tím (phụ) ---
                var how = FindTransform(canvas.transform, "HowToPlayButton");
                if (how != null && TryGetImage(how, out var howImg))
                {
                    howImg.color = BtnSecondary;
                    n++;
                }

                // --- SoundButton: tím đậm (phụ nhỏ) ---
                var sound = FindTransform(canvas.transform, "SoundButton");
                if (sound != null && TryGetImage(sound, out var soundImg))
                {
                    soundImg.color = BtnDark;
                    n++;
                }

                // --- BestScoreText: vàng, đưa lên y=-230 (an toàn màn hình nhỏ) ---
                var best = FindTransform(canvas.transform, "BestScoreText");
                if (best != null && best.TryGetComponent<TextMeshProUGUI>(out var bt))
                {
                    bt.color = BestGold;
                    bt.fontSize = 34;
                    var rt = best.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, -230f);
                    n++;
                }

                // --- HowToPlayPanel: tím đen + text sáng ---
                var panel = FindTransform(canvas.transform, "HowToPlayPanel");
                if (panel != null && TryGetImage(panel, out var panelImg))
                {
                    panelImg.color = PanelBg;
                    n++;
                }
                var panelText = FindTransform(panel, "HowToPlayText");
                if (panelText != null && panelText.TryGetComponent<TextMeshProUGUI>(out var pt))
                {
                    pt.color = new Color(0.9f, 0.93f, 1f, 1f);
                    n++;
                }
            }
            return n;
        }

        // ==================================================================
        // HELPERS
        // ==================================================================
        private static Transform FindTransform(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var deep = FindTransform(child, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private static bool TryGetImage(Transform t, out Image img)
        {
            img = t != null ? t.GetComponent<Image>() : null;
            return img != null;
        }

        private static void EnsureShadow(GameObject go, Vector2 dist)
        {
            if (go.GetComponent<Shadow>() == null)
            {
                var sh = go.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
                sh.effectDistance = dist;
            }
        }

        private static void EnsureOutline(GameObject go, Color color)
        {
            if (go.GetComponent<Outline>() == null)
            {
                var ol = go.AddComponent<Outline>();
                ol.effectColor = color;
                ol.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        [MenuItem(MenuRoot + "Overhaul UI (ép chuẩn tông hư không — 2 scene)", true)]
        private static bool ValidateOverhaul()
        {
            return Object.FindAnyObjectByType<Canvas>() != null;
        }
    }
}
#endif
