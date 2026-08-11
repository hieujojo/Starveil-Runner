using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoidRunner.Tests
{
    /// <summary>
    /// Test căn giữa lane của obstacle (v3f.5 — user: "đọc log — drone chưa đúng, lệch +1.5m").
    /// Obstacle.Awake phải tự căn giữa model con theo renderer bounds (CenterModelOnLane) —
    /// mô phỏng model 3rd-party pivot lệch (Robot_Guardian) bằng cube đặt lệch +1.5m.
    /// Tự chứa (không phụ thuộc prefab/package — asmdef PlayMode không reference UnityEditor).
    /// </summary>
    public class ObstacleCenterPlayTests
    {
        [UnityTest]
        public IEnumerator Obstacle_CentersModelOnLane_WhenPivotOffset()
        {
            float laneX = 4.5f;

            // 1. Root obstacle tại lane x=4.5 (giống ObstacleManager spawn: Instantiate rồi set localPosition)
            var root = new GameObject("TestObstacle");
            root.transform.position = new Vector3(laneX, 0.5f, 10f);
            root.AddComponent<SphereCollider>();

            // 2. Model con lệch +1.5m sang phải (mô phỏng pivot lệch của Robot_Guardian)
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Model"; // CenterModelOnLane tìm con đúng tên này
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(1.5f, 0f, 0f);

            // 3. Thêm Obstacle SAU khi đã có con "Model" — Awake chạy ngay → tự căn giữa
            root.AddComponent<Obstacle>();
            yield return null;

            // 4. Verify: renderer bounds center.x ≈ lane (self-heal đã bù pivot)
            Bounds b = default;
            bool has = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (has) b.Encapsulate(r.bounds);
                else { b = r.bounds; has = true; }
            }
            Assert.IsTrue(has, "Model phải có renderer.");

            Assert.AreEqual(laneX, b.center.x, 0.15f,
                $"Model phải nằm đúng tâm lane {laneX} (CenterModelOnLane) — thực tế {b.center.x:F2}.");

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Obstacle_ModelWithoutRenderer_DoesNotBreak()
        {
            // Model KHÔNG có renderer → CenterModelOnLane phải bỏ qua an toàn (fix bug reviewer v3f.5:
            // fallback bounds zero-size → IsValid() == false, không văng model về gốc).
            var root = new GameObject("TestObstacle");
            root.transform.position = new Vector3(0f, 0.5f, 10f);
            root.AddComponent<SphereCollider>();

            var emptyModel = new GameObject("Model");
            emptyModel.transform.SetParent(root.transform, false);
            emptyModel.transform.localPosition = new Vector3(3f, 0f, 0f);

            root.AddComponent<Obstacle>();
            yield return null;

            Assert.AreEqual(3f, emptyModel.transform.localPosition.x, 0.001f,
                "Model không renderer phải KHÔNG bị dịch chuyển (guard IsValid).");

            Object.Destroy(root);
        }
    }
}
