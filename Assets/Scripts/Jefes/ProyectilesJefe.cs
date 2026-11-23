using UnityEngine;

namespace PrimalEcho
{
    // Piezas que lanzan los jefes. Todas hacen daño al tocar a Kael (ContactHazard).
    public static class ProyectilesJefe
    {
        private static readonly Color Cian = new Color(0.25f, 0.96f, 1f);

        private static GameObject Base(string nombre, Vector2 pos, Sprite sprite, Color color, Vector2 escala, int dano, bool pogo)
        {
            var go = new GameObject(nombre);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(escala.x, escala.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = 9;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = sprite != null ? (Vector2)sprite.bounds.size * 0.85f : Vector2.one * 0.85f;
            var hz = go.AddComponent<ContactHazard>();
            SetDano(hz, dano, pogo);
            return go;
        }

        // ContactHazard tiene campos serializados privados: se ajustan con JsonUtility para no tocar ese script
        private static void SetDano(ContactHazard hz, int dano, bool pogo)
        {
            JsonUtility.FromJsonOverwrite("{\"damage\":" + dano + ",\"allowsPogo\":" + (pogo ? "true" : "false") + "}", hz);
        }

        /// <summary>Sprite opcional en Resources/Jefes/Comun o en la carpeta del jefe; si no hay, usa el de respaldo.</summary>
        public static Sprite Cargar(string carpeta, string nombre, Sprite respaldo, float alto)
        {
            var tex = Resources.Load<Texture2D>("Jefes/" + carpeta + "/" + nombre);
            if (tex == null) return respaldo;
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.height / alto);
        }

        /// <summary>Roca en arco. La de Eco cae, se queda brillando unos segundos y se puede golpear para cargar el Pulso.</summary>
        public static GameObject Roca(Vector2 desde, Vector2 velocidad, bool eco, string carpeta)
        {
            Sprite spr = Cargar(carpeta, eco ? "RocaEco" : "Roca", JefeBase.Circulo, 1.2f);
            bool propio = spr != JefeBase.Circulo;
            Color color = propio ? Color.white : eco ? Cian : new Color(0.55f, 0.45f, 0.35f);
            var go = Base(eco ? "Roca_Eco" : "Roca", desde, spr, color, propio ? Vector2.one : Vector2.one * 0.9f, 1, eco);
            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = eco ? 1.6f : 2.2f;
            rb.linearVelocity = velocidad;
            go.AddComponent<RocaJefe>().Init(eco);
            if (eco) go.AddComponent<ObjetoEco>();
            else go.AddComponent<ProyectilJefe>(); // el Destello de Kiro la puede deshacer
            return go;
        }

        /// <summary>Onda de choque a ras de suelo que avanza en 'dir' (se salta con un salto simple).</summary>
        /// <summary>
        /// Onda de choque a ras de suelo que avanza en 'dir' (se salta con un salto simple).
        /// Se ve como piedras con textura que brotan del suelo y una nube de polvo semitransparente.
        /// </summary>
        public static GameObject Onda(Vector2 pieIzq, int dir, float velocidad, float alto, float limiteX, string carpeta = null)
        {
            var go = new GameObject("Onda");
            go.transform.position = pieIzq + new Vector2(0f, alto / 2f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.1f, alto);
            SetDano(go.AddComponent<ContactHazard>(), 1, false);

            Sprite roca = carpeta != null ? Cargar(carpeta, "Roca", null, 1f) : null;
            // Polvo detrás (transparente, sin bordes)
            var polvo = new GameObject("Polvo");
            polvo.transform.SetParent(go.transform, false);
            polvo.transform.localPosition = new Vector3(-dir * 0.5f, -alto * 0.15f, 0f);
            polvo.transform.localScale = new Vector3(2.2f, alto * 1.1f, 1f);
            var ps = polvo.AddComponent<SpriteRenderer>();
            ps.sprite = JefeBase.Circulo; ps.color = new Color(0.62f, 0.55f, 0.45f, 0.35f); ps.sortingOrder = 8;
            // Piedras que brotan (tamaños distintos, la del frente más alta)
            float[] tam = { 0.42f, 0.62f, 0.5f };
            float[] off = { -0.45f, 0f, 0.42f };
            for (int i = 0; i < 3; i++)
            {
                var p = new GameObject("Piedra");
                p.transform.SetParent(go.transform, false);
                p.transform.localPosition = new Vector3(off[i] * dir, -alto / 2f + tam[i] * 0.4f, 0f);
                p.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 9;
                if (roca != null) { sr.sprite = roca; p.transform.localScale = Vector3.one * tam[i]; }
                else { sr.sprite = JefeBase.Circulo; sr.color = new Color(0.5f, 0.42f, 0.34f); p.transform.localScale = Vector3.one * tam[i]; }
                p.AddComponent<Brotar>().Init(i * 0.07f);
            }
            go.AddComponent<Movil>().Init(new Vector2(dir * velocidad, 0f), 3f, limiteX, dir);
            go.AddComponent<ProyectilJefe>();
            return go;
        }

        /// <summary>Anillo de energía horizontal disparado por la gola de Atlas.</summary>
        public static GameObject Anillo(Vector2 centro, int dir, float velocidad, float alto, float limiteX)
        {
            var go = Base("Anillo", centro, JefeBase.Circulo, new Color(Cian.r, Cian.g, Cian.b, 0.9f), new Vector2(0.9f, alto), 1, false);
            go.AddComponent<Movil>().Init(new Vector2(dir * velocidad, 0f), 4f, limiteX, dir);
            go.AddComponent<ProyectilJefe>();
            return go;
        }

