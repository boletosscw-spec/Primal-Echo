using UnityEngine;

namespace PrimalEcho
{
    // Avispa: patrulla en ocho; al ver a Kael lo rodea a cierta distancia, se lanza en picada
    // y luego se aleja (no se queda encima de él). Retrocede al recibir un golpe.
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public sealed class WaspFlyer : MonoBehaviour
    {
        private enum State { Patrol, Hover, Dive, Retreat }

        [SerializeField] private Vector2 range = new Vector2(2.5f, 0.8f);
        [SerializeField, Min(0.1f)] private float cycleSpeed = 1.3f;
        [SerializeField, Min(0f)] private float sightRange = 6f;
        [SerializeField, Min(0.5f)] private float hoverDistance = 2.3f;
        [SerializeField, Min(0f)] private float hoverSpeed = 3.5f;
        [SerializeField, Min(0f)] private float diveAttackSpeed = 8f;
        [SerializeField, Min(0.2f)] private float diveCooldown = 2.2f;
        [SerializeField] private Transform visual;

        private Rigidbody2D body;
        private EnemyFeedback feedback;
        private Transform player;
        private Vector2 home, diveTarget;
        private State state = State.Patrol;
        private float t, timer;
        private int side = 1;

        public bool Attacking => state == State.Dive;

        public void Configure(Transform bodyVisual) => visual = bodyVisual;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            feedback = GetComponent<EnemyFeedback>();
            home = transform.position;
            t = Random.value * 6f;
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 pos = body.position;
            Vector2 push = feedback != null ? feedback.Push : Vector2.zero;
            if (feedback != null && feedback.Stunned)
            {
                body.MovePosition(pos + push * dt);
                state = State.Retreat; timer = 0.5f;
                return;
            }
            t += dt * cycleSpeed;
            timer -= dt;
            Vector2 target = Vector2.zero; float spd = hoverSpeed;
            Vector2 kael = player != null ? (Vector2)player.position + Vector2.up * 0.7f : pos;
            bool sees = player != null && Vector2.Distance(kael, pos) < sightRange;

            switch (state)
            {
                case State.Patrol:
                    target = home + new Vector2(Mathf.Sin(t) * range.x, Mathf.Sin(t * 2f) * range.y);
                    spd = 4f;
                    if (sees) { state = State.Hover; timer = diveCooldown; }
                    break;
                case State.Hover:
                    side = pos.x >= kael.x ? 1 : -1;
                    target = kael + new Vector2(side * hoverDistance, 1.2f + Mathf.Sin(t * 3f) * 0.3f);
                    if (!sees) state = State.Patrol;
                    else if (timer <= 0f) { state = State.Dive; diveTarget = kael; timer = 0.7f; }
                    break;
                case State.Dive:
                    target = diveTarget; spd = diveAttackSpeed;
                    if (timer <= 0f || Vector2.Distance(pos, diveTarget) < 0.3f) { state = State.Retreat; timer = 0.7f; }
                    break;
                case State.Retreat:
                    target = kael + new Vector2(side * (hoverDistance + 0.5f), 2f);
                    if (timer <= 0f) { state = sees ? State.Hover : State.Patrol; timer = diveCooldown; }
                    break;
            }

            Vector2 next = Vector2.MoveTowards(pos, target, spd * dt) + push * dt;
            if (visual != null)
            {
                float face = state == State.Patrol ? next.x - pos.x : kael.x - pos.x;
                if (Mathf.Abs(face) > 0.001f)
                {
                    Vector3 s = visual.localScale;
                    s.x = Mathf.Abs(s.x) * (face > 0 ? -1f : 1f); // el sprite original mira a la izquierda
                    visual.localScale = s;
                }
            }
            body.MovePosition(next);
        }
    }
}
