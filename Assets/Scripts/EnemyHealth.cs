using UnityEngine;

namespace PrimalEcho
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int health = 6;
        private Rigidbody2D body;
        public int Remaining => health;
        public void SetHealth(int value) => health = Mathf.Max(1, value);
        private void Awake() => body = GetComponent<Rigidbody2D>();

        public bool Hit(int damage, Vector2 impulse)
        {
            if (health <= 0) return false;
            health = Mathf.Max(0, health - damage);
            body.AddForce(impulse, ForceMode2D.Impulse);
            if (health == 0) Destroy(gameObject);
            return true;
        }
    }
}
