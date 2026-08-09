using System.Collections.Generic;
using UnityEngine;
using VoidRunner.Core;
using VoidRunner.Core.Player;

namespace VoidRunner.Core.World
{
    /// <summary>
    /// Coin nhặt được — xoay liên tục (giữ Rotator.cs trên prefab) và phát event khi player chạm.
    /// Có thể bị Magnet hút về phía player (PowerUpSystem gọi PullToward).
    /// Tự đăng ký vào <see cref="Active"/> để PowerUpSystem duyệt không cần FindObjectsByType (tránh GC).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Coin : MonoBehaviour
    {
        /// <summary>Registry coin đang tồn tại — PowerUpSystem (Magnet) duyệt qua list này.</summary>
        public static readonly List<Coin> Active = new List<Coin>();

        [Header("Vật lý hút (Magnet)")]
        [SerializeField] private float pullSpeed = 18f;
        [SerializeField] private float minDistance = 0.5f;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>Magnet kéo coin về phía player — ko dùng physics, chỉ di chuyển thẳng.</summary>
        public void PullToward(Vector3 target, float deltaTime)
        {
            Vector3 direction = (target - transform.position).normalized;
            transform.position += direction * (pullSpeed * deltaTime);
            if (Vector3.Distance(transform.position, target) <= minDistance)
            {
                Collect();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            GameEvents.RaiseCoinCollected(1);
            Destroy(gameObject);
        }
    }
}
