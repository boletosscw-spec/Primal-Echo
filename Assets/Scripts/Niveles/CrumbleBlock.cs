using System.Collections;
using UnityEngine;

namespace PrimalEcho
{
    // Piedra que tiembla al pisarla, cae y reaparece.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CrumbleBlock : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float shakeSeconds = 0.45f;
        [SerializeField, Min(0.5f)] private float respawnSeconds = 2.5f;
        [SerializeField] private Transform visual;
        private BoxCollider2D box;
        private SpriteRenderer[] renderers;
        private Vector3 rest;
        private bool busy;

        public void Configure(Transform v) => visual = v;

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (visual != null) rest = visual.localPosition;
        }

        private void OnCollisionEnter2D(Collision2D c) => TryStart(c.collider);
        private void OnCollisionStay2D(Collision2D c) => TryStart(c.collider);

        private void TryStart(Collider2D other)
        {
            if (busy || other.GetComponentInParent<PlayerController>() == null) return;
            if (other.bounds.min.y < box.bounds.max.y - 0.12f) return; // solo si Kael está encima
            StartCoroutine(Crumble());
        }

        private IEnumerator Crumble()
        {
            busy = true;
            float end = Time.time + shakeSeconds;
            while (Time.time < end)
            {
                if (visual != null) visual.localPosition = rest + (Vector3)(Random.insideUnitCircle * 0.05f);
                yield return null;
            }
            if (visual != null) visual.localPosition = rest;
            Set(false);
            yield return new WaitForSeconds(respawnSeconds);
            var player = GameObject.FindGameObjectWithTag("Player");
            while (player != null && box.bounds.Contains(new Vector3(player.transform.position.x, player.transform.position.y + 0.3f, box.bounds.center.z)))
                yield return new WaitForSeconds(0.2f);
            Set(true);
            busy = false;
        }

        private void Set(bool solid)
        {
            box.enabled = solid;
            foreach (var r in renderers) r.enabled = solid;
        }
    }
}
