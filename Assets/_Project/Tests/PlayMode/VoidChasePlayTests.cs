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
    /// Play Mode tests cho cơ chế Void 2 NẤC CỐ ĐỊNH (R0.4):
    /// - NẤC 0: Void giữ baseDistance (9m) sau lưng player — không tự tăng tốc.
    /// - Đụng obstacle lần 1 → NẤC 1: Void tiến sát closeDistance (5m).
    /// - Né sạch hết cửa sổ → Void nới về 9m (reset nấc 0).
    /// - Đụng lần 2 trong cửa sổ → Void nuốt → Game Over.
    /// GameManager chỉ dùng để có Instance + State=Playing (disable để chặn Start noise).
    /// </summary>
    public class VoidChasePlayTests
    {
        private GameObject _playerGo;
        private GameObject _voidGo;
        private GameObject _gmGo;
        private VoidChase _void;
        private bool _gameOverRaised;
        private System.Action _gameOverHandler;

        private const float BaseDist = 9f;
        private const float CloseDist = 5f;

        [SetUp]
        public void SetUp()
        {
            // Player (chỉ cần Transform — Void bám theo vị trí)
            _playerGo = new GameObject("TestPlayer");
            _playerGo.transform.position = Vector3.zero;

            // Void: primitive sphere (có Collider — yêu cầu của VoidChase)
            _voidGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _voidGo.name = "TestVoid";
            _voidGo.transform.position = new Vector3(0f, 0f, -BaseDist); // sau lưng player 9m
            _void = _voidGo.AddComponent<VoidChase>();
            _void.Setup(_playerGo.transform);

            // Tăng tốc co/nới + cửa sổ ngắn để test nhanh
            SetPrivateField(_void, "distanceLerpSpeed", 50f);
            SetPrivateField(_void, "relaxWindow", 0.3f);

            // GameManager để có Instance + State=Playing — disable để Start không chạy (tránh StartRun noise).
            // ⚠️ `enabled = false` kích hoạt OnDisable NGAY LẬP TỨC → GameManager.OnDisable set Instance = null
            // → Void gate theo Instance sẽ bị chặn → phải khôi phục Instance + State bằng reflection sau khi disable.
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
            Object.DestroyImmediate(_voidGo);
            Object.DestroyImmediate(_playerGo);
        }

        private float DistanceBehind()
        {
            return _playerGo.transform.position.z - _voidGo.transform.position.z;
        }

        [UnityTest]
        public IEnumerator Stage0_HoldsBaseDistance_WithoutHits()
        {
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(BaseDist, DistanceBehind(), 0.8f,
                "NẤC 0: Void phải giữ ~9m sau lưng player (không tự tăng tốc).");
        }

        [UnityTest]
        public IEnumerator FirstHit_MovesVoidCloser_ToStage1()
        {
            yield return new WaitForSeconds(0.1f);
            GameEvents.RaiseObstacleHit();

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(CloseDist, DistanceBehind(), 0.8f,
                "Đụng lần 1: Void phải tiến sát còn ~5m (nấc 1) nhưng chưa chết.");
            Assert.IsFalse(_gameOverRaised, "Nấc 1 không được Game Over.");
        }

        [UnityTest]
        public IEnumerator CleanRun_AfterRelaxWindow_VoidRelaxesBack()
        {
            GameEvents.RaiseObstacleHit(); // → nấc 1
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(CloseDist, DistanceBehind(), 0.8f, "Tiền đề: đang ở nấc 1.");

            // Không đụng gì nữa → hết cửa sổ (0.3s) → nới về nấc 0
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(BaseDist, DistanceBehind(), 0.8f,
                "Né sạch hết cửa sổ: Void phải nới lại về ~9m (reset nấc 0).");
        }

        [UnityTest]
        public IEnumerator SecondHit_WithinWindow_GameOver()
        {
            GameEvents.RaiseObstacleHit(); // → nấc 1
            yield return new WaitForSeconds(0.1f);

            GameEvents.RaiseObstacleHit(); // đụng lần 2 trong cửa sổ → Void nuốt
            yield return null;

            Assert.IsTrue(_gameOverRaised, "Đụng lần 2 trong cửa sổ phải kích hoạt Game Over.");
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
                "Đụng sau khi Void đã nới lại là 'lần 1 mới' — không được chết.");
            Assert.AreEqual(CloseDist, DistanceBehind(), 0.8f, "Void phải tiến sát lại nấc 1.");
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
