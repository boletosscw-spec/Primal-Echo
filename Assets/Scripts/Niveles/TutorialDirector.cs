using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Guía del tutorial (solo en la escena Nivel_00_Tutorial):
    //  · Muestra UNA instrucción a la vez, arriba al centro, con el estilo del HUD.
    //  · Cada instrucción se da por cumplida cuando Kael hace de verdad la acción; hasta entonces
    //    una barrera de energía cierra el paso a la siguiente zona.
    //  · Los enemigos del tutorial no se mueven ni atacan: son blancos de práctica.
    // Las posiciones (en tiles) corresponden a Tools/niveles/tutorial.py: si cambias el mapa, ajusta los pasos.
    public sealed class TutorialDirector : MonoBehaviour
    {
        private const string SceneName = "Nivel_00_Tutorial";

        private sealed class Step
        {
            public string pad, keys;      // texto con control de Xbox / con teclado
            public float gateX = -1f;     // barrera que se abre al cumplir el paso (-1 = sin barrera)
            public float gateY;           // altura del suelo donde está la barrera
            public Func<bool> done;
            public GameObject gate;
        }

        private readonly List<Step> steps = new List<Step>();
        private PlayerController player;
        private Collider2D body;
        private BraceletMenu bracelet;
        private InputAction dash;
        private HoloText panel;
        private string shown;
        private int current;
        private float alpha, praiseUntil, nextEnemyScan, startX;
        private bool dashed, braceletOpened, groundAlive = true, flyerAlive = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName || FindFirstObjectByType<TutorialDirector>() != null) return;
            new GameObject("Tutorial_Director").AddComponent<TutorialDirector>();
        }

        private float X => player.transform.position.x;
        private float Feet => body != null ? body.bounds.min.y : player.transform.position.y;
        private bool StandingAt(float minX, float minY) => player.IsGrounded && X >= minX && Feet >= minY - 0.3f;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) { enabled = false; return; }
            body = player.GetComponent<Collider2D>();
            bracelet = player.GetComponent<BraceletMenu>();
            dash = InputSystem.actions.FindAction("Player/Dash", false);
            startX = X;

            // Los carteles fijos del mapa no se usan aquí: la guía muestra un texto a la vez.
            var signs = GameObject.Find("Carteles");
            if (signs != null) signs.SetActive(false);

            Add("Usa el [STICK_IZQUIERDO] para MOVERTE", "Usa [A] y [D] para MOVERTE",
                () => Mathf.Abs(X - startX) > 5f);
            Add("Pulsa [A] para SALTAR · Sube al escalon", "Pulsa [ESPACIO] para SALTAR · Sube al escalon",
                () => StandingAt(23f, 10f));
            Add("Salta el hueco y llega al muro de piedra", "Salta el hueco y llega al muro de piedra",
                () => StandingAt(38f, 10f));
            Add("En el aire pulsa [A] otra vez: DOBLE SALTO · Sube el muro", "En el aire pulsa [ESPACIO] otra vez: DOBLE SALTO · Sube el muro",
                () => StandingAt(48f, 14f));
            Add("Abre tu BRAZALETE con [START] · Ahi estan tus MEJORAS y tu INVENTARIO · Cierralo con [B]",
                "Abre tu BRAZALETE con [TAB] · Ahi estan tus MEJORAS y tu INVENTARIO · Cierralo con [ESC]",
                () => braceletOpened && (bracelet == null || !bracelet.IsActive), 60.4f, 14f);
            Add("Cruza el foso: DOBLE SALTO y en el aire [RB] para el DASH", "Cruza el foso: DOBLE SALTO y en el aire [SHIFT] para el DASH",
                () => dashed && StandingAt(71f, 14f), 77f, 14f);
            Add("Las RAMAS se atraviesan saltando desde abajo · Subete a una", "Las RAMAS se atraviesan saltando desde abajo · Subete a una",
                () => player.IsGrounded && X >= 91f && X <= 104f && Feet >= 9.7f, 104f, 8f);
            Add("Ataca con la LANZA: [X] · Destruye al centinela", "Ataca con la LANZA: [J] · Destruye al centinela",
                () => !groundAlive, 113.6f, 8f);
            Add("Dispara el ARCO con [Y] · Derriba al volador · Con [ARRIBA] apuntas en diagonal",
                "Dispara el ARCO con [K] · Derriba al volador · Con [W] apuntas en diagonal",
                () => !flyerAlive, 128.6f, 8f);
            Add("Opcional: en el aire, [ABAJO] + [X] sobre las espinas hace POGO · O simplemente saltalas",
                "Opcional: en el aire, [S] + [J] sobre las espinas hace POGO · O simplemente saltalas",
                () => X >= 134.5f);
            Add("Entra al GEISER: el agua te sube al risco", "Entra al GEISER: el agua te sube al risco",
                () => StandingAt(140f, 15f));
            Add("Esas piedras se DERRUMBAN al pisarlas · Cruza sin detenerte", "Esas piedras se DERRUMBAN al pisarlas · Cruza sin detenerte",
                () => StandingAt(153f, 15f));
            Add("TUTORIAL COMPLETO · Sigue hasta la salida para entrar al VALLE DEL ECO",
                "TUTORIAL COMPLETO · Sigue hasta la salida para entrar al VALLE DEL ECO", () => false);
        }

        private void Add(string pad, string keys, Func<bool> done, float gateX = -1f, float gateY = 0f)
        {
            var step = new Step { pad = pad, keys = keys, done = done, gateX = gateX, gateY = gateY };
            if (gateX >= 0f) step.gate = BuildGate(gateX, gateY);
            steps.Add(step);
        }

        // Barrera de energía: pared fina en la capa del terreno con un haz cian visible.
        private GameObject BuildGate(float x, float y)
        {
            var gate = new GameObject("Barrera_Tutorial") { layer = 6 };
            gate.transform.SetParent(transform);
            gate.transform.position = new Vector3(x, y, 0f);
            var wall = gate.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(0.4f, 40f);
            wall.offset = new Vector2(0f, 20f);
            var beam = new GameObject("Haz");
            beam.transform.SetParent(gate.transform, false);
            beam.transform.localScale = new Vector3(0.22f, 12f, 1f);
            var renderer = beam.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 4f);
            var reference = player.GetComponentInChildren<SpriteRenderer>();
            if (reference != null) { renderer.sortingLayerID = reference.sortingLayerID; renderer.sortingOrder = reference.sortingOrder + 1; }
            return gate;
        }

        private void Update()
        {
            if (player == null || steps.Count == 0) return;
            if (bracelet == null) bracelet = player.GetComponent<BraceletMenu>();
            if (bracelet != null && bracelet.IsActive && current >= 4) braceletOpened = true;
            if (dash != null && dash.WasPerformedThisFrame() && current == 5) dashed = true;

            if (Time.unscaledTime >= nextEnemyScan)
            {
                nextEnemyScan = Time.unscaledTime + 0.2f;
                FreezeEnemies();
            }

            // Haz de las barreras: late mientras están cerradas
            foreach (var step in steps)
            {
                if (step.gate == null) continue;
                var renderer = step.gate.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null)
                    renderer.color = new Color(0.25f, 0.92f, 1f, 0.35f + 0.2f * Mathf.Sin(Time.time * 5f + step.gateX));
            }

            if (Time.timeScale > 0f && current < steps.Count - 1 && steps[current].done())
            {
                if (steps[current].gate != null) Destroy(steps[current].gate);
                current++;
                praiseUntil = Time.unscaledTime + 0.9f;
            }
        }

        // Blancos de práctica: sin IA; el terrestre además no se desplaza con los golpes.
        private void FreezeEnemies()
        {
            bool ground = false, flyer = false;
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy.TryGetComponent(out CentinelaTerrestre walker))
                {
                    ground = true;
                    if (walker.enabled)
                    {
                        walker.enabled = false;
                        if (enemy.TryGetComponent(out Rigidbody2D rb)) rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
                    }
                }
                else if (enemy.TryGetComponent(out CentinelaVolador wings))
                {
                    flyer = true;
                    if (wings.enabled)
                    {
                        wings.enabled = false;
                        foreach (var hazard in enemy.GetComponentsInChildren<ContactHazard>(true)) hazard.CanDealDamage = () => false;
                    }
                }
                else if (enemy.GetComponent<RobotPatrol>() != null) ground = true;   // aún sin convertir
                else flyer = true;
            }
            groundAlive = ground; flyerAlive = flyer;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || player == null || steps.Count == 0) return;
            if (bracelet != null && bracelet.IsActive) return; // el menú del brazalete tapa la guía
            bool praise = Time.unscaledTime < praiseUntil && current > 0;
            var step = steps[praise ? current - 1 : current];
            string text = Gamepad.current != null ? step.pad : step.keys;
            if (praise) text = "BIEN HECHO";
            if (text != shown) { shown = text; panel = new HoloText(text, 230); alpha = 0f; }
            alpha = Mathf.MoveTowards(alpha, 1f, Time.unscaledDeltaTime * 4f);
            int ui = HoloText.Scale;
            float x = (Screen.width - panel.Width(ui)) / 2f;
            float y = 8 * ui - (1f - alpha) * 6f * ui; // arriba al centro, a la altura del HUD
            int oldDepth = GUI.depth;
            GUI.depth = 4;
            panel.Draw(x, y, ui, alpha, praise ? HoloText.Green : HoloText.Cyan);
            GUI.depth = oldDepth;
        }
    }
}
