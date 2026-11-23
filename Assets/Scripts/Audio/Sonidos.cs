using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // EFECTOS DE SONIDO Y VOCES de Primal Echo. Se crea solo al arrancar el juego.
    //
    //  · Sonidos.Play("kael_salto") busca Assets/Resources/Sonidos/kael_salto (.wav/.ogg/.mp3).
    //    Si hay variantes (kael_salto_1, kael_salto_2...) elige una al azar para que no suene repetido.
    //  · Si el archivo no existe, usa un sonido sintetizado por código (Sintesis.cs), así el juego
    //    ya suena desde hoy y cada archivo que agregues lo reemplaza automáticamente.
    //  · Variación ligera de tono, paneo según dónde ocurre en la pantalla, eco de cueva dentro de
    //    las cuevas y tono más grave en cámara lenta.
    //  · Voces sin palabras: cada línea de diálogo suena con la "voz" del personaje
    //    (voz_orin, voz_kael, voz_kiro, voz_nemesis); sin archivo, se generan sílabas sintetizadas.
    //  · Volumen en Opciones → "Efectos" (PlayerPrefs "pe_vol_efectos").
    public sealed class Sonidos : MonoBehaviour
    {
        private const string Carpeta = "Sonidos/";
        private const string PrefVolumen = "pe_vol_efectos";
        private const int Canales = 24;

        private static Sonidos instancia;
        private readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();
        private readonly Dictionary<string, float> ultimaVez = new Dictionary<string, float>();
        private AudioSource[] canales;
        private AudioReverbFilter[] ecos;
        private AudioSource ambiente;
        private string ambienteActual;
        private int siguiente;
        private Transform kael;
        private bool enCueva;
        private float proximaGota;
        private Coroutine vozActual;
        private int hayAmbienteCueva = -1; // -1 sin revisar, 0 no, 1 sí

        public static float Volumen
        {
            get => PlayerPrefs.GetFloat(PrefVolumen, 1f);
            set { PlayerPrefs.SetFloat(PrefVolumen, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            if (instancia != null) return;
            var go = new GameObject("Sonidos");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<Sonidos>();
        }

        private void Awake()
        {
            canales = new AudioSource[Canales];
            ecos = new AudioReverbFilter[Canales];
            for (int i = 0; i < Canales; i++)
            {
                var c = new GameObject("Canal_" + i);
                c.transform.SetParent(transform, false);
                var src = c.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 0f;
                canales[i] = src;
                var eco = c.AddComponent<AudioReverbFilter>();
                eco.reverbPreset = AudioReverbPreset.Cave;
                eco.enabled = false;
                ecos[i] = eco;
            }
            ambiente = gameObject.AddComponent<AudioSource>();
            ambiente.loop = true; ambiente.playOnAwake = false; ambiente.volume = 0f;
            SceneManager.sceneLoaded += (_, __) => { kael = null; ambienteActual = null; ambiente.Stop(); };
        }

        // ---------------- API ----------------

        /// <summary>Reproduce un efecto. 'en' = posición en el mundo (para el paneo y la distancia).</summary>
        public static void Play(string nombre, float volumen = 1f, float tono = 1f, Vector3? en = null, float variacion = 0.06f)
        {
            if (instancia == null || string.IsNullOrEmpty(nombre)) return;
            instancia.Reproducir(nombre, volumen, tono, en, variacion);
        }

        /// <summary>"Voz" de un personaje para una línea de diálogo: orin, kael, kiro, nemesis.</summary>
        public static void Voz(string personaje, string texto)
        {
            if (instancia == null) return;
            if (instancia.vozActual != null) instancia.StopCoroutine(instancia.vozActual);
            instancia.vozActual = instancia.StartCoroutine(instancia.Hablar(personaje, texto ?? ""));
        }

        // ---------------- Reproducción ----------------

        private void Reproducir(string nombre, float volumen, float tono, Vector3? en, float variacion)
        {
            // Evita que el mismo sonido se apile en el mismo instante (p. ej. 3 rocas a la vez)
            if (ultimaVez.TryGetValue(nombre, out float t) && Time.unscaledTime - t < 0.035f) return;
            ultimaVez[nombre] = Time.unscaledTime;

            var clips = Clips(nombre);
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];

            float pan = 0f, distancia = 1f;
            var cam = Camera.main;
            if (en.HasValue && cam != null)
            {
                float dx = en.Value.x - cam.transform.position.x;
                float dy = en.Value.y - cam.transform.position.y;
                pan = Mathf.Clamp(dx / 14f, -1f, 1f) * 0.6f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                distancia = d < 14f ? 1f : Mathf.Clamp01(1f - (d - 14f) / 16f); // se apaga fuera de pantalla
                if (distancia <= 0.01f) return;
            }

            var src = canales[siguiente];
            ecos[siguiente].enabled = enCueva;
            siguiente = (siguiente + 1) % Canales;
            src.Stop();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volumen) * distancia * Volumen;
            src.pitch = tono * Random.Range(1f - variacion, 1f + variacion) * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(Time.timeScale));
            src.panStereo = pan;
            src.Play();
        }

        private AudioClip[] Clips(string nombre)
        {
            if (cache.TryGetValue(nombre, out var lista)) return lista;
            var encontrados = new List<AudioClip>();
            var base0 = Resources.Load<AudioClip>(Carpeta + nombre);
            if (base0 != null) encontrados.Add(base0);
            for (int i = 1; i <= 8; i++)
            {
                var v = Resources.Load<AudioClip>(Carpeta + nombre + "_" + i);
                if (v != null) encontrados.Add(v);
            }
            if (encontrados.Count == 0)
            {
                var sint = Sintesis.Crear(nombre);
                if (sint != null) encontrados.Add(sint);
            }
            lista = encontrados.ToArray();
            cache[nombre] = lista;
            return lista;
        }

        // ---------------- Voces ----------------

        private IEnumerator Hablar(string personaje, string texto)
        {
            string archivo = "voz_" + personaje;
            var propios = new List<AudioClip>();
            var b = Resources.Load<AudioClip>(Carpeta + archivo);
            if (b != null) propios.Add(b);
            for (int i = 1; i <= 8; i++) { var v = Resources.Load<AudioClip>(Carpeta + archivo + "_" + i); if (v != null) propios.Add(v); }

            if (propios.Count > 0)
            {
                // Con archivos: una frase corta por línea
                Reproducir(archivo, 0.8f, 1f, null, 0.04f);
                yield break;
            }

            // Sin archivos: sílabas sintetizadas con el tono de cada personaje
            int palabras = Mathf.Clamp(texto.Split(' ').Length, 2, 9);
            float pausa = personaje == "orin" ? 0.13f : personaje == "kiro" ? 0.07f : 0.1f;
            int silabas = personaje == "kiro" ? Mathf.Min(palabras, 5) : palabras;
            for (int i = 0; i < silabas; i++)
            {
                string blip = "voz_" + personaje + "_silaba_" + Random.Range(0, 4);
                Reproducir(blip, personaje == "kiro" ? 0.35f : 0.45f, Random.Range(0.93f, 1.08f), null, 0.03f);
                ultimaVez.Remove(blip);
                yield return new WaitForSecondsRealtime(pausa * Random.Range(0.8f, 1.25f));
            }
        }

        // ---------------- Ambiente ----------------

        private void Update()
        {
            if (kael == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) kael = p.transform;
            }
            enCueva = kael != null && CaveAmbience.IsInside((Vector2)kael.position + Vector2.up * 0.5f);

            // Ambiente de fondo: archivo ambiente_cueva / ambiente_bosque si existen
            string quiero = kael == null ? null : enCueva ? "ambiente_cueva" : SceneManager.GetActiveScene().name.Contains("Nivel_01") ? "ambiente_bosque" : null;
            if (quiero != ambienteActual)
            {
                var clip = quiero != null ? Resources.Load<AudioClip>(Carpeta + quiero) : null;
                if (clip != null || quiero == null)
                {
                    ambienteActual = quiero;
                    if (clip != null) { ambiente.clip = clip; ambiente.Play(); }
                }
                else ambienteActual = quiero; // no hay archivo: solo las gotas sintetizadas
            }
            float objetivo = ambiente.clip != null && ambienteActual != null && ambiente.isPlaying ? 0.5f * Volumen : 0f;
            ambiente.volume = Mathf.MoveTowards(ambiente.volume, objetivo, Time.unscaledDeltaTime * 0.6f);

            // Gotas en la cueva (si no hay archivo de ambiente de cueva)
            if (hayAmbienteCueva < 0) hayAmbienteCueva = Resources.Load<AudioClip>(Carpeta + "ambiente_cueva") != null ? 1 : 0;
            if (enCueva && hayAmbienteCueva == 0 && Time.time > proximaGota && Time.timeScale > 0f)
            {
                proximaGota = Time.time + Random.Range(0.6f, 2.4f);
                var cam = Camera.main;
                Vector3 donde = cam != null ? cam.transform.position + new Vector3(Random.Range(-10f, 10f), 0f, 0f) : Vector3.zero;
                Play("gota", Random.Range(0.12f, 0.28f), Random.Range(0.8f, 1.35f), donde, 0f);
            }
        }
    }
}
