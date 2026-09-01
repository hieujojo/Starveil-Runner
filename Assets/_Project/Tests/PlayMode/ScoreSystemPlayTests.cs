using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Systems.Score;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Play Mode tests — cần Update/Start chạy nên phải chạy trong Play Mode (Test Runner → PlayMode tab).
    /// Tự dựng scene test bằng code: player + score system.
    /// </summary>
    public class ScoreSystemPlayTests
    {
        private GameObject _playerGo;
        private ScoreSystem _system;

        private GameObject _gmGo;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Player có Rigidbody (yêu cầu của PlayerController) — đủ để ScoreSystem resolve player
            _playerGo = new GameObject("TestPlayer", typeof(Rigidbody), typeof(PlayerController));
            var player = _playerGo.transform;
            player.position = new Vector3(0f, 0f, 0f);

            _system = new GameObject("TestScoreSystem").AddComponent<ScoreSystem>();

            // GameManager để PlayerController di chuyển (cần State=Playing)
            _gmGo = new GameObject("TestGameManager");
            GameManager gm = _gmGo.AddComponent<GameManager>();
            gm.enabled = false;
            FieldInfo instanceField = typeof(GameManager).GetField("<Instance>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Static);
            instanceField.SetValue(null, gm);
            // Dùng backing field trực tiếp — an toàn hơn property reflection trong Unity 6
            FieldInfo stateField = typeof(GameManager).GetField("<State>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            stateField.SetValue(gm, GameState.Playing);

            yield return null; // chờ 1 frame để Start() chạy → _active = true
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // BUG FIX: GameManager singleton persist giữa tests
            FieldInfo instanceField = typeof(GameManager).GetField("<Instance>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Static);
            instanceField.SetValue(null, null);

            Object.DestroyImmediate(_gmGo);
            Object.Destroy(_playerGo);
            Object.Destroy(_system.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Combo_IncreasesAfterInterval()
        {
            Assert.AreEqual(1, _system.Multiplier);
            // comboInterval = 5s; chờ đủ 1 chu kỳ + 1 frame dư
            yield return new WaitForSeconds(5.3f);
            Assert.AreEqual(2, _system.Multiplier, "Combo phải tăng lên ×2 sau 5s sống liên tục.");
        }

        [UnityTest]
        public IEnumerator Combo_ClampsAtMax()
        {
            // Chờ đủ 4 lần tăng (5s mỗi lần) → tối đa ×5
            yield return new WaitForSeconds(21f);
            Assert.AreEqual(5, _system.Multiplier, "Combo tối đa phải là ×5.");
            Assert.LessOrEqual(_system.Multiplier, 5, "Multiplier không được vượt maxCombo.");
        }

        [UnityTest]
        public IEnumerator ObstacleHit_ResetsCombo()
        {
            yield return new WaitForSeconds(5.3f);
            Assert.AreEqual(2, _system.Multiplier, "Tiền đề: combo đang ×2.");

            GameEvents.RaiseObstacleHit();

            Assert.AreEqual(1, _system.Multiplier, "Dính obstacle phải reset combo về ×1.");
        }

        [UnityTest]
        public IEnumerator CoinCollection_AddsToScore()
        {
            int scoreBefore = _system.Score;
            GameEvents.RaiseCoinCollected(1);
            yield return null; // UnityTest bắt buộc có yield (nhường frame cho engine)
            Assert.Greater(_system.Score, scoreBefore, "Nhặt coin phải tăng score.");
        }

        [UnityTest]
        public IEnumerator Score_IncreasesWithDistance()
        {
            // Player di chuyển tới (do PlayerController FixedUpdate) → score tăng theo deltaZ
            int scoreBefore = _system.Score;
            yield return new WaitForSeconds(1f);
            Assert.Greater(_system.Score, scoreBefore, "Chạy 1s phải có score theo khoảng cách.");
        }
    }
}
