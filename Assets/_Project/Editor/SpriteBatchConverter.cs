#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tool editor: convert toàn bộ PNG của 6 gói Kenney thành Sprite (2D and UI)
    /// để kéo được vào Image/Button/Particle. Chạy: menu Tools → Void Runner → Convert Kenney PNG to Sprites.
    /// Ghi chú: idempotent — chạy lại không đổi gì (đã là Sprite thì bỏ qua).
    /// Lưu ý: folder "Black background" (particle-pack) BỊ BỎ QUA — nền đen không dùng làm sprite được;
    /// dùng folder "PNG (Transparent)" thay thế.
    /// </summary>
    public static class SpriteBatchConverter
    {
        private const string MenuRoot = "Tools/Void Runner/";

        // FIX 2026-08-12: chỉ còn 2 folder UI (đang dùng trong scene) — 4 folder khác
        // (game-icons, particle-pack, space-kit, space-station-kit) ĐÃ XÓA (~58MB) vì không
        // được scene/prefab/material/code tham chiếu (thay bằng OlegWER Asteroid + Eric VFX + Cartoon FX).
        private static readonly string[] Roots =
        {
            "Assets/_Project/Art/kenney_ui-pack",
            "Assets/_Project/Art/kenney_ui-pack-space-expansion",
        };

        private static readonly string[] ExcludeFolders =
        {
            "black background", // particle-pack: PNG nền đen — không dùng được làm sprite
        };

        [MenuItem(MenuRoot + "Convert Kenney PNG to Sprites")]
        public static void ConvertKenneyToSprites()
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
                        // Bỏ qua file nằm trong folder bị loại (vd "Black background")
                        string lower = file.ToLowerInvariant();
                        if (ExcludeFolders.Any(f => lower.Contains(f))) continue;

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
                        importer.mipmapEnabled = false;      // UI/particle không cần mipmap
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
                $"Đã convert {converted} PNG thành Sprite.\n(Đã là Sprite sẵn: {skipped})\n\nGiờ mở scene và kéo sprite vào UI/particle được rồi!",
                "OK");
        }

        [MenuItem(MenuRoot + "Convert Kenney PNG to Sprites", true)]
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
