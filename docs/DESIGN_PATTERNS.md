# 🏗️ Design Patterns — Starveil Runner

> Tài liệu tham khảo về các Design Patterns được sử dụng trong dự án.
> Mục đích: demonstrate kiến thức design pattern cho CV + phỏng vấn Unity Developer.

---

## 📋 Tổng quan Patterns

| Pattern | Loại | File chính | Triển khai |
|---|---|---|---|
| **Strategy** | Behavioral | `IEnemyStrategy.cs` | Enemy behaviors (Chase, Patroller) |
| **Command** | Behavioral | `ICommand.cs` | Input handling + game actions |
| **Factory** | Creational | `ITileFactory.cs`, `IEnemyFactory.cs` | Object creation (tiles, enemies) |
| **Event-Driven** | Behavioral | `GameEvents.cs` | System communication |
| **Singleton** | Creational | `GameManager.cs` | Global state access |
| **Object Pool** | Creational | `ObjectPool.cs` | Performance optimization |
| **State Machine** | Behavioral | `GameManager.cs` | Game state management |
| **ScriptableObject** | Data-Driven | `ObstacleData.cs` | Configuration |

---

## 1. Strategy Pattern

### Định nghĩa
Cho phép thay đổi hành vi của object tại runtime bằng cách inject strategy object thay vì hardcode logic.

### Tại sao dùng trong game
- Mỗi loại enemy có hành vi DI CHUYỂN khác nhau
- Flying Beetle: đuổi theo player, 2 nấc cố định
- PatrollerDrone: lắc ngang lane, predicted movement
- Thêm enemy mới = thêm class strategy, KHÔNG sửa code cũ (Open/Closed Principle)

### Cấu trúc

```
IEnemyStrategy (interface)
├── ChaseStrategy.cs      → logic đuổi theo (Flying Beetle)
├── PatrollerStrategy.cs  → logic lắc ngang (PatrollerDrone)
└── SniperStrategy.cs     → enemy mới? → chỉ thêm file

EnemyChase.cs     → giữ lifecycle (Awake, OnEnable, OnDisable)
                  → delegate movement → ChaseStrategy.Execute()
PatrollerDrone.cs → giữ lifecycle
                  → delegate movement → PatrollerStrategy.Execute()
```

### Code example

```csharp
// Interface
public interface IEnemyStrategy
{
    string StrategyName { get; }
    void Execute(Transform self, Transform player, float deltaTime);
    void ResetState();
    void Setup(Transform player);
}

// Implementation
public class ChaseStrategy : IEnemyStrategy
{
    public string StrategyName => "Chase";
    
    public void Execute(Transform self, Transform player, float deltaTime)
    {
        // Logic đuổi theo player
        float targetDistance = _stage == 1 ? _closeDistance : _baseDistance;
        Vector3 target = player.position - Vector3.forward * targetDistance;
        self.position = Vector3.MoveTowards(self.position, target, speed * deltaTime);
    }
}

// Usage
public class EnemyChase : MonoBehaviour
{
    private ChaseStrategy _strategy;
    
    private void Awake()
    {
        _strategy = new ChaseStrategy();
    }
    
    private void LateUpdate()
    {
        // Delegate movement to strategy
        _strategy.Execute(transform, player, Time.deltaTime);
    }
}
```

### Files

| File | Vai trò |
|---|---|
| `Core/Interfaces/IEnemyStrategy.cs` | Interface definition |
| `Core/World/Strategies/ChaseStrategy.cs` | Flying Beetle behavior |
| `Core/World/Strategies/PatrollerStrategy.cs` | PatrollerDrone behavior |
| `Core/World/EnemyChase.cs` | MonoBehaviour uses ChaseStrategy |
| `Core/World/PatrollerDrone.cs` | MonoBehaviour uses PatrollerStrategy |

---

## 2. Command Pattern

### Định nghĩa
Đóng gói mỗi hành động thành 1 object có `Execute()` và `Undo()`. Controller chỉ gọi command, không biết chi tiết bên trong.

### Tại sao dùng trong game
- Input handling phức tạp (keyboard + swipe + UI buttons)
- Mỗi input = 1 command object
- Dễ test (mock command), dễ thêm input mới (chỉ thêm class)
- History support (undo/redo nếu cần)

### Cấu trúc

