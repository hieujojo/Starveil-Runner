using NUnit.Framework;
using VoidRunner.Core;
using VoidRunner.Data;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Test event hub tĩnh GameEvents — mọi hệ thống giao tiếp qua đây, phải hoạt động đúng.
    /// </summary>
    public class GameEventsTests
    {
        [Test]
        public void RaiseCoinCollected_CallsSubscriberWithCount()
        {
            int received = -1;
            GameEvents.OnCoinCollected += count => received = count;

            GameEvents.RaiseCoinCollected(3);

            Assert.AreEqual(3, received, "OnCoinCollected phải nhận đúng số coin.");
        }

        [Test]
        public void RaiseCoinCollectedAt_CallsSubscriberWithPosition()
        {
            UnityEngine.Vector3 received = UnityEngine.Vector3.zero;
            GameEvents.OnCoinCollectedAt += pos => received = pos;

            var pos = new UnityEngine.Vector3(1f, 2f, 3f);
            GameEvents.RaiseCoinCollectedAt(pos);

            Assert.AreEqual(pos, received);
        }

        [Test]
        public void RaiseObstacleHit_CallsSubscriber()
        {
            bool hit = false;
            GameEvents.OnObstacleHit += () => hit = true;

            GameEvents.RaiseObstacleHit();

            Assert.IsTrue(hit, "OnObstacleHit phải được gọi.");
        }

        [Test]
        public void RaisePowerUpActivated_CallsSubscriberWithType()
        {
            PowerUpType received = PowerUpType.SlowMo;
            GameEvents.OnPowerUpActivated += type => received = type;

            GameEvents.RaisePowerUpActivated(PowerUpType.Shield);

            Assert.AreEqual(PowerUpType.Shield, received);
        }

        [Test]
        public void RaiseRestart_CallsSubscriber()
        {
            bool restarted = false;
            GameEvents.OnRestart += () => restarted = true;

            GameEvents.RaiseRestart();

            Assert.IsTrue(restarted);
        }

    }
}
