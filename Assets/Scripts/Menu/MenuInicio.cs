using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PrimalEcho
{
    // Menú de inicio de Primal Echo. Construye toda la interfaz al arrancar con el arte de
    // Assets/Resources/MenuInicio, reproduce la intro animada y luego deja el menú en reposo
    // (partículas de eco, niebla, lanza de Kael, pulsos de eco y fallas de NÉMESIS en el título).
    // Se maneja con teclado, control de Xbox o mouse.
    public sealed class MenuInicio : MonoBehaviour
    {
        [Header("Escenas")]
        [SerializeField] private string escenaNuevaPartida = "Cinematica_Intro";

        [Header("Video de entrada (logo del estudio)")]
        [SerializeField] private VideoClip videoEstudio;

        [Header("Audio")]
        [SerializeField] private AudioClip ambiente;
        [SerializeField, Range(0f, 1f)] private float volumenAmbiente = 0.2f;
        [SerializeField, Range(0f, 1f)] private float volumenEfectos = 0.8f;

        [Header("Textos")]
        [SerializeField] private string titulo = "PRIMAL  ECHO";
        [SerializeField] private string subtitulo = "EL MUNDO DESPERTÓ";


        private const string Ruta = "MenuInicio/";
        private const float FinIntro = 4.8f;
        private static readonly Color Cian = new Color(0.45f, 0.95f, 1f, 1f);
        private static readonly Color Hueso = new Color(0.93f, 0.97f, 1f, 1f);
        private static readonly Color Apagado = new Color(0.72f, 0.79f, 0.82f, 1f);
        private static readonly Color Rojo = new Color(1f, 0.25f, 0.3f, 1f);

        private sealed class Entrada
        {
            public string texto;
            public Func<string> valor;
            public Action confirmar;
            public Action<int> cambiar;
            public bool habilitada = true;
            public Text ui;
            public float aparicion;
        }

        private sealed class Mota
        {
            public RectTransform rt;
            public Image img;
            public Vector2 pos, vel;
            public float vida, duracion, tam, fase, alfa;
            public bool luciernaga;
        }

        private sealed class Anillo
        {
            public RectTransform rt;
            public Image img;
            public float t = -1f, duracion, escala, inicio;
            public Color color;
        }

        // Capas
        private RectTransform lienzo, fondo, lanza, amuleto, follajeIzq, follajeDer, tituloGrupo, menuGrupo;
        private RectTransform mascaraSup, mascaraInf, selIzq, selDer, selBrillo, contMotas, contAnillos;
        private Image velo, fundido, brilloAbajo, brilloTitulo, brilloLanza, ojoCentinela, imgLanza, imgRestos;
        private Image imgFollajeIzq, imgFollajeDer, imgSelIzq, imgSelDer, imgSelBrillo, imgPunto;
        private RawImage niebla1, niebla2;
        private Text txtTitulo, txtGlitchA, txtGlitchB, txtSubtitulo, txtVersion, txtAyuda;
        private CanvasGroup grupoPie, grupoAyuda, grupoMenu;
        private Font fuenteTitulo, fuenteMenu, fuenteNegrita;

        // Estado
        private readonly List<Entrada> pagina = new List<Entrada>();
        private readonly List<Mota> motas = new List<Mota>();
        private readonly List<Anillo> anillos = new List<Anillo>();
        private float tIntro;
        private bool introLista, ocupado, boomSono, chispaSono, musicaSono;
        private float tFin, entradaDesde;
        private int sel;
        private float selY, selAncho, tPagina;
        private float proximoGlitch = 10f, glitchHasta = -1f, proximoOjo = 7f, ojoHasta = -1f;
        private float repeticionY, repeticionX;
        private Vector2 ultimoMouse;
        private AudioSource fuenteSfx, fuenteAmb, fuenteDron;
        private AudioClip sBoom, sTick, sAceptar, sVolver;
        private float volumenMusica = 1f;
        private VideoPlayer reproductor;
        private RawImage pantallaVideo;
        private RenderTexture texturaVideo;
        private bool enVideo, videoIniciado;
        private int cuadrosEstables;
        private float tVideo;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            AudioListener.volume = PlayerPrefs.GetFloat("pe_volumen", 1f);

            fuenteTitulo = CargarFuente("CinzelDecorative-Bold");
            fuenteMenu = CargarFuente("Cinzel-Medium");
            fuenteNegrita = CargarFuente("Cinzel-Bold");

            CrearCamara();
            CrearLienzo();
            CrearMundo();
            CrearTitulo();
            CrearMenu();
            CrearPie();
            CrearAudio();

            fundido = Img("Fundido", lienzo, null, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            Estirar(fundido.rectTransform);
            fundido.raycastTarget = false;

            AbrirPrincipal(false);
            AplicarIntro(0f);
            IniciarVideo();
        }

        // ------------------------------------------------------------------ construcción

        private void CrearCamara()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Camara Menu");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.AddComponent<AudioListener>();
        }

        private void CrearLienzo()
        {
            var go = new GameObject("Menu Inicio", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            lienzo = (RectTransform)go.transform;
        }

        private void CrearMundo()
        {
            RectTransform mundo = Nuevo("Mundo", lienzo);
            Estirar(mundo);

            RectTransform marco = Nuevo("Marco Fondo", mundo);
            Estirar(marco);
            Sprite sFondo = CargarSprite("fondo_noche", false);
            Image iFondo = Img("Fondo", marco, sFondo, Vector2.zero, new Vector2(1920f, 1080f), new Color(0.62f, 0.64f, 0.66f, 1f));
            var fit = iFondo.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = sFondo != null ? sFondo.rect.width / sFondo.rect.height : 16f / 9f;
            fondo = marco; // el zoom y la deriva se aplican al marco, no a la imagen con AspectRatioFitter

            brilloAbajo = Img("Brillo Eco", mundo, CargarSprite("brillo", false), Vector2.zero, new Vector2(1800f, 950f), Con(Cian, 0.4f));
            Anclar(brilloAbajo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f));

            niebla2 = Niebla("Niebla Lejana", mundo, 420f, 70f, 0.16f);
            niebla1 = Niebla("Niebla", mundo, 520f, -70f, 0.26f);

            imgFollajeDer = Img("Follaje Der", mundo, CargarSprite("follaje_der", true), Vector2.zero, new Vector2(780f, 720f), Color.white);
            follajeDer = imgFollajeDer.rectTransform;
            Anclar(follajeDer, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(40f, -30f));

            imgLanza = Img("Lanza de Kael", mundo, CargarSprite("lanza", true), Vector2.zero, new Vector2(200f, 950f), Color.white);
            lanza = imgLanza.rectTransform;
            Anclar(lanza, new Vector2(1f, 0f), new Vector2(0.5f, 0.02f), new Vector2(-300f, 40f));
            lanza.localRotation = Quaternion.Euler(0f, 0f, -7f);
            brilloLanza = Img("Brillo Cristal", lanza, CargarSprite("lanza_brillo", true), Vector2.zero, Vector2.zero, Color.white);
            Estirar(brilloLanza.rectTransform);
            Image halo = Img("Halo Cristal", lanza, CargarSprite("brillo", false), Vector2.zero, new Vector2(260f, 360f), Con(Cian, 0.25f));
            Anclar(halo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 950f * (163f / 190f)));
            halo.transform.SetSiblingIndex(0);
            Image iAmuleto = Img("Amuleto", lanza, CargarSprite("amuleto", true), Vector2.zero, new Vector2(70f, 200f), Color.white);
            amuleto = iAmuleto.rectTransform;
            Anclar(amuleto, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(14f, 690f));

            imgRestos = Img("Restos Centinela", mundo, CargarSprite("restos", true), Vector2.zero, new Vector2(720f, 336f), Color.white);
            Anclar(imgRestos.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-360f, -10f));
            ojoCentinela = Img("Ojo", imgRestos.rectTransform, CargarSprite("brillo", false), Vector2.zero, new Vector2(46f, 46f), Con(Cian, 0f));
            Anclar(ojoCentinela.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(165f, 171f));

            imgFollajeIzq = Img("Follaje Izq", mundo, CargarSprite("follaje_izq", true), Vector2.zero, new Vector2(780f, 720f), Color.white);
            follajeIzq = imgFollajeIzq.rectTransform;
            Anclar(follajeIzq, Vector2.zero, Vector2.zero, new Vector2(-40f, -30f));

            contMotas = Nuevo("Motas de Eco", mundo);
            Estirar(contMotas);
            Sprite sMota = CargarSprite("mota", false);
            for (int i = 0; i < 110; i++)
            {
                var m = new Mota();
                m.img = Img("Mota", contMotas, sMota, Vector2.zero, Vector2.one * 8f, Color.clear);
                m.rt = m.img.rectTransform;
                Anclar(m.rt, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
                ReiniciarMota(m, true);
                motas.Add(m);
            }

            Image vineta = Img("Vineta", mundo, CargarSprite("vineta", false), Vector2.zero, Vector2.zero, Color.white);
            Estirar(vineta.rectTransform);
            Img("Sombra Centro", mundo, CargarSprite("brillo", false), new Vector2(0f, 80f), new Vector2(1500f, 1100f), new Color(0f, 0f, 0f, 0.55f));

            velo = Img("Velo", lienzo, null, Vector2.zero, Vector2.zero, Color.black);
            Estirar(velo.rectTransform);
        }

        private RawImage Niebla(string nombre, RectTransform padre, float alto, float y, float alfa)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var raw = go.AddComponent<RawImage>();
            Texture2D tex = Resources.Load<Texture2D>(Ruta + "niebla");
            if (tex != null) tex.wrapMode = TextureWrapMode.Repeat;
            raw.texture = tex;
            raw.color = new Color(0.55f, 0.82f, 0.88f, alfa);
            raw.raycastTarget = false;
            raw.uvRect = new Rect(UnityEngine.Random.value, 0f, 1.6f, 1f);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, alto);
            rt.anchoredPosition = new Vector2(0f, y);
            return raw;
        }

        private void CrearTitulo()
        {
            tituloGrupo = Nuevo("Titulo", lienzo);
            tituloGrupo.anchoredPosition = new Vector2(0f, 255f);

            brilloTitulo = Img("Brillo Titulo", tituloGrupo, CargarSprite("brillo", false), Vector2.zero, new Vector2(1400f, 430f), Con(Cian, 0f));
            imgPunto = Img("Punto de Eco", tituloGrupo, CargarSprite("brillo", false), Vector2.zero, new Vector2(60f, 60f), Con(Cian, 0f));

            contAnillos = Nuevo("Anillos de Eco", tituloGrupo);
            Sprite sAnillo = CargarSprite("anillo", false);
            for (int i = 0; i < 6; i++)
            {
                var a = new Anillo();
                a.img = Img("Anillo", contAnillos, sAnillo, Vector2.zero, new Vector2(400f, 400f), Color.clear);
                a.rt = a.img.rectTransform;
                anillos.Add(a);
            }

            mascaraSup = Mascara("Ornamento Sup", new Vector2(0f, 117f), CargarSprite("orn_sup", false), new Vector2(1000f, 150f));
            mascaraInf = Mascara("Ornamento Inf", new Vector2(0f, -123f), CargarSprite("orn_inf", false), new Vector2(820f, 110f));

            txtGlitchA = Txt("Glitch A", tituloGrupo, fuenteTitulo, 128, Con(Rojo, 0f), Vector2.zero, new Vector2(1700f, 220f));
            txtGlitchB = Txt("Glitch B", tituloGrupo, fuenteTitulo, 128, Con(Cian, 0f), Vector2.zero, new Vector2(1700f, 220f));
            txtTitulo = Txt("Titulo", tituloGrupo, fuenteTitulo, 128, Hueso, Vector2.zero, new Vector2(1700f, 220f));
            var sombra = txtTitulo.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0.05f, 0.08f, 0.85f);
            sombra.effectDistance = new Vector2(0f, -5f);
            txtGlitchA.text = txtGlitchB.text = titulo;

            txtSubtitulo = Txt("Subtitulo", tituloGrupo, fuenteNegrita, 24, Con(new Color(0.6f, 0.86f, 0.9f), 0f), new Vector2(0f, -175f), new Vector2(1400f, 50f));
            txtSubtitulo.text = Espaciar(subtitulo);
        }

        private RectTransform Mascara(string nombre, Vector2 pos, Sprite s, Vector2 tam)
        {
            RectTransform m = Nuevo(nombre, tituloGrupo);
            m.anchoredPosition = pos;
            m.sizeDelta = new Vector2(0f, tam.y + 10f);
            var mask = m.gameObject.AddComponent<RectMask2D>();
            mask.softness = new Vector2Int(60, 0);
            Img("Ornamento", m, s, Vector2.zero, tam, Hueso);
            return m;
        }

        private void CrearMenu()
        {
            menuGrupo = Nuevo("Menu", lienzo);
            menuGrupo.anchoredPosition = new Vector2(0f, -40f);
            grupoMenu = menuGrupo.gameObject.AddComponent<CanvasGroup>();

            imgSelBrillo = Img("Brillo Seleccion", menuGrupo, CargarSprite("brillo", false), Vector2.zero, new Vector2(560f, 110f), Con(Cian, 0f));
            selBrillo = imgSelBrillo.rectTransform;
            Sprite sSel = CargarSprite("selector", false);
            imgSelIzq = Img("Selector Izq", menuGrupo, sSel, Vector2.zero, new Vector2(72f, 40f), Con(Cian, 0f));
            imgSelDer = Img("Selector Der", menuGrupo, sSel, Vector2.zero, new Vector2(72f, 40f), Con(Cian, 0f));
            selIzq = imgSelIzq.rectTransform;
            selDer = imgSelDer.rectTransform;
            selDer.localScale = new Vector3(-1f, 1f, 1f);
        }

        private void CrearPie()
        {
            RectTransform pie = Nuevo("Pie", lienzo);
            Anclar(pie, Vector2.zero, new Vector2(0.5f, 0f), new Vector2(130f, 50f));
            pie.sizeDelta = new Vector2(320f, 160f);
            grupoPie = pie.gameObject.AddComponent<CanvasGroup>();
            Img("Monster Byte Studios", pie, CargarSprite("logo_monsterbyte", false), new Vector2(0f, 22f), new Vector2(172f, 120f), new Color(1f, 1f, 1f, 0.92f));
            txtVersion = Txt("Version", pie, fuenteNegrita, 15, new Color(0.47f, 0.58f, 0.6f), new Vector2(0f, -54f), new Vector2(300f, 26f));
            txtVersion.text = "v" + Application.version;

            RectTransform ayuda = Nuevo("Ayuda", lienzo);
            Anclar(ayuda, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 40f));
            ayuda.sizeDelta = new Vector2(900f, 40f);
            grupoAyuda = ayuda.gameObject.AddComponent<CanvasGroup>();
            txtAyuda = Txt("Texto Ayuda", ayuda, fuenteNegrita, 16, new Color(0.6f, 0.72f, 0.75f, 0.75f), Vector2.zero, new Vector2(900f, 40f));
            txtAyuda.alignment = TextAnchor.MiddleRight;
            txtAyuda.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        private void CrearAudio()
        {
            fuenteSfx = gameObject.AddComponent<AudioSource>();
            fuenteSfx.playOnAwake = false;
            fuenteAmb = gameObject.AddComponent<AudioSource>();
            fuenteAmb.loop = true;
            fuenteAmb.playOnAwake = false;
            fuenteAmb.volume = 0f;
            fuenteDron = gameObject.AddComponent<AudioSource>();
            fuenteDron.loop = true;
            fuenteDron.playOnAwake = false;
            fuenteDron.volume = 0f;

            sBoom = SonidoBoom();
            sTick = Sintetizar("tick", 0.12f, t =>
                (Mathf.Sin(2f * Mathf.PI * 1250f * t) * 0.22f + Mathf.Sin(2f * Mathf.PI * 625f * t) * 0.16f) * Mathf.Exp(-t * 55f));
            sAceptar = SonidoCampana(new[] { 523.25f, 783.99f, 1046.5f }, 1.8f);
            sVolver = SonidoCampana(new[] { 392f, 523.25f }, 1.1f);
            fuenteDron.clip = SonidoDron();

            if (ambiente != null)
            {
                fuenteAmb.clip = ambiente;
                fuenteAmb.Play();
            }
        }

        // ------------------------------------------------------------------ páginas

        private void AbrirPrincipal(bool animar)
        {
            var e = new List<Entrada>
            {
                new Entrada { texto = "Iniciar partida", confirmar = () => Cargar(escenaNuevaPartida) },
                new Entrada { texto = "Elegir nivel", confirmar = AbrirNiveles },
                new Entrada { texto = "Opciones", confirmar = AbrirOpciones },
                new Entrada { texto = "Salir", confirmar = Salir },
            };
            CambiarPagina(e, 0, animar, "ENTER / A   ACEPTAR");
        }

        private void AbrirNiveles()
        {
            var e = new List<Entrada>
            {
                Nivel("Cinemática de intro", "Cinematica_Intro"),
                Nivel("Tutorial", "Nivel_00_Tutorial"),
                Nivel("Nivel 1 · Valle del Eco", "Nivel_01_Valle_del_Eco"),
                Nivel("Nivel 2 · Base de NÉMESIS", "Nivel_02_Templo_Nemesis"),
                new Entrada { texto = "Volver", confirmar = Volver },
            };
            CambiarPagina(e, 0, true, "ENTER / A   ACEPTAR        ESC / B   VOLVER");
        }

        private Entrada Nivel(string texto, string escena)
        {
            return new Entrada
            {
                texto = texto,
                habilitada = Application.CanStreamedLevelBeLoaded(escena),
                confirmar = () => Cargar(escena),
            };
        }

        private void AbrirOpciones()
        {
            var e = new List<Entrada>
            {
                new Entrada
                {
                    texto = "Volumen",
                    valor = () => Mathf.RoundToInt(AudioListener.volume * 100f) + "%",
                    cambiar = d =>
                    {
                        AudioListener.volume = Mathf.Clamp01(Mathf.Round((AudioListener.volume + d * 0.1f) * 10f) / 10f);
                        PlayerPrefs.SetFloat("pe_volumen", AudioListener.volume);
                    },
                },
                new Entrada
                {
                    texto = "Efectos",
                    valor = () => Mathf.RoundToInt(Sonidos.Volumen * 100f) + "%",
                    cambiar = d =>
                    {
                        Sonidos.Volumen = Mathf.Round((Sonidos.Volumen + d * 0.1f) * 10f) / 10f;
                        Sonidos.Play("objeto_recoger", 0.6f); // prueba
                    },
                },
                new Entrada
                {
                    texto = "Pantalla completa",
                    valor = () => Screen.fullScreen ? "Sí" : "No",
                    cambiar = d => Screen.fullScreen = !Screen.fullScreen,
                },
                new Entrada
                {
                    texto = "Vibración del control",
                    valor = () => Vibracion.Activa ? "Sí" : "No",
                    cambiar = d =>
                    {
                        Vibracion.Activa = !Vibracion.Activa;
                        if (Vibracion.Activa) Vibracion.Pulso(0.5f, 0.5f, 0.25f); // prueba
                    },
                },
                new Entrada { texto = "Volver", confirmar = Volver },
            };
            foreach (var x in e)
                if (x.cambiar != null) { var c = x.cambiar; x.confirmar = () => c(1); }
            CambiarPagina(e, 0, true, "IZQ / DER   CAMBIAR        ESC / B   VOLVER");
        }

        private bool EnPrincipal => pagina.Count > 0 && pagina[0].texto == "Iniciar partida";

        private void Volver()
        {
            if (EnPrincipal) return;
            Sonar(sVolver, 0.6f);
            AbrirPrincipal(true);
        }

        private void CambiarPagina(List<Entrada> nuevas, int seleccion, bool animar, string ayuda)
        {
            if (animar) StartCoroutine(TransicionPagina(nuevas, seleccion, ayuda));
            else MontarPagina(nuevas, seleccion, ayuda);
        }

        private IEnumerator TransicionPagina(List<Entrada> nuevas, int seleccion, string ayuda)
        {
            ocupado = true;
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                grupoMenu.alpha = 1f - t / 0.22f;
                yield return null;
            }
            grupoMenu.alpha = 0f;
            MontarPagina(nuevas, seleccion, ayuda);
            grupoMenu.alpha = 1f;
            ocupado = false;
        }

        private void MontarPagina(List<Entrada> nuevas, int seleccion, string ayuda)
        {
            foreach (var e in pagina) if (e.ui != null) Destroy(e.ui.gameObject);
            pagina.Clear();
            pagina.AddRange(nuevas);
            for (int i = 0; i < pagina.Count; i++)
            {
                var e = pagina[i];
                e.ui = Txt("Opcion " + e.texto, menuGrupo, fuenteMenu, 40, Apagado, new Vector2(0f, -i * 64f), new Vector2(900f, 60f));
                e.ui.raycastTarget = false;
                e.aparicion = i * 0.09f;
                e.ui.text = Etiqueta(e);
            }
            sel = Mathf.Clamp(seleccion, 0, pagina.Count - 1);
            if (!pagina[sel].habilitada) Mover(1, false);
            selY = -sel * 64f;
            selAncho = pagina[sel].ui.preferredWidth;
            tPagina = 0f;
            txtAyuda.text = ayuda;
        }

        private static string Etiqueta(Entrada e)
        {
            return e.valor == null ? e.texto : e.texto + ":   " + e.valor();
        }

        // ------------------------------------------------------------------ bucle

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Time.unscaledTime;

            if (enVideo)
            {
                ActualizarVideo(dt);
                return;
            }

            if (!introLista)
            {
                tIntro += dt;
                if (tIntro > 0.4f && AceptarPulsado(true)) tIntro = FinIntro;
                if (tIntro >= FinIntro)
                {
                    tIntro = FinIntro;
                    introLista = true;
                    tFin = t;
                    entradaDesde = t + 0.2f;
                    proximoGlitch = t + 7f;
                    proximoOjo = t + 5f;
                }
                AplicarIntro(tIntro);
            }

            AnimarReposo(t, dt);
            AnimarMotas(dt);
            AnimarAnillos(dt);
            AnimarMenu(dt, t);

            if (introLista && !ocupado && t >= entradaDesde) LeerEntrada();
        }

        private void AplicarIntro(float ti)
        {
            // Velo negro que deja ver el mundo
            velo.color = new Color(0f, 0f, 0f, 1f - Suave(Rango(ti, 0.9f, 3.4f)));

            // Punto de eco que crece y estalla
            float p = Rango(ti, 0.5f, 1.0f);
            float pf = 1f - Rango(ti, 1.0f, 1.6f);
            imgPunto.color = Con(Cian, Mathf.Min(p, pf) * 0.95f);
            imgPunto.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(10f, 120f, Suave(p)) * (1f + (1f - pf) * 4f);
            if (ti >= 0.6f && !musicaSono)
            {
                musicaSono = true;
                MusicaJuego.Reproducir(Pista.Menu, 3f);
            }
            if (ti >= 0.95f && !boomSono)
            {
                boomSono = true;
                Sonar(sBoom, 1f);
                LanzarAnillo(0.25f, 4.2f, 2.2f, Cian);
                LanzarAnillo(0.15f, 3.0f, 2.8f, Hueso);
            }

            // Fondo: acercamiento lento
            float zoom = Mathf.Lerp(1.18f, 1.06f, Suave(Rango(ti, 0.9f, FinIntro)));
            fondo.localScale = Vector3.one * zoom;

            // Título: letras que aparecen del centro hacia fuera
            float r = Rango(ti, 1.0f, 2.7f);
            txtTitulo.text = RevelarCentro(titulo, r, Hueso);
            float tsc = Mathf.Lerp(1.1f, 1f, Suave(Rango(ti, 1.0f, 3.0f)));
            txtTitulo.rectTransform.localScale = Vector3.one * tsc;
            float destello = Rango(ti, 1.0f, 1.5f) * (1f - Rango(ti, 1.5f, 3.2f));
            brilloTitulo.color = Con(Cian, 0.14f * Rango(ti, 1.0f, 2.0f) + destello * 0.4f);

            // Ornamentos que se dibujan desde el centro
            mascaraSup.sizeDelta = new Vector2(1060f * Suave(Rango(ti, 1.8f, 3.0f)), 160f);
            mascaraInf.sizeDelta = new Vector2(880f * Suave(Rango(ti, 2.0f, 3.1f)), 120f);

            float sub = Suave(Rango(ti, 2.6f, 3.4f));
            txtSubtitulo.color = Con(new Color(0.6f, 0.86f, 0.9f), sub);
            txtSubtitulo.rectTransform.anchoredPosition = new Vector2(0f, -175f - (1f - sub) * 14f);

            // Cristal de la lanza que se enciende
            if (ti >= 2.4f && !chispaSono)
            {
                chispaSono = true;
                Sonar(sTick, 0.5f);
            }

            float pie = Suave(Rango(ti, 3.8f, 4.6f));
            grupoPie.alpha = pie;
            grupoAyuda.alpha = pie;

            volumenMusica = Suave(Rango(ti, 0.6f, 4f));
        }

        private void AnimarReposo(float t, float dt)
        {
            float encendido = introLista ? 1f : Suave(Rango(tIntro, 2.4f, 3.2f));

            // Fondo que respira y deriva
            if (introLista) fondo.localScale = Vector3.one * (1.06f + 0.012f * Mathf.Sin((t - tFin) * 0.09f));
            fondo.anchoredPosition = new Vector2(Mathf.Sin(t * 0.05f) * 14f, Mathf.Sin(t * 0.07f) * 6f);

            brilloAbajo.color = Con(Cian, (0.32f + 0.08f * Mathf.Sin(t * 0.8f)) * (1f - velo.color.a));

            Desplazar(niebla1, dt * 0.012f);
            Desplazar(niebla2, -dt * 0.006f);

            follajeIzq.localRotation = Quaternion.Euler(0f, 0f, 1.4f * Mathf.Sin(t * 0.55f));
            follajeDer.localRotation = Quaternion.Euler(0f, 0f, -1.2f * Mathf.Sin(t * 0.5f + 1.3f));

            // Lanza y amuleto
            float pulso = 0.55f + 0.3f * Mathf.Sin(t * 1.7f) + (Ruido(t * 9f) - 0.5f) * 0.15f;
            float chispa = introLista ? 0f : Mathf.Max(0f, 1f - Mathf.Abs(tIntro - 2.6f) * 3f);
            brilloLanza.color = new Color(1f, 1f, 1f, Mathf.Clamp01(pulso * encendido + chispa));
            amuleto.localRotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(t * 1.25f) + 2.5f * Mathf.Sin(t * 2.9f));

            // Ojo del centinela caído: casi apagado, a veces parpadea
            float ojo = 0.12f + 0.06f * Mathf.Sin(t * 2.2f);
            if (introLista && t >= proximoOjo)
            {
                ojoHasta = t + 1.6f;
                proximoOjo = t + UnityEngine.Random.Range(8f, 15f);
            }
            if (t < ojoHasta)
            {
                float k = 1f - (ojoHasta - t) / 1.6f;
                ojo += Mathf.Sin(k * Mathf.PI) * (Ruido(t * 30f) > 0.3f ? 0.85f : 0.2f);
            }
            ojoCentinela.color = Con(Cian, ojo * encendido);

            if (!introLista) return;

            // El brillo y los anillos detrás del título solo salen en la intro; después queda un brillo fijo.
            brilloTitulo.color = Con(Cian, 0.14f);

            // Falla de NÉMESIS en el título
            if (t >= proximoGlitch)
            {
                glitchHasta = t + 0.35f;
                proximoGlitch = t + UnityEngine.Random.Range(9f, 16f);
            }
            if (t < glitchHasta)
            {
                bool on = Ruido(t * 40f) > 0.35f;
                float dx = on ? UnityEngine.Random.Range(-9f, 9f) : 0f;
                txtGlitchA.color = Con(Rojo, on ? 0.55f : 0f);
                txtGlitchB.color = Con(Cian, on ? 0.55f : 0f);
                txtGlitchA.rectTransform.anchoredPosition = new Vector2(-7f + dx, 2f);
                txtGlitchB.rectTransform.anchoredPosition = new Vector2(7f - dx, -2f);
                txtTitulo.rectTransform.anchoredPosition = new Vector2(on ? dx * 0.4f : 0f, 0f);
            }
            else
            {
                txtGlitchA.color = Color.clear;
                txtGlitchB.color = Color.clear;
                txtTitulo.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private void AnimarMenu(float dt, float t)
        {
            float base0 = introLista ? 0f : 3.4f;
            float tm = introLista ? tPagina : tIntro - base0;
            tPagina += dt;

            for (int i = 0; i < pagina.Count; i++)
            {
                var e = pagina[i];
                if (e.ui == null) continue;
                float a = Suave(Mathf.Clamp01((tm - e.aparicion) / 0.4f));
                bool elegido = i == sel;
                Color c = !e.habilitada ? new Color(0.4f, 0.45f, 0.47f, 1f) : elegido ? Hueso : Apagado;
                e.ui.color = Con(c, a);
                e.ui.rectTransform.anchoredPosition = new Vector2(0f, -i * 64f - (1f - a) * 24f);
                float esc = Mathf.Lerp(e.ui.rectTransform.localScale.x, elegido ? 1.06f : 1f, dt * 12f);
                e.ui.rectTransform.localScale = Vector3.one * esc;
                if (e.valor != null) e.ui.text = Etiqueta(e);
            }

            if (pagina.Count == 0) return;
            float objetivoY = -sel * 64f;
            float objetivoAncho = pagina[sel].ui.preferredWidth * 1.06f;
            selY = Mathf.Lerp(selY, objetivoY, 1f - Mathf.Exp(-dt * 16f));
            selAncho = Mathf.Lerp(selAncho, objetivoAncho, 1f - Mathf.Exp(-dt * 16f));
            float aSel = Suave(Mathf.Clamp01((tm - 0.3f) / 0.4f));
            float vaiven = 5f + 5f * Mathf.Sin(t * 3.2f);
            float x = selAncho * 0.5f + 52f + vaiven;
            selIzq.anchoredPosition = new Vector2(-x, selY + 3f);
            selDer.anchoredPosition = new Vector2(x, selY + 3f);
            imgSelIzq.color = Con(Cian, aSel * 0.95f);
            imgSelDer.color = Con(Cian, aSel * 0.95f);
            selBrillo.anchoredPosition = new Vector2(0f, selY);
            selBrillo.sizeDelta = new Vector2(selAncho + 260f, 110f);
            imgSelBrillo.color = Con(Cian, aSel * (0.13f + 0.04f * Mathf.Sin(t * 2f)));

            fuenteAmb.volume = volumenAmbiente * volumenMusica;
            fuenteDron.volume = 0f; // el tema del menú (MusicaJuego) sustituye al zumbido sintetizado
        }

        // ------------------------------------------------------------------ entrada

        private void LeerEntrada()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            Mouse m = Mouse.current;

            int dy = 0, dx = 0;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dy = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dy = 1;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) dx = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dx = 1;
            }
            if (g != null)
            {
                if (g.dpad.up.wasPressedThisFrame) dy = -1;
                if (g.dpad.down.wasPressedThisFrame) dy = 1;
                if (g.dpad.left.wasPressedThisFrame) dx = -1;
                if (g.dpad.right.wasPressedThisFrame) dx = 1;
                Vector2 s = g.leftStick.ReadValue();
                dy = dy != 0 ? dy : Repetir(-s.y, ref repeticionY);
                dx = dx != 0 ? dx : Repetir(s.x, ref repeticionX);
            }

            if (dy != 0) Mover(dy, true);
            if (dx != 0 && pagina[sel].cambiar != null && pagina[sel].habilitada)
            {
                pagina[sel].cambiar(dx);
                Sonar(sTick, 0.7f);
            }

            bool atras = (k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame))
                         || (g != null && g.buttonEast.wasPressedThisFrame)
                         || (m != null && m.rightButton.wasPressedThisFrame);
            if (atras) { Volver(); return; }

            if (m != null)
            {
                Vector2 mp = m.position.ReadValue();
                bool movio = (mp - ultimoMouse).sqrMagnitude > 4f;
                ultimoMouse = mp;
                int bajo = -1;
                for (int i = 0; i < pagina.Count; i++)
                {
                    var rt = pagina[i].ui.rectTransform;
                    if (pagina[i].habilitada && RectTransformUtility.RectangleContainsScreenPoint(rt, mp, null)
                        && Mathf.Abs(rt.InverseTransformPoint(mp).x) < pagina[i].ui.preferredWidth * 0.5f + 60f)
                        bajo = i;
                }
                if (movio && bajo >= 0 && bajo != sel)
                {
                    sel = bajo;
                    Sonar(sTick, 0.5f);
                }
                if (m.leftButton.wasPressedThisFrame && bajo >= 0)
                {
                    sel = bajo;
                    Confirmar();
                    return;
                }
            }

            if (AceptarPulsado(false)) Confirmar();
        }

        private static int Repetir(float v, ref float timer)
        {
            if (Mathf.Abs(v) < 0.35f) { timer = 0f; return 0; }
            if (Mathf.Abs(v) < 0.6f) return 0;
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return 0;
            timer = timer < -1f || timer == 0f ? 0.4f : 0.16f;
            return v > 0f ? 1 : -1;
        }

        private static bool AceptarPulsado(bool cualquiera)
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                return true;
            if (k != null && cualquiera && (k.anyKey.wasPressedThisFrame)) return true;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame)) return true;
            if (cualquiera && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            return false;
        }

        private void Mover(int d, bool sonido)
        {
            int n = pagina.Count;
            int s = sel;
            for (int i = 0; i < n; i++)
            {
                s = (s + d + n) % n;
                if (pagina[s].habilitada) break;
            }
            if (s == sel) return;
            sel = s;
            if (sonido) Sonar(sTick, 0.6f);
        }

        private void Confirmar()
        {
            var e = pagina[sel];
            if (!e.habilitada || e.confirmar == null) return;
            if (e.cambiar != null)
            {
                Sonar(sTick, 0.7f);
                e.confirmar();
                return;
            }
            if (e.texto != "Volver") Sonar(sAceptar, 0.8f);
            LanzarAnilloEn(new Vector2(0f, menuGrupo.anchoredPosition.y + selY - tituloGrupo.anchoredPosition.y), 0.1f, 1.6f, 1.2f, Cian);
            StartCoroutine(Destello(e.ui));
            e.confirmar();
        }

        private IEnumerator Destello(Text txt)
        {
            for (float t = 0f; t < 0.3f && txt != null; t += Time.unscaledDeltaTime)
            {
                txt.rectTransform.localScale = Vector3.one * (1.06f + Mathf.Sin(t / 0.3f * Mathf.PI) * 0.08f);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ salidas

        private void Cargar(string escena)
        {
            if (ocupado) return;
            string destino = escena;
            if (!Application.CanStreamedLevelBeLoaded(destino))
            {
                string[] respaldo = { "Cinematica_Intro", "Nivel_00_Tutorial", "Nivel_01_Valle_del_Eco" };
                destino = null;
                foreach (var r in respaldo)
                    if (Application.CanStreamedLevelBeLoaded(r)) { destino = r; break; }
                if (destino == null)
                {
                    Debug.LogWarning("Menú: no hay escenas en Build Settings para cargar '" + escena + "'.");
                    return;
                }
            }
            StartCoroutine(Salida(destino));
        }

        private void Salir()
        {
            if (ocupado) return;
            StartCoroutine(Salida(null));
        }

        private IEnumerator Salida(string escena)
        {
            ocupado = true;
            MusicaJuego.Detener(1.4f);
            LanzarAnillo(0.2f, 5f, 1.6f, Cian);
            LanzarAnillo(0.1f, 3.5f, 2.2f, Hueso);
            float inicioMusica = volumenMusica;
            const float dur = 1.4f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur;
                grupoMenu.alpha = 1f - Suave(Mathf.Clamp01(k * 2.2f));
                grupoPie.alpha = grupoAyuda.alpha = grupoMenu.alpha;
                fundido.color = new Color(0f, 0f, 0f, Suave(Mathf.Clamp01((k - 0.25f) / 0.75f)));
                tituloGrupo.localScale = Vector3.one * (1f + k * 0.04f);
                volumenMusica = inicioMusica * (1f - k);
                yield return null;
            }
            fundido.color = Color.black;
            volumenMusica = 0f;
            yield return new WaitForSecondsRealtime(0.25f);

            if (escena != null)
            {
                SceneManager.LoadScene(escena);
                yield break;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ video del estudio

        private void IniciarVideo()
        {
            VideoClip clip = videoEstudio != null ? videoEstudio : Resources.Load<VideoClip>(Ruta + "MonsterByte_Intro");
            if (clip == null) return;

            int w = clip.width > 0 ? (int)clip.width : 1920;
            int h = clip.height > 0 ? (int)clip.height : 1080;
            texturaVideo = new RenderTexture(w, h, 0);
            texturaVideo.Create();

            var go = new GameObject("Video Monster Byte", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            go.transform.SetSiblingIndex(fundido.transform.GetSiblingIndex());
            var negro = go.AddComponent<Image>();
            negro.color = Color.black;
            negro.raycastTarget = false;
            Estirar((RectTransform)go.transform);

            var pantalla = new GameObject("Pantalla", typeof(RectTransform));
            pantalla.transform.SetParent(go.transform, false);
            pantallaVideo = pantalla.AddComponent<RawImage>();
            pantallaVideo.texture = texturaVideo;
            pantallaVideo.color = new Color(1f, 1f, 1f, 0f);
            pantallaVideo.raycastTarget = false;
            var fit = pantalla.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = w / (float)h;

            var audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            reproductor = gameObject.AddComponent<VideoPlayer>();
            reproductor.playOnAwake = false;
            reproductor.isLooping = false;
            reproductor.source = VideoSource.VideoClip;
            reproductor.clip = clip;
            reproductor.renderMode = VideoRenderMode.RenderTexture;
            reproductor.targetTexture = texturaVideo;
            reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
            reproductor.controlledAudioTrackCount = 1;
            reproductor.EnableAudioTrack(0, true);
            reproductor.SetTargetAudioSource(0, audio);
            reproductor.skipOnDrop = false;   // si el juego se traba al arrancar, el video espera en vez de brincarse cuadros
            reproductor.waitForFirstFrame = true;
            reproductor.loopPointReached += vp => TerminarVideo();
            reproductor.errorReceived += (vp, msg) =>
            {
                Debug.LogWarning("Menú: no se pudo reproducir el video del estudio: " + msg);
                TerminarVideo();
            };
            reproductor.Prepare();
            enVideo = true;
            tVideo = 0f;
        }

        private void ActualizarVideo(float dt)
        {
            tVideo += dt;
            // Se espera a que el video esté preparado y a que el juego lleve unos cuadros estables
            // (al arrancar Unity se traba un momento); así el inicio del video no se pierde.
            if (!videoIniciado && reproductor != null && reproductor.isPrepared)
            {
                cuadrosEstables = Time.unscaledDeltaTime < 0.05f ? cuadrosEstables + 1 : 0;
                if (cuadrosEstables >= 10 || tVideo > 4f)
                {
                    videoIniciado = true;
                    reproductor.Play();
                }
            }
            // No se muestra hasta que hay un cuadro listo (evita un destello de basura en la textura)
            if (reproductor != null && reproductor.isPlaying && reproductor.frame > 0)
                pantallaVideo.color = Color.white;
            bool atascado = tVideo > 8f && (reproductor == null || !reproductor.isPlaying);
            if (atascado || (videoIniciado && tVideo > 0.8f && AceptarPulsado(true))) TerminarVideo();
        }

        private void TerminarVideo()
        {
            if (!enVideo) return;
            enVideo = false;
            StartCoroutine(OcultarVideo());
        }

        private IEnumerator OcultarVideo()
        {
            Transform contenedor = pantallaVideo != null ? pantallaVideo.transform.parent : null;
            Image negro = contenedor != null ? contenedor.GetComponent<Image>() : null;
            float inicio = pantallaVideo != null ? pantallaVideo.color.a : 0f;
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / 0.5f;
                if (pantallaVideo != null) pantallaVideo.color = new Color(1f, 1f, 1f, inicio * k);
                if (reproductor != null) reproductor.SetDirectAudioVolume(0, k);
                AudioSource a = reproductor != null ? reproductor.GetTargetAudioSource(0) : null;
                if (a != null) a.volume = k;
                yield return null;
            }
            if (reproductor != null) reproductor.Stop();
            if (contenedor != null) Destroy(contenedor.gameObject);
            if (negro != null) negro.enabled = false;
            if (texturaVideo != null)
            {
                texturaVideo.Release();
                Destroy(texturaVideo);
            }
        }

        // ------------------------------------------------------------------ efectos

        private void ReiniciarMota(Mota m, bool primera)
        {
            float w = lienzo != null ? lienzo.rect.width : 1920f;
            if (w <= 0f) w = 1920f;
            const float h = 1080f;
            m.luciernaga = UnityEngine.Random.value < 0.22f;
            m.pos = new Vector2(UnityEngine.Random.Range(-w * 0.55f, w * 0.55f),
                primera ? UnityEngine.Random.Range(-h * 0.5f, h * 0.5f) : (m.luciernaga ? UnityEngine.Random.Range(-h * 0.45f, h * 0.2f) : -h * 0.5f - 20f));
            m.vel = m.luciernaga
                ? new Vector2(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-6f, 10f))
                : new Vector2(UnityEngine.Random.Range(-8f, 18f), UnityEngine.Random.Range(22f, 70f));
            m.duracion = m.luciernaga ? UnityEngine.Random.Range(4f, 9f) : UnityEngine.Random.Range(9f, 18f);
            m.vida = primera ? UnityEngine.Random.Range(0f, m.duracion) : 0f;
            float r = UnityEngine.Random.value;
            m.tam = m.luciernaga ? UnityEngine.Random.Range(8f, 14f) : Mathf.Lerp(3f, 16f, r * r);
            m.fase = UnityEngine.Random.Range(0f, 100f);
            m.alfa = m.luciernaga ? 0.9f : Mathf.Lerp(0.25f, 0.85f, UnityEngine.Random.value);
            m.rt.sizeDelta = Vector2.one * m.tam;
        }

        private void AnimarMotas(float dt)
        {
            float visible = 1f - velo.color.a;
            float t = Time.unscaledTime;
            Color colEco = new Color(0.5f, 0.96f, 1f);
            Color colLuz = new Color(0.82f, 1f, 0.55f);
            foreach (var m in motas)
            {
                m.vida += dt;
                if (m.vida >= m.duracion || m.pos.y > 600f) { ReiniciarMota(m, false); continue; }
                m.pos += (m.vel + new Vector2(Mathf.Sin(t * 0.8f + m.fase) * 14f, 0f)) * dt;
                float k = m.vida / m.duracion;
                float a = Mathf.Clamp01(k * 5f) * Mathf.Clamp01((1f - k) * 4f) * m.alfa;
                if (m.luciernaga) a *= 0.5f + 0.5f * Mathf.Sin(t * 3f + m.fase);
                m.rt.anchoredPosition = m.pos;
                m.img.color = Con(m.luciernaga ? colLuz : colEco, a * visible);
            }
        }

        private void LanzarAnillo(float escalaInicial, float escalaFinal, float duracion, Color color)
        {
            LanzarAnilloEn(Vector2.zero, escalaInicial, escalaFinal, duracion, color);
        }

        private void LanzarAnilloEn(Vector2 pos, float escalaInicial, float escalaFinal, float duracion, Color color)
        {
            Anillo libre = null;
            foreach (var a in anillos) if (a.t < 0f) { libre = a; break; }
            if (libre == null) libre = anillos[0];
            libre.t = 0f;
            libre.duracion = duracion;
            libre.escala = escalaFinal;
            libre.color = color;
            libre.rt.anchoredPosition = pos;
            libre.inicio = escalaInicial;
            libre.rt.sizeDelta = new Vector2(400f, 400f);
            libre.rt.localScale = new Vector3(escalaInicial, escalaInicial * 0.55f, 1f);
        }

        private void AnimarAnillos(float dt)
        {
            foreach (var a in anillos)
            {
                if (a.t < 0f) continue;
                a.t += dt;
                float k = a.t / a.duracion;
                if (k >= 1f) { a.t = -1f; a.img.color = Color.clear; continue; }
                float e = 1f - Mathf.Pow(1f - k, 3f);
                float s = Mathf.Lerp(a.inicio, a.escala, e);
                a.rt.localScale = new Vector3(s, s * 0.55f, 1f);
                a.img.color = Con(a.color, (1f - k) * (1f - k) * 0.8f);
            }
        }

        // ------------------------------------------------------------------ audio sintetizado

        private AudioClip SonidoBoom()
        {
            const int sr = 44100;
            const float dur = 3.2f;
            int n = (int)(sr * dur);
            var baseOnda = new float[n];
            var rnd = new System.Random(7);
            float ruido = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float fase = 2f * Mathf.PI * (62f * t - (62f - 34f) * Mathf.Min(t, 1.2f) * Mathf.Min(t, 1.2f) / 2.4f);
                ruido = ruido * 0.97f + ((float)rnd.NextDouble() * 2f - 1f) * 0.03f;
                baseOnda[i] = (Mathf.Sin(fase) * 0.75f + Mathf.Sin(fase * 2.01f) * 0.12f) * Mathf.Exp(-t * 2.4f)
                              * Mathf.Clamp01(t * 200f) + ruido * 3f * Mathf.Exp(-t * 6f);
            }
            var datos = new float[n];
            for (int eco = 0; eco < 5; eco++)
            {
                int d = (int)(eco * 0.36f * sr);
                float g = Mathf.Pow(0.45f, eco);
                for (int i = 0; i + d < n; i++) datos[i + d] += baseOnda[i] * g;
            }
            return Clip("eco", datos, sr, 0.9f);
        }

        private AudioClip SonidoCampana(float[] notas, float dur)
        {
            const int sr = 44100;
            int n = (int)(sr * dur);
            var baseOnda = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float v = 0f;
                for (int j = 0; j < notas.Length; j++)
                {
                    float inicio = j * 0.05f;
                    if (t < inicio) continue;
                    float tt = t - inicio;
                    v += Mathf.Sin(2f * Mathf.PI * notas[j] * tt) * Mathf.Exp(-tt * 4.5f) / (1f + j * 0.6f);
                }
                baseOnda[i] = v * Mathf.Clamp01(t * 400f);
            }
            var datos = new float[n];
            for (int eco = 0; eco < 4; eco++)
            {
                int d = (int)(eco * 0.22f * sr);
                float g = Mathf.Pow(0.4f, eco);
                for (int i = 0; i + d < n; i++) datos[i + d] += baseOnda[i] * g;
            }
            return Clip("campana", datos, sr, 0.5f);
        }

        private static AudioClip SonidoDron()
        {
            const int sr = 22050;
            const float dur = 8f;
            int n = (int)(sr * dur);
            var datos = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float lfo = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * t / 8f);
                float lfo2 = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 4f + 1f);
                datos[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f
                            + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.3f * lfo2
                            + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.18f
                            + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.08f * (1f - lfo2)) * lfo;
            }
            return Clip("dron", datos, sr, 0.35f);
        }

        private static AudioClip Sintetizar(string nombre, float dur, Func<float, float> f)
        {
            const int sr = 44100;
            int n = (int)(sr * dur);
            var datos = new float[n];
            for (int i = 0; i < n; i++) datos[i] = f(i / (float)sr);
            return Clip(nombre, datos, sr, 0f);
        }

        private static AudioClip Clip(string nombre, float[] datos, int sr, float normalizar)
        {
            if (normalizar > 0f)
            {
                float max = 0.0001f;
                foreach (float v in datos) max = Mathf.Max(max, Mathf.Abs(v));
                float g = normalizar / max;
                for (int i = 0; i < datos.Length; i++) datos[i] *= g;
            }
            var clip = AudioClip.Create(nombre, datos.Length, 1, sr, false);
            clip.SetData(datos, 0);
            return clip;
        }

        private void Sonar(AudioClip c, float vol)
        {
            if (c != null && fuenteSfx != null) fuenteSfx.PlayOneShot(c, vol * volumenEfectos);
        }

        // ------------------------------------------------------------------ utilidades

        private static string RevelarCentro(string s, float progreso, Color color)
        {
            if (progreso >= 1f) return s;
            var sb = new StringBuilder(s.Length * 24);
            float centro = (s.Length - 1) * 0.5f;
            string rgb = ColorUtility.ToHtmlStringRGB(color);
            for (int i = 0; i < s.Length; i++)
            {
                float dist = Mathf.Abs(i - centro) / Mathf.Max(1f, centro);
                float a = Mathf.Clamp01((progreso * 1.6f - dist * 0.6f) / 0.4f);
                int ai = Mathf.RoundToInt(Suave(a) * 255f);
                sb.Append("<color=#").Append(rgb).Append(ai.ToString("X2")).Append('>').Append(s[i]).Append("</color>");
            }
            return sb.ToString();
        }

        private static string Espaciar(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s.ToUpperInvariant())
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(c == ' ' ? "  " : c.ToString());
            }
            return sb.ToString();
        }

        private static void Desplazar(RawImage raw, float dx)
        {
            if (raw == null) return;
            Rect r = raw.uvRect;
            r.x = Mathf.Repeat(r.x + dx, 1f);
            raw.uvRect = r;
        }

        private static float Ruido(float x)
        {
            return Mathf.PerlinNoise(x, 0.37f);
        }

        private static float Rango(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));
        private static float Suave(float x) => x * x * (3f - 2f * x);
        private static Color Con(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private Font CargarFuente(string nombre)
        {
            Font f = Resources.Load<Font>(Ruta + nombre);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        private static Sprite CargarSprite(string nombre, bool pixelArt)
        {
            Sprite s = Resources.Load<Sprite>(Ruta + nombre);
            if (s == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(Ruta + nombre);
                if (tex == null)
                {
                    Debug.LogWarning("Menú: falta Resources/" + Ruta + nombre);
                    return null;
                }
                s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (pixelArt && s.texture != null) s.texture.filterMode = FilterMode.Point;
            return s;
        }

        private static RectTransform Nuevo(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        private static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = ancla;
            rt.pivot = pivote;
            rt.anchoredPosition = pos;
        }

        private static Image Img(string nombre, Transform padre, Sprite s, Vector2 pos, Vector2 tam, Color c)
        {
            RectTransform rt = Nuevo(nombre, padre);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        private static Text Txt(string nombre, Transform padre, Font f, int tam, Color c, Vector2 pos, Vector2 caja)
        {
            RectTransform rt = Nuevo(nombre, padre);
            rt.anchoredPosition = pos;
            rt.sizeDelta = caja;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = f;
            t.fontSize = tam;
            t.color = c;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }
    }
}
