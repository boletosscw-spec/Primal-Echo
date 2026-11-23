using UnityEngine;

namespace PrimalEcho
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ArrowProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float gravityScale = 0.12f;
        [SerializeField] private float lifeTime = 3.5f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = gravityScale;
            }
        }

        public void Launch(Vector2 direction)
        {
            direction = direction.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }

            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.2f)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || other.isTrigger) return;
            if (other.GetComponent<PlayerController>() != null) return;
            Destroy(gameObject);
        }
    }
}
