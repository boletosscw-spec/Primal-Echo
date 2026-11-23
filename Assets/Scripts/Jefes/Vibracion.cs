using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Vibración del control (Xbox u otro gamepad). Se crea sola al arrancar.
    //  · Solo vibra si el jugador está usando el control (no el teclado).
    //  · Se apaga sola al terminar, al pausar (timeScale 0), al cambiar de escena o al perder el foco.
    //  · Se puede desactivar en Opciones → "Vibración" (se guarda en PlayerPrefs).
    public sealed class Vibracion : MonoBehaviour
    {
        private const string Pref = "pe_vibracion";
        private static Vibracion instancia;
        private float hasta, bajo, alto;
        private bool vibrando;

        public static bool Activa
        {
            get => PlayerPrefs.GetInt(Pref, 1) == 1;
            set { PlayerPrefs.SetInt(Pref, value ? 1 : 0); PlayerPrefs.Save(); if (!value) Detener(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            if (instancia != null) return;
            var go = new GameObject("Vibracion del control");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<Vibracion>();
            SceneManager.sceneLoaded += (_, __) => Detener();
        }

        /// <summary>Vibra con el motor grave ('bajo', 0-1) y el agudo ('alto', 0-1) durante 'segundos'.</summary>
        public static void Pulso(float bajo, float alto, float segundos)
        {
            if (instancia == null || !Activa || !UsandoControl()) return;
            var pad = Gamepad.current;
            float ahora = Time.unscaledTime;
            if (instancia.vibrando && ahora < instancia.hasta)
            {
                // Si ya vibraba, se queda con lo más fuerte y lo más largo
                instancia.bajo = Mathf.Max(instancia.bajo, bajo);
                instancia.alto = Mathf.Max(instancia.alto, alto);
                instancia.hasta = Mathf.Max(instancia.hasta, ahora + segundos);
            }
            else
            {
                instancia.bajo = bajo; instancia.alto = alto; instancia.hasta = ahora + segundos;
            }
            instancia.vibrando = true;
            pad.SetMotorSpeeds(Mathf.Clamp01(instancia.bajo), Mathf.Clamp01(instancia.alto));
        }

        public static void Detener()
        {
            if (instancia != null) instancia.vibrando = false;
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }

        // ¿El último dispositivo que se tocó fue el control?
        private static bool UsandoControl()
        {
            var pad = Gamepad.current;
            if (pad == null) return false;
            var kb = Keyboard.current;
            return kb == null || pad.lastUpdateTime >= kb.lastUpdateTime - 0.25;
        }

        private void Update()
        {
            if (!vibrando) return;
            if (Time.unscaledTime >= hasta || Time.timeScale <= 0f) Detener();
        }

        private void OnApplicationFocus(bool foco) { if (!foco) Detener(); }
        private void OnApplicationPause(bool pausa) { if (pausa) Detener(); }
        private void OnDisable() => Detener();
    }

    /// <summary>Temblor de cámara + vibración del control juntos. fuerza ≈ 1 golpe normal, 4 impacto enorme.</summary>
    public static class Sacudida
    {
        public static void Hacer(float fuerza, float segundos)
        {
            if (Camera.main && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.Temblor(fuerza, segundos);
            float v = Mathf.Clamp01(fuerza / 4f);
            Vibracion.Pulso(v, v * 0.75f, segundos);
        }
    }
}
