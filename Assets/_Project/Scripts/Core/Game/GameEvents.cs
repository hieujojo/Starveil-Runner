using System;

namespace VoidRunner.Core
{
    /// <summary>
    /// Event hub tĩnh — các hệ thống giao tiếp qua event, không coupling trực tiếp.
    /// Hệ thống khác chỉ subscribe (event) và raise (RaiseXxx).
    /// </summary>
    public static class GameEvents
    {
        public static event Action OnGameStarted;
        public static event Action OnGameOver;
        public static event Action OnRestart;
        public static event Action<int> OnLaneChanged;
        public static event Action<int> OnCoinCollected;
        public static event Action OnObstacleHit;

        public static void RaiseGameStarted() => OnGameStarted?.Invoke();
        public static void RaiseGameOver() => OnGameOver?.Invoke();
        public static void RaiseRestart() => OnRestart?.Invoke();
        public static void RaiseLaneChanged(int lane) => OnLaneChanged?.Invoke(lane);
        public static void RaiseCoinCollected(int coins) => OnCoinCollected?.Invoke(coins);
        public static void RaiseObstacleHit() => OnObstacleHit?.Invoke();
    }
}
