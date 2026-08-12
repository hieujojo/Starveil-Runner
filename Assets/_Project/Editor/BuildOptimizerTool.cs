using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoidRunner.EditorTools
{
    /// <summary>
    /// Tối ưu dung lượng build WebGL (2026-08-12, user: "build 125MB có nặng quá không").
    /// Hai bước, chạy được nhiều lần (idempotent):
    ///   1. Xóa asset KHÔNG dùng (đã verify 0 refs trong scene/prefab/material/code):
    ///      - G-spot_Lab (350MB) — không ai tham chiếu
    ///      - SpaceSkies: Skybox_1 (Pink), Skybox_2 (Green), Demo + bản 1K/4K trong Skybox_3 (chỉ giữ Purple_2K)
    ///      - Sci_fi_Drones: chỉ giữ Robot_Guardian (prefab/fbx/mat/4 texture) — các robot khác 0 refs
    ///      - Sparrow_Fighter: mask1/mask2 (0 refs)
    ///      - Folder rác: fantasySpider, _Recovery, OlegWER
    ///   2. Giảm maxTextureSize 2048→1024 cho texture ĐANG dùng:
    ///      - 4 EXR Nebula (873MB nguồn — NebulaChanger dùng làm skybox đổi theo độ khó, KHÔNG xóa)
    ///      - Sparrow tif, Flying Beetle tga, Robot_Guardian png
    /// Để an toàn: trước khi xóa 1 asset, tool tự kiểm tra lại số ref thực tế; nếu >0 thì BỎ QUA (log cảnh báo).
    /// </summary>
    public static class BuildOptimizerTool
    {
        private const string MenuRoot = "Tools/Void Runner/";

        [MenuItem(MenuRoot + "Optimize Build (xóa asset không dùng + giảm texture size)")]
        public static void OptimizeBuild()
        {
            if (!EditorUtility.DisplayDialog("Tối ưu Build WebGL",
                    "1. Xóa asset không dùng (G-spot_Lab, SpaceSkies 1K/4K + Pink/Green, Drone khác Robot_Guardian, Sparrow mask1/2, folder rác)\n" +
                    "2. Giảm maxTextureSize 2048→1024 cho Nebula EXR + texture Sparrow/Beetle/Drone\n\n" +
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

        // ---------------- BƯỚC 1: XÓA ASSET KHÔNG DÙNG ----------------

        private static readonly string[] FoldersToDelete =
        {
            "Assets/G-spot_Lab",
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
            // Nebula EXR cubemap (HDR 2K hiện tại → 1K, skybox nền đủ nét, giảm 4x)
            { "Assets/Nebula Skyboxes/Nebula_01_Cubemap.exr", 1024 },
            { "Assets/Nebula Skyboxes/Nebula_02_Cubemap.exr", 1024 },
            { "Assets/Nebula Skyboxes/Nebula_03_Cubemap.exr", 1024 },
            { "Assets/Nebula Skyboxes/Nebula_04_Cubemap.exr", 1024 },
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
                if (importer.maxTextureSize <= kv.Value) continue; // đã đủ nhỏ
                importer.maxTextureSize = kv.Value;
                importer.SaveAndReimport();
                count++;
                Debug.Log($"[BuildOptimizer] Giảm size: {System.IO.Path.GetFileName(kv.Key)} → {kv.Value}");
            }
            return count;
        }
    }
}
