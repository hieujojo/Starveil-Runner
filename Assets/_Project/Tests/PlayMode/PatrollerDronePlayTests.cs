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
    /// Play Mode tests cho PatrollerDrone (UPGRADE_PLAN Mục 6 — enemy mới: drone tuần tra ngang lane):
    /// - Luôn giữ khoảng cách cố định PHÍA TRƯỚC player (aheadDistance) — không tụt sau.
    /// - Lắc ngang giữa 2 lane theo hàm cos (x thay đổi theo thời gian, quỹ đạo mượt).
    /// - Chỉ hoạt động khi GameManager.State == Playing.
    /// GameManager dựng bằng reflection (giống EnemyChasePlayTests) — disable để chặn Start noise.
    /// </summary>
    public class PatrollerDronePlayTests
    {
        private GameObject _playerGo;
        private GameObject _droneGo;
        private GameObject _gmGo;
        private PatrollerDrone _drone;

        private const float Ahead = 16f;
        private const float LaneWidth = 2f;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("TestPlayer");
            _playerGo.transform.position = Vector3.zero;

            // Drone: sphere (có Collider — yêu cầu Obstacle/PatrollerDrone)
            _droneGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _droneGo.name = "TestPatroller";
            _droneGo.transform.position = new Vector3(0f, 0f, Ahead);
            _drone = _droneGo.AddComponent<PatrollerDrone>();
            _drone.Setup(_playerGo.transform);

            // GameManager để có Instance + State=Playing (pattern EnemyChasePlayTests)
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

            // Fire event để set _active = true — giống game thật (drone spawn trong Playing state)
            GameEvents.RaiseGameStarted();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gmGo);
            Object.DestroyImmediate(_droneGo);
            Object.DestroyImmediate(_playerGo);
        }

        [UnityTest]
        public IEnumerator Keeps_AheadOfPlayer()
        {
            // Player tiến về trước → drone phải bám theo, luôn cách aheadDistance
            _playerGo.transform.position = new Vector3(0f, 0f, 30f);
            yield return new WaitForSeconds(0.2f);

            float zDiff = _droneGo.transform.position.z - _playerGo.transform.position.z;
            Assert.AreEqual(Ahead, zDiff, 0.5f,
                "Drone phải luôn ở đúng aheadDistance phía trước player.");
        }

        [UnityTest]
        public IEnumerator Patrols_Laterally_BetweenLanes()
        {
            // Lắc ngang: x phải THAY ĐỔI theo thời gian (không đứng yên)
            float x0 = _droneGo.transform.position.x;
            yield return new WaitForSeconds(1.2f); // ~1/3 chu kỳ 3.2s — đủ để x đổi rõ
            float x1 = _droneGo.transform.position.x;

            Assert.Greater(Mathf.Abs(x1 - x0), 0.3f,
                "Drone phải lắc ngang (x thay đổi) trong 1.2s.");
            // Giới hạn trong dải lane [min..max] (0..2 → x ∈ [-2, +2])
            Assert.LessOrEqual(Mathf.Abs(x1), LaneWidth + 0.01f,
                "Drone không được lắc vượt quá lane ngoài cùng.");
        }

        [UnityTest]
        public IEnumerator DoesNotMove_WhenNotPlaying()
        {
            // State = Menu (không phải Playing) → drone đứng yên
            GameManager gm = _gmGo.GetComponent<GameManager>();
            FieldInfo stateField = typeof(GameManager).GetField("<State>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            stateField.SetValue(gm, GameState.Menu);

            Vector3 before = _droneGo.transform.position;
            yield return new WaitForSeconds(0.3f);
            Vector3 after = _droneGo.transform.position;

            Assert.AreEqual(before, after,
                "Không ở trạng thái Playing thì drone phải đứng yên.");
        }
    }
}
