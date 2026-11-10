using UnityEngine;

namespace PrimalEcho
{
    // Robot de NÉMESIS: patrulla; al ver a Kael se acerca SIN atravesarlo, se detiene a un paso,
    // carga (tiembla) y embiste corto; luego retrocede. Al recibir un golpe sale empujado hacia atrás.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyHealth))]
    public sealed class RobotPatrol : MonoBehaviour
    {
        private enum State { Patrol, Chase, Windup, Lunge, Recover }

        [SerializeField, Min(0f)] private float speed = 1.6f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.2f;
        [SerializeField, Min(0f)] private float sightRange = 6f;
        [Header("Ataque")]
        [Tooltip("Espacio (borde a borde) que deja frente a Kael antes de embestir")]
        [SerializeField, Min(0.1f)] private float keepDistance = 0.6f;
        [SerializeField, Min(0f)] private float windupSeconds = 0.45f;
        [SerializeField, Min(0f)] private float lungeSpeed = 8f;
        [SerializeField, Min(0f)] private float lungeSeconds = 0.18f;
        [SerializeField, Min(0f)] private float recoverSeconds = 0.6f;
        [SerializeField] private LayerMask groundLayers = 1 << 6;
        [SerializeField] private Transform visual;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private EnemyFeedback feedback;
        private Transform player;
        private Collider2D playerCollider;
        private State state = State.Patrol;
        private int direction = -1, lungeDir;
        private float timer;
        private Vector3 visualRest;

        public bool Attacking => state == State.Windup || state == State.Lunge;

        public void Configure(LayerMask ground, Transform bodyVisual, float patrolSpeed, float chase)
        {
            groundLayers = ground; visual = bodyVisual; speed = patrolSpeed; chaseSpeed = chase;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            feedback = GetComponent<EnemyFeedback>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (visual != null) visualRest = visual.localPosition;
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            playerCollider = tagged.GetComponent<Collider2D>();
            // No se empujan físicamente (evita que se metan uno dentro del otro); el daño va por la caja de golpe
            foreach (var c in tagged.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(box, c);
        }

        private bool FloorAhead(int dir)
        {
            Bounds b = box.bounds;
            Vector2 ahead = new Vector2(b.center.x + dir * (b.extents.x + 0.15f), b.min.y + 0.1f);
            return Physics2D.Raycast(ahead, Vector2.down, 0.6f, groundLayers);
        }

        private bool WallAhead(int dir)
        {
            Bounds b = box.bounds;
            return Physics2D.Raycast(b.center, Vector2.right * dir, b.extents.x + 0.12f, groundLayers);
        }

        private void FixedUpdate()
        {
            if (visual != null) visual.localPosition = visualRest;
            if (feedback != null && feedback.Stunned) { state = State.Recover; timer = 0.25f; return; } // deja actuar el retroceso
            Bounds b = box.bounds;
            bool grounded = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.2f, groundLayers);
            if (!grounded) return;

            // Distancia borde a borde con Kael (para no meterse dentro de él)
            float dx = 0f, gap = 99f; bool sees = false;
            if (player != null)
            {
                dx = player.position.x - transform.position.x;
                float playerHalf = playerCollider != null ? playerCollider.bounds.extents.x : 0.25f;
                gap = Mathf.Abs(dx) - b.extents.x - playerHalf;
                sees = Mathf.Abs(dx) < sightRange && Mathf.Abs(player.position.y - transform.position.y) < 1.6f;
            }
            int toPlayer = dx > 0f ? 1 : -1;
            timer -= Time.fixedDeltaTime;
            float vx = 0f;

            switch (state)
            {
                case State.Patrol:
                    if (!FloorAhead(direction) || WallAhead(direction)) direction = -direction;
                    vx = direction * speed;
                    if (sees) state = State.Chase;
                    break;

                case State.Chase:
                    direction = toPlayer;
                    if (!sees) { state = State.Patrol; break; }
                    if (gap <= keepDistance + 0.1f) { state = State.Windup; timer = windupSeconds; break; }
                    if (gap > keepDistance && FloorAhead(direction) && !WallAhead(direction)) vx = direction * chaseSpeed;
                    break;

                case State.Windup:
                    direction = toPlayer;
                    if (visual != null) visual.localPosition = visualRest + new Vector3(Random.Range(-0.04f, 0.04f), 0f, 0f);
                    if (timer <= 0f) { state = State.Lunge; timer = lungeSeconds; lungeDir = toPlayer; }
                    break;

                case State.Lunge:
                    // Embiste hasta tocar a Kael, sin pasar a través de él
                    if (gap > 0.05f && FloorAhead(lungeDir) && !WallAhead(lungeDir)) vx = lungeDir * lungeSpeed;
                    if (timer <= 0f || gap <= 0.05f) { state = State.Recover; timer = recoverSeconds; }
                    break;

                case State.Recover:
                    if (FloorAhead(-lungeDir) && !WallAhead(-lungeDir)) vx = -lungeDir * 1.4f; // retrocede un poco
                    if (timer <= 0f) state = sees ? State.Chase : State.Patrol;
                    break;
            }

            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
            if (visual != null)
            {
                Vector3 s = visual.localScale;
                s.x = Mathf.Abs(s.x) * (direction > 0 ? 1f : -1f);
                visual.localScale = s;
            }
        }
    }
}
