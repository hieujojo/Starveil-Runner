using System.Collections.Generic;
using NUnit.Framework;
using VoidRunner.Systems.Leaderboard;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Test logic thuần của LeaderboardService — không cần scene/network:
    /// SanitizeName (tên arcade 3 ký tự) + ParseTopScores (JSON mảng từ Supabase).
    /// </summary>
    public class LeaderboardServiceEditTests
    {
        [Test]
        public void SanitizeName_Empty_ReturnsAAA()
        {
            Assert.AreEqual("AAA", LeaderboardService.SanitizeName(""));
        }

        [Test]
        public void SanitizeName_Null_ReturnsAAA()
        {
            Assert.AreEqual("AAA", LeaderboardService.SanitizeName(null));
        }

        [Test]
        public void SanitizeName_LongName_TruncatedTo3()
        {
            Assert.AreEqual("ACE", LeaderboardService.SanitizeName("ace pilot"));
        }

        [Test]
        public void SanitizeName_Lowercase_Uppercased()
        {
            Assert.AreEqual("VIP", LeaderboardService.SanitizeName("vip"));
        }

        [Test]
        public void SanitizeName_ShortName_PaddedWithA()
        {
            Assert.AreEqual("XAA", LeaderboardService.SanitizeName("x"));
        }

        [Test]
        public void SanitizeName_StripsSpecialChars()
        {
            Assert.AreEqual("A1B", LeaderboardService.SanitizeName("a@1b!c"));
        }

        [Test]
        public void ParseTopScores_Empty_ReturnsEmptyList()
        {
            Assert.AreEqual(0, LeaderboardService.ParseTopScores("").Count);
            Assert.AreEqual(0, LeaderboardService.ParseTopScores(null).Count);
        }

        [Test]
        public void ParseTopScores_ValidArray_ParsesAll()
        {
            string json = "[{\"name\":\"ACE\",\"score\":120},{\"name\":\"VIP\",\"score\":95}]";
            List<LeaderboardEntry> list = LeaderboardService.ParseTopScores(json);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("ACE", list[0].name);
            Assert.AreEqual(120, list[0].score);
            Assert.AreEqual("VIP", list[1].name);
            Assert.AreEqual(95, list[1].score);
        }

        [Test]
        public void ParseTopScores_InvalidJson_ReturnsEmpty()
        {
            Assert.AreEqual(0, LeaderboardService.ParseTopScores("not json at all").Count);
        }
    }
}
