using System.Collections.Generic;
using UnityEngine;

namespace PrimalEcho
{
    /// <summary>Lo que el Pulso de Kiro le hace a un jefe: un aturdimiento corto.</summary>
    public interface IAturdible
    {
        void Aturdir(float segundos);
    }

    // Base común de los jefes nuevos (Atlas, Aeris, NÉMESIS...).
    //  · Vida por fases (según porcentaje), con transición invulnerable entre fases.
    //  · Hojas de sprites en Assets/Resources/Jefes/<Carpeta>/<Animacion>.png (frames en UNA fila).
    //    Si una hoja todavía no existe, se dibuja un marcador de color para poder probar la pelea ya.
    //  · Barra de vida abajo, destello al recibir daño, reinicio si Kael muere y música de jefe.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyHealth))]
    public abstract class JefeBase : MonoBehaviour, IAturdible
    {
        protected const int TerrainLayer = 6;
        private static readonly List<JefeBase> todos = new List<JefeBase>();
        /// <summary>¿Hay algún jefe nuevo peleando? (lo usa MusicaJuego)</summary>
        public static bool AlgunoPeleando
        {
            get { foreach (var j in todos) if (j != null && j.engaged && !j.muerto) return true; return false; }
        }

        [Header("Jefe")]
        [SerializeField, Min(1)] protected int maxHealth = 80;
        [SerializeField, Min(1)] protected int attackDamage = 2;

        protected Rigidbody2D body;
        protected BoxCollider2D box;
        protected EnemyHealth health;
        protected SpriteRenderer sprite;
        protected Transform player;
        protected Collider2D playerCollider;
        protected KaelHealth playerHealth;
        protected PlayerController playerController;
        protected KaelRespawn playerRespawn;
        protected Vector3 home;
        protected int facing = -1;
        protected int fase = 1;
        protected bool engaged, muerto;
        protected float transicionHasta;
        protected readonly List<GameObject> generados = new List<GameObject>();

        private readonly Dictionary<string, Sprite[]> clips = new Dictionary<string, Sprite[]>();
        private Sprite marcador;
        private float flashUntil;
        private Texture2D pixel;

        // ---- Lo que define cada jefe ----
        protected abstract string Carpeta { get; }          // carpeta en Resources/Jefes
        protected abstract string NombreVisible { get; }    // texto sobre la barra de vida
        protected abstract float AltoVisual { get; }        // alto del frame en unidades del mundo
        protected abstract Vector2 TamanoCuerpo { get; }    // collider del cuerpo
        protected abstract Color ColorMarcador { get; }     // color del marcador provisional
        /// <summary>Porcentajes de vida donde empieza cada fase nueva, de mayor a menor. Ej: {0.6, 0.25}.</summary>
        protected abstract float[] Umbrales { get; }
        protected virtual string NombreFase => "";
        protected virtual float SegundosTransicion => 1.6f;
        /// <summary>¿Tocarlo hace daño ahora? (p. ej. Atlas no daña mientras está aturdido)</summary>
        protected virtual bool DanaAlContacto => true;

        protected bool EnTransicion => Time.time < transicionHasta;
        public bool Engaged => engaged;
        protected bool UsandoMarcador { get; private set; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            health = GetComponent<EnemyHealth>();
            body.gravityScale = 3f; body.mass = 80f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            box.size = TamanoCuerpo - new Vector2(0.2f, 0.2f);
            box.edgeRadius = 0.1f; // esquinas redondas: no se atora en orillas ni paredes
            box.offset = new Vector2(0f, TamanoCuerpo.y / 2f);
            box.sharedMaterial = new PhysicsMaterial2D("Jefe_SinFriccion") { friction = 0f, bounciness = 0f };
            health.SetHealth(maxHealth);
            health.DestruirAlMorir = false;
            health.Modificar = FiltrarGolpe;
            health.Golpeado += AlRecibirGolpe;
            home = transform.position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = 8; // como el Rex: sobre la decoración, bajo Kael

            // Tocarlo hace daño
            var hurt = new GameObject("Golpe");
            hurt.transform.SetParent(transform, false);
            var hurtBox = hurt.AddComponent<BoxCollider2D>();
            hurtBox.isTrigger = true; hurtBox.size = box.size * 0.85f; hurtBox.offset = box.offset;
            var contacto = hurt.AddComponent<ContactHazard>();
            contacto.CanDealDamage = () => !muerto && engaged && DanaAlContacto;

            pixel = Texture2D.whiteTexture;
            todos.Add(this);
        }

