#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Diagnóstico temporal (solo en el editor): anota en Logs/PrimalEcho_juego.log los errores del juego y,
    // una vez por segundo mientras Kael se desplaza, qué lo está moviendo (entrada, velocidad, estado).
    // Sirve para encontrar por qué Kael "avanza solo". Se puede borrar este archivo cuando ya no haga falta.
    public sealed class MovementWatch : MonoBehaviour
    {
        private const string LogPath = "Logs/PrimalEcho_juego.log";
        private static readonly HashSet<string> Seen = new HashSet<string>();
        private PlayerController player;
        private Rigidbody2D body;
        private BraceletMenu bracelet;
        private InputAction move, dash, jump;
        private float nextSample;
        private int lines;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Write($"=== Play {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} · escena {SceneManager.GetActiveScene().name} ===");
            Attach();
        }

        private static void Loaded(Scene scene, LoadSceneMode mode) { Write("--- escena " + scene.name); Attach(); }

        private static void Attach()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null && pc.GetComponent<MovementWatch>() == null) pc.gameObject.AddComponent<MovementWatch>();
        }

        private static void Write(string line)
        {
            try { File.AppendAllText(LogPath, line + "\n"); } catch { }
        }

        private static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (!Seen.Add(condition) || Seen.Count > 40) return;
            string first = (stack ?? "").Split('\n')[0];
            Write($"[{type}] {condition} | {first}");
        }

        private void Start()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            move = InputSystem.actions.FindAction("Player/Move", false);
            dash = InputSystem.actions.FindAction("Player/Dash", false);
            jump = InputSystem.actions.FindAction("Player/Jump", false);
        }

        private void LateUpdate()
        {
            if (body == null || lines > 400 || Time.unscaledTime < nextSample) return;
            if (Mathf.Abs(body.linearVelocity.x) < 0.5f) return;
            nextSample = Time.unscaledTime + 1f;
            lines++;
            if (bracelet == null) bracelet = GetComponent<BraceletMenu>();
            Vector2 input = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
            string control = move != null && move.activeControl != null ? move.activeControl.path : "-";
            var keys = Keyboard.current;
            string held = keys == null ? "sin teclado" :
                $"A{B(keys.aKey.isPressed)} D{B(keys.dKey.isPressed)} izq{B(keys.leftArrowKey.isPressed)} der{B(keys.rightArrowKey.isPressed)} shift{B(keys.leftShiftKey.isPressed)}";
            var pad = Gamepad.current;
            string stick = pad == null ? "sin control" : $"stick {pad.leftStick.ReadValue().x:0.00} cruceta {pad.dpad.ReadValue().x:0.00}";
            Write($"t={Time.time:0.0} pos=({transform.position.x:0.0},{transform.position.y:0.0}) vel=({body.linearVelocity.x:0.0},{body.linearVelocity.y:0.0}) " +
                $"entrada={input.x:0.00} por={control} | {held} | {stick} | dash{B(dash != null && dash.IsPressed())} salto{B(jump != null && jump.IsPressed())} | " +
                $"controlador={(player.enabled ? "on" : "OFF")} puedeActuar={B(player.CanAct)} suelo={B(player.IsGrounded)} estado={player.CurrentState} " +
                $"escalaTiempo={Time.timeScale:0.0} brazalete={(bracelet != null ? bracelet.Phase.ToString() : "-")} foco={B(Application.isFocused)}");
        }

        private static string B(bool value) => value ? "=1" : "=0";
    }
}
#endif
