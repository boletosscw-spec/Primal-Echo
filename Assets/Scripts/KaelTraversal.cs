using System;
using UnityEngine;

namespace PrimalEcho
{
    [Serializable]
    public sealed class KaelTraversal
    {
        [Header("Dash")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private float dashCooldown = 0.45f;

        [Header("Salto de pared")]
        [SerializeField] private float wallReach = 0.08f;
        [SerializeField, Range(0f, 1f)] private float wallProbeHeight = 0.45f;
        [SerializeField] private Vector2 wallJump = new Vector2(9f, 12.5f);
        [SerializeField] private float wallControlDelay = 0.12f;

        private float dashEnds, nextDash, dashDirection;

        public Vector2 WallJump => wallJump;
        public float WallControlDelay => wallControlDelay;

        public Vector2 ProbeSize(Bounds bounds) => new Vector2(wallReach, bounds.size.y * wallProbeHeight);

        public bool StepDash(Rigidbody2D body, bool requested, float facing, float gravityStep)
        {
            if (requested && Time.fixedTime >= nextDash)
            {
                dashDirection = facing;
                dashEnds = Time.fixedTime + dashDuration;
                nextDash = dashEnds + dashCooldown;
            }
            if (Time.fixedTime >= dashEnds) return false;
            body.linearVelocity = new Vector2(dashDirection * dashSpeed, -gravityStep);
            return true;
        }

        public void CancelDash() => dashEnds = 0f;
    }
}