        protected virtual void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            playerCollider = tagged.GetComponent<Collider2D>();
            playerHealth = tagged.GetComponent<KaelHealth>();
            playerController = tagged.GetComponent<PlayerController>();
            playerRespawn = tagged.GetComponent<KaelRespawn>();
            if (playerRespawn != null) playerRespawn.Respawned += ReiniciarPelea;
            var kaelSprite = tagged.GetComponentInChildren<SpriteRenderer>();
            if (kaelSprite != null)
            {
                sprite.sharedMaterial = kaelSprite.sharedMaterial;
                sprite.sortingLayerID = kaelSprite.sortingLayerID;
            }
            foreach (var c in tagged.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(box, c);
        }

        protected virtual void OnDestroy()
        {
            todos.Remove(this);
            if (playerRespawn != null) playerRespawn.Respawned -= ReiniciarPelea;
            if (health != null) health.Golpeado -= AlRecibirGolpe;
            LimpiarGenerados();
        }

        // ---------- Vida y fases ----------

        /// <summary>Decide cuánto daño entra (0 = bloqueado). Por defecto: nada durante transición o muerte.</summary>
        protected virtual int FiltrarGolpe(int dano, Vector2 impulso)
        {
            if (muerto || EnTransicion) return 0;
            return dano;
        }

        private void AlRecibirGolpe(int dano)
        {
            flashUntil = Time.time + 0.09f;
            if (!engaged) Despertar();
            if (health.Remaining <= 0) { Morir(); return; }
            int nueva = FaseSegunVida();
            if (nueva > fase)
            {
                fase = nueva;
                transicionHasta = Time.time + SegundosTransicion;
                AlCambiarFase(fase);
            }
        }

        private int FaseSegunVida()
        {
            float pct = (float)health.Remaining / maxHealth;
            int f = 1;
            foreach (float u in Umbrales) if (pct <= u) f++;
            return f;
        }

        protected virtual void Despertar() { engaged = true; }
        protected abstract void AlCambiarFase(int nuevaFase);
        protected virtual void Morir()
        {
            muerto = true;
            engaged = false;
            LimpiarGenerados();
            SincronizacionPartida.RegistrarJefeDerrotado(Carpeta);
        }
        public abstract void Aturdir(float segundos);

        /// <summary>Kael murió: el jefe vuelve a su sitio con la vida llena.</summary>
        protected virtual void ReiniciarPelea()
        {
            if (muerto) return;
            health.SetHealth(maxHealth);
            fase = 1; transicionHasta = 0f;
            body.linearVelocity = Vector2.zero;
            transform.position = home;
            engaged = false; facing = -1;
            LimpiarGenerados();
        }

        protected GameObject Registrar(GameObject go) { generados.Add(go); return go; }

        protected void LimpiarGenerados()
        {
            foreach (var g in generados) if (g != null) Destroy(g);
            generados.Clear();
        }

        // ---------- Ayudas para atacar ----------

        protected Vector2 Kael => player != null ? (Vector2)player.position : (Vector2)transform.position;
        protected float AltoKael => playerCollider != null ? playerCollider.bounds.size.y : 1.6f;

        /// <summary>Daña a Kael si su collider toca la zona.</summary>
        protected bool Golpear(Bounds zona, int dano)
        {
            if (playerCollider == null || playerHealth == null) return false;
            if (!zona.Intersects(playerCollider.bounds)) return false;
            playerHealth.Hit(dano, transform.position);
            return true;
        }

        protected bool HaySuelo(float x, float y) =>
            Physics2D.Raycast(new Vector2(x, y + 0.3f), Vector2.down, 1.3f, 1 << TerrainLayer);

        protected static float SueloBajo(Vector2 desde, float maximo = 14f)
        {
            var hit = Physics2D.Raycast(desde, Vector2.down, maximo, 1 << TerrainLayer);
            return hit.collider != null ? hit.point.y : desde.y;
        }

        // ---------- Sprites ----------

