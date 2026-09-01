using VoidRunner.Core.Interfaces;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.Commands
{
    /// <summary>
    /// COMMAND PATTERN — Di chuyển player sang lane phải.
    /// </summary>
    public class MoveRightCommand : ICommand
    {
        public string CommandName => "MoveRight";

        private readonly PlayerController _player;

        public MoveRightCommand(PlayerController player)
        {
            _player = player;
        }

        public void Execute()
        {
            if (_player != null)
                _player.MoveRight();
        }

        public void Undo()
        {
            if (_player != null)
                _player.MoveLeft();
        }
    }
}
