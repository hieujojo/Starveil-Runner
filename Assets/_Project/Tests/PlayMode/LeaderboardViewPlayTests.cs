using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VoidRunner.Core;
using VoidRunner.UI;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Play Mode tests — UI leaderboard (Mục 2 UPGRADE_PLAN):
    /// panel được dựng bằng code (idempotent) + hiện khi Game Over + có đủ NameInput/SubmitButton.
    /// Không test network (UnityWebRequest không chạy trong test) — logic đó nằm ở EditMode tests.
    /// </summary>
    public class LeaderboardViewPlayTests
    {
        private GameObject _canvasGo;
        private GameObject _gameOverPanel;
        private UIManager _uiManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Canvas tối thiểu (UIManager cần Canvas cho leaderboard panel)
            _canvasGo = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Game Over panel (anchor giữa, kích thước như scene thật)
            _gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)_gameOverPanel.transform;
            prt.SetParent(canvas.transform, false);
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(680f, 560f);
            _gameOverPanel.SetActive(false);

            _uiManager = _canvasGo.AddComponent<UIManager>();
            // Gán panel qua reflection (field private — scene thường kéo thả)
            var field = typeof(UIManager).GetField("gameOverPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_uiManager, _gameOverPanel);

            yield return null; // chờ Start() chạy → panel được dựng
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_canvasGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameOver_LeaderboardPanel_IsBuiltAndShown()
        {
            Transform lb = _gameOverPanel.transform.Find("LeaderboardPanel");
            Assert.IsNotNull(lb, "LeaderboardPanel phải được dựng bằng code trong Start().");
            Assert.IsFalse(lb.gameObject.activeSelf, "Panel phải ẩn sẵn trước Game Over.");

            // Các thành phần bắt buộc
            Assert.IsNotNull(lb.Find("List"), "Phải có List (text top 10).");
            Assert.IsNotNull(lb.Find("NameInput"), "Phải có NameInput (nhập tên 3 ký tự).");
            Assert.IsNotNull(lb.Find("SubmitButton"), "Phải có SubmitButton.");

            GameEvents.RaiseGameOver();
            yield return null;

            Assert.IsTrue(lb.gameObject.activeSelf, "Panel phải hiện khi Game Over.");
            Assert.IsTrue(_gameOverPanel.activeSelf, "Game Over panel phải hiện.");
        }

        [UnityTest]
        public IEnumerator GameOver_LeaderboardPanel_IsIdempotent()
        {
            // Gọi Ensure lần nữa — không được tạo panel thứ 2
            GameObject second = LeaderboardView.Ensure(_gameOverPanel.transform);
            Assert.AreEqual(_gameOverPanel.transform.Find("LeaderboardPanel").gameObject, second,
                "Ensure phải trả về panel đã có, không tạo mới.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Restart_HidesLeaderboardPanel()
        {
            GameEvents.RaiseGameOver();
            yield return null;
            Assert.IsTrue(_gameOverPanel.transform.Find("LeaderboardPanel").gameObject.activeSelf);

            GameEvents.RaiseRestart();
            yield return null;

            Assert.IsFalse(_gameOverPanel.transform.Find("LeaderboardPanel").gameObject.activeSelf,
                "Restart phải ẩn panel leaderboard.");
        }
    }
}
