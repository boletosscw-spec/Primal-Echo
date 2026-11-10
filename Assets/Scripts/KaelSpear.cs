using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrimalEcho
{
    [RequireComponent(typeof(PlayerController), typeof(Animator))]
    public sealed class KaelSpear : MonoBehaviour
    {
        [SerializeField] private Vector2 hitbox = new Vector2(1.3f, .65f);
        [SerializeField] private float reach = .5f;
        [SerializeField] private float aimThreshold = .5f;
        [SerializeField] private float cooldown = .36f;
        [SerializeField] private Vector2 activeWindow = new Vector2(.22f, .48f);
        [SerializeField] private LayerMask targets = ~0;
        [SerializeField] private int damage = 2;
        [SerializeField] private float knockback = 5f;
        [SerializeField] private float pogoSpeed = 13f;
        private readonly HashSet<Component> struck = new HashSet<Component>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private PlayerController player;
        private Animator animator;
        private Collider2D body;
        private InputAction attack;
        private Vector2 direction;
        private int state;
        private float nextAttack;
        private bool connected;
        private bool thrustShown;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<CapsuleCollider2D>();
            animator = GetComponent<Animator>();
            attack = InputSystem.actions.FindAction("Player/Attack", true);
        }

        private void OnEnable() => attack.performed += Strike;
        private void OnDisable() { attack.performed -= Strike; state = 0; }

        private void Strike(InputAction.CallbackContext context)
        {
            if (!player.CanAct || Time.time < nextAttack) return;
            float aim = player.MoveInput.y;
            direction = Vector2.right * player.Facing;
            if (aim > aimThreshold) direction = Vector2.up;
            else if (aim < -aimThreshold && !player.IsGrounded) direction = Vector2.down;
            nextAttack = Time.time + cooldown;
            connected = false;
            thrustShown = false;
            struck.Clear();
            state = Animator.StringToHash(player.IsGrounded ? "Base Layer.Ataque_Lanza" : "Base Layer.Ataque_Aereo_Lanza");
            animator.Play(state, 0, 0f);
        }

        private void LateUpdate()
        {
            if (state == 0) return;
            var animation = animator.GetCurrentAnimatorStateInfo(0);
            if (!player.CanAct || animation.fullPathHash != state || animation.normalizedTime > activeWindow.y)
            {
                state = 0;
                return;
            }
            if (animation.normalizedTime < activeWindow.x) return;
            Bounds bounds = body.bounds;
            float edge = direction.x == 0f ? bounds.extents.y : bounds.extents.x;
            Vector2 center = (Vector2)bounds.center + direction * (edge + reach);
            if (!thrustShown)
            {
                thrustShown = true;
                KaelVFX.SpearThrust(transform, (Vector2)bounds.center + direction * edge, direction);
            }
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(targets);
            Physics2D.OverlapBox(center, hitbox, direction.x == 0f ? 90f : 0f, filter, overlaps);
            foreach (var other in overlaps)
            {
                var enemy = other.GetComponentInParent<EnemyHealth>();
                var hazard = other.GetComponentInParent<ContactHazard>();
                if (!enemy && (!hazard || !hazard.AllowsPogo || direction != Vector2.down)) continue;
                Component target = enemy ? (Component)enemy : hazard;
                if (!struck.Add(target)) continue;
                if (enemy && !enemy.Hit(damage, direction * knockback)) continue;
                KaelVFX.Impact(other.ClosestPoint(center), target);
                if (connected) continue;
                connected = true;
                if (direction == Vector2.down) player.Bounce(pogoSpeed);
                if (Camera.main && Camera.main.TryGetComponent(out CameraFollow2D camera)) camera.Shake(direction);
            }
        }
    }
}
