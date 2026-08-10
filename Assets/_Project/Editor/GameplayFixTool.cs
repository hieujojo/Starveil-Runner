#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using VoidRunner.Core.Player;
using VoidRunner.Core.World;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool fix gameplay feel (2026-08-11):
    /// 1) Fix Void Chase — xóa NavMeshAgent THỪA trên Void (script mới không dùng nữa,
    ///    track vô tận → NavMesh hết vùng → Void đứng yên) + đặt Void đúng vị trí sau lưng player
    ///    (camera offset -10 → Void cách player 9 = NGAY SAU camera, người chơi nhìn thấy được).
    /// 2) Fix Audio Listener — xóa listener THỪA trên Main Camera (giữ duy nhất 1 listener).
    ///    Idempotent — chạy lại an toàn.
    /// </summary>
    public static class GameplayFixTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Fix Void Chase (kẻ thù đuổi theo — chạy scene Game)")]
        public static void FixVoidChase()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Void Runner — Void", "Mở scene Game rồi chạy lại.", "OK");
                return;
            }

            var voidChase = Object.FindAnyObjectByType<VoidChase>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (voidChase == null || player == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Void", "Không tìm thấy VoidChase hoặc PlayerController.", "OK");
                return;
            }

            // 1) Xóa NavMeshAgent cũ (script mới không dùng — để lại chỉ tốn + có thể giữ Void đứng yên)
            var navAgent = voidChase.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                Object.DestroyImmediate(navAgent);
            }

            // 2) Gán player + đặt vị trí sau lưng player (z-9: ngay sau camera → nhìn thấy được)
            voidChase.Setup(player.transform);
            voidChase.transform.position = player.transform.position - Vector3.forward * 9f;
            voidChase.transform.localScale = Vector3.one;

            // 3) Ép cấu hình qua SerializedObject (chống scene giữ giá trị cũ)
            var so = new SerializedObject(voidChase);
            var playerProp = so.FindProperty("player");
            if (playerProp != null) playerProp.objectReferenceValue = player.transform;
            SetField(so, "startDistance", 9f);
            SetField(so, "minDistance", 1.5f);
            SetField(so, "swallowDistance", 1.6f);
            SetField(so, "lateralFollow", 4f);
            SetField(so, "startScale", 1f);
            SetField(so, "maxScale", 2.5f);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[VoidRunner] Void Chase fix: bỏ NavMeshAgent, Void bám player sau lưng (z-9) co dần tới z-1.5 = nuốt player.");
            EditorUtility.DisplayDialog("Void Runner — Void",
                "✓ Đã fix Void Chase:\n• Bỏ NavMeshAgent (thủ phạm khiến Void đứng yên khi chạy xa)\n• Void bám theo player sau lưng — lúc đầu xa, càng lâu càng áp sát → cuối game nuốt player\n\n▶ Play để test: chạy ~60s sẽ thấy Void đuổi sát tới nuốt!", "OK");
        }

        [MenuItem(MenuRoot + "Fix Void Chase (kẻ thù đuổi theo — chạy scene Game)", true)]
        private static bool ValidateFixVoid()
        {
            return SceneManager.GetActiveScene().name == "Game";
        }

        [MenuItem(MenuRoot + "Fix Audio Listener (xoá thừa — cả 2 scene)")]
        public static void FixAudioListeners()
        {
            int removed = 0;
            foreach (var scenePath in new[] { "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/Game.unity" })
            {
                var scene = SceneManager.GetActiveScene();
                bool isTarget = scene.path == scenePath;

                // Mở scene nếu chưa mở (không overwrite trạng thái đang mở)
                Scene loaded = isTarget ? scene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                // Xóa mọi listener trên GameObject có Camera (Main Camera) — giữ listener trên AudioManager
                var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                var redundant = listeners.Where(l => l.GetComponent<Camera>() != null).ToArray();
                foreach (var listener in redundant)
                {
                    Object.DestroyImmediate(listener);
                    removed++;
                }

                if (isTarget) EditorSceneManager.MarkSceneDirty(scene);
                else
                {
                    EditorSceneManager.SaveScene(loaded);
                    EditorSceneManager.CloseScene(loaded, true);
                }
            }

            Debug.Log($"[VoidRunner] Fix Audio Listener: xóa {removed} listener thừa (trên Main Camera) — giữ 1 listener duy nhất.");
            EditorUtility.DisplayDialog("Void Runner — Audio",
                $"✓ Đã xóa {removed} AudioListener thừa trên Main Camera (cả 2 scene).\n\nGiờ chỉ còn 1 listener duy nhất — hết warning \"2 audio listeners\".", "OK");
        }

        [MenuItem(MenuRoot + "Fix Audio Listener (xoá thừa — cả 2 scene)", true)]
        private static bool ValidateFixAudio()
        {
            return true;
        }

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
    }
}
#endif
