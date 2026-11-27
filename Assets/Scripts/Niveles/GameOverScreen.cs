using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    /// <summary>
    /// Pantalla de GAME OVER: aparece cuando Kael se queda sin corazones.
    /// Opciones: reintentar desde el último checkpoint o volver al menú de inicio.
    /// Funciona con teclado, ratón y control de Xbox (stick / cruceta + A). Congela el juego mientras está abierta.
    /// </summary>
    public sealed class GameOverScreen : MonoBehaviour
    {
        public const string MenuScene = "Menu_Inicio";

        private static GameOverScreen instance;
        public static bool IsOpen => instance != null && instance.open;

        private Action onRetry;
        private string checkpointName;
        private bool open, choosing;
        private int selection;
        private float openedAt, nextMove, previousTimeScale = 1f;
        private InputActionMap controls;
        private InputAction navigate, select;
        private bool selectRequested;

        private static Font titleFont, bodyFont;
        private static Texture2D pixel;

        /// <summary>Muestra la pantalla. onRetry se llama cuando el jugador elige reintentar.</summary>
        public static void Show(string checkpoint, Action retry)
        {
            if (instance == null)
            {
                var go = new GameObject("GameOver");
                instance = go.AddComponent<GameOverScreen>();
            }
            instance.Open(checkpoint, retry);
        }

        private void Awake()
        {
            controls = new InputActionMap("GameOver");
            select = controls.AddAction("Elegir", InputActionType.Button);
            select.AddBinding("<Keyboard>/enter");
            select.AddBinding("<Keyboard>/space");
            select.AddBinding("<Gamepad>/buttonSouth");
            select.AddBinding("<Gamepad>/start");
            select.performed += _ => selectRequested = true;
            navigate = controls.AddAction("Navegar", InputActionType.Value, expectedControlLayout: "Vector2");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        }

        private void Open(string checkpoint, Action retry)
        {
            checkpointName = string.IsNullOrEmpty(checkpoint) ? "el inicio" : checkpoint;
            onRetry = retry;
            selection = 0;
            open = true;
            choosing = false;
            selectRequested = false;
            openedAt = Time.unscaledTime;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            controls.Enable();
            MusicaJuego.GameOver();
            Vibracion.Pulso(0.4f, 0.6f, 0.35f);
        }

        private void Update()
        {
            if (!open) return;
            float t = Time.unscaledTime - openedAt;
            // Medio segundo para que el oscurecido entre y no se elija nada por accidente
            if (t < 0.9f) { selectRequested = false; return; }
            choosing = true;

            Vector2 move = navigate.ReadValue<Vector2>();
            if (Mathf.Abs(move.y) > 0.5f && Time.unscaledTime >= nextMove)
            {
                selection = 1 - selection; // solo hay dos opciones
                nextMove = Time.unscaledTime + 0.25f;
                Sonidos.Play("objeto_recoger", 0.25f, 1.4f, null, 0f);
            }
            else if (Mathf.Abs(move.y) < 0.2f) nextMove = 0f;

            if (selectRequested) { selectRequested = false; Choose(selection); }
        }

        private void Choose(int option)
        {
            open = false;
            controls.Disable();
            Time.timeScale = previousTimeScale;
            if (option == 0)
            {
                MusicaJuego.FinGameOver();
                var retry = onRetry;
                onRetry = null;
                retry?.Invoke();
            }
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(MenuScene);
            }
        }

        private void OnDestroy()
        {
            controls?.Dispose();
            if (open) Time.timeScale = previousTimeScale;
            if (instance == this) instance = null;
        }

        // ───────────── Dibujo ─────────────
        private static readonly Color Cyan = new Color(0.45f, 0.95f, 1f), Ember = new Color(0.95f, 0.35f, 0.25f),
            Muted = new Color(0.62f, 0.72f, 0.7f);

        private static void LoadAssets()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            titleFont = Resources.Load<Font>("MenuInicio/CinzelDecorative-Bold");
            bodyFont = Resources.Load<Font>("MenuInicio/Cinzel-Bold");
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont == null) bodyFont = titleFont;
        }

        private static GUIStyle Style(Font f, int size, Color c)
        {
            var s = new GUIStyle { font = f, fontSize = size, alignment = TextAnchor.MiddleCenter, richText = false };
            s.normal.textColor = c;
            return s;
        }

        private static void Shadowed(Rect r, string text, GUIStyle style, float offset)
        {
            Color c = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
            GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, style);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        private void OnGUI()
        {
            if (!open) return;
            LoadAssets();
            GUI.depth = -1000;
            float t = Time.unscaledTime - openedAt;
            float fade = Mathf.Clamp01(t / 0.8f);
            float textFade = Mathf.Clamp01((t - 0.35f) / 0.6f);
            float w = Screen.width, h = Screen.height, u = h / 1080f;

            // Oscurecido con viñeta: negro en los bordes, rojizo apagado al centro
            GUI.color = new Color(0.02f, 0.01f, 0.02f, 0.82f * fade);
            GUI.DrawTexture(new Rect(0, 0, w, h), pixel);
            GUI.color = new Color(0.25f, 0.03f, 0.03f, 0.25f * fade);
            GUI.DrawTexture(new Rect(0, h * 0.3f, w, h * 0.4f), pixel);
            GUI.color = Color.white;

            // Título
            var title = Style(titleFont, Mathf.RoundToInt(132 * u), new Color(Ember.r, Ember.g, Ember.b, textFade));
            float rise = (1f - textFade) * 30f * u;
            Shadowed(new Rect(0, h * 0.28f + rise, w, 170 * u), "GAME OVER", title, 6 * u);

            // Línea decorativa
            GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f * textFade);
            float lineW = 520 * u * textFade;
            GUI.DrawTexture(new Rect((w - lineW) * 0.5f, h * 0.28f + 178 * u, lineW, Mathf.Max(1f, 2 * u)), pixel);
            GUI.color = Color.white;


            if (!choosing && t < 0.9f) return;

            string[] options = { "Reintentar · " + checkpointName, "Volver al menu" };
            float y0 = h * 0.6f;
            for (int i = 0; i < options.Length; i++)
            {
                var r = new Rect(0, y0 + i * 70 * u, w, 60 * u);
                bool hover = r.Contains(Event.current.mousePosition) && Mathf.Abs(Event.current.mousePosition.x - w * 0.5f) < 380 * u;
                if (hover && Event.current.type == EventType.MouseMove) selection = i;
                if (hover && Event.current.type == EventType.MouseDown) { selection = i; Choose(i); Event.current.Use(); return; }

                bool sel = selection == i;
                var style = Style(bodyFont, Mathf.RoundToInt((sel ? 40 : 34) * u),
                    sel ? new Color(Cyan.r, Cyan.g, Cyan.b, textFade) : new Color(Muted.r, Muted.g, Muted.b, 0.8f * textFade));
                string label = sel ? "-  " + options[i] + "  -" : options[i];
                Shadowed(r, label, style, 2 * u);
            }

            var hint = Style(bodyFont, Mathf.RoundToInt(22 * u), new Color(Muted.r, Muted.g, Muted.b, 0.6f * textFade));
            GUI.Label(new Rect(0, h - 80 * u, w, 40 * u), "A / Enter  elegir      Stick / Flechas  mover", hint);
        }
    }
}