```
ICommand (interface)
├── MoveLeftCommand.cs    → di chuyển trái
├── MoveRightCommand.cs   → di chuyển phải
├── PauseCommand.cs       → toggle pause
└── RestartCommand.cs     → restart game

PlayerController.cs → ExecuteCommand(ICommand)
                     → UndoLastCommand()
InputReader.cs      → tạo command theo input
```

### Code example

```csharp
// Interface
public interface ICommand
{
    void Execute();
    void Undo();
    string CommandName { get; }
}

// Implementation
public class MoveLeftCommand : ICommand
{
    private readonly PlayerController _player;
    
    public MoveLeftCommand(PlayerController player)
    {
        _player = player;
    }
    
    public void Execute() => _player.MoveLeft();
    public void Undo() => _player.MoveRight();
    public string CommandName => "MoveLeft";
}

// Usage in PlayerController
public class PlayerController : MonoBehaviour
{
    private readonly Stack<ICommand> _commandHistory = new Stack<ICommand>();
    
    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        _commandHistory.Push(command);
    }
    
    public void UndoLastCommand()
    {
        if (_commandHistory.Count > 0)
        {
            var cmd = _commandHistory.Pop();
            cmd.Undo();
        }
    }
}
```

### Files

| File | Vai trò |
|---|---|
| `Core/Interfaces/ICommand.cs` | Interface definition |
| `Core/Commands/MoveLeftCommand.cs` | Move left action |
| `Core/Commands/MoveRightCommand.cs` | Move right action |
| `Core/Commands/PauseCommand.cs` | Pause toggle action |
| `Core/Commands/RestartCommand.cs` | Restart action |
| `Core/Player/PlayerController.cs` | Executes commands |

---

## 3. Factory Pattern

### Định nghĩa
Tách việc "tạo object" ra khỏi logic sử dụng. Khi muốn thay đổi cách tạo → chỉ sửa Factory, không sửa người dùng.

### Tại sao dùng trong game
- Tile spawning: pool pattern + factory pattern kết hợp
- Enemy spawning: tạo enemy theo type mà không hardcode Instantiate
- Thay đổi prefab/cách tạo = chỉ sửa Factory

### Cấu trúc

```
ITileFactory (interface)
├── DefaultTileFactory.cs → tạo tile từ prefab

IEnemyFactory (interface)
└── EnemyFactory.cs       → tạo enemy theo type (Chase/Patroller)

TileSpawner.cs    → gọi ITileFactory.Create()
PatrollerSpawner.cs → gọi IEnemyFactory.Create()
```

### Code example

```csharp
// Interface
public interface ITileFactory
{
    Tile Create();
    void Release(Tile tile);
}

// Implementation
public class DefaultTileFactory : ITileFactory
{
    private readonly Tile _prefab;
    private readonly Transform _parent;
    
    public Tile Create()
    {
        Tile tile = Object.Instantiate(_prefab, _parent);
        tile.name = "Tile";
        tile.gameObject.SetActive(false);
        return tile;
    }
    
    public void Release(Tile tile)
    {
        if (tile != null) tile.Deactivate();
    }
}

// Usage in TileSpawner
public class TileSpawner : MonoBehaviour
{
    private ITileFactory _tileFactory;
    
    private void Awake()
    {
        _tileFactory = new DefaultTileFactory(tilePrefab, transform);
    }
    
    private Tile CreateTile()
    {
        return _tileFactory.Create(); // thay vì Instantiate()
    }
}
```

### Files

| File | Vai trò |
|---|---|
| `Core/Interfaces/ITileFactory.cs` | Tile factory interface |
| `Core/Interfaces/IEnemyFactory.cs` | Enemy factory interface |
| `Core/Factories/DefaultTileFactory.cs` | Creates tiles from prefab |
| `Core/Factories/EnemyFactory.cs` | Creates enemies by type |
| `Core/World/TileSpawner.cs` | Uses ITileFactory |
| `Core/World/PatrollerSpawner.cs` | Uses IEnemyFactory |

---

## 4. Event-Driven Pattern (Observer)

### Định nghĩa
Các hệ thống giao tiếp qua event thay vì gọi trực tiếp. Publisher không biết subscriber, subscriber không biết publisher.

### Tại sao dùng trong game
- Phân tán logic: ScoreSystem, UIManager, AudioManager, VFXManager... đều lắng nghe event
- Loose coupling: thêm hệ thống mới = chỉ subscribe event, không sửa code cũ
- Dễ test: mock event thay vì mock toàn bộ hệ thống

### Triển khai

