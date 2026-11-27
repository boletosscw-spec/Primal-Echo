using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    public enum Pista { Ninguna, Menu, Exploracion, Combate, Jefe, Nemesis, GameOver }

    // Música de todo el juego. Se crea sola al arrancar y sobrevive entre escenas.
    //  · Menú de inicio: el menú pide su tema cuando termina el video del estudio.
    //  · Niveles: exploración; cambia a combate cuando un centinela está cerca de Kael
    //    y vuelve a exploración (donde se quedó) al alejarse o vencerlos.
    //  · Centinela Rex despierto: tema del jefe. Al vencerlo: tema de NÉMESIS.
    //  · Cinemáticas: silencio (tienen narración), salvo El Juicio de Némesis.
    //  · Game Over: su propio tema mientras está la pantalla; al reintentar vuelve la música del nivel.
    public sealed class MusicaJuego : MonoBehaviour
    {
        /// <summary>
        /// La música solo cambia con un enemigo grande (el Centinela Rex → tema de jefe).
        /// Con los centinelas normales sigue sonando la exploración. Pon true si algún día
        /// quieres que el tema de combate vuelva a sonar cerca de cualquier enemigo.
        /// </summary>
        public static bool CombateConEnemigosNormales = false;
        private const float DistanciaCombate = 10f;
        private const float DistanciaCalma = 16f;
        private const float EsperaCalma = 5f;

        private static MusicaJuego instancia;
        private PistasMusica pistas;
        private readonly Dictionary<Pista, AudioSource> fuentes = new Dictionary<Pista, AudioSource>();
        private readonly Dictionary<Pista, float> volumenPista = new Dictionary<Pista, float>();
        private Pista actual = Pista.Ninguna;
        private float velocidadFundido = 1f, volumenEscena = 1f;
        private bool esNivel, combatePermitido, rexVencido;
        private float proximaRevision, ultimoEnemigoCerca = -99f;
        private Transform kael;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            if (instancia != null) return;
            var go = new GameObject("Musica del juego");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<MusicaJuego>();
        }

        /// <summary>Cambia de tema con un fundido de los segundos indicados.</summary>
        public static void Reproducir(Pista pista, float fundido = 1.5f)
        {
            if (instancia != null) instancia.Cambiar(pista, fundido);
        }

        public static void Detener(float fundido = 1.5f) => Reproducir(Pista.Ninguna, fundido);

        private bool enGameOver;

        /// <summary>Kael cayó: suena el tema de Game Over (la revisión de jefes/exploración se pausa).</summary>
        public static void GameOver()
        {
            if (instancia == null) return;
            instancia.enGameOver = true;
            var f = instancia.fuentes[Pista.GameOver];
            if (f.clip != null) f.time = 0f;
            instancia.Cambiar(Pista.GameOver, 0.6f);
        }

        /// <summary>Reintentar: vuelve la música del nivel (la exploración sigue donde iba; si hay jefe, su tema).</summary>
        public static void FinGameOver()
        {
            if (instancia == null) return;
            instancia.enGameOver = false;
            instancia.proximaRevision = 0f;
            instancia.Cambiar(Pista.Exploracion, 1.5f);
        }

        private void Awake()
        {
            pistas = Resources.Load<PistasMusica>("PistasMusica");
            if (pistas == null) Debug.LogWarning("Música: falta Assets/Resources/PistasMusica.asset");
            Agregar(Pista.Menu, pistas != null ? pistas.menu : null);
            Agregar(Pista.Exploracion, pistas != null ? pistas.exploracion : null);
            Agregar(Pista.Combate, pistas != null ? pistas.combate : null);
            Agregar(Pista.Jefe, pistas != null ? pistas.jefe : null);
            Agregar(Pista.Nemesis, pistas != null ? pistas.nemesis : null);
            Agregar(Pista.GameOver, pistas != null ? pistas.gameOver : null);
            SceneManager.sceneLoaded += AlCargarEscena;
            EnemyHealth.Died += AlMorirEnemigo;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            EnemyHealth.Died -= AlMorirEnemigo;
        }

        private void Agregar(Pista p, AudioClip clip)
        {
            var f = gameObject.AddComponent<AudioSource>();
            f.clip = clip;
            f.loop = true;
            f.playOnAwake = false;
            f.volume = 0f;
            f.spatialBlend = 0f;
            fuentes[p] = f;
            volumenPista[p] = 0f;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            enGameOver = false;
            string n = escena.name;
            kael = null;
            rexVencido = false;
            ultimoEnemigoCerca = -99f;
            esNivel = n.StartsWith("Nivel_");
            combatePermitido = esNivel && !n.Contains("Tutorial"); // en el tutorial los enemigos no se mueven
            volumenEscena = 1f;

            if (esNivel)
            {
                // La exploración empieza desde el inicio en cada nivel nuevo
                var exp = fuentes[Pista.Exploracion];
                if (exp.clip != null) exp.time = 0f;
                Cambiar(Pista.Exploracion, 2f);
            }
            else if (n == "Menu_Inicio")
            {
                Cambiar(Pista.Ninguna, 0.5f); // el menú pide su tema al terminar el video
            }
            else if (n.Contains("Nemesis"))
            {
                volumenEscena = 0.45f; // por debajo de la voz de NÉMESIS
                Cambiar(Pista.Nemesis, 2f);
            }
            else
            {
                Cambiar(Pista.Ninguna, 1f); // cinemáticas con narración
            }
        }

        private void AlMorirEnemigo(EnemyHealth e)
        {
            if (e != null && e.GetComponent<CentinelaRex>() != null)
            {
                rexVencido = true;
                Cambiar(Pista.Nemesis, 3f);
            }
        }

        private void Cambiar(Pista pista, float fundido)
        {
            if (pista == actual) return;
            AudioSource nueva = pista != Pista.Ninguna ? fuentes[pista] : null;
            if (nueva != null && nueva.clip == null) nueva = null;
            if (nueva != null)
            {
                // Combate, jefe y NÉMESIS arrancan desde el principio; exploración y menú siguen donde iban
                bool reinicia = pista == Pista.Combate || pista == Pista.Jefe || pista == Pista.Nemesis || pista == Pista.GameOver;
                if (volumenPista[pista] < 0.05f && reinicia) nueva.time = 0f;
                if (!nueva.isPlaying) { nueva.Play(); }
            }
            actual = pista;
            velocidadFundido = 1f / Mathf.Max(0.05f, fundido);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float maestro = (pistas != null ? pistas.volumen : 0.6f) * volumenEscena;
            foreach (var par in fuentes)
            {
                float objetivo = par.Key == actual ? 1f : 0f;
                float v = Mathf.MoveTowards(volumenPista[par.Key], objetivo, dt * velocidadFundido);
                volumenPista[par.Key] = v;
                AudioSource f = par.Value;
                f.volume = v * v * maestro; // curva suave
                if (v <= 0f && f.isPlaying) f.Pause();
            }

            if (esNivel && !rexVencido && !enGameOver && Time.unscaledTime >= proximaRevision)
            {
                proximaRevision = Time.unscaledTime + 0.4f;
                RevisarPeligro();
            }
        }

        private void RevisarPeligro()
        {
            if (kael == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                kael = pc.transform;
            }
            Vector2 pos = kael.position;

            // ¿Algún jefe nuevo (Atlas, Aeris...) está peleando?
            if (JefeBase.AlgunoPeleando)
            {
                Cambiar(Pista.Jefe, 1f);
                return;
            }

            // ¿El Rex está peleando?
            foreach (var rex in FindObjectsByType<CentinelaRex>(FindObjectsSortMode.None))
            {
                if (rex.Engaged)
                {
                    Cambiar(Pista.Jefe, 1f);
                    return;
                }
            }

            if (!combatePermitido || !CombateConEnemigosNormales)
            {
                Cambiar(Pista.Exploracion, 2f);
                return;
            }

            float masCerca = float.MaxValue;
            foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (e.Remaining <= 0 || e.GetComponent<CentinelaRex>() != null || e.GetComponent<JefeBase>() != null) continue;
                float d = Vector2.Distance(pos, e.transform.position);
                if (d < masCerca) masCerca = d;
            }

            bool enCombate = actual == Pista.Combate;
            if (masCerca < (enCombate ? DistanciaCalma : DistanciaCombate)) ultimoEnemigoCerca = Time.unscaledTime;

            if (masCerca < DistanciaCombate) Cambiar(Pista.Combate, 0.8f);
            else if (Time.unscaledTime - ultimoEnemigoCerca > EsperaCalma) Cambiar(Pista.Exploracion, 2.5f);
        }
    }
}
