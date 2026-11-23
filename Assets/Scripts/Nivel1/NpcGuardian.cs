using System;
using UnityEngine;

namespace PrimalEcho
{
    // Orin (el ermitaño): cuando Kael se acerca, voltea a verlo y le habla con globos (estilo del HUD).
    // La cámara encuadra la conversación. Al terminar Orin puede retirarse a pie.
    public sealed class NpcGuardian : MonoBehaviour
    {
        [Serializable]
        public struct Hoja
        {
            public Texture2D textura;
            public int columnas, cuadros, ancho, alto, separacion;
            public float fps;
        }

        [SerializeField, TextArea] private string[] lineas = Array.Empty<string>();
        [SerializeField, Min(1f)] private float radio = 6.5f;
        [SerializeField] private bool desaparece;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Hoja quieto, camina, habla;
        [SerializeField] private Transform luz;
        [Tooltip("Si tiene a Kiro: la línea \"#REPARAR\" del diálogo pausa la plática mientras Kael lo repara")]
        [SerializeField] private ReparacionKiro reparacion;

        private enum Estado { Esperando, Hablando, Pausa, Reparando, Terminado, Saliendo, Ido }
        private const string MarcaReparar = "#REPARAR";
        private const string PrefijoKael = "Kael:";
        private Estado estado;
        public const float EscalaArte = 0.52f;
        public const float AlturaPivoteArte = 2.04f; // (432 / 2 - 12) / 100, de las plantas de los pies al pivote.
        private const float TiempoSalida = 1.65f;
        private Sprite[] fQuieto, fCamina, fHabla;
        private Sprite[] animacionActual;
        private float tiempoAnimacion;
        private Transform player;
        private int linea;
        private int lineaSonada = -1;
        private float tLinea, tEstado, alpha;
        private HoloText panel;
        private int panelDe = -1;
        private bool lejosDesdeFin = true;
        private GameObject bloqueo;      // sala de cámara fija + muros invisibles mientras habla
        private CameraFollow2D seguir;
        private CameraRoom2D sala;

        public void Configure(string[] textos, float distancia, bool seVa, SpriteRenderer sr, Hoja q, Hoja c, Hoja h, Transform brillo,
                              ReparacionKiro kiro = null)
        {
            lineas = textos; radio = distancia; desaparece = seVa; visual = sr; quieto = q; camina = c; habla = h; luz = brillo; reparacion = kiro;
        }

        private bool EsMarca(int i) => i >= 0 && i < lineas.Length && lineas[i] == MarcaReparar;
        private bool EsDeKael(int i) => i >= 0 && i < lineas.Length && lineas[i].StartsWith(PrefijoKael);

        private static Sprite[] Cortar(Hoja h) => AnimacionHoja.Cortar(h.textura, h.columnas, h.cuadros, h.ancho, h.alto, h.separacion);

