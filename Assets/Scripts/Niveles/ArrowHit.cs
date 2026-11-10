using UnityEngine;

namespace PrimalEcho
{
    // Hace que la flecha dañe enemigos (robots, avispas, plantas) y se destruya al impactar.
    public sealed class ArrowHit : MonoBehaviour
    {
        private Vector2 dir;
        private int damage = 2;
        private bool done;

        public void Init(Vector2 direction, int dmg) { dir = direction; damage = dmg; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (done || other.GetComponentInParent<PlayerController>() != null) return;
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy == null) return;
            done = true;
            KaelVFX.Impact(other.ClosestPoint(transform.position), enemy);
            enemy.Hit(damage, dir * 2f);
            Destroy(gameObject);
        }
    }
}
