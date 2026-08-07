using UnityEngine;

namespace VoidRunner.Core.World
{
    /// <summary>Một đoạn track — được tái sử dụng qua ObjectPool (không Instantiate/Destroy giữa chừng).</summary>
    public class Tile : MonoBehaviour
    {
        [SerializeField] private float length = 10f;

        public float Length => length;

        public void Activate(Vector3 position)
        {
            transform.position = position;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            // Dọn obstacle con khi tile về pool (G2 sẽ pool luôn obstacle)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            gameObject.SetActive(false);
        }

        public bool IsBehind(Transform reference, float distance)
        {
            return transform.position.z < reference.position.z - distance;
        }
    }
}
