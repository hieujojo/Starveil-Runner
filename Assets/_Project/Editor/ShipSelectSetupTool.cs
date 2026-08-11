#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRunner.Core.Player;
using VoidRunner.Systems.Save;
using VoidRunner.UI;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Task D (2026-08-11): gán 2 tàu model (SF Fighter / Sparrow) vào:
    ///   • Game scene  → PlayerController.shipPrefabs (Player đọc SaveSystem.SelectedShip khi vào game)
    ///   • MainMenu    → ShipSelectManager.shipPrefabs (preview 3D trong panel chọn)
    /// Idempotent — chạy lại không nhân đôi.
    /// </summary>
    public static class ShipSelectSetupTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // Một nguồn sự thật duy nhất — dùng ShipCatalog (tránh lệch path giữa tool và self-heal)
        private static readonly string[] ShipPaths = ShipCatalog.ShipPaths;

        [MenuItem(MenuRoot + "Setup Ship Select (Task D — 2 fighter)")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game" && scene.name != "MainMenu")
            {
                EditorUtility.DisplayDialog("Void Runner — Ship Select",
                    $"Scene đang mở là '{scene.name}'. Mở scene Game HOẶC MainMenu rồi chạy lại.", "OK");
                return;
            }

            bool any = false;

            // Game scene → PlayerController
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var so = new SerializedObject(player);
                var prop = so.FindProperty("shipPrefabs");
                if (prop.arraySize < ShipPaths.Length)
                {
                    prop.arraySize = ShipPaths.Length;
                    for (int i = 0; i < ShipPaths.Length; i++)
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPaths[i]);
                        if (go != null) prop.GetArrayElementAtIndex(i).objectReferenceValue = go;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    any = true;
                }
            }

            // MainMenu → ShipSelectManager (tạo SẴN component trong scene để gán prefab —
            // trước đây nó được tạo runtime nên tool không tìm thấy → preview rỗng)
            var shipSelect = Object.FindAnyObjectByType<ShipSelectManager>();
            if (shipSelect == null && scene.name == "MainMenu")
            {
                var mm = Object.FindAnyObjectByType<MainMenuManager>();
                var host = mm != null ? mm.gameObject : new GameObject("ShipSelectManager");
                if (mm == null) SceneManager.MoveGameObjectToScene(host, scene);
                shipSelect = host.AddComponent<ShipSelectManager>();
                any = true; // đánh dấu để lưu scene
            }
            if (shipSelect != null)
            {
                var so2 = new SerializedObject(shipSelect);
                var prop2 = so2.FindProperty("shipPrefabs");
                if (prop2.arraySize < ShipPaths.Length)
                {
                    prop2.arraySize = ShipPaths.Length;
                    for (int i = 0; i < ShipPaths.Length; i++)
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPaths[i]);
                        if (go != null) prop2.GetArrayElementAtIndex(i).objectReferenceValue = go;
                    }
                    so2.ApplyModifiedPropertiesWithoutUndo();
                    any = true;
                }
            }

            if (any)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorUtility.DisplayDialog("Void Runner — Ship Select",
                    $"✓ Đã gán {ShipPaths.Length} tàu (SF Fighter + Sparrow).\n\nNhớ Ctrl+S lưu scene.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Void Runner — Ship Select",
                    "Chưa gán gì mới (đã gán trước đó).\n\nỞ MainMenu: nếu SHIP button chưa thấy, bấm Play 1 lần để MainMenuManager tự sinh rồi chạy tool lại.", "OK");
            }
        }
    }
}
#endif
