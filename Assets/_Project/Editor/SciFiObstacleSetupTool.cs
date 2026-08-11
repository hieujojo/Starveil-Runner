#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core.World;
using VoidRunner.Data;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// FIX 2026-08-12 v3f.5 (user: "cổng trông như cái cổng chứ đâu phải bãi mìn → XÓA HẲN;
    /// chỉ drone + vài hiệu ứng là đủ cho trò chơi vũ trụ"): obstacle DUY NHẤT = drone Robot_Guardian
    /// (Sci fi Drones). Đã bỏ hẳn rào/cổng Fence_Long_01 (BarrierObstacle.prefab + BarrierWarning.mat).
    ///
    /// Tool làm (idempotent — chạy lại an toàn):
    /// 1. Tạo prefab wrapper `Prefabs/Obstacles/DroneObstacle.prefab` (nếu chưa có): root SphereCollider
    ///    (isTrigger) + Obstacle; con = Robot_Guardian, scale chuẩn chiều cao, bù pivot theo bounds thật.
    /// 2. Gán drone vào CẢ 2 ObstacleData (Ramp + DynamicBox) — giữ nguyên list gán trong scene
    ///    (không sửa scene tay — R7.6); spawnWeight từng entry vẫn phân biệt mật độ.
    ///
    /// Hiệu ứng drone (đèn đỏ + hạt năng lượng + lơ lửng) do ObstacleFX tạo RUNTIME lúc spawn —
    /// KHÔNG nướng vào prefab (R3.1: material tạo bằng code lúc edit-time không serialize →
    /// objectReference {fileID: 0} → null → MÀU TÍM — bug đã gặp ở rào chắn).
    /// </summary>
    public static class SciFiObstacleSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // Nguồn model — gói GIỮ LOCAL qua .gitignore (xem .gitignore mục assets 3rd-party)
        private const string DroneSourcePath = "Assets/Sci_fi_Drones/Prefabs/Robot_Guardian.prefab";

        // Nơi lưu prefab obstacle (folder COMMIT được — không phụ thuộc gói khi clone)
        private const string DroneOutputPath = "Assets/_Project/Prefabs/Obstacles/DroneObstacle.prefab";

        private const float DroneTargetHeight = 1.2f; // drone bay giữa lane (vừa né được, đọc rõ)

        // Asset CỔNG/RÀO cũ đã bỏ (v3f.5) — Rebuild dọn dẹp nếu còn sót từ bản trước
        private const string OldBarrierPrefabPath = "Assets/_Project/Prefabs/Obstacles/BarrierObstacle.prefab";
        private const string OldBarrierMatPath = "Assets/_Project/Materials/Obstacles/BarrierWarning.mat";

        [MenuItem(MenuRoot + "Setup Obstacle = Drone (Robot_Guardian)")]
        public static void Setup()
        {
            SetupCore();
        }

        [MenuItem(MenuRoot + "Rebuild Drone Obstacle (ép kích thước + dọn asset cũ)")]
        public static void Rebuild()
        {
            // Xóa prefab cũ → dựng lại sạch (fix kích thước/pivot)
            AssetDatabase.DeleteAsset(DroneOutputPath);
            // Dọn asset cổng/rào cũ nếu còn sót từ bản v3f.4 trở về trước
            AssetDatabase.DeleteAsset(OldBarrierPrefabPath);
            AssetDatabase.DeleteAsset(OldBarrierMatPath);
            SetupCore();
        }

        private static void SetupCore()
        {
            var drone = AssetDatabase.LoadAssetAtPath<GameObject>(DroneSourcePath);
            if (drone == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Drone Obstacle",
                    $"Không tìm thấy Robot_Guardian.\nPath kỳ vọng: {DroneSourcePath}\n\n" +
                    "Gói 'Sci fi Drones' (Lukas Bobor) giữ LOCAL qua .gitignore. " +
                    "Tải lại từ Unity Asset Store rồi chạy lại tool.", "OK");
                return;
            }

            // Tạo prefab wrapper (idempotent — có rồi thì load)
            GameObject droneObst = LoadOrBuild(DroneOutputPath, drone, DroneTargetHeight);
            if (droneObst == null) return;

            // Gán drone vào CẢ 2 ObstacleData — list scene giữ nguyên (Ramp + DynamicBox đều = drone,
            // spawnWeight từng entry vẫn điều mật độ). Không sửa scene tay (R7.6).
            int updated = AssignPrefab("Assets/_Project/ScriptableObjects/Ramp.asset", droneObst);
            updated += AssignPrefab("Assets/_Project/ScriptableObjects/DynamicBox.asset", droneObst);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log($"[DroneObstacle] Xong: Ramp + DynamicBox → Drone (gán {updated} ObstacleData).");
            EditorUtility.DisplayDialog("Void Runner — Drone Obstacle",
                $"✅ Đã tạo: {DroneOutputPath} (Robot_Guardian)\n\n" +
                $"và gán vào {updated} ObstacleData (Ramp + DynamicBox → drone).\n\n" +
                "Giờ mở scene Game → PLAY — obstacle duy nhất là drone (đã có hiệu ứng đèn đỏ + hạt năng lượng).", "OK");
        }

        private static GameObject LoadOrBuild(string outputPath, GameObject source, float targetHeight)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (existing != null) return existing;

            var root = new GameObject("DroneObstacle");
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            root.AddComponent<Obstacle>();

            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            Bounds b = GetRenderBounds(model);
            if (b.size.y > 0.001f)
            {
                float s = targetHeight / b.size.y;
                model.transform.localScale = Vector3.one * s;
            }

            // ⚠️ Vô hiệu collider CON (góp ý reviewer 2026-08-12 v3f): prefab có thể kèm MeshCollider
            // solid → ĐẨY VẬT LÝ player thay vì OnTriggerEnter trên root (pattern EnemyChase.BuildEnemyVisual).
            foreach (var childCol in model.GetComponentsInChildren<Collider>())
            {
                if (childCol == null) continue;
                childCol.enabled = false;
            }

            // Collider theo model sau scale — center theo bounds thật
            Bounds final = GetRenderBounds(model);
            float radius = Mathf.Max(final.extents.x, final.extents.z) * 1.05f;
            col.radius = Mathf.Max(radius, 0.5f);
            col.center = new Vector3(0f, final.center.y, 0f);

            // Bù PIVOT LỆCH theo bounds (Robot_Guardian mesh lệch ~+1.5) — mesh nằm ĐÚNG TÂM lane.
            // Lưu ý: Obstacle.Awake còn tự căn giữa lại theo bounds thật lúc spawn (v3f.5, self-heal).
            model.transform.localPosition = new Vector3(-final.center.x, -final.center.y, -final.center.z);

            EnsureFolder(outputPath);
            Debug.Log($"[DroneObstacle] Prefab {outputPath}: bounds={final.size.ToString("F2")} pivotOffset={model.transform.localPosition.ToString("F2")}");
            bool saved = PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            Object.DestroyImmediate(root); // ⚠️ hủy cả cây con — KHÔNG truy cập model sau dòng này (R7.10)
            if (!saved)
            {
                Debug.LogError("[DroneObstacle] Không lưu được prefab: " + outputPath);
                return null;
            }
            Debug.Log($"[DroneObstacle] Prefab tạo xong: {outputPath}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        }

        private static int AssignPrefab(string dataPath, GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<ObstacleData>(dataPath);
            if (data == null)
            {
                Debug.LogWarning($"[DroneObstacle] Không tìm thấy ObstacleData: {dataPath}");
                return 0;
            }
            if (data.prefab == prefab) return 0; // idempotent

            var so = new SerializedObject(data);
            var prop = so.FindProperty("prefab");
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return 1;
        }

        private static Bounds GetRenderBounds(GameObject go)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool has = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (has) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; has = true; }
            }
            return has ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        /// <summary>Đảm bảo folder chứa assetPath tồn tại — tạo từng cấp (generic: Prefabs, Materials, ...).</summary>
        private static void EnsureFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            string current = "Assets";
            string[] parts = folder.Substring("Assets/".Length).Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                string parent = current;
                current = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, parts[i]);
                }
            }
        }
    }
}
#endif
