using System;
using System.Collections;
using UnityEngine;

namespace PrimalEcho
{
    // Reaparición estilo Hollow Knight:
    //  · Espinas y fosos: -1 de vida y vuelta al último suelo seguro.
    //  · Sin vida: vuelta al último checkpoint (farola) con la vida llena.
    [RequireComponent(typeof(PlayerController), typeof(KaelHealth))]
    public sealed class KaelRespawn : MonoBehaviour
    {
        [SerializeField] private float killHeight = -3f;
        [SerializeField, Min(0.1f)] private float deathDelay = 1.4f;
        [SerializeField, Min(0f)] private float hazardDelay = 0.45f;
        [SerializeField, Min(0f)] private float graceSeconds = 1.2f;
        [SerializeField] private string startName = "Inicio";
        [SerializeField] private LayerMask groundLayers = 1 << 6;

        private PlayerController player;
        private KaelHealth health;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private Vector3 checkpoint, safeGround;
        private float nextSample;
        private bool busy;
        private readonly Collider2D[] nearby = new Collider2D[16];

        public string CheckpointName { get; private set; }
        public event Action Respawned;
        public event Action<string> CheckpointChanged;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            health = GetComponent<KaelHealth>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            checkpoint = safeGround = transform.position;
            CheckpointName = startName;
        }

        private void Update()
        {
            if (busy) return;
            if (health.Remaining <= 0) { StartCoroutine(DeathRespawn()); return; }
            if (transform.position.y < killHeight) { HazardRespawn(); return; }
            if (player.IsGrounded && Time.time >= nextSample)
            {
                nextSample = Time.time + 0.1f;
                if (IsSafeHere()) safeGround = transform.position;
            }
        }

        private bool IsSafeHere()
        {
            Bounds b = bodyCollider.bounds;
            // Debe estar del todo sobre suelo firme (no en el borde) y no sobre piedras que se derrumban
            Vector2 feet = new Vector2(b.center.x, b.min.y - 0.08f);
            for (int side = -1; side <= 1; side += 2)
            {
                var under = Physics2D.OverlapPoint(feet + Vector2.right * side * 0.45f, groundLayers);
                if (under == null || under.GetComponentInParent<CrumbleBlock>() != null) return false;
            }
            int count = Physics2D.OverlapCircleNonAlloc(b.center, 1.6f, nearby);
            for (int i = 0; i < count; i++)
            {
                if (nearby[i].GetComponentInParent<HazardZone>() != null) return false;
                if (nearby[i].GetComponentInParent<EnemyHealth>() != null) return false;
            }
            return true;
        }

        public void SetCheckpoint(Vector3 position, string label)
        {
            health.Restore(0f); // como sentarse en una banca: cura
            if (busy || (checkpoint - position).sqrMagnitude < 0.01f) return;
            checkpoint = safeGround = position;
            CheckpointName = label;
            CheckpointChanged?.Invoke(label);
        }

        public void HazardRespawn()
        {
            if (busy || health.Remaining <= 0) return;
            StartCoroutine(HazardRoutine());
        }

        private IEnumerator HazardRoutine()
        {
            busy = true;
            health.Hit(1, transform.position + Vector3.down);
            if (health.Remaining <= 0)
            {
                busy = false;
                yield break; // Update se encarga de la muerte
            }
            yield return new WaitForSeconds(hazardDelay);
            Teleport(safeGround);
            busy = false;
        }

        private IEnumerator DeathRespawn()
        {
            busy = true;
            yield return new WaitForSeconds(deathDelay);
            Teleport(checkpoint);
            health.Restore(graceSeconds);
            player.ResetState();
            // El Animator se quedaba en el estado de muerte (tirado): se reinicia al estado inicial
            var anim = GetComponent<Animator>();
            if (anim != null) { anim.Rebind(); anim.Update(0f); }
            player.PlaySpawn();
            Respawned?.Invoke();
            busy = false;
        }

        private void Teleport(Vector3 position)
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = position;
            }
            transform.position = position;
        }
    }
}
