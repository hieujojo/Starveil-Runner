#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core.World;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Fix 2026-08-12 (user chốt): Enemy DUY NHẤT = Flying Beetle ("flying carnivorous") —
    /// đổi từ VoidMonsterSetupTool (gán 3 monster random) → gán 1 prefab cố định vào
    /// EnemyChase.enemyPrefab. Prefab có Animator (controller flying loop) → instantiate là bay.
    /// Idempotent: đã có prefab thì thôi. Chạy trên scene Game.
    /// </summary>
    public static class EnemyMonsterSetupTool
    {
        private const string MenuRoot = "Tools/Starveil Runner/Setup/";

        private const string EnemyPrefabPath = "Assets/Flying Beetle/prefab/Flying beetle.prefab";

        [MenuItem(MenuRoot + "Enemy (Flying Beetle — 1 kẻ thù duy nhất)")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Void Runner — Enemy Setup",
                    $"Scene đang mở là '{scene.name}'. Mở scene Game rồi chạy lại.", "OK");
                return;
            }

            var enemy = Object.FindAnyObjectByType<EnemyChase>();
            if (enemy == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Enemy Setup", "Không tìm thấy EnemyChase trong scene.", "OK");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Enemy Setup",
                    $"Không load được prefab: {EnemyPrefabPath}", "OK");
                return;
            }

            // Gán 1 enemy prefab qua SerializedObject — idempotent (đã gán đúng thì thôi)
            var so = new SerializedObject(enemy);
            var prop = so.FindProperty("enemyPrefab");
            if (prop.objectReferenceValue != null && prop.objectReferenceValue == prefab)
            {
                EditorUtility.DisplayDialog("Void Runner — Enemy Setup",
                    $"✓ EnemyChase đã có {prefab.name} (không thay đổi).", "OK");
                return;
            }

            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[EnemySetup] Đã gán enemy = {prefab.name} vào EnemyChase ({enemy.name}).");
            EditorUtility.DisplayDialog("Void Runner — Enemy Setup",
                $"✓ Đã gán {prefab.name} (Flying Beetle) vào EnemyChase.\n\nNhớ Ctrl+S lưu scene.", "OK");
        }
    }
}
#endif
