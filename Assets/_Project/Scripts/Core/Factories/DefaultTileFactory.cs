using UnityEngine;
using VoidRunner.Core.Interfaces;

namespace VoidRunner.Core.Factories
{
    /// <summary>
    /// FACTORY PATTERN — Tạo tile từ prefab.
    /// TileSpawner gọi DefaultTileFactory.Create() thay vì Instantiate() trực tiếp.
    /// Thay đổi cách tạo tile (pool, cache, etc.) = chỉ sửa Factory.
    /// </summary>
    public class DefaultTileFactory : ITileFactory
    {
        private readonly Tile _prefab;
        private readonly Transform _parent;

        public DefaultTileFactory(Tile prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public Tile Create()
        {
            Tile tile = Object.Instantiate(_prefab, _parent);
            tile.name = "Tile";
            tile.gameObject.SetActive(false);
            return tile;
        }

        public void Release(Tile tile)
        {
            if (tile != null)
                tile.Deactivate();
        }
    }
}
