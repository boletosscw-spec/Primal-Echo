using UnityEngine;

namespace PrimalEcho
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class KaelHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int health = 5;
        [SerializeField, Min(0f)] private float invulnerability = 2f;
        [SerializeField, Min(.01f)] private float blinkInterval = .09f;
        [SerializeField] private Vector2 knockback = new Vector2(6, 5);
        [SerializeField] private float controlDelay = .16f;
        private float vulnerableAt;
        private PlayerController player;
        private SpriteRenderer visual;
        private bool blinking;
        private int maxHealth;
        public int Remaining => health;
        public int Max => maxHealth > 0 ? maxHealth : health;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            maxHealth = health;
            visual = GetComponentInChildren<SpriteRenderer>();
        }

        public void IncreaseMaxHealth(int amount)
        {
            maxHealth = Max + amount;
            health = Mathf.Min(maxHealth, health + amount);
        }

        /// <summary>Recupera vida sin pasar del máximo. Devuelve false si ya estaba llena.</summary>
        public bool Heal(int amount)
        {
            if (health <= 0 || health >= Max) return false;
            health = Mathf.Min(Max, health + amount);
            return true;
        }

        public void Hit(int damage, Vector2 source)
        {
            if (!player.CanAct || Time.time < vulnerableAt || health <= 0) return;
            health = Mathf.Max(0, health - damage);
            float side = transform.position.x >= source.x ? 1f : -1f;
            var direction = new Vector2(side * knockback.x, knockback.y);
            if (Camera.main && Camera.main.TryGetComponent(out CameraFollow2D camera)) camera.Shake(direction);
            if (health == 0)
            {
                player.PlayDeath();
                return;
            }
            vulnerableAt = Time.time + invulnerability;
            blinking = true;
            player.PlayHurt();
            player.ApplyImpulse(direction, controlDelay);
        }

        /// <summary>Vida llena y unos segundos de invulnerabilidad (lo usa KaelRespawn).</summary>
        public void Restore(float graceSeconds)
        {
            health = Max;
            vulnerableAt = Time.time + graceSeconds;
            blinking = graceSeconds > 0f;
        }

        private void LateUpdate()
        {
            if (!blinking) return;
            blinking = Time.time < vulnerableAt;
            if (visual) visual.enabled = !blinking || Mathf.FloorToInt((vulnerableAt - Time.time) / blinkInterval) % 2 == 0;
        }

        private void OnDisable()
        {
            if (blinking && visual) visual.enabled = true;
            blinking = false;
        }
    }
}
