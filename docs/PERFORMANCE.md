# 📊 Performance — Starveil Runner

> Tài liệu đo & tối ưu hiệu năng. Cập nhật mỗi lần chạy Unity Profiler / build WebGL mới.
> **Nguyên tắc:** chỉ ghi con số ĐO ĐƯỢC thật (không đoán), kèm ngày + môi trường đo.

## 🎯 Mục tiêu

| Nền tảng | FPS mục tiêu | Ghi chú |
|---|---|---|
| WebGL (itch.io / Unity Play) | 60 FPS desktop · 30+ FPS máy yếu | Đối tượng chính (đã live) |
| Android (LDPlayer / thiết bị thật) | 60 FPS | Mục 4 UPGRADE_PLAN — chưa build |

## 🧪 Cách đo

1. **Trong Editor** — chỉ để test nhanh (con số nhanh hơn build thật 30–50%):
   - `Window → Analysis → Profiler` → tab CPU Usage → chụp màn hình
2. **Trên WebGL build thật** (con số chuẩn nhất):
   - Build WebGL (Compression Gzip) → mở trên Chrome
   - F12 → Performance tab → Record → chơi 30s → Stop
   - Xem: tổng frame time, phần lớn thời gian nằm ở đâu (Script / Rendering / GC)
3. **Android (khi có build):** LDPlayer hoặc thiết bị thật + `adb shell` / Unity Profiler kết nối device

## 📈 Kết quả đo

> Bảng dưới ghi lại từng lần đo. **Điền số liệu thật sau mỗi lần profile.**

| Ngày | Môi trường | FPS | Frame time | Ghi chú / điểm nóng |
|---|---|---|---|---|
| (trống) | Editor | — | — | — |
| (trống) | WebGL Chrome | — | — | — |

## 🔍 Điểm nóng đã biết & cách giảm

### Texture (đã xử lý 2026-08-15)
- **Trước:** 4 skybox Nebula EXR @2048 → ~100MB trong build (69% build size). Texture chiếm **97.2%** build.
- **Sau:** ép `maxTextureSize` 2048→1024 (**cả WebGL platform override** — override thắng setting default) qua `BuildOptimizerTool` → build ~60MB.
- **Nếu vẫn nặng:** Nebula → 512 (nền tinh vân mờ, khó thấy khác biệt) hoặc đổi skybox động thành 2 material.

### Object pooling (đã có)
- Track vô tận + obstacle + coin dùng `ObjectPool<T>` — không Instantiate/Destroy trong gameplay loop → ít GC.

### Shader strip (đã xử lý 2026-08-15)
- Shader dùng runtime (`Shader.Find`) phải nằm trong **Always Included Shaders** — thiếu → `new Material(null)` → model/VFX mất + ArgumentNullException. Tool: `Tools → Starveil Runner → Fix → Always Included Shaders`.

## ✅ Checklist tối ưu WebGL

- [ ] Compression Format = **Gzip** (KHÔNG Brotli — white screen itch.io, R7.18)
- [ ] Texture import: maxTextureSize ≤1024, compression phù hợp (ASTC/ETC2 cho mobile)
- [ ] `m_AlwaysIncludedShaders` đủ shader runtime (chạy tool Fix → Always Included Shaders)
- [ ] Không `Debug.Log` spam trong gameplay loop (R7.16)
- [ ] UI không dùng `RaycastTarget = true` thừa (text/panel tĩnh → false)
- [ ] Particle maxParticles giới hạn hợp lý (sao 500, exhaust 120, ...)
