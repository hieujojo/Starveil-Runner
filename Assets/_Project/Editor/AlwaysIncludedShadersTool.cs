using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Thêm shader dùng RUNTIME (Shader.Find) vào Graphics Settings → Always Included Shaders.
    ///
    /// 2026-08-15 — chẩn đoán Unity Play (đọc browser console bằng headless Chrome):
    /// vào Game scene bắn hàng loạt `ArgumentNullException: Value cannot be null. Parameter name: shader`
    /// → vài model/VFX KHÔNG render. Nguyên nhân gốc:
    ///   - BlobShadow + VFXManager tạo material runtime bằng Shader.Find("Sprites/Default");
    ///   - Tile tạo lane markers bằng Shader.Find("Universal Render Pipeline/Unlit") (fallback Unlit/Color);
    ///   - KHÔNG .mat asset nào trong scene tham chiếu các shader này (grep 0 file) → WebGL build
    ///     STRIP chúng → Shader.Find trả về null → new Material(null) → exception → mất bóng/VFX/lane marker.
    /// Fix gốc: ép các shader này LUÔN vào build (GraphicsSettings.m_AlwaysIncludedShaders).
    /// Idempotent: bỏ qua shader đã có trong danh sách. Chạy lại an toàn.
    /// </summary>
    public static class AlwaysIncludedShadersTool
    {
        private const string MenuRoot = "Tools/Starveil Runner/Fix/";

        private static readonly string[] ShaderNames =
        {
            "Sprites/Default",                 // BlobShadow + VFXManager (sao trôi, lửa, burst, popup, trail)
            "Universal Render Pipeline/Lit",   // Tile road (đã có .mat — bảo hiểm)
            "Universal Render Pipeline/Unlit", // Tile lane markers (tạo runtime)
            "Unlit/Color",                     // Fallback lane markers (Tile.cs)
            "Standard",                        // Fallback road (Tile.cs) — bảo hiểm
        };

        [MenuItem(MenuRoot + "Always Included Shaders (fix model/VFX không render trên WebGL)")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("Always Included Shaders",
                "Thêm shader dùng RUNTIME (Shader.Find) vào Graphics Settings để KHÔNG bị strip khỏi WebGL build:\n\n" +
                string.Join("\n", ShaderNames) + "\n\n" +
                "Nguyên nhân lỗi \"vài model không render\" trên Unity Play:\n" +
                "các shader này không được .mat asset nào tham chiếu → bị strip → Shader.Find = null\n" +
                "→ ArgumentNullException → mất bóng mềm / VFX hạt / vạch lane.\n\n" +
                "Chạy xong → Build WebGL lại (Gzip) → hết lỗi.",
                "Chạy", "Hủy")) return;

            var gs = GraphicsSettings.GetGraphicsSettings();
            if (gs == null) { Debug.LogError("[AlwaysIncludedShaders] Không lấy được GraphicsSettings."); return; }

            var so = new SerializedObject(gs);
            var prop = so.FindProperty("m_AlwaysIncludedShaders");
            if (prop == null)
            {
                Debug.LogError("[AlwaysIncludedShaders] Không tìm thấy m_AlwaysIncludedShaders trong GraphicsSettings.");
                return;
            }

            int added = 0, missing = 0, skipped = 0;
            foreach (string name in ShaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    missing++;
                    Debug.LogWarning($"[AlwaysIncludedShaders] KHÔNG tìm thấy shader '{name}' (tên sai hoặc package chưa import) — bỏ qua.");
                    continue;
                }

                bool exists = false;
                for (int i = 0; i < prop.arraySize; i++)
                {
                    if (prop.GetArrayElementAtIndex(i).objectReferenceValue == shader) { exists = true; break; }
                }
                if (exists) { skipped++; continue; }

                prop.InsertArrayElementAtIndex(prop.arraySize);
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = shader;
                added++;
                Debug.Log($"[AlwaysIncludedShaders] Đã thêm: {name}");
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[AlwaysIncludedShaders] Xong: thêm {added} · đã có {skipped} · thiếu {missing}. Build WebGL lại để áp dụng.");
        }
    }
}
