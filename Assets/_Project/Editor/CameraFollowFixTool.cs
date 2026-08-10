#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// BUG NGHIÊM TRỌNG đã tìm ra: CinemachineCamera trong scene Game chỉ có
    /// `CinemachineCamera` (xoay nhìn player) + `CinemachineRotationComposer` (xoay bù)
    /// nhưng THIẾU body component `CinemachineFollow` → camera KHÔNG di chuyển theo bóng.
    ///
    /// Hệ quả: bóng lăn về trước, camera đứng yên tại (0, 8, -10) → chạy xa là mất bóng
    /// khỏi màn hình. Đúng như người dùng mô tả "game chưa có cơ chế cố định".
    ///
    /// Fix: thêm `CinemachineFollow` + FollowOffset (0, 7, -10) (khớp camera hiện tại
    /// so với player y=1) + PositionDamping 0.5 (bám sát nhưng mượt).
    /// Idempotent: chạy lại không thêm component trùng.
    /// </summary>
    public static class CameraFollowFixTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Fix Camera Follow (bóng chạy xa vẫn thấy)")]
        public static void FixCameraFollow()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Void Runner — Camera",
                    $"Scene đang mở là '{scene.name}' — mở scene Game rồi chạy lại.", "OK");
                return;
            }

            var cmCam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (cmCam == null)
            {
                EditorUtility.DisplayDialog("Void Runner — Camera", "Không tìm thấy CinemachineCamera trong scene.", "OK");
                return;
            }

            string msg;
            var follow = cmCam.GetComponent<CinemachineFollow>();
            if (follow == null)
            {
                follow = cmCam.gameObject.AddComponent<CinemachineFollow>();
                msg = "Đã THÊM mới component CinemachineFollow.";
            }
            else
            {
                msg = "CinemachineFollow đã có — chỉ cập nhật cấu hình.";
            }

            // Offset khớp camera hiện tại (player y=1 → camera y=8 = +7, z=-10)
            follow.FollowOffset = new Vector3(0f, 7f, -10f);

            // TrackerSettings là struct — set qua field component
            var ts = follow.TrackerSettings;
            ts.BindingMode = BindingMode.WorldSpace; // giữ góc nhìn thế giới (không xoay theo bóng)
            ts.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f); // bám sát nhưng mượt
            ts.AngularDampingMode = AngularDampingMode.Euler;
            ts.RotationDamping = Vector3.one;
            follow.TrackerSettings = ts;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[VoidRunner] Camera Follow fix: {msg} FollowOffset={follow.FollowOffset} Damping=0.5");
            EditorUtility.DisplayDialog("Void Runner — Camera",
                $"✓ {msg}\n\nFollowOffset (0, 7, -10) — camera giờ CHẠY THEO bóng.\n\nNhớ Ctrl+S lưu scene, bấm ▶ Play test: bóng chạy mãi vẫn thấy.", "OK");
        }

        [MenuItem(MenuRoot + "Fix Camera Follow (bóng chạy xa vẫn thấy)", true)]
        private static bool ValidateFix()
        {
            return SceneManager.GetActiveScene().name == "Game";
        }
    }
}
#endif
