#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core.World;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Task B (2026-08-11): gán 3 quái vật (Monster Skin1 / Flying Beetle / Fantasy Spider) vào
    /// VoidChase.monsterPrefabs — Void = quái vật đuổi theo (random 1 mỗi lần vào game).
    /// Idempotent: có sẵn 3 phần tử thì thôi. Chạy trên scene Game.
    /// </summary>
    public static class VoidMonsterSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        private static readonly string[] MonsterPaths =
        {
            "Assets/Monster/Prefab/Monster Skin1.prefab",
            "Assets/Flying Beetle/prefab/Flying beetle.prefab",
            "Assets/fantasySpider/spider_myOldOne.FBX",
        };

        [MenuItem(MenuRoot + "Setup Void Monster (Task B — 3 quái vật)")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Void Runner — Void Monster",
                    $"Scene đang mở là '{scene.name}'. Mở scene Game rồi chạy lại.", "OK");
                return;
            }

            var voidChase = Object.FindAnyObjectByType<VoidChase>();
            if (voidChase == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Void Monster", "Không tìm thấy VoidChase trong scene.", "OK");
                return;
            }

            // Gán 3 monster prefab qua SerializedObject
            var so = new SerializedObject(voidChase);
            var prop = so.FindProperty("monsterPrefabs");

            // Idempotent: đã có đủ 3 thì thôi
            if (prop.arraySize >= MonsterPaths.Length)
            {
                EditorUtility.DisplayDialog("Void Runner — Void Monster",
                    $"✓ VoidChase đã có {prop.arraySize} monster (không thay đổi).", "OK");
                return;
            }

            prop.arraySize = MonsterPaths.Length;
            for (int i = 0; i < MonsterPaths.Length; i++)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPaths[i]);
                if (go == null)
                {
                    Debug.LogWarning($"[VoidMonster] Không load được prefab: {MonsterPaths[i]}");
                    continue;
                }
                prop.GetArrayElementAtIndex(i).objectReferenceValue = go;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[VoidMonster] Đã gán {MonsterPaths.Length} quái vật vào VoidChase ({voidChase.name}).");
            EditorUtility.DisplayDialog("Void Runner — Void Monster",
                $"✓ Đã gán 3 quái vật vào VoidChase.\n\nNhớ Ctrl+S lưu scene.", "OK");
        }
    }
}
#endif
