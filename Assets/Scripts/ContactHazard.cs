using UnityEngine;

namespace PrimalEcho
{
    public sealed class ContactHazard : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private bool allowsPogo = true;
        public bool AllowsPogo => allowsPogo;

        private void OnCollisionStay2D(Collision2D hit) => Touch(hit.collider);
        private void OnCollisionEnter2D(Collision2D hit) => Touch(hit.collider);
        private void OnTriggerEnter2D(Collider2D other) => Touch(other);
        private void OnTriggerStay2D(Collider2D other) => Touch(other);

        private void Touch(Collider2D other)
        {
            var health = other.GetComponentInParent<KaelHealth>();
            if (health) health.Hit(damage, transform.position);
        }
    }
}
