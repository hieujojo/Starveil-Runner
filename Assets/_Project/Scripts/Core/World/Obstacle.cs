using UnityEngine;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Marker component cho obstacle — Player phát hiện bằng TryGetComponent (không phụ thuộc tag string).
    /// Tự bật isTrigger cho collider để Player dùng OnTriggerEnter.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Obstacle : MonoBehaviour
    {
        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }
}
