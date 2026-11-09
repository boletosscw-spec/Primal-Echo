using UnityEngine;

namespace PrimalEcho
{
    // Proyectil del disparo de Kael: rápido, en línea recta, daña al primer enemigo y choca con el terreno.
    public sealed class EnergyShot : MonoBehaviour
    {
        private Vector2 dir;
        private int damage;
        private Color color;
        private Sprite spark;
        private bool done;

        public void Init(Vector2 direction, int dmg, float life, Color tint, Sprite sparkSprite)
        {
            dir = direction; damage = dmg; color = tint; spark = sparkSprite;
            Destroy(gameObject, life);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (done || other.GetComponentInParent<PlayerController>() != null) return;
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.Hit(damage, dir * 1.5f);
                Pop();
                return;
            }
            // Terreno (suelo, paredes, compuertas); ignora triggers como checkpoints o géiseres
            if (!other.isTrigger && other.gameObject.layer == 6) Pop();
        }

        private void Pop()
        {
            done = true;
            if (spark != null)
            {
                var go = new GameObject("Impacto");
                go.transform.position = transform.position;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spark; sr.color = color; sr.sortingOrder = 13;
                go.transform.localScale = Vector3.one * 1.6f;
                Destroy(go, 0.06f);
            }
            Destroy(gameObject);
        }
    }
}
