using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    /// <summary>
    /// Sincroniza el progreso local (PlayerPrefs y componentes en escena) con Cloud Firestore
    /// a través de GuardadoNube. Agrupa guardados seguidos en una ventana de 3 segundos,
    /// funciona sin conexión o en modo invitado usando solo PlayerPrefs y reintenta subidas pendientes.
    /// </summary>
    public sealed class SincronizacionPartida : MonoBehaviour
    {
        private const string PrefCheckpoint = "PrimalEcho_Checkpoint_v1";
        private const string PrefNivelActual = "PrimalEcho_NivelActual_v1";
        private const string PrefJefes = "PrimalEcho_JefesDerrotados_v1";
        private const string PrefCorazonesMax = "PrimalEcho_CorazonesMax_v1";
        private const string PrefPendiente = "PrimalEcho_GuardadoPendiente_v1";
        private const float VentanaAgrupacion = 3f;
        private const int CorazonesBase = 5;

        [Serializable]
        private sealed class ListaJefes
        {
            public List<string> jefes = new List<string>();
        }

        private static SincronizacionPartida instancia;
        private static bool cerrando;

        private readonly List<string> jefesDerrotados = new List<string>();
        private string ultimoCheckpoint = "";
        private string ultimoNivel = "";
        private string nivelPendienteOverride;
        private bool subidaProgramada;
        private bool subiendo;
        private bool descargando;
        private float subirEn;

        public static SincronizacionPartida Instancia
        {
            get
            {
                if (instancia != null) return instancia;
                if (cerrando) return null;
                var go = new GameObject("Sincronizacion Partida");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<SincronizacionPartida>();
                return instancia;
            }
        }

        /// <summary>Último checkpoint encendido guardado localmente.</summary>
        public static string UltimoCheckpoint => Instancia != null ? Instancia.ultimoCheckpoint : PlayerPrefs.GetString(PrefCheckpoint, "");

        /// <summary>True si la última subida a Firestore falló y está pendiente de reintento.</summary>
        public static bool HayGuardadoPendiente => PlayerPrefs.GetInt(PrefPendiente, 0) == 1;

        /// <summary>Copia de la lista de jefes derrotados ("Atlas", "Aeris", "CentinelaRex").</summary>
        public static List<string> ObtenerJefesDerrotados() =>
            Instancia != null ? new List<string>(Instancia.jefesDerrotados) : LeerJefesPrefs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstaticos()
        {
            cerrando = false;
            instancia = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            _ = Instancia;
        }

        private void Awake()
        {
            if (instancia != null && instancia != this) { Destroy(gameObject); return; }
            instancia = this;
            DontDestroyOnLoad(gameObject);
            CargarEstadoLocal();
            CuentaJugador.CambioSesion -= AlCambiarSesion;
            CuentaJugador.CambioSesion += AlCambiarSesion;
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDestroy()
        {
            if (instancia == this)
            {
                CuentaJugador.CambioSesion -= AlCambiarSesion;
                SceneManager.sceneLoaded -= AlCargarEscena;
                instancia = null;
            }
        }

        private void OnApplicationQuit()
        {
            cerrando = true;
            if (subidaProgramada && GuardadoNube.Disponible)
                MarcarPendiente(true);
        }

        private void Update()
        {
            if (!subidaProgramada || subiendo) return;
            if (Time.unscaledTime < subirEn) return;

            subidaProgramada = false;
            string overrideEscena = nivelPendienteOverride;
            nivelPendienteOverride = null;
            EjecutarSubida(overrideEscena);
        }

        // ------------------------------------------------------------------ eventos

        private void AlCambiarSesion()
        {
            if (GuardadoNube.Disponible)
            {
                SincronizarConNube();
            }
            else
            {
                subidaProgramada = false;
                nivelPendienteOverride = null;
            }
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            if (EsEscenaJugable(escena.name))
            {
                ultimoNivel = escena.name;
                PlayerPrefs.SetString(PrefNivelActual, ultimoNivel);
                PlayerPrefs.Save();
            }
        }

        // ------------------------------------------------------------------ API pública

        /// <summary>Guarda el último checkpoint en PlayerPrefs y programa una subida a la nube.</summary>
        public static void RegistrarCheckpoint(string label)
        {
            var inst = Instancia;
            if (inst == null) return;
            if (!string.IsNullOrEmpty(label))
            {
                inst.ultimoCheckpoint = label;
                PlayerPrefs.SetString(PrefCheckpoint, label);
            }
            inst.ActualizarMetadatosEscena();
            PlayerPrefs.Save();
            Guardar();
        }

        /// <summary>Registra a un jefe derrotado ("Atlas", "Aeris", "CentinelaRex") en PlayerPrefs y guarda en la nube.</summary>
        public static void RegistrarJefeDerrotado(string nombreJefe)
        {
            if (string.IsNullOrWhiteSpace(nombreJefe)) return;
            var inst = Instancia;
            if (inst == null) return;
            string limpio = nombreJefe.Trim();
            if (!inst.jefesDerrotados.Contains(limpio))
            {
                inst.jefesDerrotados.Add(limpio);
                GuardarJefesPrefs(inst.jefesDerrotados);
            }
            inst.ActualizarMetadatosEscena();
            PlayerPrefs.Save();
            Guardar();
        }

        /// <summary>
        /// Solicita guardar la partida en Firestore. Si se piden varias llamadas en menos de 3 segundos,
        /// se agrupan y se sube una sola vez con el estado más reciente.
        /// </summary>
        public static void Guardar(string nivelDestino = null)
        {
            var inst = Instancia;
            if (inst == null) return;

            inst.ActualizarMetadatosEscena(nivelDestino);
            if (!string.IsNullOrEmpty(nivelDestino))
                inst.nivelPendienteOverride = nivelDestino;

            // Si no hay sesión activa, es invitado o Firebase no está configurado, funciona solo con PlayerPrefs.
            if (!GuardadoNube.Disponible) return;

            if (!inst.subidaProgramada)
            {
                inst.subidaProgramada = true;
                inst.subirEn = Time.unscaledTime + VentanaAgrupacion;
            }
        }

        /// <summary>
        /// Descarga la partida desde Firestore al iniciar sesión o al elegir "Iniciar partida" con sesión activa.
        /// Si hay datos en la nube, los aplica localmente; si no hay datos en la nube, sube los locales.
        /// </summary>
        public static void SincronizarConNube(Action<bool> alTerminar = null)
        {
            if (!GuardadoNube.Disponible)
            {
                alTerminar?.Invoke(false);
                return;
            }
            var inst = Instancia;
            if (inst == null)
            {
                alTerminar?.Invoke(false);
                return;
            }
            if (inst.descargando)
            {
                alTerminar?.Invoke(false);
                return;
            }

            inst.descargando = true;
            GuardadoNube.Descargar((ok, datos, error) =>
            {
                inst.descargando = false;
                if (!ok)
                {
                    Debug.LogWarning("SincronizacionPartida: no se pudo descargar el progreso de la nube. " + error);
                    alTerminar?.Invoke(false);
                    return;
                }

                if (datos != null)
                {
                    inst.AplicarDatosDeNube(datos);
                    alTerminar?.Invoke(true);
                }
                else
                {
                    // El jugador todavía no tiene partida en la nube: se suben los datos locales.
                    inst.subidaProgramada = false;
                    inst.EjecutarSubida(null, alTerminar);
                }
            });
        }

        /// <summary>Arma un DatosPartida con el estado actual del juego y de PlayerPrefs.</summary>
        public static DatosPartida ArmarDatos(string nivelOverride = null)
        {
            var inst = Instancia;
            if (inst != null) inst.ActualizarMetadatosEscena(nivelOverride);

            string escenaActiva = SceneManager.GetActiveScene().name;
            string nivel = !string.IsNullOrEmpty(nivelOverride)
                ? nivelOverride
                : EsEscenaJugable(escenaActiva) || inst == null || string.IsNullOrEmpty(inst.ultimoNivel)
                    ? escenaActiva
                    : inst.ultimoNivel;

            string cp = inst != null ? inst.ultimoCheckpoint : PlayerPrefs.GetString(PrefCheckpoint, "");
            var respawn = FindFirstObjectByType<KaelRespawn>();
            if (respawn != null && !string.IsNullOrEmpty(respawn.CheckpointName) && string.IsNullOrEmpty(cp))
                cp = respawn.CheckpointName;

            int corazones = PlayerPrefs.GetInt(PrefCorazonesMax, CorazonesBase);
            var health = FindFirstObjectByType<KaelHealth>();
            if (health != null && health.Max > 0)
                corazones = health.Max;

            var datos = new DatosPartida
            {
                nivelActual = nivel ?? "",
                checkpoint = cp ?? "",
                jefesDerrotados = ObtenerJefesDerrotados(),
                llaveBase = LabKey.Has,
                corazonesMax = Mathf.Max(1, corazones),
            };

            BraceletProgress.RellenarDatosPartida(datos);
            return datos;
        }

        // ------------------------------------------------------------------ internos

        private void EjecutarSubida(string nivelOverride, Action<bool> alTerminar = null)
        {
            if (!GuardadoNube.Disponible)
            {
                alTerminar?.Invoke(false);
                return;
            }

            DatosPartida datos = ArmarDatos(nivelOverride);
            subiendo = true;
            GuardadoNube.Subir(datos, (ok, error) =>
            {
                subiendo = false;
                if (!ok)
                {
                    MarcarPendiente(true);
                    Debug.LogWarning("SincronizacionPartida: falló la subida a la nube (quedó pendiente). " + error);
                    alTerminar?.Invoke(false);
                    return;
                }

                MarcarPendiente(false);
                alTerminar?.Invoke(true);
            });
        }

        private void AplicarDatosDeNube(DatosPartida datos)
        {
            if (datos == null) return;

            if (!string.IsNullOrEmpty(datos.nivelActual))
            {
                ultimoNivel = datos.nivelActual;
                PlayerPrefs.SetString(PrefNivelActual, ultimoNivel);
            }

            ultimoCheckpoint = datos.checkpoint ?? "";
            PlayerPrefs.SetString(PrefCheckpoint, ultimoCheckpoint);

            jefesDerrotados.Clear();
            if (datos.jefesDerrotados != null)
            {
                foreach (string j in datos.jefesDerrotados)
                    if (!string.IsNullOrWhiteSpace(j) && !jefesDerrotados.Contains(j))
                        jefesDerrotados.Add(j);
            }
            GuardarJefesPrefs(jefesDerrotados);

            LabKey.Has = datos.llaveBase;

            if (datos.corazonesMax > 0)
                PlayerPrefs.SetInt(PrefCorazonesMax, datos.corazonesMax);

            BraceletProgress.AplicarDesdeNube(datos);
            MarcarPendiente(false);
            PlayerPrefs.Save();
        }

        private void ActualizarMetadatosEscena(string nivelDestino = null)
        {
            string escena = !string.IsNullOrEmpty(nivelDestino) ? nivelDestino : SceneManager.GetActiveScene().name;
            if (EsEscenaJugable(escena))
            {
                ultimoNivel = escena;
                PlayerPrefs.SetString(PrefNivelActual, ultimoNivel);
            }

            var respawn = FindFirstObjectByType<KaelRespawn>();
            if (respawn != null && !string.IsNullOrEmpty(respawn.CheckpointName) && string.IsNullOrEmpty(ultimoCheckpoint))
            {
                ultimoCheckpoint = respawn.CheckpointName;
                PlayerPrefs.SetString(PrefCheckpoint, ultimoCheckpoint);
            }

            var health = FindFirstObjectByType<KaelHealth>();
            if (health != null && health.Max > 0)
                PlayerPrefs.SetInt(PrefCorazonesMax, health.Max);
        }

        private void CargarEstadoLocal()
        {
            ultimoCheckpoint = PlayerPrefs.GetString(PrefCheckpoint, "");
            ultimoNivel = PlayerPrefs.GetString(PrefNivelActual, "");
            jefesDerrotados.Clear();
            jefesDerrotados.AddRange(LeerJefesPrefs());
        }

        private static List<string> LeerJefesPrefs()
        {
            string json = PlayerPrefs.GetString(PrefJefes, "");
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try
            {
                var contenedor = JsonUtility.FromJson<ListaJefes>(json);
                return contenedor != null && contenedor.jefes != null ? contenedor.jefes : new List<string>();
            }
            catch (ArgumentException)
            {
                return new List<string>();
            }
        }

        private static void GuardarJefesPrefs(List<string> jefes)
        {
            var contenedor = new ListaJefes { jefes = jefes ?? new List<string>() };
            PlayerPrefs.SetString(PrefJefes, JsonUtility.ToJson(contenedor));
        }

        private static void MarcarPendiente(bool pendiente)
        {
            PlayerPrefs.SetInt(PrefPendiente, pendiente ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static bool EsEscenaJugable(string escena) =>
            !string.IsNullOrEmpty(escena) && escena != "Menu_Inicio" && escena != "Cinematica_Intro";
    }
}