        /// <summary>Carga Resources/Jefes/Carpeta/nombre.png partida en 'frames' cuadros (una fila).</summary>
        protected void CargarClip(string nombre, int frames)
        {
            var tex = Resources.Load<Texture2D>("Jefes/" + Carpeta + "/" + nombre);
            if (tex == null) { clips[nombre] = null; return; }
            tex.filterMode = FilterMode.Point;
            float ancho = (float)tex.width / frames;
            float ppu = tex.height / AltoVisual;
            var lista = new Sprite[frames];
            for (int i = 0; i < frames; i++)
                lista[i] = Sprite.Create(tex, new Rect(i * ancho, 0f, ancho, tex.height),
                    new Vector2(0.5f, 0.04f), ppu, 0, SpriteMeshType.FullRect);
            clips[nombre] = lista;
        }

        /// <summary>Muestra un clip. Con 'progreso' (0-1) se reproduce una vez; si es negativo, en bucle a 'fps'.</summary>
        protected void Mostrar(string nombre, float progreso = -1f, float fps = 8f)
        {
            Vector3 s = sprite.transform.localScale;
            s.x = Mathf.Abs(s.x) * facing; // las hojas miran a la derecha
            sprite.transform.localScale = s;
            sprite.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.4f, 1f) : ColorBase();

            clips.TryGetValue(nombre, out var frames);
            if (frames == null) clips.TryGetValue("Idle", out frames);
            if (frames == null)
            {
                UsandoMarcador = true;
                if (marcador == null) marcador = CrearMarcador();
                sprite.sprite = marcador;
                return;
            }
            UsandoMarcador = false;
            int i = progreso >= 0f
                ? Mathf.Min(frames.Length - 1, (int)(Mathf.Clamp01(progreso) * frames.Length))
                : (int)(Time.time * fps) % frames.Length;
            sprite.sprite = frames[i];
        }

        protected virtual Color ColorBase() => Color.white;

        private Sprite CrearMarcador()
        {
            // Rectángulo redondeado con contorno oscuro y un "ojo" al frente: solo para probar la pelea
            const int ppu = 16;
            int w = Mathf.Max(8, Mathf.RoundToInt(TamanoCuerpo.x * ppu));
            int h = Mathf.Max(8, Mathf.RoundToInt(TamanoCuerpo.y * ppu));
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
            Color fondo = ColorMarcador, borde = ColorMarcador * 0.35f, ojo = new Color(1f, 0.75f, 0.2f);
            borde.a = 1f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool esquina = (x < 3 || x > w - 4) && (y < 3 || y > h - 4);
                bool contorno = x < 2 || y < 2 || x > w - 3 || y > h - 3;
                Color c = esquina ? Color.clear : contorno ? borde : fondo;
                if (x > w - 10 && x < w - 5 && y > h - 9 && y < h - 4) c = ojo;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), ppu);
        }

        /// <summary>Sprite circular suave para efectos (núcleo, ondas, chispas).</summary>
        public static Sprite Circulo
        {
            get
            {
                if (circulo != null) return circulo;
                const int n = 32;
                var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * 1.6f));
                }
                tex.Apply();
                circulo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                return circulo;
            }
        }
        private static Sprite circulo;

        /// <summary>Sprite cuadrado blanco de 1x1 unidad para barreras, ondas y marcadores.</summary>
        public static Sprite Cuadro
        {
            get
            {
                if (cuadro == null)
                    cuadro = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                return cuadro;
            }
        }
        private static Sprite cuadro;

        // ---------- Barra de vida ----------

        private void OnGUI()
        {
            if (!engaged || muerto || health == null || pixel == null) return;
            float width = Mathf.Min(520f, Screen.width * 0.5f), x = (Screen.width - width) / 2f, y = Screen.height - 54f;
            var label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            label.normal.textColor = new Color(1f, 0.92f, 0.8f);
            string sufijo = string.IsNullOrEmpty(NombreFase) ? "" : " · " + NombreFase;
            GUI.Label(new Rect(x, y - 26f, width, 24f), NombreVisible + sufijo, label);
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(x - 3f, y - 3f, width + 6f, 18f), pixel);
            GUI.color = fase >= 3 ? new Color(1f, 0.35f, 0.2f) : fase == 2 ? new Color(1f, 0.75f, 0.25f) : new Color(0.25f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01((float)health.Remaining / maxHealth), 12f), pixel);
            // Marcas de fase
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            foreach (float u in Umbrales) GUI.DrawTexture(new Rect(x + width * u - 1f, y - 2f, 2f, 16f), pixel);
            GUI.color = old;
        }
    }
}
