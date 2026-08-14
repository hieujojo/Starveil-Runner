using UnityEngine;

namespace VoidRunner.Utils
{
    /// <summary>
    /// Bóng mềm giả (blob shadow) — 1 quad đen mờ quay mặt lên, bám dưới object.
    /// FIX 2026-08-12 v3f.10 (user: "con bọ có bóng, làm bóng với tàu luôn"): tàu + enemy
    /// bay ở y=1, track ở y=0 → đặt quad ở y = parent.y - 0.98 (sát mặt track, tránh z-fighting).
    ///
    /// Tại sao dùng blob thay vì shadow map: game tối ưu WebGL/mobile — shadow map tốn cost,
    /// và directional light intensity rất thấp (0.8) nên bóng thật gần như không nhìn thấy.
    /// Blob rẻ, chắc chắn thấy trên dải track xanh.
    ///
    /// Lưu ý: gắn vào ROOT (player/enemy root transform), KHÔNG gắn vào con ship — ship banking
    /// (nghiêng khi đổi lane) sẽ làm bóng nghiêng theo nếu gắn vào con.
    /// </summary>
    public static class BlobShadow
    {
        // Material + texture dùng chung (tạo 1 lần — không cấp phát lại mỗi lần Attach)
        private static Material _cachedMat;

        /// <summary>
        /// Gắn bóng mềm cho object. Idempotent — đã có con tên "BlobShadow" thì trả về ngay.
        /// Scale bóng theo bounds thực (gộp mọi renderer con) × widthScale.
        /// </summary>
        /// <param name="target">Root transform của object (player/enemy).</param>
        /// <param name="yOffset">Khoảng cách từ root xuống track. v3f.10.1: 0.98→0.93 — road là CUBE
        /// scale y=0.1 (top y=0.05), blob trước đặt y=1-0.98=0.02 → CHÌM dưới mặt road (0.05) → vô
        /// hình. 0.93 → blob y=0.07 = trên road 0.02 (hiển thị, không z-fighting).</param>
        /// <param name="widthScale">Hệ số phóng bóng so với bounds (mặc định 1.25 — bóng rộng hơn vật 1 chút).</param>
        public static Transform Attach(Transform target, float yOffset = 0.93f, float widthScale = 1.25f)
        {
            if (target == null) return null;

            Transform existing = target.Find("BlobShadow");
            if (existing != null) return existing; // idempotent — chạy lại không nhân đôi

            if (!EnsureMaterial()) return null; // 2026-08-15: shader Sprites/Default bị strip khỏi WebGL build → Shader.Find null → đừng crash, bỏ qua bóng

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BlobShadow";
            go.transform.SetParent(target, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // mặt quay lên (nhìn từ camera trên cao)
            go.transform.localPosition = new Vector3(0f, -yOffset, 0f);

            Collider col = go.GetComponent<Collider>();
            // Chỉ visual — không va chạm. Disable TRƯỚC khi Destroy để khỏi log lỗi
            // "Concave Mesh Colliders are not supported when used with dynamic Rigidbody" (2026-08-15).
            if (col != null) { col.enabled = false; Object.Destroy(col); }

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = _cachedMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            // Quad mesh nằm trong mặt XY — sau rotation X=90, scale (w, d, 1) → rộng X, sâu Z
            float w = GetWidth(target) * widthScale;
            go.transform.localScale = new Vector3(w, w, 1f);
            return go.transform;
        }

        /// <summary>Bề ngang lớn nhất (X hoặc Z) của object, đo từ renderer con.</summary>
        private static float GetWidth(Transform target)
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.one);
            bool has = false;
            foreach (var r in target.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                // Không đo trail/hạt (TrailRenderer/ParticleSystemRenderer đều là Renderer) —
                // bounds của vệt lửa/khói lệch bóng về phía sau đuôi
                if (r is TrailRenderer || r is ParticleSystemRenderer) continue;
                if (has) b.Encapsulate(r.bounds);
                else { b = r.bounds; has = true; }
            }
            if (!has) return 1f;
            return Mathf.Max(b.size.x, b.size.z);
        }

        private static bool EnsureMaterial()
        {
            if (_cachedMat != null) return true;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                // 2026-08-15: shader bị strip khỏi WebGL build (không .mat nào tham chiếu) → Shader.Find null.
                // Fix gốc: chạy tool "Setup Always Included Shaders" rồi build lại. Đây chỉ là chống crash.
                Debug.LogWarning("[BlobShadow] Không tìm thấy shader 'Sprites/Default' (bị strip khỏi build?) — bỏ qua bóng mềm. Chạy tool 'Tools/Void Runner/Setup Always Included Shaders' rồi build lại.");
                return false;
            }

            var mat = new Material(shader);
            mat.color = new Color(0f, 0f, 0f, 0.5f); // v3f.10.1: 0.38→0.5 (road tối — cần đậm hơn để thấy)
            mat.mainTexture = BuildRadialTexture();
            _cachedMat = mat;
            return true;
        }

        /// <summary>Texture tròn mềm (radial alpha) — mép mờ dần, không lộ góc vuông.</summary>
        private static Texture2D BuildRadialTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha; // mềm hơn ở mép
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