        private void Awake()
        {
            fQuieto = Cortar(quieto); fCamina = Cortar(camina); fHabla = Cortar(habla);
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        private float Duracion(string s) => 2.4f + (s?.Length ?? 0) * 0.045f;

        private void Update()
        {
            tEstado += Time.deltaTime;
            float dist = player != null ? Vector2.Distance(player.position, transform.position) : 99f;
            bool cerca = dist < radio;

            switch (estado)
            {
                case Estado.Esperando:
                    if (cerca && lineas.Length > 0) { estado = Estado.Hablando; linea = 0; tLinea = 0f; Bloquear(); }
                    break;
                case Estado.Hablando:
                    if (bloqueo == null && dist > radio * 1.8f) { estado = Estado.Pausa; break; }
                    if (EsMarca(linea))
                    {
                        if (reparacion != null && !reparacion.Hecha)
                        {
                            estado = Estado.Reparando;
                            reparacion.Iniciar(() => { linea++; tLinea = 0f; estado = Estado.Hablando; });
                            break;
                        }
                        linea++; tLinea = 0f;   // sin Kiro (o ya reparado) se brinca la marca
                        if (linea >= lineas.Length) { estado = Estado.Terminado; tEstado = 0f; lejosDesdeFin = false; linea = lineas.Length - 1; Liberar(); }
                        break;
                    }
                    tLinea += Time.deltaTime;
                    if (tLinea > Duracion(lineas[linea]))
                    {
                        linea++; tLinea = 0f;
                        if (linea >= lineas.Length) { estado = Estado.Terminado; tEstado = 0f; lejosDesdeFin = false; linea = lineas.Length - 1; Liberar(); }
                    }
                    break;
                case Estado.Pausa:
                    if (cerca) estado = Estado.Hablando;
                    break;
                case Estado.Reparando:
                    break;   // ReparacionKiro avisa al terminar
                case Estado.Terminado:
                    if (desaparece && tEstado > 0.8f) { estado = Estado.Saliendo; tEstado = 0f; break; }
                    // si Kael se aleja y vuelve, repite la última frase
                    if (dist > radio * 1.8f) lejosDesdeFin = true;
                    if (lejosDesdeFin && cerca && !EsMarca(lineas.Length - 1)) { estado = Estado.Hablando; tLinea = 0f; linea = lineas.Length - 1; }
                    break;
                case Estado.Saliendo:
                    transform.position += Vector3.right * 2.25f * Time.deltaTime;
                    if (tEstado >= TiempoSalida) { estado = Estado.Ido; gameObject.SetActive(false); return; }
                    break;
            }

            // Voz: suena una vez al empezar cada línea (Kael con la suya, el NPC con la de Orin)
            if (estado == Estado.Hablando && linea != lineaSonada && !EsMarca(linea) && linea < lineas.Length)
            {
                lineaSonada = linea;
                Sonidos.Voz(EsDeKael(linea) ? "kael" : "orin", lineas[linea]);
            }
            else if (estado != Estado.Hablando) lineaSonada = -1;

            bool hablando = estado == Estado.Hablando;
            alpha = Mathf.MoveTowards(alpha, hablando ? 1f : 0f, Time.deltaTime * 4f);
            Animar(cerca);
        }

        private void Animar(bool cerca)
        {
            if (visual == null) return;
            Sprite[] f = fQuieto;
            float fps = quieto.fps > 0f ? quieto.fps : 16f;
            if (estado == Estado.Saliendo && fCamina != null && fCamina.Length > 0)
            {
                f = fCamina;
                fps = camina.fps;
            }
            else if (estado == Estado.Hablando && !EsMarca(linea) && !EsDeKael(linea) && fHabla != null && fHabla.Length > 0)
            {
                f = fHabla;
                fps = habla.fps;
            }

            if (animacionActual != f)
            {
                animacionActual = f;
                tiempoAnimacion = 0f;
            }
            if (f != null && f.Length > 0) visual.sprite = f[(int)(tiempoAnimacion * fps) % f.Length];
            tiempoAnimacion += Time.deltaTime;
            if (player != null && estado != Estado.Saliendo && (cerca || estado == Estado.Hablando))
                visual.flipX = player.position.x < transform.position.x;

            if (estado == Estado.Saliendo)
            {
                visual.flipX = false;
                var c = visual.color;
                c.a = Mathf.Clamp01((TiempoSalida - tEstado) / 0.4f);
                visual.color = c;
                if (luz != null && c.a < 1f) luz.gameObject.SetActive(false);
            }
            if (luz != null)
            {
                Vector3 p = luz.localPosition;
                p.x = (visual.flipX ? -1f : 1f) * Mathf.Abs(p.x);
                luz.localPosition = p;
            }
        }

        // ── bloqueo de cámara durante el diálogo ──
        private void Bloquear()
        {
            if (bloqueo != null) return;
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float alto = cam.orthographicSize, ancho = alto * cam.aspect;
            // Encuadre centrado entre Kael y Orin, con el suelo abajo y espacio arriba para el globo de diálogo.
            // La cámara se desliza suavemente a este cuadro (mezcla de salas de CameraFollow2D) y ahí se queda.
            float kaelX = player != null ? player.position.x : transform.position.x;
            float altoNpc = visual != null ? visual.bounds.size.y : 2.2f;
            Vector2 c = new Vector2((kaelX + transform.position.x) * 0.5f,
                                    transform.position.y + Mathf.Min(alto - 1.5f, altoNpc + 1.4f));

            bloqueo = new GameObject("Bloqueo_Dialogo_" + name) { layer = 2 };   // Ignore Raycast
            bloqueo.transform.position = Vector3.zero;
            // Sala de cámara del tamaño exacto de lo que se ve: la cámara ya no se puede mover
            var poly = bloqueo.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            Vector2 a = c - new Vector2(ancho, alto), b = c + new Vector2(ancho, alto);
            poly.points = new[] { a, new Vector2(a.x, b.y), b, new Vector2(b.x, a.y) };
            sala = bloqueo.AddComponent<CameraRoom2D>();
            seguir = cam.GetComponent<CameraFollow2D>();
            if (seguir != null) seguir.SetRoom(sala, true);

            // Muros invisibles en las orillas (muy altos para que no se puedan brincar)
            const float margen = 0.6f;   // Kael se queda completo dentro de la pantalla
            foreach (float lado in new[] { -1f, 1f })
            {
                var muro = new GameObject(lado < 0f ? "Muro_Izquierdo" : "Muro_Derecho");
                muro.transform.SetParent(bloqueo.transform, false);
                muro.transform.position = new Vector3(c.x + lado * (ancho - margen + 0.5f), c.y, 0f);
                var box = muro.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1f, alto * 2f + 40f);
            }
        }

        private void Liberar()
        {
            if (reparacion != null) reparacion.LiberarKiro();
            if (bloqueo == null) return;
            if (seguir != null && sala != null) seguir.SetRoom(sala, false);   // la cámara vuelve a seguir a Kael
            Destroy(bloqueo);
            bloqueo = null; sala = null;
        }

        private void OnDisable() => Liberar();

        private void OnGUI()
        {
            if (alpha <= 0.01f || Event.current.type != EventType.Repaint || Time.timeScale <= 0f) return;
            Camera cam = Camera.main;
            if (cam == null || linea < 0 || linea >= lineas.Length) return;
            if (EsMarca(linea)) return;
            bool deKael = EsDeKael(linea) && player != null;
            if (panel == null || panelDe != linea)
            {
                string texto = deKael ? lineas[linea].Substring(PrefijoKael.Length).Trim() : lineas[linea];
                panel = new HoloText(texto, 210); panelDe = linea;
            }
            int ui = HoloText.Scale;
            Vector3 cabeza = deKael ? player.position + Vector3.up * 3.2f
                                    : transform.position + Vector3.up * (visual != null ? visual.bounds.size.y + 0.7f : 2.6f);
            Vector3 sp = cam.WorldToScreenPoint(cabeza);
            if (sp.z < 0f) return;
            float w = panel.Width(ui), h = panel.Height(ui);
            float x = Mathf.Clamp(sp.x - w / 2f, 8f, Screen.width - w - 8f);
            float yTop = Mathf.Clamp(Screen.height - sp.y - h, 8f, Screen.height - h - 8f);
            panel.Draw(x, yTop, ui, alpha, deKael ? HoloText.Green : HoloText.Cyan);
        }
    }
}
