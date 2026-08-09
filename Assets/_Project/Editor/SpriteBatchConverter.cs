#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool editor: convert toàn bộ PNG của 2 gói Kenney UI thành Sprite (2D and UI)
    /// để kéo được vào Image/Button. Chạy: menu Tools → Void Runner → Convert Kenney UI PNG to Sprites.
    /// Ghi chú: idempotent — chạy lại không đổi gì (đã là Sprite thì bỏ qua).
    /// </summary>
    public static class SpriteBatchConverter
    {
        private const string MenuRoot = "Tools/Void Runner/";

        private static readonly string[] Roots =
        {
            "Assets/_Project/Art/kenney_ui-pack",
            "Assets/_Project/Art/kenney_ui-pack-space-expansion",
        };

        [MenuItem(MenuRoot + "Convert Kenney UI PNG to Sprites")]
        public static void ConvertKenneyUiToSprites()
        {
            int converted = 0;
            int skipped = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string root in Roots)
                {
                    if (!Directory.Exists(root)) continue;

                    foreach (string file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                    {
                        string path = file.Replace('\\', '/');
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer == null) continue;

                        // Đã là Sprite rồi → bỏ qua (không reimport lại)
                        if (importer.textureType == TextureImporterType.Sprite)
                        {
                            skipped++;
                            continue;
                        }

                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 100f;
                        importer.mipmapEnabled = false;      // UI không cần mipmap
                        importer.alphaIsTransparency = true; // PNG Kenney có alpha
                        importer.filterMode = FilterMode.Bilinear;
                        importer.textureCompression = TextureImporterCompression.Compressed;
                        importer.SaveAndReimport();
                        converted++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[VoidRunner] Convert xong: {converted} PNG → Sprite (bỏ qua {skipped} đã là Sprite).");
            EditorUtility.DisplayDialog(
                "Void Runner — Convert Sprite",
                $"Đã convert {converted} PNG thành Sprite.\n(Đã là Sprite sẵn: {skipped})\n\nGiờ mở scene MainMenu/Game và kéo sprite vào UI được rồi!",
                "OK");
        }

        [MenuItem(MenuRoot + "Convert Kenney UI PNG to Sprites", true)]
        private static bool ValidateConvert()
        {
            foreach (string root in Roots)
            {
                if (Directory.Exists(root)) return true;
            }
            return false;
        }
    }
}
#endif
