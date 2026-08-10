#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tạo TMP font asset từ font Kenney Future.ttf (trong gói kenney_ui-pack)
    /// để gán cho toàn bộ text TMP của UI. Chạy: Tools → Void Runner → Create TMP Font (Kenney Future).
    /// </summary>
    public static class KenneyFontImporter
    {
        private const string FontPath = "Assets/_Project/Art/kenney_ui-pack/Font/Kenney Future.ttf";
        private const string OutputFolder = "Assets/_Project/Art/Fonts";
        private const string OutputAsset = OutputFolder + "/Kenney Future SDF.asset";

        [MenuItem("Tools/Void Runner/Create TMP Font (Kenney Future)")]
        public static void CreateTmpFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                EditorUtility.DisplayDialog("Void Runner",
                    $"Không tìm thấy font tại:\n{FontPath}\n\nBạn đã import gói kenney_ui-pack vào Assets/_Project/Art chưa?", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Fonts");
            }

            // ⚠️ LƯU guid cũ TRƯỚC khi xóa (bài học 2026-08-11): DeleteAsset + CreateAsset sinh GUID MỚI
            // → mọi text trong scene (ScoreText/ComboText/Button...) đang reference guid cũ sẽ MẤT FONT
            // → phải restore guid cũ vào .meta mới sau khi tạo lại.
            string oldGuid = UIBuilderHelpers.ReadGuid(OutputAsset);

            // Xóa asset cũ nếu đã tồn tại (tạo lại từ đầu cho sạch)
            if (File.Exists(OutputAsset))
            {
                AssetDatabase.DeleteAsset(OutputAsset);
                AssetDatabase.Refresh();
            }

            // Sampling 128 + atlas 2048x2048 → nét chữ sắc khi hiển thị lớn (title 110pt) VÀ đủ toàn bộ ký tự
            // ASCII (1024 chỉ chứa ~40/95 ký tự → thiếu 'x'/'2'/chữ thường → combo "x2" hiện lỗi — bug 2026-08-11).
            // Dùng helper chung: tự lưu texture + material làm sub-asset (bài học m_AtlasTextures)
            var fontAsset = UIBuilderHelpers.CreateFontAssetCore(font, OutputAsset);

            // Restore guid cũ (nếu có) để không gãy tham chiếu font trong scene
            UIBuilderHelpers.RestoreGuid(OutputAsset, oldGuid);

            Debug.Log($"[VoidRunner] Đã tạo TMP font asset: {OutputAsset}");
            EditorUtility.DisplayDialog("Void Runner",
                "Đã tạo TMP font 'Kenney Future SDF' với đủ toàn bộ ký tự ASCII (atlas 2048).\n\nGiờ chạy 'Refactor: Game Scene' để áp HUD layout (combo xuống dưới điểm).",
                "OK");
        }
    }
}
#endif
