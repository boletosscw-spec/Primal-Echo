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

        [Header("Carrera de impulso")]
        [SerializeField, Min(8f)] private float sprintSpeed = 11.2f;

        [Header("Salto de pared")]
        [SerializeField] private float wallReach = 0.08f;
        [SerializeField, Range(0f, 1f)] private float wallProbeHeight = 0.45f;
        [SerializeField] private Vector2 wallJump = new Vector2(9f, 12.5f);
        [SerializeField] private float wallControlDelay = 0.12f;
        [SerializeField, Min(0.5f)] private float wallSlideSpeed = 3.4f;

        private float dashEnds, nextDash, dashDirection;
        private bool airDashSpent, sprintArmed;

        public Vector2 WallJump => wallJump;
        public float WallControlDelay => wallControlDelay;
        public float WallSlideSpeed => wallSlideSpeed;
        public bool IsDashing => Time.fixedTime < dashEnds;
        public bool DashStartedOnGround { get; private set; }
        public bool IsSprinting { get; private set; }

        public Vector2 ProbeSize(Bounds bounds) => new Vector2(wallReach, bounds.size.y * wallProbeHeight);

        public bool TryStartDash(bool grounded, float facing)
        {
            float now = Time.fixedTime;
            if (IsDashing || now < nextDash || (!grounded && airDashSpent)) return false;
            dashDirection = facing;
            dashEnds = now + dashDuration;
            nextDash = dashEnds + dashCooldown;
            DashStartedOnGround = grounded;
            sprintArmed = grounded;
            IsSprinting = false;
            if (!grounded) airDashSpent = true;
            return true;
        }

        public bool StepDash(Rigidbody2D body, float gravityStep)
        {
            if (!IsDashing) return false;
            body.linearVelocity = new Vector2(dashDirection * dashSpeed, -gravityStep);
            return true;
        }

        public float RunSpeed(float baseSpeed, bool grounded, bool dashHeld, float input)
        {
            if (!dashHeld || Mathf.Abs(input) < 0.15f) sprintArmed = false;
            IsSprinting = sprintArmed && grounded && Mathf.Abs(input) > 0.55f;
            return IsSprinting ? Mathf.Max(baseSpeed, sprintSpeed) : baseSpeed;
        }

        public void Land() => airDashSpent = false;

        public void RechargeFromRebound()
        {
            airDashSpent = false;
            nextDash = Time.fixedTime;
        }

        public void Cancel()
        {
            dashEnds = 0f;
            sprintArmed = IsSprinting = false;
        }
    }
}
