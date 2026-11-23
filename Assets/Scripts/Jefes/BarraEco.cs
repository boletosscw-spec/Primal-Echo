using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // HABILIDAD DE KIRO (botón RB del control, Q o F del teclado). El dash del control queda en RT.
    //  · DESTELLO: Kiro suelta un fogonazo de luz que INMOVILIZA a los enemigos cercanos (sin daño) y
    //    BLOQUEA los ataques que vienen: rompe rocas, ondas y anillos cercanos y protege a Kael un instante.
    //    Siempre disponible, con recarga de unos segundos.
    //  · PULSO: si Kiro tiene energía de Eco, el mismo botón lanza un Pulso que además HACE DAÑO.
    //    La energía se consigue golpeando con la lanza los objetos cian (ObjetoEco): 1 carga cada uno, máx. 3.
    // Se crea sola al arrancar el juego. Solo funciona cuando Kiro ya despertó (KiroCompanero.Actual).
    public sealed class BarraEco : MonoBehaviour
    {
        public const int MaxCargas = 3;
        private const int DanoPulso = 6;
        private const float RadioPulso = 5f;
        private const float AturdePulso = 1.2f;
        private const float RadioDestello = 7f;
        private const float InmovilizaEnemigo = 2.5f;   // centinelas y enemigos normales
        private const float InmovilizaJefe = 1.2f;      // jefes (corta una embestida, pero no por mucho)
        private const float RecargaDestello = 8f;

        private static BarraEco instancia;
        private static bool avisadoEco, avisadoDestello;
        private static float listoEn;
        public static int Cargas { get; private set; }
        /// <summary>true solo mientras el Pulso reparte daño (los jefes lo dejan pasar aunque tengan escudo).</summary>
        public static bool PulsoActivo { get; private set; }
        /// <summary>0 = recién usado, 1 = listo.</summary>
        public static float CargaDestello => Mathf.Clamp01(1f - (listoEn - Time.time) / RecargaDestello);
        /// <summary>Se lanza cada vez que Kiro usa el Destello o el Pulso (lo usa el tutorial de Atlas).</summary>
        public static event System.Action Usado;
        /// <summary>Texto grande en el centro de la pantalla (tutoriales). null = nada.</summary>
        public static string AvisoCentral;
        /// <summary>Deja el Destello listo ya (para tutoriales).</summary>
        public static void PrepararDestello() => listoEn = 0f;
        private const float RadioBloqueo = 6f;
        private const float ProtegeKael = 0.5f;

        private InputAction boton;
        private Texture2D pixel;
        private float kiroVistoDesde = -1f, destelloPantalla;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            if (instancia != null) return;
            var go = new GameObject("Habilidad de Kiro");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<BarraEco>();
        }

        public static void Agregar(int n = 1)
        {
            int antes = Cargas;
            Cargas = Mathf.Clamp(Cargas + n, 0, MaxCargas);
            if (antes == 0 && Cargas > 0 && !avisadoEco)
            {
                avisadoEco = true;
                LevelHUD.Notify("Kiro: ¡Energía de Eco! Mi próximo destello (RB / Q) será un Pulso que hace daño", 4.5f);
            }
        }

        private void Awake()
        {
            pixel = Texture2D.whiteTexture;
            boton = new InputAction("HabilidadKiro", InputActionType.Button);
            boton.AddBinding("<Gamepad>/rightShoulder");  // RB (el dash del control quedó en RT)
            boton.AddBinding("<Keyboard>/q");
            boton.AddBinding("<Keyboard>/f");
            boton.Enable();
            SceneManager.sceneLoaded += (_, __) => { Cargas = 0; listoEn = 0f; kiroVistoDesde = -1f; };
        }

        private void OnDestroy() => boton?.Dispose();

        private void Update()
        {
            destelloPantalla = Mathf.MoveTowards(destelloPantalla, 0f, Time.unscaledDeltaTime * 4f);
            var kiro = KiroCompanero.Actual;
            if (kiro == null) { kiroVistoDesde = -1f; return; }
            if (kiroVistoDesde < 0f) kiroVistoDesde = Time.time;
            if (!avisadoDestello && Time.time - kiroVistoDesde > 6f)
            {
                avisadoDestello = true;
                LevelHUD.Notify("Kiro puede soltar un DESTELLO que paraliza enemigos y bloquea ataques: pulsa RB / Q", 5f);
            }

            if (Time.timeScale <= 0f || !boton.WasPressedThisFrame()) return;
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null || !pc.CanAct) return;
            Vector3 origen = kiro.transform.position;
            if (Cargas > 0)
            {
                Cargas--;
                LanzarPulso(origen);
                Bloquear(origen, pc);
                Usado?.Invoke();
            }
            else if (Time.time >= listoEn)
            {
                listoEn = Time.time + RecargaDestello;
                LanzarDestello(origen);
                Bloquear(origen, pc);
                Usado?.Invoke();
            }
            else { Vibracion.Pulso(0f, 0.12f, 0.05f); Sonidos.Play("kiro_pitido", 0.2f, 0.7f); } // aún recargando
        }

        // ---------- DESTELLO: inmoviliza, sin daño ----------
        private void LanzarDestello(Vector3 origen)
        {
            if (KiroCompanero.Actual != null) KiroCompanero.Actual.Destellar();
            destelloPantalla = 0.55f;
            Sonidos.Play("kiro_destello", 0.8f, 1f, origen, 0.03f);
            CaveDarkness.Deslumbrar(0.6f);
            Anillo(origen, new Color(1f, 1f, 0.92f, 0.9f), RadioDestello * 2f, 0.3f);
            Anillo(origen, new Color(0.45f, 0.95f, 1f, 0.6f), RadioDestello * 2.4f, 0.45f);
            Sacudida.Hacer(0.8f, 0.2f);
            Vibracion.Pulso(0.3f, 0.6f, 0.2f);

            var vistos = new HashSet<EnemyHealth>();
            foreach (var c in Physics2D.OverlapCircleAll(origen, RadioDestello))
            {
                var e = c.GetComponentInParent<EnemyHealth>();
                if (e == null || !vistos.Add(e) || e.Remaining <= 0) continue;
                var jefe = e.GetComponent<IAturdible>();
                if (jefe != null) { jefe.Aturdir(InmovilizaJefe); continue; }
                var fb = e.GetComponent<EnemyFeedback>();
                if (fb != null) fb.Aturdir(InmovilizaEnemigo);
                // marca visual: chispa amarilla sobre el enemigo (los jefes ya hicieron 'continue')
                Anillo(e.transform.position + Vector3.up, new Color(1f, 0.95f, 0.5f, 0.85f), 1.8f, 0.25f);
            }
        }

        // ---------- PULSO: daño + aturdimiento (gasta energía de Eco) ----------
        private void LanzarPulso(Vector3 origen)
        {
            if (KiroCompanero.Actual != null) KiroCompanero.Actual.Destellar();
            destelloPantalla = 0.35f;
            Sonidos.Play("kiro_pulso", 0.85f, 1f, origen, 0.03f);
            CaveDarkness.Deslumbrar(0.5f);
            Anillo(origen, new Color(0.25f, 0.96f, 1f, 0.8f), RadioPulso * 2f, 0.35f);
            Sacudida.Hacer(1.8f, 0.3f);

            var golpeados = new HashSet<EnemyHealth>();
            PulsoActivo = true;
            try
            {
                foreach (var c in Physics2D.OverlapCircleAll(origen, RadioPulso))
                {
                    var e = c.GetComponentInParent<EnemyHealth>();
                    if (e == null || !golpeados.Add(e)) continue;
                    Vector2 dir = ((Vector2)e.transform.position - (Vector2)origen).normalized;
                    e.Hit(DanoPulso, dir * 3f);
                    if (e == null || e.Remaining <= 0) continue;
                    var at = e.GetComponent<IAturdible>();
                    if (at != null) at.Aturdir(AturdePulso);
                    else { var fb = e.GetComponent<EnemyFeedback>(); if (fb != null) fb.Aturdir(AturdePulso); }
                }
            }
            finally { PulsoActivo = false; }
        }

        // Bloqueo: rompe los ataques cercanos (rocas, ondas, anillos) y protege a Kael un instante
        private static void Bloquear(Vector3 origen, PlayerController pc)
        {
            var kael = pc.GetComponent<KaelHealth>();
            if (kael != null) kael.Invulnerable(ProtegeKael);
            foreach (var p in FindObjectsByType<ProyectilJefe>(FindObjectsSortMode.None))
            {
                if (p == null) continue;
                Vector2 pos = p.transform.position;
                if (Vector2.Distance(pos, origen) <= RadioBloqueo || Vector2.Distance(pos, pc.transform.position) <= RadioBloqueo * 0.6f)
                    p.Deshacer();
            }
        }

        private static void Anillo(Vector3 pos, Color c, float tamano, float segundos)
        {
            var go = new GameObject("Destello_Kiro");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = JefeBase.Circulo; sr.color = c; sr.sortingOrder = 31; // por encima de la oscuridad de la cueva
            go.AddComponent<EfectoCrecer>().Init(tamano, segundos);
        }

        private void OnGUI()
        {
            if (pixel == null) return;
            // Fogonazo blanco en toda la pantalla
            if (destelloPantalla > 0.001f)
            {
                Color o = GUI.color;
                GUI.color = new Color(1f, 1f, 0.95f, destelloPantalla);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
                GUI.color = o;
            }
            if (!string.IsNullOrEmpty(AvisoCentral))
            {
                var grande = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold, wordWrap = true };
                float a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                var caja = new Rect(Screen.width * 0.15f, Screen.height * 0.18f, Screen.width * 0.7f, 90f);
                Color o = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(caja, pixel);
                GUI.color = o;
                grande.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                GUI.Label(new Rect(caja.x + 2f, caja.y + 2f, caja.width, caja.height), AvisoCentral, grande);
                grande.normal.textColor = new Color(1f, 0.95f, 0.6f, a);
                GUI.Label(caja, AvisoCentral, grande);
            }
            if (KiroCompanero.Actual == null) return;

            float s = 18f, x = 24f, y = Screen.height - 46f;
            Color old = GUI.color;
            // Destello: barra de recarga
            float w = 3 * (s + 8f) - 8f;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(x - 2f, y - 16f, w + 4f, 10f), pixel);
            bool listo = CargaDestello >= 1f;
            GUI.color = listo ? new Color(1f, 0.97f, 0.7f, 1f) : new Color(0.75f, 0.7f, 0.45f, 0.9f);
            GUI.DrawTexture(new Rect(x, y - 14f, w * CargaDestello, 6f), pixel);
            // Energía de Eco
            for (int i = 0; i < MaxCargas; i++)
            {
                var r = new Rect(x + i * (s + 8f), y, s, s);
                GUI.color = new Color(0f, 0f, 0f, 0.65f);
                GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), pixel);
                GUI.color = i < Cargas ? new Color(0.25f, 0.96f, 1f, 1f) : new Color(0.2f, 0.3f, 0.35f, 0.9f);
                GUI.DrawTexture(r, pixel);
            }
            GUI.color = old;
            var st = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            st.normal.textColor = Cargas > 0 ? new Color(0.6f, 1f, 1f) : listo ? new Color(1f, 0.97f, 0.75f) : new Color(0.7f, 0.7f, 0.7f);
            string texto = Cargas > 0 ? "PULSO  RB / Q" : listo ? "DESTELLO  RB / Q" : "DESTELLO...";
            GUI.Label(new Rect(x + MaxCargas * (s + 8f) + 6f, y - 4f, 200f, 22f), texto, st);
        }
    }

    /// <summary>Escala un sprite hasta 'tamano' y lo desvanece; luego se destruye.</summary>
    public sealed class EfectoCrecer : MonoBehaviour
    {
        private float tamano, duracion, t;
        private SpriteRenderer sr;
        private Color c0;
        public void Init(float tamanoFinal, float segundos) { tamano = tamanoFinal; duracion = segundos; }
        private void Start() { sr = GetComponent<SpriteRenderer>(); if (sr) c0 = sr.color; transform.localScale = Vector3.one * 0.2f; }
        private void Update()
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracion);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, tamano, 1f - (1f - p) * (1f - p));
            if (sr) sr.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - p));
            if (p >= 1f) Destroy(gameObject);
        }
    }
}
