using VoidRunner.Core.World;

namespace VoidRunner.Core.Interfaces
{
    /// <summary>
    /// FACTORY PATTERN — Interface cho việc tạo tile.
    /// TileSpawner gọi ITileFactory.Create() thay vì Instantiate() trực tiếp.
    /// Thay đổi cách tạo tile = chỉ sửa Factory, không sửa TileSpawner.
    /// </summary>
    public interface ITileFactory
    {
        Tile Create();
        void Release(Tile tile);
    }
}
