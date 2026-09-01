namespace VoidRunner.Core.Interfaces
{
    /// <summary>
    /// COMMAND PATTERN — Interface cho mọi hành động trong game.
    /// Mỗi input = 1 Command object: Execute() thực hiện, Undo() đảo lại.
    /// PlayerController chỉ gọi command.Execute(), không biết chi tiết bên trong.
    ///
    /// Ưu điểm: dễ test (mock command), dễ thêm input mới (chỉ thêm class),
    /// dễ undo/redo (nếu cần).
    /// </summary>
    public interface ICommand
    {
        /// <summary>Thực hiện hành động.</summary>
        void Execute();

        /// <summary>Đảo hành động (nếu khả thi).</summary>
        void Undo();

        /// <summary>Tên hành động (debug/log).</summary>
        string CommandName { get; }
    }
}