        /// <summary>Fragmento del escudo roto: zona eléctrica en el suelo durante unos segundos.</summary>
        public static GameObject Fragmento(Vector2 pie, float segundos, string carpeta = null)
        {
            Sprite propio = carpeta != null ? Cargar(carpeta, "Fragmento", null, 0.7f) : null;
            var go = propio != null
                ? Base("Fragmento_Escudo", pie + new Vector2(0f, 0.35f), propio, Color.white, Vector2.one, 1, true)
                : Base("Fragmento_Escudo", pie + new Vector2(0f, 0.2f), JefeBase.Cuadro, new Color(Cian.r, Cian.g, Cian.b, 0.75f), new Vector2(1.4f, 0.4f), 1, true);
            go.AddComponent<Parpadeo>();
            Object.Destroy(go, segundos);
            return go;
        }
    }

    /// <summary>
    /// Marca los ataques de un jefe que el Destello / Pulso de Kiro puede bloquear (rocas, ondas, anillos).
    /// Deshacer() lo rompe en pedazos que salen volando.
    /// </summary>
    public sealed class ProyectilJefe : MonoBehaviour
    {
        public void Deshacer()
        {
            Sonidos.Play("roca_romper", 0.6f, 1.15f, transform.position);
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var p = new GameObject("Pedazo_Bloqueado");
                    p.transform.position = sr.transform.position;
                    p.transform.localScale = sr.transform.lossyScale * 0.45f;
                    p.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    var r = p.AddComponent<SpriteRenderer>();
                    r.sprite = sr.sprite; r.color = sr.color; r.sortingOrder = sr.sortingOrder;
                    var rb = p.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 2f;
                    rb.linearVelocity = new Vector2(Random.Range(-5f, 5f), Random.Range(2f, 6f));
                    rb.angularVelocity = Random.Range(-500f, 500f);
                    p.AddComponent<Desvanecer>().Init(0.6f);
                }
            }
            Destroy(gameObject);
        }
    }

    /// <summary>Movimiento recto; se destruye al pasar 'limiteX' o al acabar su vida.</summary>
    public sealed class Movil : MonoBehaviour
    {
        private Vector2 vel; private float limite; private int dir;
        public void Init(Vector2 velocidad, float vida, float limiteX, int direccion)
        {
            vel = velocidad; limite = limiteX; dir = direccion;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = vel; // cinemático: se mueve por velocidad (así detecta bien los triggers)
            Destroy(gameObject, vida);
        }
        private void Update()
        {
            if ((transform.position.x - limite) * dir > 0f) Destroy(gameObject);
        }
    }

    /// <summary>Hace que una piedra de la onda suba y baje como si brotara del suelo.</summary>
    public sealed class Brotar : MonoBehaviour
    {
        private float desfase; private Vector3 basePos;
        public void Init(float retraso) => desfase = retraso;
        private void Start() => basePos = transform.localPosition;
        private void Update()
        {
            float t = Mathf.Repeat(Time.time * 3.2f + desfase * 10f, 1f);
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.25f);
        }
    }

    public sealed class Parpadeo : MonoBehaviour
    {
        private SpriteRenderer sr; private Color c;
        private void Start() { sr = GetComponent<SpriteRenderer>(); c = sr.color; }
        private void Update() { sr.color = new Color(c.r, c.g, c.b, c.a * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 14f)))); }
    }

    /// <summary>La roca se rompe al tocar el suelo; la de Eco se queda quieta brillando y luego se apaga.</summary>
    public sealed class RocaJefe : MonoBehaviour
    {
        private bool eco, posada;
        public void Init(bool esEco) { eco = esEco; Destroy(gameObject, 6f); }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (posada || other.isTrigger || other.gameObject.layer != 6) return;
            var rb = GetComponent<Rigidbody2D>();
            if (!eco)
            {
                Sonidos.Play("roca_impacto", 0.55f, 1f, transform.position);
                KaelVFX.Impact(transform.position, this);
                Destroy(gameObject);
                return;
            }
            posada = true;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            Destroy(gameObject, 3.5f);
        }
    }

    /// <summary>
    /// Objeto cian: al golpearlo con la LANZA (no con flechas) da 1 carga de Eco y desaparece.
    /// Un golpe de lanza hacia abajo hace rebotar a Kael.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ObjetoEco : MonoBehaviour
    {
        private EnemyHealth vida;
        private SpriteRenderer sr;

        private void Awake()
        {
            vida = gameObject.AddComponent<EnemyHealth>();
            vida.SetHealth(1);
            vida.Modificar = (dano, impulso) =>
            {
                if (BarraEco.PulsoActivo) return 0;
                var lanza = FindFirstObjectByType<KaelSpear>();
                if (lanza == null || !lanza.IsAttacking) return 0; // las flechas no cuentan
                if (Vector2.Distance(lanza.transform.position, transform.position) > 3.2f) return 0;
                return 1;
            };
            vida.Golpeado += _ =>
            {
                BarraEco.Agregar(1);
                Sonidos.Play("eco_carga", 0.7f, 1f, transform.position, 0.02f);
                var fx = new GameObject("Eco_Destello");
                fx.transform.position = transform.position;
                var r = fx.AddComponent<SpriteRenderer>();
                r.sprite = JefeBase.Circulo; r.color = new Color(0.25f, 0.96f, 1f, 0.9f); r.sortingOrder = 14;
                fx.AddComponent<EfectoCrecer>().Init(2.5f, 0.25f);
            };
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (sr == null) return;
            float g = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 6f)); // brillo que late para que se note
            sr.color = new Color(g, 1f, 1f, 1f);
        }
    }
}
