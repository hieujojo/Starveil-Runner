using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidRunner.Core;
using VoidRunner.Core.World;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Play Mode tests cho cơ chế Enemy 2 NẤC CỐ ĐỊNH (R0.4) — FIX 2026-08-12 v3f.3:
    /// - NẤC 0: Enemy giữ baseDistance (7m) sau lưng player — camera cách player 10m nên
    ///   7m = bọ ở 7m TRƯỚC camera → nhìn thấy cả con (16m cũ = 6m SAU camera → không bao giờ thấy).
    /// - Đụng obstacle lần 1 → NẤC 1: Enemy tiến sát closeDistance (5.5m) + vỗ cánh nhanh hơn.
    /// - Né sạch hết cửa sổ → Enemy nới về 5m (reset nấc 0).
    /// - Đụng lần 2 trong cửa sổ → Enemy LAO TỚI BẮT (atack) → sau catchDelay → Game Over.
    /// GameManager chỉ dùng để có Instance + State=Playing (disable để chặn Start noise).
    /// </summary>
    public class EnemyChasePlayTests
    {
        private GameObject _playerGo;
        private GameObject _enemyGo;
        private GameObject _gmGo;
        private EnemyChase _enemy;
        private bool _gameOverRaised;
        private System.Action _gameOverHandler;

        private const float BaseDist = 7f;
        private const float CloseDist = 5.5f;

        [SetUp]
        public void SetUp()
        {
            // Player (chỉ cần Transform — Enemy bám theo vị trí)
            _playerGo = new GameObject("TestPlayer");
            _playerGo.transform.position = Vector3.zero;

            // Enemy: primitive sphere (có Collider — yêu cầu của EnemyChase)
            _enemyGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _enemyGo.name = "TestEnemy";
            _enemyGo.transform.position = new Vector3(0f, 0f, -BaseDist); // sau lưng player
            _enemy = _enemyGo.AddComponent<EnemyChase>();
            _enemy.Setup(_playerGo.transform);

            // Tăng tốc co/nới + cửa sổ ngắn + catch nhanh để test nhanh
            SetPrivateField(_enemy, "distanceLerpSpeed", 50f);
            SetPrivateField(_enemy, "relaxWindow", 0.3f);
            SetPrivateField(_enemy, "catchDelay", 0.05f);

            // GameManager để có Instance + State=Playing — disable để Start không chạy (tránh StartRun noise).
            // ⚠️ `enabled = false` kích hoạt OnDisable NGAY LẬP TỨC → GameManager.OnDisable set Instance = null
            // → Enemy gate theo Instance sẽ bị chặn → phải khôi phục Instance + State bằng reflection sau khi disable.
            _gmGo = new GameObject("TestGameManager");
            GameManager gm = _gmGo.AddComponent<GameManager>();
            gm.enabled = false;
            FieldInfo instanceField = typeof(GameManager).GetField("<Instance>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Static);
            instanceField.SetValue(null, gm);
            PropertyInfo stateProp = typeof(GameManager).GetProperty("State");
            stateProp.GetSetMethod(true).Invoke(gm, new object[] { GameState.Playing });

            _gameOverRaised = false;
            _gameOverHandler = () => _gameOverRaised = true;
            GameEvents.OnGameOver += _gameOverHandler;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnGameOver -= _gameOverHandler;
            // DestroyImmediate: GameManager.Instance phải được clear NGAY giữa các test
            // (Destroy deferred có thể giữ Instance trỏ tới object cũ → test sau gán nhầm State)
            Object.DestroyImmediate(_gmGo);
            Object.DestroyImmediate(_enemyGo);
            Object.DestroyImmediate(_playerGo);
        }

        private float DistanceBehind()
        {
            return _playerGo.transform.position.z - _enemyGo.transform.position.z;
        }

        [UnityTest]
        public IEnumerator Stage0_HoldsBaseDistance_WithoutHits()
        {
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(BaseDist, DistanceBehind(), 0.8f,
                "NẤC 0: Enemy phải giữ ~7m sau lưng player (không tự tăng tốc).");
        }

        [UnityTest]
        public IEnumerator FirstHit_MovesEnemyCloser_ToStage1()
        {
            yield return new WaitForSeconds(0.1f);
            GameEvents.RaiseObstacleHit();

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(CloseDist, DistanceBehind(), 0.8f,
                "Đụng lần 1: Enemy phải tiến sát còn ~5.5m (nấc 1) nhưng chưa chết.");
            Assert.IsFalse(_gameOverRaised, "Nấc 1 không được Game Over.");
        }

        [UnityTest]
        public IEnumerator CleanRun_AfterRelaxWindow_EnemyRelaxesBack()
        {
            GameEvents.RaiseObstacleHit(); // → nấc 1
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(CloseDist, DistanceBehind(), 0.8f, "Tiền đề: đang ở nấc 1.");

            // Không đụng gì nữa → hết cửa sổ (0.3s) → nới về nấc 0
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(BaseDist, DistanceBehind(), 0.8f,
                "Né sạch hết cửa sổ: Enemy phải nới lại về ~7m (reset nấc 0).");
        }

        [UnityTest]
        public IEnumerator SecondHit_WithinWindow_GameOver()
        {
            GameEvents.RaiseObstacleHit(); // → nấc 1
            yield return new WaitForSeconds(0.1f);

            GameEvents.RaiseObstacleHit(); // đụng lần 2 trong cửa sổ → Enemy BẮT (atack)
            // CatchAndKill: lunge 0.3s + catchDelay 0.05s → Game Over sau ~0.4s
            yield return new WaitForSeconds(0.6f);

            Assert.IsTrue(_gameOverRaised,
                "Đụng lần 2 trong cửa sổ phải kích hoạt cảnh bắt rồi Game Over.");
        }

        [UnityTest]
        public IEnumerator Hit_AfterRelaxed_IsFreshStage0_NoDeath()
        {
            GameEvents.RaiseObstacleHit(); // → nấc 1
            yield return new WaitForSeconds(0.8f); // hết cửa sổ → nới về nấc 0

            Assert.IsFalse(_gameOverRaised, "Tiền đề: chưa chết.");

            GameEvents.RaiseObstacleHit(); // lần đụng mới sau khi đã nới lại → nấc 1 mới
            yield return new WaitForSeconds(0.1f);

            Assert.IsFalse(_gameOverRaised,
                "Đụng sau khi Enemy đã nới lại là 'lần 1 mới' — không được chết.");
            Assert.AreEqual(CloseDist, DistanceBehind(), 0.8f, "Enemy phải tiến sát lại nấc 1.");
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Không tìm thấy field {name}.");
            field.SetValue(target, value);
        }
    }
}
