using UnityEngine;

namespace PrimalEcho
{
    // Al recibir un golpe: destello, pausa corta y retroceso claro alejándose de Kael.
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(0f)] private float flashSeconds = 0.08f;
        [SerializeField, Min(0f)] private float stunSeconds = 0.35f;
        [Tooltip("Velocidad del retroceso para un enemigo ligero (los pesados retroceden menos)")]
        [SerializeField, Min(0f)] private float knockbackSpeed = 7f;
        [SerializeField, Min(0f)] private float knockbackLift = 2.5f;
        private EnemyHealth health;
        private Rigidbody2D body;
        private Transform player;
        private int last;
        private float flashUntil;
        private Color baseColor = Color.white;

        public bool Stunned => Time.time < StunnedUntil;
        public float StunnedUntil { get; private set; }
        /// <summary>Empuje para enemigos cinemáticos (avispas): lo aplica su script de movimiento.</summary>
        public Vector2 Push { get; private set; }

        public void Configure(SpriteRenderer renderer) => visual = renderer;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            body = GetComponent<Rigidbody2D>();
            last = health.Remaining;
            if (visual != null) baseColor = visual.color;
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        private void Update()
        {
            if (health.Remaining < last) OnHit();
            last = health.Remaining;
            if (visual != null) visual.color = Time.time < flashUntil ? new Color(1f, 0.35f, 0.35f, 1f) : baseColor;
            Push = Vector2.MoveTowards(Push, Vector2.zero, 30f * Time.deltaTime);
        }

        private void OnHit()
        {
            flashUntil = Time.time + flashSeconds;
            StunnedUntil = Time.time + stunSeconds;
            float side = player != null && player.position.x > transform.position.x ? -1f : 1f;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                float weight = Mathf.Sqrt(3f / Mathf.Max(0.5f, body.mass)); // pesados retroceden menos
                body.linearVelocity = new Vector2(side * knockbackSpeed * weight, knockbackLift * weight);
            }
            else
            {
                Push = new Vector2(side * knockbackSpeed, knockbackLift * 0.5f);
            }
        }
    }
}
