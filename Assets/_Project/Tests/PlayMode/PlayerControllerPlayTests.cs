using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidRunner.Core.Player;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Play Mode tests cho PlayerController — lane clamping + event lane change.
    /// </summary>
    public class PlayerControllerPlayTests
    {
        private GameObject _go;
        private PlayerController _controller;
        private Rigidbody _rb;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestPlayer", typeof(Rigidbody), typeof(PlayerController));
            _controller = _go.GetComponent<PlayerController>();
            _rb = _go.GetComponent<Rigidbody>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator MoveLeft_ThenRight_DoesNotThrow()
        {
            _controller.MoveLeft();
            _controller.MoveRight();
            yield return null;
            Assert.IsFalse(_controller.IsDead, "Đổi lane không được làm player chết.");
        }

        [UnityTest]
        public IEnumerator MoveLeft_ManyTimes_ClampsToLeftEdge()
        {
            // Lane count = 3, lane index: 0 (trái) | 1 (giữa) | 2 (phải)
            for (int i = 0; i < 10; i++)
            {
                _controller.MoveLeft();
            }
            yield return new WaitForSeconds(0.3f);

            // Vị trí x không được vượt quá lane trái cùng (bị clamp)
            float laneWidth = 2f;
            float expectedLeftEdge = -(laneWidth); // (0 - (3-1)*0.5) * 2 = -2
            Assert.GreaterOrEqual(_rb.position.x, expectedLeftEdge - 0.01f,
                "Player không được vượt quá lane trái cùng.");
        }

        [UnityTest]
        public IEnumerator MoveRight_ManyTimes_ClampsToRightEdge()
        {
            for (int i = 0; i < 10; i++)
            {
                _controller.MoveRight();
            }
            yield return new WaitForSeconds(0.3f);

            float laneWidth = 2f;
            float expectedRightEdge = laneWidth; // (2 - 1) * 2 = 2
            Assert.LessOrEqual(_rb.position.x, expectedRightEdge + 0.01f,
                "Player không được vượt quá lane phải cùng.");
        }
    }
}
