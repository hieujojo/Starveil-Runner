using System.Collections.Generic;
using UnityEngine;

namespace VoidRunner.Utils
{
    /// <summary>
    /// Chuyển material Built-in (Standard/Legacy) sang URP/Lit khi instantiate model 3rd-party
    /// (SF Fighter / Sparrow / Monster ...). Trong URP, material dùng shader Standard bị render
    /// MÀU TÍM/MAGENTA (shader không compile) — bug 2026-08-12 user báo "tàu màu tím không phải màu gốc".
    ///
    /// Cache static theo material gốc → chỉ tạo 1 material URP thay thế rồi tái dùng mọi lần
    /// (preview đổi tàu, game instantiate nhiều lần) — không leak material mới mỗi frame.
    /// </summary>
    public static class MaterialFixer
    {
        private static readonly Dictionary<Material, Material> Cache = new Dictionary<Material, Material>();

        /// <summary>
        /// Quét mọi Renderer (kể cả SkinnedMeshRenderer) trong go, thay material không-URP bằng
        /// bản URP/Lit giữ nguyên màu gốc (_Color + _EmissionColor). Idempotent + an toàn null.
        /// </summary>
        public static void EnsureURPMaterials(GameObject go)
        {
            if (go == null) return;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;

                Material[] mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material src = mats[m];
                    if (src == null) continue;
                    if (IsURPCompatible(src)) continue;

                    Material converted = GetOrCreate(src);
                    mats[m] = converted;
                }
                r.sharedMaterials = mats;
            }
        }

        private static bool IsURPCompatible(Material mat)
        {
            Shader s = mat.shader;
            if (s == null) return false;
            string name = s.name;
            // URP Lit/Unlit/ShaderGraph đều OK; Built-in "Standard"/"Legacy Shaders"/"Diffuse" → cần convert
            return name.Contains("Universal Render Pipeline") || name.Contains("Shader Graphs");
        }

        private static Material GetOrCreate(Material src)
        {
            if (Cache.TryGetValue(src, out var cached) && cached != null) return cached;

            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null)
            {
                Debug.LogWarning("[MaterialFixer] Không tìm thấy shader URP/Lit — giữ material gốc.");
                return src;
            }

            Material converted = new Material(urp);
            // Giữ màu gốc: ưu tiên _BaseColor (URP), fallback _Color (Built-in)
            if (src.HasProperty("_BaseColor")) converted.SetColor("_BaseColor", src.GetColor("_BaseColor"));
            else if (src.HasProperty("_Color")) converted.SetColor("_BaseColor", src.GetColor("_Color"));

            // ⚠️ FIX 2026-08-12 (bug "tàu thành màu trắng"): trước đây chỉ copy màu, KHÔNG copy
            // texture → URP/Lit không có _BaseMap → render trắng. Copy _MainTex → _BaseMap +
            // normal/metallic/occlusion/emission + hệ số smoothness/metallic để giữ nguyên vẻ gốc.
            if (src.HasProperty("_MainTex") && src.GetTexture("_MainTex") != null)
            {
                converted.SetTexture("_BaseMap", src.GetTexture("_MainTex"));
                if (src.HasProperty("_MainTex_ST"))
                {
                    converted.SetTextureScale("_BaseMap", src.GetTextureScale("_MainTex"));
                    converted.SetTextureOffset("_BaseMap", src.GetTextureOffset("_MainTex"));
                }
            }
            if (src.HasProperty("_BumpMap") && src.GetTexture("_BumpMap") != null)
            {
                converted.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
                converted.EnableKeyword("_NORMALMAP");
            }
            if (src.HasProperty("_MetallicGlossMap") && src.GetTexture("_MetallicGlossMap") != null)
            {
                converted.SetTexture("_MetallicGlossMap", src.GetTexture("_MetallicGlossMap"));
                converted.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (src.HasProperty("_OcclusionMap") && src.GetTexture("_OcclusionMap") != null)
            {
                converted.SetTexture("_OcclusionMap", src.GetTexture("_OcclusionMap"));
                converted.EnableKeyword("_OCCLUSIONMAP");
            }
            if (src.HasProperty("_Glossiness")) converted.SetFloat("_Smoothness", src.GetFloat("_Glossiness"));
            if (src.HasProperty("_Metallic")) converted.SetFloat("_Metallic", src.GetFloat("_Metallic"));
            if (src.HasProperty("_EmissionColor"))
            {
                Color em = src.GetColor("_EmissionColor");
                if (em.maxColorComponent > 0.05f)
                {
                    converted.EnableKeyword("_EMISSION");
                    converted.SetColor("_EmissionColor", em);
                }
                if (src.HasProperty("_EmissionMap") && src.GetTexture("_EmissionMap") != null)
                {
                    converted.SetTexture("_EmissionMap", src.GetTexture("_EmissionMap"));
                }
            }

            Cache[src] = converted;
            return converted;
        }
    }
}
