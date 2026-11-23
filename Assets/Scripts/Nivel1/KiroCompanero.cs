using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Kiro, el pequeño organismo biomecánico, acompaña a Kael después de que lo repara (Nivel 1, con Orin).
    // Qué hace:
    //   · Compañero: sigue a Kael caminando y saltando; si Kael se queda quieto un rato, se duerme.
    //   · Radar: si hay un orbe de eco, un objeto o la llave cerca, se detiene, escanea y marca dónde está.
    //   · Linterna: en las cuevas le brillan los ojos y la oscuridad pesa menos.
    //   · Alerta: cuando se acerca un centinela se sobresalta y le sale un "!".
    //   · Esconderse: en plena pelea se pone detrás de Kael, del lado contrario al enemigo.
    //   · Celebrar: al recoger un orbe o la llave, o al vencer a un enemigo.
    //   · Hackear: KiroCompanero.Hackear(terminal, listo) lo manda a conectarse a una terminal (para el Nivel 2).
    // Pasa de una escena a otra; desde el Nivel 2 aparece solo aunque se entre desde "Elegir nivel".
    // Arte: Assets/Resources/Kiro/anim/kiro_*.png (cuadros de 256x256 en fila, Kiro mirando a la derecha).
    public sealed class KiroCompanero : MonoBehaviour
    {
        private const string Carpeta = "Kiro/anim/";
        internal const float Escala = 0.6f;                       // 240 px de Kiro ≈ 1.45 unidades
        internal const float AlturaPies = (128f - 16f) / 100f;    // del centro del cuadro a las patas (a escala 1)

        private static KiroCompanero actual;
        public static KiroCompanero Actual => actual;

        private enum Estado { Siguiendo, Durmiendo, Radar, Alerta, Escondido, Celebrando, Hackeando }
        private Estado estado = Estado.Siguiendo;
        private float tEstado;
        private bool presentacion, estabaEnAire;
        private float tiempoAire, aterrizajeHasta;
        private string ultimaAnimacion;
        private float tiempoAnimacion;

        private class Clip { public Sprite[] f; public float fps; public int bucleDesde; }
        private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        private Sprite respaldo;

        private SpriteRenderer sr;
        private Light2D luz;
        private float destelloHasta;

        /// <summary>Destello / Pulso: Kiro se ilumina de golpe y pone cara de alerta.</summary>
        public void Destellar()
        {
            destelloHasta = Time.time + 0.35f;
            if (estado == Estado.Siguiendo || estado == Estado.Durmiendo || estado == Estado.Escondido) Empezar(Estado.Alerta);
        }
        private Transform kael;
        private PlayerController control;
        private Rigidbody2D kaelRb;
        private Vector3 vel;
        private float ultimaX, ultimaY, velY, quietoKael, proximaBusqueda, ultimoRadar = -99f;
        private bool llaveAntes, enCueva;
        private readonly HashSet<int> yaMarcados = new HashSet<int>(), yaAlertados = new HashSet<int>();
        private Transform enemigoCerca;
        private Vector3 terminal;
        private Action alHackear;
        private HoloText signo;

        // anillos del radar
        private static Texture2D texAnillo;
        private static Sprite spAnillo;
        private readonly List<(SpriteRenderer sr, float t0, float tam)> anillos = new List<(SpriteRenderer, float, float)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            actual = null; CaveDarkness.Atenuar = 0f;
            SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
        }

        private static void AlCargar(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single || actual != null || !escena.name.StartsWith("Nivel_02")) return;
            var kaelGo = GameObject.FindGameObjectWithTag("Player");
            if (kaelGo != null) Despertar(Resources.Load<Sprite>("Kiro/kiro_despierto"), kaelGo.transform.position + Vector3.left * 1.5f);
        }

        /// <summary>Crea a Kiro (si no existe ya) en la posición indicada.</summary>
        public static KiroCompanero Despertar(Sprite respaldo, Vector3 donde, bool duranteEncuentro = false)
        {
            if (actual != null) return null;
            var go = new GameObject("Kiro");
            DontDestroyOnLoad(go);
            actual = go.AddComponent<KiroCompanero>();
            actual.respaldo = respaldo;
            go.transform.position = donde;
            actual.ultimaX = donde.x; actual.ultimaY = donde.y;
            actual.Armar();
            actual.presentacion = duranteEncuentro;
            actual.sr.flipX = duranteEncuentro && actual.clips.ContainsKey("quieto");
            actual.Empezar(Estado.Celebrando);
            return actual;
        }

        internal void LiberarPresentacion()
        {
            if (!presentacion) return;
            presentacion = false;
            quietoKael = 0f;
            proximaBusqueda = Time.time + 0.8f;
            Empezar(Estado.Siguiendo);
        }

        /// <summary>Manda a Kiro a conectarse a una terminal. Llama a <paramref name="listo"/> al terminar.</summary>
        public static bool Hackear(Vector3 posTerminal, Action listo)
        {
            if (actual == null) return false;
            actual.terminal = posTerminal;
            actual.alHackear = listo;
            actual.Empezar(Estado.Hackeando);
            return true;
        }

        private void Armar()
        {
            Cargar("quieto", 8f, 0); Cargar("caminar", 13f, 0); Cargar("saltar", 0f, 0); Cargar("dormir", 3f, 4);
            Cargar("radar", 9f, 6); Cargar("luz", 11f, 0); Cargar("alerta", 12f, 4); Cargar("esconderse", 9f, 4);
            Cargar("celebrar", 10f, 0); Cargar("hackear", 8f, 6);

            var hijo = new GameObject("Sprite");
            hijo.transform.SetParent(transform, false);
            sr = hijo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;   // detrás de Kael (10)
            bool hojas = clips.ContainsKey("quieto");
            if (hojas)
            {
                hijo.transform.localScale = new Vector3(Escala, Escala, 1f);
                sr.sprite = clips["quieto"].f[0];
                hijo.transform.localPosition = Vector3.up * (AlturaPies * Escala);
            }
            else sr.sprite = respaldo;   // sin hojas: el dibujo fijo de Kiro despierto

            var luzGo = new GameObject("Ojos");
            luzGo.transform.SetParent(transform, false);
            luzGo.transform.localPosition = new Vector3(0.4f, 0.75f, 0f);
            luz = luzGo.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.color = new Color(0.55f, 0.95f, 1f);
            luz.intensity = 0f;
            luz.pointLightOuterRadius = 6.5f;
            luz.pointLightInnerRadius = 0.4f;

            llaveAntes = LabKey.Has;
            EchoShard.Collected += AlRecoger;
            EnemyHealth.Died += AlVencer;
        }

        internal static Sprite[] CargarCuadros(string nombre)
        {
            var tex = Resources.Load<Texture2D>(Carpeta + "kiro_" + nombre);
            const int lado = 256;
            if (tex == null || tex.height != lado || tex.width % lado != 0) return null;
            int n = tex.width / lado;
            return AnimacionHoja.Cortar(tex, n, n, lado, lado, 0);
        }

        private void Cargar(string nombre, float fps, int bucleDesde)
        {
            var f = CargarCuadros(nombre);
            if (f != null && f.Length > 0) clips[nombre] = new Clip { f = f, fps = fps, bucleDesde = Mathf.Min(bucleDesde, f.Length - 1) };
        }

        private void OnEnable() => SceneManager.sceneLoaded += Nueva;
        private void OnDisable() => SceneManager.sceneLoaded -= Nueva;
        private void OnDestroy()
        {
            EchoShard.Collected -= AlRecoger;
            EnemyHealth.Died -= AlVencer;
            if (actual == this) { actual = null; CaveDarkness.Atenuar = 0f; }
            foreach (var a in anillos) if (a.sr != null) Destroy(a.sr.gameObject);
        }

        private void Nueva(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            kael = null;
            Buscar();
            if (kael == null) { Destroy(gameObject); return; }   // menú o cinemática: Kiro se va con la escena
            transform.position = kael.position + Vector3.left * 1.5f;
            vel = Vector3.zero;
            ultimaX = transform.position.x;
            ultimaY = transform.position.y;
            velY = 0f;
            estabaEnAire = false;
            aterrizajeHasta = 0f;
            presentacion = false;
            yaMarcados.Clear(); yaAlertados.Clear();
            llaveAntes = LabKey.Has;
            Empezar(Estado.Siguiendo);
        }

        private void Buscar()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            kael = go.transform;
            control = go.GetComponent<PlayerController>();
            kaelRb = go.GetComponent<Rigidbody2D>();
        }

        private void Empezar(Estado e)
        {
            if (e != estado)
            {
                Vector3 p = transform.position;
                if (e == Estado.Alerta) Sonidos.Play("kiro_alerta", 0.35f, 1f, p);
                else if (e == Estado.Celebrando || e == Estado.Radar) Sonidos.Play("kiro_pitido", 0.3f, e == Estado.Radar ? 0.85f : 1.15f, p, 0.1f);
            }
            estado = e; tEstado = 0f;
        }

        private void AlRecoger() { if (estado != Estado.Hackeando) Empezar(Estado.Celebrando); }

        private void AlVencer(EnemyHealth e)
        {
            if (e == null || kael == null || estado == Estado.Hackeando) return;
            if (Vector2.Distance(e.transform.position, kael.position) < 16f) Empezar(Estado.Celebrando);
        }

        // ───────────── comportamiento ─────────────
        private void LateUpdate()
        {
            if (kael == null) { Buscar(); if (kael == null) return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            tEstado += dt;
            if (presentacion)
            {
                AnimarEncuentro(dt);
                return;
            }

            if (Time.time >= proximaBusqueda) { proximaBusqueda = Time.time + 0.4f; Revisar(); }

            bool kaelSeMueve = kaelRb != null ? kaelRb.linearVelocity.sqrMagnitude > 0.04f : true;
            quietoKael = kaelSeMueve ? 0f : quietoKael + dt;

            // ¿qué toca hacer?
            switch (estado)
            {
                case Estado.Siguiendo:
                    if (enemigoCerca != null) Empezar(Estado.Escondido);
                    else if (quietoKael > 12f) Empezar(Estado.Durmiendo);
                    break;
                case Estado.Durmiendo:
                    if (kaelSeMueve || enemigoCerca != null) Empezar(Estado.Siguiendo);
                    break;
                case Estado.Radar: if (tEstado > 1.8f) Empezar(Estado.Siguiendo); break;
                case Estado.Alerta: if (tEstado > 1.0f) Empezar(enemigoCerca != null ? Estado.Escondido : Estado.Siguiendo); break;
                case Estado.Escondido: if (enemigoCerca == null && tEstado > 0.6f) Empezar(Estado.Siguiendo); break;
                case Estado.Celebrando: if (tEstado > 1.1f) Empezar(Estado.Siguiendo); break;
                case Estado.Hackeando:
                    if (tEstado > 2.6f)
                    {
                        var listo = alHackear; alHackear = null;
                        Empezar(Estado.Siguiendo);
                        listo?.Invoke();
                    }
                    break;
            }

            // a dónde va
            float lado = control != null ? control.Facing : 1f;
            Vector3 meta;
            bool quieto = estado == Estado.Durmiendo || estado == Estado.Radar || estado == Estado.Alerta || estado == Estado.Celebrando;
            if (estado == Estado.Hackeando) meta = terminal + Vector3.left * 1.1f;
            else if (estado == Estado.Escondido && enemigoCerca != null)
                meta = kael.position + Vector3.right * (enemigoCerca.position.x > kael.position.x ? -0.9f : 0.9f);
            else meta = kael.position + new Vector3(-lado * 1.5f, 0f, 0f);

            if ((meta - transform.position).sqrMagnitude > 14f * 14f) { transform.position = meta; vel = Vector3.zero; }
            Vector3 p = quieto ? transform.position : Vector3.SmoothDamp(transform.position, meta, ref vel, 0.22f, 14f, dt);
            if (quieto) vel = Vector3.zero;
            // si Kael está en el suelo y Kiro quieto, que también baje al suelo
            if (quieto) p.y = Mathf.MoveTowards(p.y, kael.position.y, dt * 6f);
            float dx = p.x - ultimaX;
            velY = Mathf.Lerp(velY, (p.y - ultimaY) / dt, 0.3f);
            ultimaX = p.x; ultimaY = p.y;
            transform.position = p;

            // linterna en cuevas
            enCueva = CaveAmbience.IsInside((Vector2)p + Vector2.up * 0.5f);
            if (Time.time < destelloHasta) { luz.intensity = 6f; luz.pointLightOuterRadius = 11f; }   // fogonazo del destello
            else
            {
                luz.pointLightOuterRadius = Mathf.MoveTowards(luz.pointLightOuterRadius, 6.5f, dt * 8f);
                luz.intensity = Mathf.MoveTowards(luz.intensity, enCueva ? 1.6f + 0.12f * Mathf.Sin(Time.time * 5.3f) : 0f, dt * 2f);
            }
            CaveDarkness.Atenuar = Mathf.MoveTowards(CaveDarkness.Atenuar, enCueva ? 1f : 0f, dt * 1.2f); // 1 = linterna de Kiro encendida

            Animar(dx, dt);
            Vector3 foco = luz.transform.localPosition;
            foco.x = sr.flipX ? -0.4f : 0.4f;
            luz.transform.localPosition = foco;
            Anillos();
        }

        private void AnimarEncuentro(float dt)
        {
            // Después de levantarse, Kiro escucha la señal del este y responde con un pulso.
            if (estado == Estado.Celebrando && tEstado >= 0.72f)
            {
                Empezar(Estado.Radar);
                sr.flipX = !clips.ContainsKey("quieto");
                Pulso(transform.position + new Vector3(0.35f, 0.8f, 0f), 2.3f, 0.33f);
            }
            else if (estado == Estado.Radar && tEstado >= 1.05f)
                Empezar(Estado.Siguiendo);

            Animar(0f, dt);
            Anillos();
        }

        // Busca objetos para el radar y centinelas cerca (dos veces por segundo, no en cada cuadro).
        private void Revisar()
        {
            if (kael == null) return;
            Vector2 k = kael.position;

            enemigoCerca = null;
            float mejor = 7f;
            foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (e.Remaining <= 0) continue;
                float d = Vector2.Distance(e.transform.position, k);
                if (d < mejor) { mejor = d; enemigoCerca = e.transform; }
                if (d < 11f && yaAlertados.Add(e.GetInstanceID()) && estado != Estado.Hackeando)
                {
                    Empezar(Estado.Alerta);
                    sr.flipX = e.transform.position.x < transform.position.x;
                }
            }

            if (!llaveAntes && LabKey.Has) { llaveAntes = true; if (estado != Estado.Hackeando) Empezar(Estado.Celebrando); }

            // radar: lo más cercano que no haya marcado todavía
            if (estado != Estado.Siguiendo || Time.time - ultimoRadar < 3f) return;
            Transform hallado = null;
            float cerca = 8f;
            void Probar(Component c)
            {
                if (c == null || !c.gameObject.activeInHierarchy || yaMarcados.Contains(c.GetInstanceID())) return;
                float d = Vector2.Distance(c.transform.position, k);
                if (d < cerca) { cerca = d; hallado = c.transform; }
            }
            foreach (var o in FindObjectsByType<EchoShard>(FindObjectsSortMode.None)) Probar(o);
            foreach (var o in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None)) Probar(o);
            foreach (var o in FindObjectsByType<KeyPickup>(FindObjectsSortMode.None)) Probar(o);
            if (hallado == null) return;
            foreach (var c in hallado.GetComponents<Component>()) yaMarcados.Add(c.GetInstanceID());
            ultimoRadar = Time.time;
            Empezar(Estado.Radar);
            sr.flipX = hallado.position.x < transform.position.x;
            float lado = sr.flipX ? -1f : 1f;
            Pulso(transform.position + new Vector3(lado * 0.3f, 0.7f, 0f), 3.2f, 0.33f);
            Pulso(transform.position + new Vector3(lado * 0.3f, 0.7f, 0f), 3.2f, 0.58f);
            Pulso(hallado.position, 1.6f, 0.66f);
            Pulso(hallado.position, 1.6f, 0.96f);
        }

        private void Animar(float dx, float dt)
        {
            if (!clips.ContainsKey("quieto"))
            {
                // sin hojas de animación: saltitos con el dibujo fijo
                bool c = Mathf.Abs(dx) > 0.004f;
                sr.transform.localPosition = Vector3.up * (c ? Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.22f : 0f);
                if (c) sr.flipX = dx > 0f;
                return;
            }
            bool camina = Mathf.Abs(dx) > 0.01f;
            bool aire = !presentacion && Mathf.Abs(velY) > 2f && estado == Estado.Siguiendo;
            if (aire) tiempoAire = estabaEnAire ? tiempoAire + dt : 0f;
            else if (estabaEnAire && estado == Estado.Siguiendo) aterrizajeHasta = Time.time + 0.12f;
            estabaEnAire = aire;

            if (!presentacion && (estado == Estado.Siguiendo || estado == Estado.Escondido || estado == Estado.Hackeando))
            {
                if (camina) sr.flipX = dx < 0f;
                else if (estado == Estado.Hackeando && tEstado > 0.2f) sr.flipX = terminal.x < transform.position.x;
                else if (estado == Estado.Siguiendo) sr.flipX = kael.position.x < transform.position.x;
            }

            string nombre; int fijo = -1;
            switch (estado)
            {
                case Estado.Durmiendo: nombre = "dormir"; break;
                case Estado.Radar: nombre = "radar"; break;
                case Estado.Alerta: nombre = "alerta"; break;
                case Estado.Escondido: nombre = camina ? "caminar" : "esconderse"; break;
                case Estado.Celebrando: nombre = "celebrar"; break;
                case Estado.Hackeando: nombre = camina ? "caminar" : "hackear"; break;
                default:
                    if (aire) { nombre = "saltar"; fijo = velY < 0f ? 2 : tiempoAire < 0.08f ? 0 : 1; }
                    else if (Time.time < aterrizajeHasta) { nombre = "saltar"; fijo = 3; }
                    else if (camina) nombre = enCueva && clips.ContainsKey("luz") ? "luz" : "caminar";
                    else nombre = "quieto";
                    break;
            }
            if (!clips.TryGetValue(nombre, out var clip))
            {
                nombre = "quieto";
                clip = clips[nombre];
                fijo = -1;
            }
            if (ultimaAnimacion != nombre)
            {
                ultimaAnimacion = nombre;
                tiempoAnimacion = 0f;
            }
            int paso = (int)(tiempoAnimacion * clip.fps);
            tiempoAnimacion += dt;

            int i;
            if (fijo >= 0) i = Mathf.Min(fijo, clip.f.Length - 1);
            else if (nombre == "celebrar") i = Mathf.Min(paso, clip.f.Length - 1);
            else if (clip.bucleDesde > 0 && paso >= clip.f.Length)
                i = clip.bucleDesde + (paso - clip.f.Length) % (clip.f.Length - clip.bucleDesde);
            else i = paso % clip.f.Length;
            sr.sprite = clip.f[i];
            sr.transform.localPosition = new Vector3(0f, AlturaPies * Escala, 0f);
        }

        // ───────────── ondas del radar ─────────────
        private void Pulso(Vector3 donde, float tam, float retraso)
        {
            if (spAnillo == null)
            {
                const int n = 128;
                texAnillo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Radar_Kiro" };
                var px = new Color32[n * n];
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                        float a = Mathf.Exp(-Mathf.Pow((r - 0.85f) / 0.06f, 2f)) + 0.25f * Mathf.Exp(-Mathf.Pow((r - 0.75f) / 0.2f, 2f));
                        px[y * n + x] = new Color(0.55f, 0.95f, 1f, r <= 1f ? Mathf.Clamp01(a) : 0f);
                    }
                texAnillo.SetPixels32(px); texAnillo.Apply();
                spAnillo = Sprite.Create(texAnillo, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
            var go = new GameObject("Radar_Kiro");
            go.transform.position = donde;
            var r2 = go.AddComponent<SpriteRenderer>();
            r2.sprite = spAnillo; r2.sortingOrder = 31;   // encima de la oscuridad de las cuevas
            r2.enabled = false;
            anillos.Add((r2, Time.time + retraso, tam));
        }

        private void Anillos()
        {
            for (int i = anillos.Count - 1; i >= 0; i--)
            {
                var (r, t0, tam) = anillos[i];
                if (r == null) { anillos.RemoveAt(i); continue; }
                float p = (Time.time - t0) / 0.9f;
                if (p < 0f) continue;
                if (p >= 1f) { Destroy(r.gameObject); anillos.RemoveAt(i); continue; }
                r.enabled = true;
                float e = 1f - (1f - p) * (1f - p);
                r.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, tam, e);
                r.color = new Color(1f, 1f, 1f, 1f - p);
            }
        }

        // ───────────── signo de alerta ─────────────
        private void OnGUI()
        {
            if (estado != Estado.Alerta || Event.current.type != EventType.Repaint || Time.timeScale <= 0f) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            if (signo == null) signo = new HoloText("!", 40);
            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * (1.7f + Mathf.Abs(Mathf.Sin(tEstado * 9f)) * 0.15f));
            if (sp.z < 0f) return;
            int ui = HoloText.Scale;
            float w = signo.Width(ui), h = signo.Height(ui);
            float a = Mathf.Clamp01(tEstado / 0.1f) * Mathf.Clamp01((1f - tEstado) / 0.2f);
            signo.Draw(sp.x - w / 2f, Screen.height - sp.y - h, ui, a, HoloText.Orange);
        }
    }
}