```csharp
// GameEvents.cs — static event hub
public static class GameEvents
{
    public static event Action OnGameStarted;
    public static event Action OnGameOver;
    public static event Action OnRestart;
    public static event Action<int> OnLaneChanged;
    public static event Action<int> OnCoinCollected;
    public static event Action OnObstacleHit;
    public static event Action<PowerUpType> OnPowerUpActivated;
    
    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseRestart() => OnRestart?.Invoke();
    // ...
}

// Subscriber example
public class ScoreSystem : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.OnCoinCollected += HandleCoinCollected;
        GameEvents.OnObstacleHit += HandleObstacleHit;
    }
    
    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= HandleCoinCollected;
        GameEvents.OnObstacleHit -= HandleObstacleHit;
    }
}
```

---

## 5. Singleton Pattern

### Định nghĩa
Đảm bảo chỉ có 1 instance của class, truy cập toàn cục qua `ClassName.Instance`.

### Tại sao dùng trong game
- GameManager: trạng thái game toàn cục
- AudioManager: âm thanh xuyên scene
- DifficultyManager: cấu hình độ khó

### Triển khai

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
```

### Files

| File | Instance |
|---|---|
| `GameManager.cs` | `GameManager.Instance` |
| `AudioManager.cs` | `AudioManager.Instance` |
| `DifficultyManager.cs` | `DifficultyManager.Instance` |
| `PowerUpSystem.cs` | `PowerUpSystem.Instance` |
| `VFXManager.cs` | `VFXManager.Instance` |

---

## 6. Object Pool Pattern

### Định nghĩa
Tái sử dụng object thay vì Instantiate/Destroy liên tục. Tránh GC allocation + performance spike.

### Tại sao dùng trong game
- Tile: 12 tile tái sử dụng (track vô tận)
- Popup text: 8 popup tái sử dụng
- Không Instantiate/Destroy giữa chừng = 60 FPS ổn định

### Triển khai

```csharp
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _available = new Queue<T>();
    private readonly Func<T> _factory;
    private readonly Action<T> _onRelease;
    
    public T Get()
    {
        return _available.Count > 0 ? _available.Dequeue() : _factory();
    }
    
    public void Release(T item)
    {
        _onRelease?.Invoke(item);
        _available.Enqueue(item);
    }
}
```

---

## 7. State Machine Pattern

### Định nghĩa
Quản lý trạng thái game qua enum + transition rules. Mỗi trạng thái có hành vi riêng.

### Triển khai

```csharp
public enum GameState { Menu, Playing, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    public GameState State { get; private set; } = GameState.Menu;
    
    public void StartGame()
    {
        State = GameState.Playing;
        GameEvents.RaiseGameStarted();
    }
    
    public void SetPaused(bool paused)
    {
        if (paused && State == GameState.Playing) State = GameState.Paused;
        else if (!paused && State == GameState.Paused) State = GameState.Playing;
    }
}
```

### gate pattern
Mọi hệ thống check State trước khi chạy:
```csharp
if (GameManager.Instance.State != GameState.Playing) return;
```

---

## 8. ScriptableObject (Data-Driven)

### Định nghĩa
Tách data khỏi code. Designer thay đổi giá trị qua Inspector mà không sửa script.

### Files

| File | Dữ liệu |
|---|---|
| `ObstacleData.cs` | Loại obstacle (prefab, spawn weight) |
| `PowerUpData.cs` | Loại power-up (hiệu ứng, duration) |
| `LeaderboardConfig.cs` | Cấu hình leaderboard (URL, key) |

---

## 📊 Đánh giá

```
Design Patterns:      ████████░░  8/10
Clean Architecture:   ████████░░  8/10
Test Coverage:        ████████░░  46 test (25 Edit + 21 Play)
Event-Driven:         ██████████  10/10 (tất cả hệ thống giao tiếp qua event)
```

### Pattern quan trọng nhất cho phỏngerview

1. **Strategy** — "Làm sao để thêm enemy mới mà không sửa code cũ?"
2. **Event-Driven** — "Làm sao các hệ thống không phụ thuộc nhau?"
3. **Object Pool** — "Làm sao giữ 60 FPS?"
4. **State Machine** — "Làm sao quản lý trạng thái game?"

---

## 📝 Ghi chú

- Tất cả patterns đều được **verifie qua test** (46/46 PASSED)
- Không over-engineering: chỉ dùng pattern khi CẦN (không vì có pattern)
- Unity-specific: MonoBehaviour lifecycle (Awake/OnEnable/OnDisable)会影响 cách implement patterns
