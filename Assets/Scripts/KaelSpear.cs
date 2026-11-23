using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrimalEcho
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class KaelSpear : MonoBehaviour
    {
        [SerializeField] private Vector2 hitbox = new Vector2(1.3f, .65f);
        [SerializeField] private float reach = .5f;
        [SerializeField] private float aimThreshold = .5f;
        [SerializeField] private float cooldown = .36f;
        [SerializeField] private Vector2 activeWindow = new Vector2(.22f, .48f);
        [SerializeField, Min(0.1f)] private float swingSeconds = 0.28f;
        [SerializeField] private LayerMask targets = ~0;
        [SerializeField] private int damage = 2;
        [SerializeField] private float knockback = 5f;
        [SerializeField] private float pogoSpeed = 13f;
        private readonly HashSet<Component> struck = new HashSet<Component>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private PlayerController player;
        private Collider2D body;
        private InputAction attack;
        private Vector2 direction;
        private float swingStart, swingEnd, nextAttack;
        private float queuedUntil = float.NegativeInfinity;
        private bool connected;
        private bool thrustShown;

        public bool IsAttacking => Time.fixedTime < swingEnd;
        internal bool AimingDown => direction.y < -0.5f;
        internal float SwingProgress => Mathf.Clamp01((Time.fixedTime - swingStart) / swingSeconds);
        public void AddDamage(int amount) => damage += amount;
        public int Damage => damage;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<CapsuleCollider2D>();
            attack = InputSystem.actions.FindAction("Player/Attack", true);
        }

        private void OnEnable() => attack.performed += QueueStrike;
        private void OnDisable()
        {
            attack.performed -= QueueStrike;
            CancelStrike();
        }

        private void QueueStrike(InputAction.CallbackContext context)
        {
            if (player.CanAct && Time.timeScale > 0f) queuedUntil = Time.time + 0.14f;
        }

        internal void CancelStrike()
        {
            swingEnd = 0f;
            queuedUntil = float.NegativeInfinity;
        }

        private void BeginStrike(float now)
        {
            Vector2 aim = player.MoveInput;
            float horizontal = Mathf.Abs(aim.x) > 0.4f ? Mathf.Sign(aim.x) : 0f;
            if (horizontal != 0f) player.FaceStrike(horizontal);
            direction = aim.y > aimThreshold ? Vector2.up : Vector2.right * player.Facing;
            if (aim.y < -aimThreshold && !player.IsGrounded)
                direction = new Vector2(horizontal, -1.25f).normalized;

            swingStart = now;
            swingEnd = now + swingSeconds;
            nextAttack = now + cooldown;
            queuedUntil = float.NegativeInfinity;
            connected = false;
            thrustShown = false;
            struck.Clear();
            Vibracion.Pulso(0f, 0.18f, 0.05f); // lanzazo
            Sonidos.Play("kael_lanza", 0.5f, 1f, transform.position, 0.1f);
        }

        private void FixedUpdate()
        {
            if (!player.CanAct || (IsAttacking && AimingDown && player.IsGrounded))
            {
                CancelStrike();
                return;
            }
            float now = Time.fixedTime;
            if (!IsAttacking && now <= queuedUntil && now >= nextAttack && player.CanUseSpear) BeginStrike(now);
            if (!IsAttacking) return;
            bool diagonalDive = AimingDown && direction.x != 0f;
            float progress = SwingProgress;
            float hitEnd = AimingDown ? Mathf.Max(0.77f, activeWindow.y) : activeWindow.y;
            if (progress < activeWindow.x || progress > hitEnd) return;

            Bounds bounds = body.bounds;
            float edge = Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.y) * bounds.extents.y;
            Vector2 center = (Vector2)bounds.center + direction * (edge + reach);
            if (!thrustShown)
            {
                thrustShown = true;
                if (diagonalDive) player.BeginSpearDive(direction.x, (hitEnd - progress) * swingSeconds);
                KaelVFX.SpearThrust(transform, (Vector2)bounds.center + direction * edge, direction);
            }
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(targets);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Physics2D.OverlapBox(center, hitbox, angle, filter, overlaps);
            foreach (var other in overlaps)
            {
                var enemy = other.GetComponentInParent<EnemyHealth>();
                var hazard = other.GetComponentInParent<ContactHazard>();
                if (!enemy && (!hazard || !hazard.AllowsPogo || !AimingDown)) continue;
                Component target = enemy ? (Component)enemy : hazard;
                if (!struck.Add(target)) continue;
                if (enemy && !enemy.Hit(damage, direction * knockback)) continue;
                KaelVFX.Impact(other.ClosestPoint(center), target);
                if (connected) continue;
                connected = true;
                if (Camera.main && Camera.main.TryGetComponent(out CameraFollow2D camera)) camera.Shake(direction);
                Sonidos.Play("kael_golpe", 0.7f, 1f, transform.position);
                if (AimingDown)
                {
                    player.Bounce(pogoSpeed);
                    CancelStrike();
                    return;
                }
            }
        }
    }
}
