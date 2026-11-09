using UnityEngine;

namespace PrimalEcho
{
    // Espinas: la zona de daño se ajusta al dibujo de la planta (antes era más grande que el sprite)
    // y solo hacen daño con empujón (ContactHazard). Ya no regresan a Kael ni le bloquean el paso.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class HazardZone : MonoBehaviour
    {
        [Tooltip("Si está activo, además de dañar regresa a Kael al último suelo seguro (como Hollow Knight)")]
        [SerializeField] private bool respawnOnTouch = false;
        [SerializeField, Min(0.1f)] private float height = 0.4f;
        [SerializeField, Min(0f)] private float sideInset = 0.25f;

        private void Awake()
        {
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            float oldBottom = box.offset.y - box.size.y * 0.5f;
            box.size = new Vector2(Mathf.Max(0.3f, box.size.x - sideInset * 2f), height);
            box.offset = new Vector2(box.offset.x, oldBottom + height * 0.5f);
        }

        private void OnTriggerEnter2D(Collider2D other) => Hit(other);
        private void OnTriggerStay2D(Collider2D other) => Hit(other);

        private void Hit(Collider2D other)
        {
            if (!respawnOnTouch) return;
            var respawn = other.GetComponentInParent<KaelRespawn>();
            if (respawn != null) respawn.HazardRespawn();
        }
    }
}
