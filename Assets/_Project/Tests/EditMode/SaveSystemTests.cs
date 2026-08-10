using NUnit.Framework;
using UnityEngine;
using VoidRunner.Systems.Save;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Test logic SaveSystem (PlayerPrefs) — chạy nhanh trong EditMode, không cần scene.
    /// Lưu ý: dùng PlayerPrefs.DeleteAll() trong TearDown để không làm bẩn save thật của người chơi.
    /// </summary>
    public class SaveSystemTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [Test]
        public void BestScore_DefaultsToZero()
        {
            Assert.AreEqual(0, SaveSystem.BestScore, "Best score mặc định phải là 0.");
        }

        [Test]
        public void BestScore_OnlyWritesWhenHigher()
        {
            SaveSystem.BestScore = 100;
            Assert.AreEqual(100, SaveSystem.BestScore);

            // Ghi score thấp hơn → KHÔNG được ghi đè
            SaveSystem.BestScore = 50;
            Assert.AreEqual(100, SaveSystem.BestScore, "Best score thấp hơn không được ghi đè.");
        }

        [Test]
        public void BestScore_EqualValueKeepsSame()
        {
            SaveSystem.BestScore = 75;
            SaveSystem.BestScore = 75;
            Assert.AreEqual(75, SaveSystem.BestScore);
        }

        [Test]
        public void Volume_DefaultsToOne()
        {
            Assert.AreEqual(1f, SaveSystem.Volume, 0.001f, "Volume mặc định phải là 1 (full).");
        }

        [Test]
        public void Volume_ClampsToZeroOne()
        {
            SaveSystem.Volume = 2f;
            Assert.AreEqual(1f, SaveSystem.Volume, 0.001f, "Volume > 1 phải bị clamp về 1.");

            SaveSystem.Volume = -0.5f;
            Assert.AreEqual(0f, SaveSystem.Volume, 0.001f, "Volume < 0 phải bị clamp về 0.");
        }

        [Test]
        public void Volume_RoundTripPersists()
        {
            SaveSystem.Volume = 0.35f;
            Assert.AreEqual(0.35f, SaveSystem.Volume, 0.001f, "Volume 0.35 phải đọc lại đúng.");
        }
    }
}
