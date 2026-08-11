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
    /// Fix 2026-08-12 (user chốt): thay obstacle code-drawn (cube/Ramp) bằng model thiên thạch
    /// **OlegWER High-Poly Asteroid** (gói đã import, giữ LOCAL qua .gitignore — 180MB).
    ///
    /// Việc tool làm (idempotent — chạy lại không đổi):
    /// 1. Tạo prefab `Assets/_Project/Prefabs/Obstacles/AsteroidObstacle.prefab` (nếu chưa có):
    ///    - Root: SphereCollider (isTrigger — Obstacle.Awake tự bật) + component `Obstacle`
    ///    - Con: instance model `fbx.prefab` (Assets/OlegWER/...), scale chuẩn theo chiều cao
    ///      mục tiêu `targetHeight` (1.5 — chặn tàu rõ, không to quá che đường)
    /// 2. Gán prefab mới vào CẢ 2 ObstacleData (Ramp.asset + DynamicBox.asset) qua SerializedObject
    ///    — giữ nguyên obstacleType/spawnWeight (gameplay không đổi, chỉ đổi visual).
    ///
    /// ⚠️ KHÔNG sửa file scene tay (R7.6) — tool chạy trong Unity, thao tác object + asset.
    /// </summary>
    public static class AsteroidObstacleSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // Nguồn model — folder OlegWER GIỮ LOCAL (xem .gitignore) → nếu thiếu, tool báo hướng dẫn
        private const string AsteroidPrefabPath = "Assets/OlegWER/High-Poly_Asteroid/Prefabs/fbx.prefab";

        // Nơi lưu prefab obstacle dựng sẵn (folder COMMIT được — không phụ thuộc OlegWER khi clone)
        private const string OutputPrefabPath = "Assets/_Project/Prefabs/Obstacles/AsteroidObstacle.prefab";

        // 2 ObstacleData hiện tại (scene Game tham chiếu qua GUID của 2 file này)
        private static readonly string[] DataPaths =
        {
            "Assets/_Project/ScriptableObjects/Ramp.asset",
            "Assets/_Project/ScriptableObjects/DynamicBox.asset",
        };

        private const float TargetHeight = 1.5f; // chiều cao obstacle (đơn vị)

        [MenuItem(MenuRoot + "Setup Obstacle = Asteroid (OlegWER thiên thạch)")]
        public static void Setup()
        {
            // 1. Load model asteroid
            var asteroid = AssetDatabase.LoadAssetAtPath<GameObject>(AsteroidPrefabPath);
            if (asteroid == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Asteroid Obstacle",
                    "Không tìm thấy model thiên thạch OlegWER.\n" +
                    $"Path kỳ vọng: {AsteroidPrefabPath}\n\n" +
                    "Gói OlegWER giữ LOCAL (gitignore — GitHub chặn >100MB). " +
                    "Tải lại từ Unity Asset Store rồi chạy lại tool.", "OK");
                return;
            }

            // 2. Tạo prefab obstacle (nếu chưa có)
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null)
            {
                prefab = BuildAsteroidPrefab(asteroid);
                if (prefab == null) return;
            }

            // 3. Gán prefab mới vào 2 ObstacleData
            int updated = 0;
            foreach (string path in DataPaths)
            {
                var data = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);
                if (data == null)
                {
                    Debug.LogWarning($"[Asteroid] Không tìm thấy ObstacleData: {path}");
                    continue;
                }
                if (data.prefab == prefab) continue; // idempotent

                var so = new SerializedObject(data);
                var prop = so.FindProperty("prefab");
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                updated++;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log($"[Asteroid] Xong: prefab={OutputPrefabPath} (đã gán vào {updated} ObstacleData).");
            EditorUtility.DisplayDialog("Void Runner — Asteroid Obstacle",
                $"✅ Đã tạo {OutputPrefabPath}\n" +
                $"và gán vào {updated} ObstacleData (Ramp + DynamicBox → thiên thạch).\n\n" +
                "Giờ mở scene Game → PLAY để xem obstacle là thiên thạch.\n" +
                "Nếu muốn chỉnh to/nhỏ: sửa targetHeight trong tool rồi xóa prefab + chạy lại.", "OK");
        }

        /// <summary>
        /// Dựng prefab obstacle: root (collider trigger + Obstacle) + con là model asteroid scale chuẩn.
        /// Bounds đo thật (model scale 100 → nhỏ hơn) rồi normalize về targetHeight.
        /// </summary>
        private static GameObject BuildAsteroidPrefab(GameObject asteroid)
        {
            var root = new GameObject("AsteroidObstacle");

            // Collider trigger — Player phát hiện qua OnTriggerEnter + TryGetComponent<Obstacle>
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;

            // Marker component
            root.AddComponent<Obstacle>();

            // Con = model asteroid, scale chuẩn theo chiều cao thật
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asteroid);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            Bounds b = GetRenderBounds(model);
            if (b.size.y > 0.001f)
            {
                float s = TargetHeight / b.size.y;
                model.transform.localScale = Vector3.one * s;
            }
            model.transform.localRotation = Quaternion.identity;

            // Collider bán kính theo model sau scale (chặn vùng tàu bay tới)
            Bounds scaled = GetRenderBounds(model);
            float radius = Mathf.Max(scaled.extents.x, scaled.extents.z) * 1.05f;
            col.radius = Mathf.Max(radius, 0.5f);
            col.center = new Vector3(0f, scaled.extents.y, 0f);

            // ⚠️ Capture scale TRƯỚC khi destroy — DestroyImmediate(root) hủy luôn con Model,
            // truy cập model.transform sau đó = MissingReferenceException (bug 2026-08-12, xem CHANGELOG)
            float modelScale = model.transform.localScale.x;

            // Lưu thành prefab asset (folder commit được)
            EnsureFolder(OutputPrefabPath);
            Debug.Log($"[Asteroid] Prefab tạo xong: {OutputPrefabPath} (model bounds={b.size} → scale={modelScale:F2})");
            bool saved = PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);
            Object.DestroyImmediate(root);

            if (!saved)
            {
                Debug.LogError("[Asteroid] Không lưu được prefab: " + OutputPrefabPath);
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
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
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
                string leaf = System.IO.Path.GetFileName(folder);
                if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Obstacles"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Obstacles");
                }
            }
        }
    }
}
#endif
