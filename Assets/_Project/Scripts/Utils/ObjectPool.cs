using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRunner.Utils
{
    /// <summary>
    /// Generic object pool — tái sử dụng component thay vì Instantiate/Destroy giữa chừng.
    /// Giúp tránh GC spike, giữ FPS ổn định (đặc biệt quan trọng trên WebGL).
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> _available = new Queue<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        /// <param name="factory">Tạo instance mới khi pool cạn.</param>
        /// <param name="onGet">Gọi khi lấy item ra khỏi pool (ví dụ: SetActive(true)).</param>
        /// <param name="onRelease">Gọi khi trả item về pool (ví dụ: SetActive(false)).</param>
        /// <param name="prewarmCount">Số lượng tạo sẵn lúc khởi tạo.</param>
        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null, int prewarmCount = 0)
        {
            _factory = factory;
            _onGet = onGet;
            _onRelease = onRelease;

            for (int i = 0; i < prewarmCount; i++)
            {
                Release(factory());
            }
        }

        public T Get()
        {
            T item = _available.Count > 0 ? _available.Dequeue() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            _onRelease?.Invoke(item);
            _available.Enqueue(item);
        }
    }
}
