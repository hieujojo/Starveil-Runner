using VoidRunner.Core.Interfaces;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.Commands
{
    /// <summary>
    /// COMMAND PATTERN — Di chuyển player sang lane trái.
    /// InputReader tạo command → PlayerController gọi Execute().
    /// </summary>
    public class MoveLeftCommand : ICommand
    {
        public string CommandName => "MoveLeft";

        private readonly PlayerController _player;

        public MoveLeftCommand(PlayerController player)
        {
            _player = player;
        }

        public void Execute()
        {
            if (_player != null)
                _player.MoveLeft();
        }

        public void Undo()
        {
            if (_player != null)
                _player.MoveRight();
        }
    }
}
