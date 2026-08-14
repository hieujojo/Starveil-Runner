#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core.World;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// UPGRADE_PLAN Mục 6 — tool dựng prefab PatrollerDrone (enemy mới: drone tuần tra ngang lane).
    ///
    /// Prefab = bản sao cấu trúc DroneObstacle.prefab (Robot_Guardian + SphereCollider trigger + Obstacle)
    /// + component PatrollerDrone (logic lắc ngang giữa 3 lane phía trước player). Idempotent — chạy
    /// lại không tạo bản mới.
    ///
    /// Yêu cầu: DroneObstacle.prefab đã tồn tại (chạy tool "Obstacle = Drone" trước).
    /// Sau khi tạo prefab → mở scene Game → gắn PatrollerDronePrefab vào tham chiếu (nếu muốn)
    /// hoặc để component tự find (self-heal qua AssetDatabase — như EnemyCatalog).
    /// </summary>
    public static class PatrollerDroneSetupTool
    {
        private const string MenuRoot = "Tools/Starveil Runner/Setup/";

        // Nguồn: prefab obstacle drone đã có (tool "Obstacle = Drone" tạo)
        private const string DroneObstaclePath = "Assets/_Project/Prefabs/Obstacles/DroneObstacle.prefab";
        // Nơi lưu prefab patroller
        private const string OutputPath = "Assets/_Project/Prefabs/Obstacles/PatrollerDrone.prefab";

        [MenuItem(MenuRoot + "Patroller Drone (enemy mới — tuần tra ngang lane)")]
        public static void Setup()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DroneObstaclePath);
            if (source == null)
            {
                EditorUtility.DisplayDialog("Starveil Runner — Patroller Drone",
                    $"Không tìm thấy {DroneObstaclePath}.\n\n" +
                    "Chạy TRƯỚC tool 'Setup → Obstacle = Drone (Robot_Guardian)' để tạo DroneObstacle, rồi chạy lại tool này.",
                    "OK");
                return;
            }

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Starveil Runner — Patroller Drone",
                    $"✅ Đã có: {OutputPath}\n\nPrefab patroller drone đã tồn tại — không tạo lại (idempotent).",
                    "OK");
                return;
            }

            // Dựng từ DroneObstacle (đã có sẵn collider trigger + Obstacle + Model chuẩn tâm lane)
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            root.name = "PatrollerDrone";

            // Gán component logic tuần tra — đã có Obstacle từ nguồn, thêm PatrollerDrone
            var patrol = root.GetComponent<PatrollerDrone>();
            if (patrol == null) patrol = root.AddComponent<PatrollerDrone>();

            EnsureFolder(OutputPath);
            bool saved = PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
            Object.DestroyImmediate(root);
            if (!saved)
            {
                Debug.LogError("[PatrollerDrone] Không lưu được prefab: " + OutputPath);
                return;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[PatrollerDrone] Đã tạo: {OutputPath}");

            EditorUtility.DisplayDialog("Starveil Runner — Patroller Drone",
                $"✅ Đã tạo: {OutputPath}\n\n" +
                "Gắn vào game: mở scene Game → chọn GameObject chứa EnemyChase (hoặc Managers) → " +
                "thêm component PatrollerSpawner (spawn theo tile) HOẶC kéo prefab vào scene thủ công.\n\n" +
                "Chi tiết tích hợp: xem UPGRADE_PLAN Mục 6.", "OK");
        }

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
