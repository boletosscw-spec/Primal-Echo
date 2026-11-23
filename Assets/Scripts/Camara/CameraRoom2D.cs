using UnityEngine;

namespace PrimalEcho
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class CameraRoom2D : MonoBehaviour
    {
        public PolygonCollider2D Bounds => GetComponent<PolygonCollider2D>();
        private CameraFollow2D follow;

        private void Reset() => Bounds.isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.GetComponentInParent<PlayerController>()) return;
            follow = Camera.main ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (follow) follow.SetRoom(this, true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (follow && other.GetComponentInParent<PlayerController>()) follow.SetRoom(this, false);
        }

        private void OnDisable()
        {
            if (follow) follow.SetRoom(this, false);
        }
    }
}
