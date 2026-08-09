using UnityEngine;
using VoidRunner.Core.Player;
using VoidRunner.Data;
using VoidRunner.Systems.PowerUp;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Pickup power-up — player chạm là kích hoạt hiệu ứng qua PowerUpSystem.
    /// Data gán trên prefab (Inspector) hoặc qua SetData khi spawn từ PickupSpawner.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PowerUpPickup : MonoBehaviour
    {
        [SerializeField] private PowerUpData data;

        public PowerUpData Data => data;

        /// <summary>Gán data khi spawn từ PickupSpawner (runtime).</summary>
        public void SetData(PowerUpData powerUpData) => data = powerUpData;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            if (data == null)
            {
                Debug.LogWarning("PowerUpPickup thiếu data — không kích hoạt được.", this);
                return;
            }

            if (PowerUpSystem.Instance != null)
            {
                PowerUpSystem.Instance.Activate(data);
            }
            Destroy(gameObject);
        }
    }
}
