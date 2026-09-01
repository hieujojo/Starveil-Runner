# 🧪 TESTING — Kết quả Test

> **Cập nhật:** 2026-09-01

## Tổng quan

| Assembly | Số test | Trạng thái |
|---|---|---|
| **EditMode** | 25 | ✅ 25/25 PASSED |
| **PlayMode** | 21 | ✅ 21/21 PASSED |
| **Tổng** | **46** | ✅ **46/46 PASSED** |

## Chi tiết EditMode (25 test)

| File | Số test | Nội dung |
|---|---|---|
| `GameEventsTests.cs` | 5 | Event subscribe/unsubscribe/raise |
| `LeaderboardServiceEditTests.cs` | 9 | SanitizeName ×6 + ParseTopScores ×3 |
| `SaveSystemTests.cs` | 6 | Save/Load/Delete ship, score, volume |
| `ScoreSystemEditTests.cs` | 5 | AddScore, Multiplier, Combo logic |

## Chi tiết PlayMode (21 test)

| File | Số test | Nội dung |
|---|---|---|
| `EnemyChasePlayTests.cs` | 5 | 2-stage chase + relax + catch |
| `PatrollerDronePlayTests.cs` | 3 | Patrol lateral + ahead of player |
| `PlayerControllerPlayTests.cs` | 3 | MoveLeft/Right + lane clamping |
| `ScoreSystemPlayTests.cs` | 5 | Score + combo + coin + distance |
| `LeaderboardViewPlayTests.cs` | 3 | Panel show/hide + idempotent |
| `ObstacleCenterPlayTests.cs` | 2 | Obstacle spawn centering |

## Bugs đã fix trong quá trình test

| Bug | Nguyên nhân | Fix |
|---|---|---|
| `ChaseStrategy._currentDistance = 0` | Không init distance trong Configure() | Thêm `_currentDistance = baseDist` |
| `HandleObstacleHit` order | check Stage trước IsCatching → CatchAndKill never called | Đảo thứ tự check |
| `ScoreSystem` test fail | FindAnyObjectByType fail trong test context | Set player ref qua reflection |
| `PatrollerDrone` test fail | GameManager singleton stale giữa tests | Clear static Instance trong TearDown |
| CS0414 warnings | SerializeField chỉ dùng ở Awake() | `#pragma warning disable CS0414` |

## Cách chạy test

```
Unity Editor → Window → General → Test Runner
→ Tab EditMode → Run All
→ Tab PlayMode → Run All
→ Kết quả: 46/46 PASSED
```

## Design Patterns trong code

| Pattern | Files | Mục đích |
|---|---|---|
| **Strategy** | `IEnemyStrategy`, `ChaseStrategy`, `PatrollerStrategy` | Tách hành vi enemy ra Strategy class |
| **Command** | `ICommand`, `MoveLeftCommand`, `MoveRightCommand`, `PauseCommand`, `RestartCommand` | Đóng gói hành động input thành object |
| **Factory** | `ITileFactory`, `IEnemyFactory`, `DefaultTileFactory`, `EnemyFactory` | Tách việc tạo object ra khỏi logic sử dụng |
| **Event-Driven** | `GameEvents` | Giao tiếp hệ thống qua event, không coupling |
| **Singleton** | `GameManager`, `AudioManager`, etc. | Instance management |
| **Object Pool** | `ObjectPool<T>` | Tái sử dụng tile, không Instantiate/Destroy |
| **State Machine** | `GameManager.State` enum | Gate mọi hệ thống theo trạng thái |
| **ScriptableObject** | `ObstacleData`, `PowerUpData`, `LeaderboardConfig` | Data-driven config |

Documentation chi tiết: `docs/DESIGN_PATTERNS.md`
