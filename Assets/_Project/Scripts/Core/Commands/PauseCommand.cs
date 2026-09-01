using VoidRunner.Core.Interfaces;

namespace VoidRunner.Core.Commands
{
    /// <summary>
    /// COMMAND PATTERN — Toggle pause game.
    /// PauseManager gọi command thay vì gọi GameManager trực tiếp.
    /// </summary>
    public class PauseCommand : ICommand
    {
        public string CommandName => "Pause";

        public void Execute()
        {
            if (GameManager.Instance == null) return;

            bool isPaused = GameManager.Instance.State == GameState.Paused;
            GameManager.Instance.SetPaused(!isPaused);
        }

        public void Undo()
        {
            Execute(); // toggle lại
        }
    }
}
