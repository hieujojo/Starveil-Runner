using UnityEngine;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Rig trung gian giữa CinemachineCamera và Player — KHÓA TRỤC X = 0.
    ///
    /// Bug 2026-08-11 (user: "bấm nút di chuyển thì cảnh vật di chuyển theo, tôi muốn chỉ
    /// tàu di chuyển"): Camera Follow thẳng vào Player → khi tàu đổi lane (di chuyển X),
    /// camera TRÔI NGANG theo → cảnh vật trên màn hình trượt, tàu gần như đứng yên giữa
    /// khung hình. Hệ quả: mất cảm giác "tàu đang rẽ", khó căn đường né vật cản.
    ///
    /// Fix: CinemachineCamera theo dõi RIG này (vị trí = (0, player.y, player.z)) — camera
    /// luôn đứng GIỮA ĐƯỜNG (x=0), chỉ lùi theo trục Z. Tàu mới thực sự di chuyển trên màn hình.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [Tooltip("Player cần bám (chỉ dùng trục Y + Z — X luôn = 0).")]
        public Transform target;

        private void LateUpdate()
        {
            if (target == null) return;
            // X = 0 (giữa đường) — chỉ bám cao độ + tiến lùi. Nếu player teleport (restart)
            // thì rig bám theo luôn → camera không lag.
            transform.position = new Vector3(0f, target.position.y, target.position.z);
        }
    }
}
