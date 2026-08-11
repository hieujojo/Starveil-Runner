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
    /// Fix 2026-08-12 (user chốt: "Fence + Drone Guardian" — không muốn vẽ bằng code, dùng asset thật):
    /// thay asteroid bằng vật cản đúng chất trạm kiểm soát vũ trụ từ 2 gói đã tải:
    ///   - OBSTACLE CHÍNH (Ramp.asset)  = Fence_Long_01   (rào chắn trạm — 3D Scifi Kit Starter Kit, Creepy_Cat)
    ///   - OBSTACLE PHỤ  (DynamicBox)   = Robot_Guardian  (drone bảo vệ — Sci fi Drones, Lukas Bobor)
    ///
    /// Việc tool làm (idempotent — chạy lại an toàn):
    /// 1. Tạo prefab wrapper (nếu chưa có):
    ///    - `Prefabs/Obstacles/BarrierObstacle.prefab`: root SphereCollider (isTrigger) + Obstacle; con = Fence_Long_01
    ///      scale chuẩn theo chiều cao mục tiêu + XOAY 90° quanh Y nếu rào dài theo Z (chắn NGANG lane, không dọc đường)
    ///    - `Prefabs/Obstacles/DroneObstacle.prefab`: root SphereCollider (isTrigger) + Obstacle; con = Robot_Guardian
    ///      scale chuẩn theo chiều cao mục tiêu (nhỏ — drone bay giữa lane)
    /// 2. Gán Barrier vào Ramp.asset, Drone vào DynamicBox.asset qua SerializedObject — giữ obstacleType/spawnWeight.
    ///
    /// Material: KHÔNG cần MaterialFixer ở đây — Obstacle.Awake() tự ép URP/Lit lúc spawn (R3.16, fix 2026-08-12).
    /// ⚠️ KHÔNG sửa file scene tay (R7.6) — tool chạy trong Unity.
    /// </summary>
    public static class SciFiObstacleSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // Nguồn model — 2 gói GIỮ LOCAL qua .gitignore (xem .gitignore mục assets 3rd-party)
        private const string FenceSourcePath = "Assets/Creepy_Cat/3D Scifi Kit Starter Kit_HD/Prefabs/Fences/Fence_Long_01.prefab";
        private const string DroneSourcePath = "Assets/Sci_fi_Drones/Prefabs/Robot_Guardian.prefab";

        // Nơi lưu prefab obstacle (folder COMMIT được — không phụ thuộc gói khi clone)
        private const string BarrierOutputPath = "Assets/_Project/Prefabs/Obstacles/BarrierObstacle.prefab";
        private const string DroneOutputPath = "Assets/_Project/Prefabs/Obstacles/DroneObstacle.prefab";

        private const float BarrierTargetHeight = 1.6f; // rào chắn lane (vừa đủ chặn tàu, không to quá che đường)
        private const float DroneTargetHeight = 1.2f;   // drone bay giữa lane (nhỏ — né dễ, đọc rõ)

        [MenuItem(MenuRoot + "Setup Obstacle = SciFi (Fence + Drone Guardian)")]
        public static void Setup()
        {
            // 1. Load 2 model nguồn
            var fence = AssetDatabase.LoadAssetAtPath<GameObject>(FenceSourcePath);
            if (fence == null)
            {
                EditorUtility.DisplayDialog("Void Runner — SciFi Obstacle",
                    $"Không tìm thấy Fence_Long_01.\nPath kỳ vọng: {FenceSourcePath}\n\n" +
                    "Gói '3D Scifi Kit Starter Kit' (Creepy_Cat) giữ LOCAL qua .gitignore. " +
                    "Tải lại từ Unity Asset Store rồi chạy lại tool.", "OK");
                return;
            }
            var drone = AssetDatabase.LoadAssetAtPath<GameObject>(DroneSourcePath);
            if (drone == null)
            {
                EditorUtility.DisplayDialog("Void Runner — SciFi Obstacle",
                    $"Không tìm thấy Robot_Guardian.\nPath kỳ vọng: {DroneSourcePath}\n\n" +
                    "Gói 'Sci fi Drones' (Lukas Bobor) giữ LOCAL qua .gitignore. " +
                    "Tải lại từ Unity Asset Store rồi chạy lại tool.", "OK");
                return;
            }

            // 2. Tạo 2 prefab wrapper (idempotent — có rồi thì load)
            GameObject barrier = LoadOrBuild(BarrierOutputPath, fence, BarrierTargetHeight, "BarrierObstacle", "Model", true);
            GameObject droneObst = LoadOrBuild(DroneOutputPath, drone, DroneTargetHeight, "DroneObstacle", "Model", false);
            if (barrier == null || droneObst == null) return;

            // 3. Gán vào 2 ObstacleData — Ramp → Barrier, DynamicBox → Drone
            int updated = AssignPrefab("Assets/_Project/ScriptableObjects/Ramp.asset", barrier);
            updated += AssignPrefab("Assets/_Project/ScriptableObjects/DynamicBox.asset", droneObst);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log($"[SciFiObstacle] Xong: Ramp→Barrier, DynamicBox→Drone (gán {updated} ObstacleData).");
            EditorUtility.DisplayDialog("Void Runner — SciFi Obstacle",
                $"✅ Đã tạo:\n  {BarrierOutputPath} (Fence)\n  {DroneOutputPath} (Drone)\n\n" +
                $"và gán vào {updated} ObstacleData (Ramp→rào chắn, DynamicBox→drone).\n\n" +
                "Giờ mở scene Game → PLAY để xem obstacle mới.", "OK");
        }

        private static GameObject LoadOrBuild(string outputPath, GameObject source, float targetHeight,
            string rootName, string childName, bool rotateToBlockLane)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (existing != null) return existing;

            var root = new GameObject(rootName);
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            root.AddComponent<Obstacle>();

            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = childName;
            model.transform.SetParent(root.transform, false);

            Bounds b = GetRenderBounds(model);
            if (b.size.y > 0.001f)
            {
                float s = targetHeight / b.size.y;
                model.transform.localScale = Vector3.one * s;
            }

            // Rào chắn: xoay 90° quanh Y nếu trục dài theo Z → chắn NGANG lane (X)
            if (rotateToBlockLane)
            {
                Bounds scaled = GetRenderBounds(model);
                if (scaled.size.z > scaled.size.x)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }
            else
            {
                model.transform.localRotation = Quaternion.identity;
            }

            // ⚠️ Vô hiệu collider CON (góp ý reviewer 2026-08-12 v3f): prefab Scifi Kit có thể kèm
            // MeshCollider/BoxCollider solid ở con → ĐẨY VẬT LÝ player thay vì OnTriggerEnter trên
            // root (pattern giống EnemyChase.BuildEnemyVisual — chỉ root trigger nuốt).
            foreach (var childCol in model.GetComponentsInChildren<Collider>())
            {
                if (childCol == null) continue;
                childCol.enabled = false;
            }

            // Collider theo model sau scale — center theo bounds thật (final.center.y chính xác hơn
            // extents.y nếu pivot model không nằm ở chân — góp ý reviewer)
            Bounds final = GetRenderBounds(model);
            float radius = Mathf.Max(final.extents.x, final.extents.z) * 1.05f;
            col.radius = Mathf.Max(radius, 0.5f);
            col.center = new Vector3(0f, final.center.y, 0f);

            EnsureFolder(outputPath);
            bool saved = PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            Object.DestroyImmediate(root); // ⚠️ hủy cả cây con — KHÔNG truy cập model sau dòng này (R7.10)
            if (!saved)
            {
                Debug.LogError("[SciFiObstacle] Không lưu được prefab: " + outputPath);
                return null;
            }
            Debug.Log($"[SciFiObstacle] Prefab tạo xong: {outputPath}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        }

        private static int AssignPrefab(string dataPath, GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<ObstacleData>(dataPath);
            if (data == null)
            {
                Debug.LogWarning($"[SciFiObstacle] Không tìm thấy ObstacleData: {dataPath}");
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

        private static void EnsureFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Obstacles"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Obstacles");
        }
    }
}
#endif
