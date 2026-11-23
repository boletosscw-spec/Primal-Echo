using UnityEngine;

namespace PrimalEcho
{
    // Hongo azul: si Kael cae encima, lo lanza hacia arriba (le devuelve también el doble salto).
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class HongoRebote : MonoBehaviour
    {
        [SerializeField, Min(5f)] private float fuerza = 21f;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Texture2D hoja;
        [SerializeField] private int columnas = 8, cuadros = 20, anchoCuadro = 1, altoCuadro = 1, separacion = 2;
        [SerializeField, Min(1f)] private float fps = 40f;

        private Sprite[] frames;
        private float inicioAnim = -10f, siguiente;
        private Vector3 escalaBase;

        public void Configure(SpriteRenderer sr, Texture2D tex, int cols, int count, int fw, int fh, int pad, float force)
        {
            visual = sr; hoja = tex; columnas = cols; cuadros = count;
            anchoCuadro = fw; altoCuadro = fh; separacion = pad; fuerza = force;
        }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            frames = AnimacionHoja.Cortar(hoja, columnas, cuadros, anchoCuadro, altoCuadro, separacion);
            if (visual != null)
            {
                escalaBase = visual.transform.localScale;
                if (frames != null && frames.Length > 0) visual.sprite = frames[0];
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => Probar(other);
        private void OnTriggerStay2D(Collider2D other) => Probar(other);

        private void Probar(Collider2D other)
        {
            if (Time.time < siguiente) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.CanAct) return;
            var body = player.GetComponent<Rigidbody2D>();
            if (body == null || body.linearVelocity.y > 0.5f) return;
            // Solo desde arriba: los pies de Kael deben estar en la parte alta del hongo
            var box = (BoxCollider2D)GetComponent<Collider2D>();
            float top = box.bounds.max.y;
            if (other.bounds.min.y < top - box.bounds.size.y - 0.15f) return;
            player.Bounce(fuerza);
            Sonidos.Play("hongo", 0.55f, 1f, transform.position);
            siguiente = Time.time + 0.25f;
            inicioAnim = Time.time;
        }

        private void Update()
        {
            if (visual == null) return;
            float t = Time.time - inicioAnim;
            if (frames != null && frames.Length > 0)
            {
                int i = (int)(t * fps);
                visual.sprite = frames[i >= 0 && i < frames.Length ? i : 0];
            }
            // aplastarse y estirarse al rebotar
            float k = Mathf.Clamp01(t / 0.35f);
            float squash = t < 0.35f ? Mathf.Sin(k * Mathf.PI) * (1f - k) : 0f;
            visual.transform.localScale = new Vector3(escalaBase.x * (1f + squash * 0.25f), escalaBase.y * (1f - squash * 0.3f), escalaBase.z);
        }
    }
}
