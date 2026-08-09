# Repository Workflow

## Delivery và Commits

- Với mỗi yêu cầu thay đổi file trong repository, hãy thực hiện công việc, validate, rồi tự động tạo commit.
- Không commit nếu validation thất bại. Báo lỗi và sửa trước khi commit.
- Giữ mỗi commit tập trung vào một thay đổi nhất quán.

## Quy ước Commit

### Định dạng
```
<type>(<scope>): <subject>
```

### Các Type được phép
| Type | Khi nào dùng |
|---|---|
| `feat` | Thêm tính năng mới (gameplay, hệ thống, UI) |
| `fix` | Sửa bug |
| `refactor` | Refactor code, không thay đổi logic |
| `chore` | Cập nhật package, cấu hình, build settings, gitignore |
| `opt` | Tối ưu hiệu năng, FPS, memory, GC allocation |
| `test` | Thêm/sửa test |
| `build` | Build WebGL và triển khai (itch.io, Unity Play) |
| `docs` | README, plan, tài liệu |

### Các Scope được phép
| Scope | Mô tả |
|---|---|
| `core` | GameManager, state machine, luồng game, GameEvents |
| `player` | PlayerController, điều khiển lane, physics |
| `world` | TileSpawner, Tile, object pool, track |
| `void` | VoidChase, NavMesh AI, tốc độ/kích thước void |
| `obstacle` | ObstacleManager, ObstacleData, loại obstacle |
| `pickup` | Coin, thu thập, Rotator |
| `powerup` | PowerUpSystem, PowerUpData, hiệu ứng shield/magnet/slow-mo |
| `score` | ScoreSystem, combo multiplier, event score |
| `audio` | AudioManager, SFX, BGM, volume |
| `save` | SaveSystem, PlayerPrefs, best score |
| `difficulty` | DifficultyManager, AnimationCurve, tuning |
| `ui` | HUD, MainMenu, GameOver, fade, màn hình |
| `scene` | Setup scene, camera, light, NavMesh bake |
| `prefab` | Tạo/cập nhật prefab |
| `data` | ScriptableObject instances |
| `vfx` | Particle, post-processing, screen shake, trail |
| `config` | Packages/manifest.json, ProjectSettings, .gitignore |
| `deps` | Dependencies và lockfile |
| `build` | WebGL build settings, deploy |

### Ví dụ
```
feat(player): viết lại điều khiển chuyển lane 3 làn
feat(world): thêm TileSpawner dùng object pool
feat(void): thêm AI đuổi theo, tốc độ tăng dần theo thời gian
fix(world): vá lỗi hở giữa các tile khi spawn nhanh
opt(world): giảm GC alloc trong vòng lặp recycle tile
feat(ui): thêm màn Game Over hiển thị best score
chore(config): cài package Cinemachine và DOTween
build(build): build WebGL v1.0 với Brotli và publish itch.io
docs(readme): cập nhật README với link demo
```

### Quy tắc
1. Subject dùng tiếng Việt **có đầy đủ dấu** (không viết tắt không dấu), nhất quán trong 1 PR — ví dụ `feat(player): thêm khả năng chuyển lane` (không phải `feat(player): them kha nang chuyen lane`)
2. Subject **KHÔNG** viết hoa chữ đầu
3. Subject **KHÔNG** có dấu chấm cuối
4. Viết commit body khi cần giải thích thêm logic hoặc lý do thay đổi
5. Asset của Unity (`prefab`, `scene`, `.meta`) luôn commit **kèm trong cùng commit** với code liên quan — không commit file `.meta` riêng lẻ, không xóa `.meta` của asset đang được tham chiếu

## Validation

- Chạy các kiểm tra hẹp nhất trước, sau đó mở rộng khi cần.
- Luôn chạy `git diff --check` trước khi commit.
- ⚠️ Unity: `git diff --check` sẽ luôn báo trailing whitespace ở file `.meta` (Unity sinh sẵn `userData: `/`assetBundleName: ` có space cuối — là chuẩn) và thư mục vendor (`Assets/Plugins/Demigiant/DOTween`) — **bỏ qua các cảnh báo này**, chỉ quan tâm cảnh báo từ code C# của mình.
- Thay đổi **C# script**: mở Unity Editor và xác nhận Console **không có lỗi compile** (hoặc chạy Unity batchmode `-batchmode -quit` để verify import). Playtest nhanh tính năng liên quan.
- Thay đổi **scene/prefab**: mở scene trong Unity, kiểm tra **không missing script / missing reference**, playtest nhanh luồng liên quan.
- Thay đổi **package/config**: kiểm tra `Packages/manifest.json` hợp lệ và Unity import không lỗi.
- Thay đổi **build**: build WebGL và chạy thử trên trình duyệt.
- Không commit nếu validation thất bại — báo lỗi và sửa trước.

## Documentation

- Review `README.md` sau mỗi thay đổi.
- Cập nhật `README.md` trong cùng commit khi có thay đổi về setup, cấu hình, lệnh, kiến trúc, hoặc hành vi người dùng thấy được.
- Không chỉnh tài liệu cho các thay đổi nội bộ không ảnh hưởng đến cách dùng repository.
