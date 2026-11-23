using System.Collections.Generic;
using UnityEngine;

namespace PrimalEcho
{
    // Pensamientos de Kael y pistas: al cruzar la zona por primera vez aparece un texto
    // arriba al centro de la pantalla. Si llegan varios seguidos, se muestran en orden.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class NarracionZona : MonoBehaviour
    {
        [SerializeField, TextArea] private string texto = "";
        [SerializeField, Min(1f)] private float duracion = 4.5f;
        private bool usada;

        private static readonly Queue<(string texto, float dur)> cola = new Queue<(string, float)>();
        private static NarracionZona pantalla;     // la zona que dibuja (la primera que se activó)
        private static HoloText actual;
        private static float hasta, inicio;

        public void Configure(string value, float seconds) { texto = value; duracion = seconds; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() { cola.Clear(); pantalla = null; actual = null; hasta = 0f; }

        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (usada || other.GetComponentInParent<PlayerController>() == null) return;
            usada = true;
            cola.Enqueue((texto, duracion));
            if (pantalla == null) pantalla = this;
        }

        private void OnDestroy() { if (pantalla == this) pantalla = null; }

        private void Update()
        {
            if (pantalla == null) pantalla = this;
            if (pantalla != this) return;
            if (Time.time >= hasta && cola.Count > 0)
            {
                var (t, d) = cola.Dequeue();
                actual = new HoloText(t, 240);
                inicio = Time.time; hasta = Time.time + d;
            }
            if (Time.time >= hasta + 0.4f) actual = null;
        }

        private void OnGUI()
        {
            if (pantalla != this || actual == null || Event.current.type != EventType.Repaint || Time.timeScale <= 0f) return;
            float a = Mathf.Clamp01((Time.time - inicio) / 0.35f) * Mathf.Clamp01((hasta + 0.4f - Time.time) / 0.4f);
            int ui = HoloText.Scale;
            float w = actual.Width(ui);
            actual.Draw((Screen.width - w) / 2f, Screen.height * 0.13f, ui, a, HoloText.Green);
        }
    }
}
