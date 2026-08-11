using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoidRunner.Systems.Save
{
    /// <summary>
    /// Catalog 2 tàu model (Task D) — dùng chung cho:
    ///   • ShipSelectManager (MainMenu) — preview 3D trong panel chọn
    ///   • PlayerController (Game)     — dựng tàu đã chọn khi vào game
    ///
    /// Self-heal (R4.18): nếu shipPrefabs chưa được gán trong scene (tool Setup Ship Select chưa
    /// chạy — ví dụ vừa pull code mới), tự tải qua path. Chỉ hoạt động ở EDITOR (AssetDatabase).
    /// Build: prefab model bị gitignore (chỉ có ở máy dev) — bắt buộc chạy tool để gán đúng.
    /// </summary>
    public static class ShipCatalog
    {
        /// <summary>Đúng thứ tự ShipNames trong ShipSelectManager (SF FIGHTER, SPARROW).</summary>
        public static readonly string[] ShipPaths =
        {
            "Assets/SF_Fighter/SF_Free-Fighter.prefab",
            "Assets/Sparrow_Fighter/Prefabs/Sparrow_blue Variant.prefab",
        };

        /// <summary>Tải prefab tàu theo index (0..ShipPaths.Length-1). Rỗng nếu không tìm thấy.</summary>
        public static GameObject Load(int index)
        {
            if (index < 0 || index >= ShipPaths.Length) return null;
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(ShipPaths[index]);
#else
            return null; // build: prefab phải được gán qua tool (model chỉ tồn tại ở editor)
#endif
        }
    }
}
