using NUnit.Framework;
using UnityEngine;
using VoidRunner.Systems.Score;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Test logic ScoreSystem — gọi trực tiếp public method AddScore (không cần scene).
    /// </summary>
    public class ScoreSystemEditTests
    {
        private GameObject _go;
        private ScoreSystem _system;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ScoreSystemTest");
            _system = _go.AddComponent<ScoreSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Score_StartsAtZero()
        {
            Assert.AreEqual(0, _system.Score);
        }

        [Test]
        public void Multiplier_StartsAtOne()
        {
            Assert.AreEqual(1, _system.Multiplier);
        }

        [Test]
        public void AddScore_Positive_AddsToScore()
        {
            _system.AddScore(10);
            Assert.AreEqual(10, _system.Score);
        }

        [Test]
        public void AddScore_ZeroOrNegative_Ignored()
        {
            _system.AddScore(0);
            _system.AddScore(-5);
            Assert.AreEqual(0, _system.Score, "Raw <= 0 không được cộng.");
        }

        [Test]
        public void AddScore_RaisesOnScoreChanged()
        {
            int last = -1;
            _system.OnScoreChanged += s => last = s;

            _system.AddScore(25);
            Assert.AreEqual(25, last, "Event OnScoreChanged phải phát với giá trị mới.");
        }
    }
}
