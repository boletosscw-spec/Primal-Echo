using UnityEngine;

namespace PrimalEcho
{
    // Slimes del bosque. Se mueven a saltitos y se aplastan como gelatina.
    //  · Verde: patrulla su tramo; si ve a Kael, se acerca a saltitos.
    //  · Naranja: salta en arco hacia Kael cuando lo tiene cerca. Más vida.
    // Nunca salen de su tramo [min, max] ni caen por un borde.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyHealth))]
    public sealed class SlimeMusgo : MonoBehaviour
    {
        public enum Tipo { Verde, Naranja }

        [SerializeField] private Tipo tipo = Tipo.Verde;
        [SerializeField] private float minX = -999f, maxX = 999f;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private AnimacionHoja animacion;
        [SerializeField] private LayerMask suelo = 1 << 6;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private EnemyFeedback feedback;
        private EnemyHealth health;
        private Transform player;
        private Vector3 escalaVisual;
        private int dir = -1;
        private float proximoSalto, aterrizaje = -10f;
        private bool enSuelo, estabaEnSuelo;

        public void Configure(Tipo t, float min, float max, SpriteRenderer sr, AnimacionHoja anim)
        {
            tipo = t; minX = min; maxX = max; visual = sr; animacion = anim;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            feedback = GetComponent<EnemyFeedback>();
            health = GetComponent<EnemyHealth>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (visual != null) escalaVisual = visual.transform.localScale;
            proximoSalto = Time.time + Random.Range(0.3f, 1.2f);
        }

        private void OnEnable() => EnemyHealth.Died += AlMorir;
        private void OnDisable() => EnemyHealth.Died -= AlMorir;

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            foreach (var c in tagged.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(box, c);
        }

        private bool PisoAdelante(int d, float distancia)
        {
            Bounds b = box.bounds;
            Vector2 punto = new Vector2(b.center.x + d * (b.extents.x + distancia), b.min.y + 0.1f);
            return Physics2D.Raycast(punto, Vector2.down, 1.2f, suelo);
        }

        private bool ParedAdelante(int d)
        {
            Bounds b = box.bounds;
            return Physics2D.Raycast(b.center, Vector2.right * d, b.extents.x + 0.5f, suelo);
        }

        private void FixedUpdate()
        {
            Bounds b = box.bounds;
            enSuelo = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, suelo)
                      && body.linearVelocity.y <= 0.05f;
            if (enSuelo && !estabaEnSuelo) aterrizaje = Time.time;
            estabaEnSuelo = enSuelo;
            if (feedback != null && feedback.Stunned) return;
            if (!enSuelo) return;

            body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, 0f, 30f * Time.fixedDeltaTime), body.linearVelocity.y);
            if (Time.time < proximoSalto) return;

            float dx = player != null ? player.position.x - transform.position.x : 99f;
            float dy = player != null ? player.position.y - transform.position.y : 99f;
            bool cerca = Mathf.Abs(dy) < 3f && Mathf.Abs(dx) < (tipo == Tipo.Naranja ? 8f : 6f)
                         && player.position.x > minX - 1f && player.position.x < maxX + 1f;

            if (cerca) dir = dx > 0f ? 1 : -1;
            float x = transform.position.x;
            if (x <= minX + 0.3f) dir = 1;
            else if (x >= maxX - 0.3f) dir = -1;
            if (!PisoAdelante(dir, 0.6f) || ParedAdelante(dir)) dir = -dir;

            float vx, vy, pausa;
            if (tipo == Tipo.Naranja && cerca)
            {
                // salto en arco que cae cerca de Kael, sin salir de su tramo
                float destino = Mathf.Clamp(player.position.x, minX + 0.5f, maxX - 0.5f);
                vy = 10f;
                float tiempoAire = 2f * vy / (Mathf.Abs(Physics2D.gravity.y) * body.gravityScale);
                vx = Mathf.Clamp((destino - x) / tiempoAire, -6.5f, 6.5f);
                pausa = Random.Range(1.3f, 1.8f);
            }
            else
            {
                vx = dir * (cerca ? 2.6f : 1.8f);
                vy = cerca ? 5.5f : 4.2f;
                pausa = cerca ? Random.Range(0.55f, 0.8f) : Random.Range(0.9f, 1.5f);
            }
            // si el salto lo sacaría del tramo, lo acorta
            float alcance = vx * 2f * vy / (Mathf.Abs(Physics2D.gravity.y) * body.gravityScale);
            if (x + alcance < minX || x + alcance > maxX) vx *= 0.3f;
            body.linearVelocity = new Vector2(vx, vy);
            proximoSalto = Time.time + pausa;
        }

        private void Update()
        {
            if (visual == null) return;
            // gelatina: se estira al subir, se aplasta al caer y al aterrizar
            float vy = body.linearVelocity.y;
            float estira = Mathf.Clamp(vy * 0.035f, -0.18f, 0.22f);
            float t = Time.time - aterrizaje;
            if (t < 0.25f) estira -= Mathf.Sin(t / 0.25f * Mathf.PI) * 0.22f;
            float sx = escalaVisual.x * (1f - estira * 0.6f);
            visual.transform.localScale = new Vector3(dir > 0 ? -Mathf.Abs(sx) : Mathf.Abs(sx), escalaVisual.y * (1f + estira), escalaVisual.z);
            if (animacion != null) animacion.Fps = enSuelo ? 20f : 34f;
        }

        private void AlMorir(EnemyHealth e)
        {
            if (e != health || visual == null) return;
            SalpicaduraSlime.Crear(visual.transform.position, visual.sprite, tipo == Tipo.Naranja
                ? new Color(1f, 0.6f, 0.2f) : new Color(0.45f, 1f, 0.35f));
        }
    }
}
