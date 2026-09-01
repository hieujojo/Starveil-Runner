using VoidRunner.Core.Interfaces;

namespace VoidRunner.Core.Commands
{
    /// <summary>
    /// COMMAND PATTERN — Restart game.
    /// UIManager/PauseManager gọi command thay vì gọi GameManager.Restart() trực tiếp.
    /// </summary>
    public class RestartCommand : ICommand
    {
        public string CommandName => "Restart";

        public void Execute()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Restart();
        }

        public void Undo()
        {
            // Restart không thể undo — no-op
        }
    }
}
