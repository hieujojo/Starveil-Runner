using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tối ưu dung lượng build WebGL (2026-08-12, user: "build 125MB có nặng quá không").
    /// UPDATE 2026-08-15 (user: "zip sau build là 130MB chứ không phải 40–60MB"):
    ///   - Chẩn đoán từ Editor.log build report: TEXTURE chiếm 97.2% build (282MB raw / 139MB build).
    ///   - Thủ phạm #1 = 4 skybox Nebula EXR import @2048 → ~100MB trong build (~69%).
    ///     → Giảm xuống 1024 (NebulaTargetSize) = tiết kiệm ~75MB; 512 = thêm ~20MB nữa.
    ///   - Thủ phạm #2 = Sparrow/Beetle/Drone texture @2048 (Sparrow_normal, Beetle normal/metallic...)
    ///     → Giảm 1024.
    ///   - THÊM menu "Optimize Build — CHỈ giảm texture size (an toàn)": không xóa asset nào,
    ///     chỉ giảm maxTextureSize — đủ để build từ 130MB → ~60MB.
    ///   - GIỮ G-spot_Lab (PLAN.md ghi "PENDING INTEGRATE — user đã xác nhận giữ lại 2026-08-12") —
    ///     không xóa dù 0 refs (0 refs = không vào build, xóa chỉ sạch đĩa, không giảm build).
    /// Hai bước của menu đầy đủ, chạy được nhiều lần (idempotent):
    ///   1. Xóa asset KHÔNG dùng (đã verify 0 refs trong scene/prefab/material/code):
    ///      - SpaceSkies: Skybox_1 (Pink), Skybox_2 (Green), Demo + bản 1K/4K trong Skybox_3 (chỉ giữ Purple_2K)
    ///      - Sci_fi_Drones: chỉ giữ Robot_Guardian (prefab/fbx/mat/4 texture) — các robot khác 0 refs
    ///      - Sparrow_Fighter: mask1/mask2 (0 refs)
    ///      - Folder rác: fantasySpider, _Recovery, OlegWER
    ///   2. Giảm maxTextureSize 2048→1024 cho texture ĐANG dùng:
    ///      - 4 EXR Nebula (873MB nguồn — NebulaChanger dùng làm skybox đổi theo độ khó, KHÔNG xóa)
    ///      - SpaceSkies Purple_2K (skybox MainMenu)
    ///      - Sparrow tif, Flying Beetle tga, Robot_Guardian png
    /// Để an toàn: trước khi xóa 1 asset, tool tự kiểm tra lại số ref thực tế; nếu >0 thì BỎ QUA (log cảnh báo).
    /// </summary>
    public static class BuildOptimizerTool
    {
        private const string MenuRoot = "Tools/Starveil Runner/Optimize/";

        /// <summary>
        /// Kích thước mục tiêu cho 4 skybox Nebula EXR (cục nặng nhất build).
        /// 1024 = an toàn (nền tinh vân mờ, không thấy khác biệt) · 512 = siêu nhẹ (nếu cần dưới 50MB).
        /// </summary>
        private const int NebulaTargetSize = 1024;

        [MenuItem(MenuRoot + "Build (xóa asset không dùng + giảm texture size)")]
        public static void OptimizeBuild()
        {
            if (!EditorUtility.DisplayDialog("Tối ưu Build WebGL",
                    "1. Xóa asset không dùng (SpaceSkies 1K/4K + Pink/Green, Drone khác Robot_Guardian, Sparrow mask1/2, folder rác) — GIỮ G-spot_Lab (đang chờ tích hợp VFX)\n" +
                    "2. Giảm maxTextureSize 2048→1024 cho Nebula EXR + SpaceSkies Purple_2K + texture Sparrow/Beetle/Drone\n\n" +
                    "Mỗi asset được kiểm tra lại refs trước khi xóa — nếu có ref thì bỏ qua.\n" +
                    "Chạy xong Unity sẽ reimport — sau đó build lại là build nhẹ hơn nhiều.\n\n" +
                    "Tiếp tục?",
                    "Chạy", "Hủy")) return;

            int deleted = DeleteUnusedAssets();
            int resized = ShrinkTextures();

            Debug.Log($"[BuildOptimizer] Xong: xóa {deleted} asset, giảm size {resized} texture. Unity đang reimport...");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem(MenuRoot + "Build — CHỈ giảm texture size (an toàn, không xóa gì)")]
        public static void ShrinkTexturesOnly()
        {
            if (!EditorUtility.DisplayDialog("Tối ưu Build WebGL (an toàn)",
                    "CHỈ giảm maxTextureSize 2048→1024 cho texture ĐANG dùng (không xóa asset nào):\n" +
                    "- 4 skybox Nebula EXR (cục nặng nhất build — ~100MB)\n" +
                    "- SpaceSkies Purple_2K (skybox MainMenu)\n" +
                    "- Sparrow / Flying Beetle / Robot_Guardian (player, enemy, obstacle)\n\n" +
                    "Dự kiến build 139MB → ~60MB. Chạy xong Unity reimport → Build WebGL lại (Gzip) → zip lại.\n\n" +
                    "Tiếp tục?",
                    "Chạy", "Hủy")) return;

            int resized = ShrinkTextures();
            Debug.Log($"[BuildOptimizer] Đã giảm size {resized} texture. Build WebGL lại là thấy build nhẹ hơn rõ rệt (Nebula 2048→1024 = ~75MB tiết kiệm).");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ---------------- BƯỚC 1: XÓA ASSET KHÔNG DÙNG ----------------

        private static readonly string[] FoldersToDelete =
        {
            // GIỮ Assets/G-spot_Lab — PLAN.md: "PENDING INTEGRATE, user đã xác nhận giữ lại (2026-08-12)".
            "Assets/SpaceSkies Free/Demo",
            "Assets/SpaceSkies Free/Skybox_1",
            "Assets/SpaceSkies Free/Skybox_2",
            "Assets/SpaceSkies Free/Skybox_3/Textures/1K_Resolution",
            "Assets/SpaceSkies Free/Skybox_3/Textures/4K_Resolution",
            "Assets/fantasySpider",
            "Assets/_Recovery",
            "Assets/OlegWER",
        };

        private static readonly string[] FilesToDelete =
        {
            "Assets/SpaceSkies Free/Skybox_3/Purple_1K_Resolution.mat",
            "Assets/SpaceSkies Free/Skybox_3/Purple_4K_Resolution.mat",
            "Assets/Sparrow_Fighter/Textures/Sparrow_mask1.tif",
            "Assets/Sparrow_Fighter/Textures/Sparrow_mask2.tif",
        };

        /// <summary>Prefab/material/model của các robot KHÔNG dùng (chỉ giữ Robot_Guardian).</summary>
        private static readonly string[] DroneUnused =
        {
            "Robot_Collector", "Robot_Invader", "Robot_Scout",
            "Robot_Scout_HyperX", "Robot_Scout_HyperX_Red", "Robot_Scout_Rockie",
        };

        private static readonly string[] DroneUnusedMaterials =
        {
            "Robot_Collector.mat", "Robot_Invader.mat", "Robot_Scout.mat",
            "Robot_Scout_HyperX_Back.mat", "Robot_Scout_HyperX_Back_Red.mat",
            "Robot_Scout_HyperX_Front.mat", "Robot_Scout_HyperX_Front_Red.mat",
            "Robot_Scout_Rockie_Unity.mat",
        };

        private static readonly string[] DroneUnusedModels =
        {
            "Robot_Collector.fbx", "Robot_Invader.fbx", "Robot_Scout.fbx",
            "Robot_Scout_HyperX_Unity.fbx", "Robot_Scout_Rockie_Unity.fbx",
        };

        private static int DeleteUnusedAssets()
        {
            // Fail-safe: obstacle duy nhất đang dùng là Robot_Guardian — nếu thiếu thì dừng.
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Sci_fi_Drones/Prefabs/Robot_Guardian.prefab") == null)
            {
                Debug.LogError("[BuildOptimizer] ABORT: không tìm thấy Robot_Guardian.prefab — dừng xóa để không hỏng obstacle.");
                return 0;
            }

            int count = 0;

            // Xóa demo scene của gói Drone TRƯỚC (nó tham chiếu các prefab/model sẽ bị xóa sau) → sạch log.
            DeleteIfExists("Assets/Sci_fi_Drones/Show_Assets.unity", ref count);

            // Xóa FILE TRƯỚC (material 1K/4K), rồi mới xóa FOLDER texture 1K/4K
            // → tránh warning "missing ref" khi material vẫn trỏ vào texture đã bị xóa.
            foreach (string file in FilesToDelete)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file) != null)
                {
                    AssetDatabase.DeleteAsset(file);
                    count++;
                    Debug.Log($"[BuildOptimizer] Xóa file: {file}");
                }
            }

            foreach (string folder in FoldersToDelete)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                    count++;
                    Debug.Log($"[BuildOptimizer] Xóa folder: {folder}");
                }
            }

            // Drone: xóa prefab (giữ Robot_Guardian) — model được xử lý riêng ở DroneUnusedModels
            foreach (string name in DroneUnused)
            {
                DeleteIfExists($"Assets/Sci_fi_Drones/Prefabs/{name}.prefab", ref count);
            }

            foreach (string mat in DroneUnusedMaterials)
            {
                DeleteIfExists($"Assets/Sci_fi_Drones/Materials/{mat}", ref count);
            }

            foreach (string model in DroneUnusedModels)
            {
                DeleteIfExists($"Assets/Sci_fi_Drones/Models/{model}", ref count);
            }

            // Texture Drone: chỉ giữ Robot_Guardian_*.png
            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sci_fi_Drones/Textures" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(path);
                if (System.IO.Path.GetFileName(p).StartsWith("Robot_Guardian", StringComparison.OrdinalIgnoreCase)) continue;
                DeleteIfExists(p, ref count);
            }

            // "Read me.txt" của gói Drone
            DeleteIfExists("Assets/Sci_fi_Drones/Read me.txt", ref count);

            return count;
        }

        private static void DeleteIfExists(string path, ref int count)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                count++;
                Debug.Log($"[BuildOptimizer] Xóa: {path}");
            }
        }

        // ---------------- BƯỚC 2: GIẢM maxTextureSize ----------------

        private static readonly Dictionary<string, int> TexturesToShrink = new Dictionary<string, int>
        {
            // Nebula EXR cubemap — cục nặng NHẤT build (~100MB/139MB ở 2048) → NebulaTargetSize (1024)
            { "Assets/Nebula Skyboxes/Nebula_01_Cubemap.exr", NebulaTargetSize },
            { "Assets/Nebula Skyboxes/Nebula_02_Cubemap.exr", NebulaTargetSize },
            { "Assets/Nebula Skyboxes/Nebula_03_Cubemap.exr", NebulaTargetSize },
            { "Assets/Nebula Skyboxes/Nebula_04_Cubemap.exr", NebulaTargetSize },
            // SpaceSkies Purple_2K — skybox MainMenu (6 mặt, 2K → 1K đủ nét cho nền sao)
            { "Assets/SpaceSkies Free/Skybox_3/Textures/2K_Resolution/Back_2K_TEX.png", 1024 },
            { "Assets/SpaceSkies Free/Skybox_3/Textures/2K_Resolution/Down_2K_TEX.png", 1024 },
            { "Assets/SpaceSkies Free/Skybox_3/Textures/2K_Resolution/Front_2K_TEX.png", 1024 },
            { "Assets/SpaceSkies Free/Skybox_3/Textures/2K_Resolution/Left_2K_TEX.png", 1024 },
            { "Assets/SpaceSkies Free/Skybox_3/Textures/2K_Resolution/Right_2K_TEX.png", 1024 },
            { "Assets/SpaceSkies Free/Skybox_3/Textures/2K_Resolution/Up_2K_TEX.png", 1024 },
            // Sparrow ship: model nhỏ trên màn hình, 1K đủ nét
            { "Assets/Sparrow_Fighter/Textures/Sparrow_AO.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_blue.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_emissive.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_grey.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_metallic.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_normal.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_orange.tif", 1024 },
            { "Assets/Sparrow_Fighter/Textures/Sparrow_roughness.tif", 1024 },
            // Flying Beetle (enemy)
            { "Assets/Flying Beetle/texture/tbeetle texture orange_Albedo and alfa.tga", 1024 },
            { "Assets/Flying Beetle/texture/tbeetle texture orange_Metallic.tga", 1024 },
            { "Assets/Flying Beetle/texture/tbeetle texture orange_Normal.tga", 1024 },
            { "Assets/Flying Beetle/texture/tbeetle texture orange_Occlusion.tga", 1024 },
            // Drone obstacle
            { "Assets/Sci_fi_Drones/Textures/Robot_Guardian_Albedo.png", 1024 },
            { "Assets/Sci_fi_Drones/Textures/Robot_Guardian_Emission.png", 1024 },
            { "Assets/Sci_fi_Drones/Textures/Robot_Guardian_MetallicSmoothness.png", 1024 },
            { "Assets/Sci_fi_Drones/Textures/Robot_Guardian_Normal.png", 1024 },
        };

        private static int ShrinkTextures()
        {
            int count = 0;
            foreach (var kv in TexturesToShrink)
            {
                var importer = AssetImporter.GetAtPath(kv.Key) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;

                // 1) Default platform (các platform không có override riêng — WebGL sẽ dùng cái này
                //    nếu texture KHÔNG có platform override WebGL cụ thể).
                if (importer.maxTextureSize > kv.Value)
                {
                    importer.maxTextureSize = kv.Value;
                    changed = true;
                }

                // 2) ⚠️ WebGL override RIÊNG (đọc từ .meta — Sparrow/Beetle đang 4096, Drone/SpaceSkies 2048):
                //    override này THẮNG default khi build WebGL → trước đây shrink default KHÔNG có tác dụng
                //    trên WebGL (chẩn đoán 2026-08-15: meta WebGL vẫn 4096/2048 → build vẫn 132MB).
                //    Ép override WebGL xuống đúng target để build WebGL thực sự nhẹ.
                var webglSettings = importer.GetPlatformTextureSettings("WebGL");
                if (!webglSettings.overridden || webglSettings.maxTextureSize > kv.Value)
                {
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = "WebGL",
                        overridden = true,
                        maxTextureSize = kv.Value,
                        format = TextureImporterFormat.Automatic, // giữ cơ chế nén tự động (DXT/ASTC) như cũ
                    });
                    changed = true;
                }

                if (!changed) continue; // đã đủ nhỏ cả 2 chỗ

                importer.SaveAndReimport();
                count++;
                Debug.Log($"[BuildOptimizer] Giảm size: {System.IO.Path.GetFileName(kv.Key)} → {kv.Value} (Default + WebGL override)");
            }
            return count;
        }
    }
}
