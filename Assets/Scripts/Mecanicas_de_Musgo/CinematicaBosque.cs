using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PrimalEcho
{
    // Cinemática al empezar el Nivel 1, en tercera persona desde atrás: Kael está de espaldas en la orilla
    // de un risco con musgo, mirando el valle del bosque desde arriba. A lo lejos, abajo, aparece un
    // destello cian (la señal de la base de NÉMESIS); Kael se asoma con curiosidad. Fundido a negro y,
    // ya en el juego, Kael entra caminando solo un par de segundos antes de devolver el control.
    // El juego queda en pausa mientras dura (Time.timeScale = 0). Espacio / Enter / Esc / A / Start la saltan.
    //
    // Arte en Assets/Cinematicas/Valle_del_Eco (composición de 3840x2160, se ve a la mitad en una pantalla 1920x1080):
    //   vista_1_cielo, vista_2_valle, vista_3_copas, vista_4_risco, vista_5_frente  (3840x2160 cada una)
    //   kael_espalda (8 cuadros de 1024x1024: 0-3 respira, 4-5 se asoma, 6-7 curiosidad)  ·  destello (8 de 256x256)
    public sealed class CinematicaBosque : MonoBehaviour
    {
        [SerializeField] private bool activa = true;
        [SerializeField] private Texture2D[] capas = new Texture2D[5];
        [SerializeField] private Texture2D kaelEspalda, destello;

        [Header("Puntos de la composición (píxeles de la imagen, desde abajo a la izquierda)")]
        [SerializeField] private Vector2 piesKael = new Vector2(1500f, 2160f - 1900f);
        [SerializeField] private Vector2 puntoDestello = new Vector2(2650f, 2160f - 1350f);
        [SerializeField, Range(0.3f, 1.5f)] private float escalaKael = 0.85f;

        [Header("Al terminar: Kael entra caminando solo")]
        [SerializeField, Range(0f, 1f)] private float pasoGuiado = 0.5f;
        [SerializeField, Min(0f)] private float segundosGuiado = 1.8f;

        public void Configure(Texture2D[] layers, Texture2D back, Texture2D flash)
        {
            capas = layers; kaelEspalda = back; destello = flash;
        }

        private const float AnchoComp = 3840f, AltoComp = 2160f;
        private static readonly float[] Factor = { 0.2f, 0.45f, 0.7f, 1f, 1.3f };
        private const float Barra = 110f;
        private const float ZoomFinal = 1.28f;

        // guion (segundos)
        private const float TDestello1 = 4.5f, TAsoma = 4.9f, TDestello2 = 7.6f, TFundido = 9.6f, TFin = 10.6f;
        private static readonly float[] Destellos = { TDestello1, TDestello2 };

        private RectTransform[] capaRt;
        private RawImage kael, flash, halo;
        private RawImage[] ondas;
        private Texture2D anillo;
        private Image negro, luz, barraArriba, barraAbajo;
        private Text subtitulo, aviso;
        private LevelHUD hud;
        private PlayerController jugador;
        private float t, salidaT, alfaSalida, guiadoHasta;
        private enum Fase { Corriendo, Saltando, Regresando, Guiando, Terminada }
        private Fase fase = Fase.Corriendo;
        private GameObject canvasGo;

        private void Awake()
        {
            bool completo = activa && capas != null && capas.Length >= 5 && kaelEspalda != null && destello != null;
            if (completo) foreach (var c in capas) completo &= c != null;
            if (!completo)
            {
                if (activa) Debug.LogWarning("[Cinemática Nivel 1] Falta arte en Assets/Cinematicas/Valle_del_Eco; se omite.");
                fase = Fase.Terminada;
                Destroy(gameObject);
                return;
            }
            hud = FindFirstObjectByType<LevelHUD>();
            if (hud != null) hud.enabled = false;   // su Start (y el aviso de inicio) espera a que termine la cinemática
            jugador = FindFirstObjectByType<PlayerController>();
            Time.timeScale = 0f;
            Construir();
            Aplicar(0f);
        }

        private void OnDestroy()
        {
            if (fase != Fase.Terminada)
            {
                if (Time.timeScale == 0f) Time.timeScale = 1f;
                if (hud != null) hud.enabled = true;
                if (jugador != null) jugador.MovimientoGuiado = float.NaN;
            }
            if (canvasGo != null) Destroy(canvasGo);
            if (anillo != null) Destroy(anillo);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            switch (fase)
            {
                case Fase.Corriendo:
                    if (t > 0.6f && SaltarPulsado()) { fase = Fase.Saltando; salidaT = 0f; alfaSalida = negro.color.a; break; }
                    t += dt;
                    Aplicar(t);
                    if (t >= TFin) Regresar();
                    break;
                case Fase.Saltando:
                    salidaT += dt;
                    SetAlpha(negro, Mathf.Lerp(alfaSalida, 1f, salidaT / 0.35f));
                    if (salidaT >= 0.45f) Regresar();
                    break;
                case Fase.Regresando:
                    salidaT += dt;
                    SetAlpha(negro, 1f - Mathf.Clamp01(salidaT / 0.7f));
                    if (salidaT >= 0.7f)
                    {
                        Destroy(canvasGo);
                        if (hud != null) hud.enabled = true;
                        fase = Fase.Guiando;
                    }
                    break;
                case Fase.Guiando:
                    if (jugador == null || Time.time >= guiadoHasta)
                    {
                        if (jugador != null) jugador.MovimientoGuiado = float.NaN;
                        fase = Fase.Terminada;
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        // Fin de la cinemática: se apaga todo menos el negro, el juego sigue, la pantalla se aclara
        // y Kael empieza a caminar solo.
        private void Regresar()
        {
            fase = Fase.Regresando;
            salidaT = 0f;
            SetAlpha(negro, 1f);
            foreach (Transform hijo in negro.transform.parent)
                if (hijo != negro.transform) hijo.gameObject.SetActive(false);
            Time.timeScale = 1f;
            if (jugador != null && segundosGuiado > 0f && pasoGuiado > 0f)
            {
                jugador.MovimientoGuiado = pasoGuiado;
                guiadoHasta = Time.time + segundosGuiado;
            }
        }

        private static bool SaltarPulsado()
        {
            Keyboard k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame))
                return true;
            Gamepad g = Gamepad.current;
            return g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame);
        }

        // ───────────── guion ─────────────
        private void Aplicar(float time)
        {
            // Acercamiento lento hacia el valle (la cámara se va hacia el punto entre Kael y el destello)
            float u = Suave(Mathf.Clamp01(time / TFin));
            float zoom = Mathf.Lerp(1f, ZoomFinal, u);
            Vector2 centro = new Vector2(AnchoComp, AltoComp) * 0.5f;
            Vector2 foco = Vector2.Lerp(centro, Vector2.Lerp(piesKael + new Vector2(0f, 500f), puntoDestello, 0.6f), u);
            for (int i = 0; i < capaRt.Length; i++)
            {
                float z = 0.5f * (1f + (zoom - 1f) * Factor[i]);
                Vector2 f = Vector2.Lerp(centro, foco, Factor[i]);
                capaRt[i].localScale = new Vector3(z, z, 1f);
                capaRt[i].anchoredPosition = new Vector2(960f, 540f) - f * z;
            }

            // Kael de espaldas: respira, y al ver el destello se asoma con curiosidad
            int cuadro;
            if (time < TAsoma) cuadro = PingPong((int)(time * 5f), 4);                    // 0-3
            else if (time < TAsoma + 0.5f) cuadro = 4 + Mathf.Min(1, (int)((time - TAsoma) * 4f)); // 4-5
            else cuadro = 6 + (int)((time - TAsoma) * 2.5f) % 2;                          // 6-7
            kael.uvRect = new Rect(cuadro / 8f, 0f, 0.125f, 1f);

            // Destellos
            float pico = 0f; int cuadroFlash = -1;
            foreach (float t0 in Destellos)
            {
                int f = (int)((time - t0) * 12f);
                if (f >= 0 && f < 8) { cuadroFlash = f; pico = Mathf.Max(pico, 1f - Mathf.Abs(f - 4f) / 4f); }
            }
            flash.enabled = cuadroFlash >= 0;
            if (cuadroFlash >= 0) flash.uvRect = new Rect(cuadroFlash / 8f, 0f, 0.125f, 1f);
            bool visto = time >= TDestello1 + 0.3f;
            halo.enabled = visto;
            if (visto) halo.color = new Color(1f, 1f, 1f, 0.18f + 0.08f * Mathf.Sin(time * 3.1f) + 0.3f * pico);
            SetAlpha(luz, pico * 0.09f);

            // Onda: anillos que se expanden sobre las copas del valle (elipse por la perspectiva)
            for (int i = 0; i < ondas.Length; i++)
            {
                float t0 = Destellos[i / 3] + (i % 3) * 0.28f;
                float p = (time - t0) / 1.7f;
                bool viva = p >= 0f && p < 1f;
                ondas[i].enabled = viva;
                if (!viva) continue;
                float e = 1f - (1f - p) * (1f - p) * (1f - p);          // sale rápido y frena
                float ancho = Mathf.Lerp(40f, 1300f - (i % 3) * 260f, e);
                ondas[i].rectTransform.sizeDelta = new Vector2(ancho, ancho * 0.36f);
                ondas[i].color = new Color(1f, 1f, 1f, Mathf.Pow(1f - p, 1.4f) * (i % 3 == 0 ? 1f : 0.7f));
            }

            // Barras, textos y fundidos
            float b = Barra * Suave(Mathf.Clamp01(time / 0.8f));
            barraArriba.rectTransform.sizeDelta = new Vector2(0f, b);
            barraAbajo.rectTransform.sizeDelta = new Vector2(0f, b);
            Subtitulo(time);
            SetAlpha(aviso, 0.45f * Mathf.Clamp01((time - 0.8f) / 0.6f));
            float a = 1f - Mathf.Clamp01(time / 1.2f);
            if (time > TFundido) a = Mathf.Clamp01((time - TFundido) / (TFin - TFundido));
            SetAlpha(negro, a);
        }

        private void Subtitulo(float time)
        {
            string texto = null; float desde = 0f, hasta = 0f;
            if (time >= 5.8f && time < 8.0f) { texto = "Kael: ...¿Qué fue eso?"; desde = 5.8f; hasta = 8.0f; }
            else if (time >= 8.3f && time < 10.2f) { texto = "Kael: Tengo que bajar a ver."; desde = 8.3f; hasta = 10.2f; }
            if (texto == null) { SetAlpha(subtitulo, 0f); return; }
            subtitulo.text = texto;
            SetAlpha(subtitulo, Mathf.Clamp01((time - desde) / 0.3f) * Mathf.Clamp01((hasta - time) / 0.3f));
        }

        private static int PingPong(int i, int n) { int p = i % (2 * n - 2); return p < n ? p : 2 * n - 2 - p; }
        private static float Suave(float u) => u * u * (3f - 2f * u);
        private static void SetAlpha(Graphic g, float a) { var c = g.color; c.a = a; g.color = c; }

        // ───────────── armado del Canvas ─────────────
        private void Construir()
        {
            canvasGo = new GameObject("Cinematica_Mirador_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;   // llena la pantalla aunque no sea 16:9
            Transform root = canvasGo.transform;

            var escena = Nuevo(root, "Escena");                              // área de 1920x1080 centrada
            escena.anchorMin = escena.anchorMax = new Vector2(0.5f, 0.5f);
            escena.pivot = new Vector2(0.5f, 0.5f);
            escena.sizeDelta = new Vector2(1920f, 1080f);
            Lleno(Img(root, "Fondo", Color.black)).SetAsFirstSibling();

            capaRt = new RectTransform[5];
            for (int i = 0; i < 5; i++)
            {
                capaRt[i] = Nuevo(escena, "Capa_" + (i + 1));
                Raw(capaRt[i], "Imagen", capas[i], Vector2.zero, new Vector2(AnchoComp, AltoComp));
            }
            halo = Raw(capaRt[1], "Halo_Senal", destello, new Vector2(0.5f, 0.5f), new Vector2(256f, 256f) * 2.6f);
            halo.rectTransform.anchoredPosition = puntoDestello;
            halo.uvRect = new Rect(4f / 8f, 0f, 0.125f, 1f);
            anillo = CrearAnillo(256);
            ondas = new RawImage[Destellos.Length * 3];
            for (int i = 0; i < ondas.Length; i++)
            {
                ondas[i] = Raw(capaRt[1], "Onda_" + i, anillo, new Vector2(0.5f, 0.5f), Vector2.one);
                ondas[i].rectTransform.anchoredPosition = puntoDestello;
                ondas[i].enabled = false;
            }
            flash = Raw(capaRt[1], "Destello", destello, new Vector2(0.5f, 0.5f), new Vector2(256f, 256f) * 1.1f);
            flash.rectTransform.anchoredPosition = puntoDestello;

            kael = Raw(capaRt[3], "Kael_Espalda", kaelEspalda, new Vector2(0.5f, 40f / 1024f), new Vector2(1024f, 1024f) * escalaKael);
            kael.rectTransform.anchoredPosition = piesKael;

            luz = Img(root, "Luz_Destello", new Color(0.55f, 0.95f, 1f, 0f)); Lleno(luz);

            barraArriba = Img(root, "Barra_Arriba", Color.black);
            var rt = barraArriba.rectTransform; rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 1f);
            barraAbajo = Img(root, "Barra_Abajo", Color.black);
            rt = barraAbajo.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0f);

            Font fuente = Resources.Load<Font>("MenuInicio/Cinzel-Bold");
            if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitulo = Texto(root, "Subtitulo", fuente, 34, new Color(0.93f, 0.97f, 0.94f, 0f), TextAnchor.MiddleCenter);
            rt = subtitulo.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1600f, 80f); rt.anchoredPosition = new Vector2(0f, 55f);
            var sombra = subtitulo.gameObject.AddComponent<Shadow>(); sombra.effectColor = new Color(0f, 0f, 0f, 0.8f); sombra.effectDistance = new Vector2(2f, -2f);
            aviso = Texto(root, "Aviso_Saltar", fuente, 20, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleRight);
            rt = aviso.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 40f); rt.anchoredPosition = new Vector2(-40f, 55f);
            aviso.text = "Espacio / A  ·  Saltar";

            negro = Img(root, "Fundido", Color.black); Lleno(negro);
        }

        // Anillo suave cian generado por código (la onda): borde brillante con un resplandor hacia adentro.
        private static Texture2D CrearAnillo(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Onda_Cinematica" };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float borde = Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.045f, 2f));
                    float brillo = 0.35f * Mathf.Exp(-Mathf.Pow((r - 0.8f) / 0.18f, 2f)) * (r < 0.86f ? 1f : 0.3f);
                    float a = Mathf.Clamp01(borde + brillo) * (r <= 1f ? 1f : 0f);
                    float blanco = borde * 0.6f;
                    px[y * n + x] = new Color(Mathf.Lerp(0.45f, 1f, blanco), Mathf.Lerp(0.92f, 1f, blanco), 1f, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static RectTransform Nuevo(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static RawImage Raw(Transform parent, string name, Texture tex, Vector2 pivot, Vector2 size)
        {
            var rt = Nuevo(parent, name);
            rt.pivot = pivot;
            rt.sizeDelta = size;
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            return raw;
        }

        private static Image Img(Transform parent, string name, Color color)
        {
            var rt = Nuevo(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static RectTransform Lleno(Graphic g)
        {
            var rt = g.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static Text Texto(Transform parent, string name, Font font, int size, Color color, TextAnchor anchor)
        {
            var rt = Nuevo(parent, name);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = font; txt.fontSize = size; txt.color = color; txt.alignment = anchor;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
