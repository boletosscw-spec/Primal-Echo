using UnityEngine;

namespace PrimalEcho
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int health = 6;
        private Rigidbody2D body;
        /// <summary>Se lanza justo antes de destruir al enemigo vencido.</summary>
        public static event System.Action<EnemyHealth> Died;
        /// <summary>Daño real recibido por este enemigo (lo usan los jefes para el destello).</summary>
        public event System.Action<int> Golpeado;
        /// <summary>
        /// Opcional (jefes): recibe el daño y el impulso del golpe y devuelve el daño final.
        /// Devolver 0 = golpe bloqueado (p. ej. el escudo de Atlas).
        /// </summary>
        public System.Func<int, Vector2, int> Modificar { get; set; }
        /// <summary>Los jefes ponen false para reproducir su animación de muerte antes de desaparecer.</summary>
        public bool DestruirAlMorir { get; set; } = true;
        public int Remaining => health;
        public void SetHealth(int value) => health = Mathf.Max(1, value);
        private void Awake() => body = GetComponent<Rigidbody2D>();

        public bool Hit(int damage, Vector2 impulse)
        {
            if (health <= 0) return false;
            if (Modificar != null)
            {
                damage = Modificar(damage, impulse);
                if (damage <= 0) return false;
            }
            health = Mathf.Max(0, health - damage);
            body.AddForce(impulse, ForceMode2D.Impulse);
            Golpeado?.Invoke(damage);
            if (health == 0)
            {
                Died?.Invoke(this);
                if (DestruirAlMorir) Destroy(gameObject);
            }
            return true;
        }
    }
}
