#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool editor: dựng ambient 2 bên đường bằng mô hình Kenney Space Kit (FBX).
    /// Tìm GameObject chứa GameManager/Player → tạo/đồng bộ GameObject "Ambient" với
    /// component AmbientScroller (gán propPrefabs + player). Idempotent — chạy lại không nhân đôi.
    /// </summary>
    public static class AmbientSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // Model Space Kit phù hợp làm trụ/kiến trúc 2 bên đường (đã verify có trong kit)
        private static readonly string[] PreferredModels =
        {
            "corridor_wall", "corridor", "chimney_detailed", "chimney",
            "satelliteDish", "structure", "pipe_ringHigh", "monorail_trackSupport",
            "gate_simple", "supports_high", "rock_crystals", "craft_speederB",
        };

        // Model Station Kit dự phòng (đã verify có trong kit)
        private static readonly string[] StationModels =
        {
            "wall-pillar", "structure", "structure-panel", "pipe-ring",
            "container-tall", "floor-panel", "stairs", "table",
        };

        [MenuItem(MenuRoot + "Setup Ambient in Game Scene")]
        public static void SetupAmbient()
        {
            GameObject host = FindHostObject();
            if (host == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Ambient", "Không tìm thấy GameManager/Player trong scene mở.", "OK");
                return;
            }

            List<GameObject> prefabs = CollectPropPrefabs();
            if (prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("Void Runner — Ambient", "Không tìm thấy FBX model Space Kit trong Art/kenney_space-kit.", "OK");
                return;
            }

            AmbientScroller scroller = EnsureScroller(host);
            if (scroller == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Ambient", "Không gắn được AmbientScroller.", "OK");
                return;
            }

            // Gán field qua SerializedObject — ÉP GHI ĐÈ toàn bộ giá trị layout chuẩn
            // (tránh scene giữ giá trị CŨ của bản tool trước → đồ đạc vẫn lung tung)
            var so = new SerializedObject(scroller);
            var playerProp = so.FindProperty("player");
            var prefabList = so.FindProperty("propPrefabs");
            SetField(so, "sideOffset", 6f);
            SetField(so, "spacing", 9f);
            SetField(so, "countPerSide", 10);
            SetField(so, "recycleDistance", 18f);
            SetField(so, "jitter", 0.15f);
            SetField(so, "maxRotY", 20f);
            SetField(so, "scaleVariation", 0.1f);

            if (playerProp != null && playerProp.objectReferenceValue == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) playerProp.objectReferenceValue = player.transform;
            }
            if (prefabList != null)
            {
                prefabList.arraySize = prefabs.Count;
                for (int i = 0; i < prefabs.Count; i++) prefabList.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }
            so.ApplyModifiedProperties();

            scroller.BuildProps();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[VoidRunner] Ambient OK: {prefabs.Count} prop types, 2 bên × {countPerSide(scroller)} prop.");
            EditorUtility.DisplayDialog("Void Runner — Ambient",
                $"Đã dựng ambient 2 bên đường với {prefabs.Count} loại mô hình Space Kit.\nNhớ Ctrl+S lưu scene.", "OK");
        }

        /// <summary>Ép ghi đè 1 field serialized của AmbientScroller (luôn áp giá trị chuẩn).</summary>
        private static void SetField(SerializedObject so, string name, object value)
        {
            var prop = so.FindProperty(name);
            if (prop == null) return;

            switch (value)
            {
                case float f when prop.propertyType == SerializedPropertyType.Float:
                    prop.floatValue = f;
                    break;
                case int i when prop.propertyType == SerializedPropertyType.Integer:
                    prop.intValue = i;
                    break;
            }
        }

        private static int countPerSide(AmbientScroller s)
        {
            var so = new SerializedObject(s);
            var p = so.FindProperty("countPerSide");
            return p != null ? p.intValue : 8;
        }

        private static GameObject FindHostObject()
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) return gm.gameObject;

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) return player.gameObject;

            return null;
        }

        private static AmbientScroller EnsureScroller(GameObject host)
        {
            var existing = host.GetComponentInChildren<AmbientScroller>(true);
            if (existing != null) return existing;

            var ambient = new GameObject("Ambient");
            ambient.transform.SetParent(host.transform, false);
            return ambient.AddComponent<AmbientScroller>();
        }

        private static List<GameObject> CollectPropPrefabs()
        {
            var result = new List<GameObject>();

            // Tìm model CHÍNH XÁC (tên file = tên model, không fuzzy — tránh "corridor" khớp 14 model)
            foreach (string model in PreferredModels)
            {
                var go = LoadExactModel("Assets/_Project/Art/kenney_space-kit/Models/FBX format", model);
                if (go != null && !result.Contains(go)) result.Add(go);
            }

            // Station Kit FBX (dự phòng)
            foreach (string model in StationModels)
            {
                var go = LoadExactModel("Assets/_Project/Art/kenney_space-station-kit/Models/FBX format", model);
                if (go != null && !result.Contains(go)) result.Add(go);
            }

            return result.Take(8).ToList(); // giới hạn 8 loại cho đa dạng mà không nặng
        }

        private static GameObject LoadExactModel(string folder, string modelName)
        {
            string path = $"{folder}/{modelName}.fbx";
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        [MenuItem(MenuRoot + "Setup Ambient in Game Scene", true)]
        private static bool ValidateAmbient()
        {
            return FindHostObject() != null;
        }

        private static T FindAnyObjectByType<T>() where T : Object
        {
            return Object.FindAnyObjectByType<T>();
        }
    }
}
#endif
