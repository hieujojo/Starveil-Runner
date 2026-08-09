#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

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

            // Xóa asset cũ nếu đã tồn tại (tạo lại từ đầu cho sạch)
            if (File.Exists(OutputAsset))
            {
                AssetDatabase.DeleteAsset(OutputAsset);
                AssetDatabase.Refresh();
            }

            // Sampling 128 + atlas 1024x1024 → nét chữ sắc hơn khi hiển thị lớn (title 110pt)
            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 128, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            AssetDatabase.CreateAsset(fontAsset, OutputAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VoidRunner] Đã tạo TMP font asset: {OutputAsset}");
            EditorUtility.DisplayDialog("Void Runner",
                "Đã tạo TMP font 'Kenney Future SDF'.\n\nGiờ bấm tiếp tool 'Build MainMenu UI' để tự dựng menu đẹp với font + sprite này.",
                "OK");
        }
    }
}
#endif
