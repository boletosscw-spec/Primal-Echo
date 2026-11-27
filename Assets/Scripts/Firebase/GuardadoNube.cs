using System;
using System.Collections.Generic;

namespace PrimalEcho
{
    /// <summary>
    /// Progreso e inventario del jugador tal como se guardan en Firestore, en jugadores/{uid}.
    /// La PWA y la app de Android leen este mismo documento.
    /// </summary>
    [Serializable]
    public sealed class DatosPartida
    {
        public string nivelActual = "";            // nombre de la escena
        public string checkpoint = "";             // último checkpoint encendido
        public List<string> jefesDerrotados = new List<string>(); // "Atlas", "Aeris", "CentinelaRex"
        public bool llaveBase;
        public int fragmentos, frutas, nucleos, corazonesMax;
        public List<string> mejoras = new List<string>();         // ids de BraceletSettings
        public List<string> chips = new List<string>();           // ids de los chips de memoria
    }

    /// <summary>
    /// Conexión del juego con Firestore. Todavía no se llama desde el juego:
    /// queda lista para enlazarla a los checkpoints, jefes y mejoras.
    /// </summary>
    public static class GuardadoNube
    {
        private static string Ruta => "jugadores/" + CuentaJugador.Instancia.Uid;

        /// <summary>True si hay cuenta iniciada y Firebase configurado (si es invitado, no se sube nada).</summary>
        public static bool Disponible =>
            CuentaJugador.Configurado && CuentaJugador.Instancia.HaySesion && !CuentaJugador.ModoInvitado;

        /// <summary>Sube el progreso y el inventario.</summary>
        public static void Subir(DatosPartida p, Action<bool, string> listo = null)
        {
            if (p == null) { listo?.Invoke(false, "Sin datos"); return; }
            var campos = new Dictionary<string, object>
            {
                ["nivelActual"] = p.nivelActual ?? "",
                ["checkpoint"] = p.checkpoint ?? "",
                ["jefesDerrotados"] = p.jefesDerrotados ?? new List<string>(),
                ["llaveBase"] = p.llaveBase,
                ["fragmentos"] = p.fragmentos,
                ["frutas"] = p.frutas,
                ["nucleos"] = p.nucleos,
                ["corazonesMax"] = p.corazonesMax,
                ["mejoras"] = p.mejoras ?? new List<string>(),
                ["chips"] = p.chips ?? new List<string>(),
                ["actualizado"] = DateTime.UtcNow,
            };
            Escribir(campos, listo);
        }

        /// <summary>Descarga el progreso guardado. Entrega null si el jugador todavía no tiene partida en la nube.</summary>
        public static void Descargar(Action<bool, DatosPartida, string> listo)
        {
            if (!Disponible) { listo(false, null, "No hay sesión iniciada"); return; }
            var cuenta = CuentaJugador.Instancia;
            cuenta.ObtenerToken(token =>
            {
                if (token == null) { listo(false, null, "No se pudo validar la sesión"); return; }
                cuenta.StartCoroutine(Firestore.Leer(Ruta, token, (ok, d, error) =>
                {
                    if (!ok) { listo(false, null, error); return; }
                    if (d == null || !d.ContainsKey("nivelActual")) { listo(true, null, null); return; }
                    listo(true, new DatosPartida
                    {
                        nivelActual = Texto(d, "nivelActual"),
                        checkpoint = Texto(d, "checkpoint"),
                        jefesDerrotados = Lista(d, "jefesDerrotados"),
                        llaveBase = d.TryGetValue("llaveBase", out object b) && b is bool bb && bb,
                        fragmentos = Entero(d, "fragmentos"),
                        frutas = Entero(d, "frutas"),
                        nucleos = Entero(d, "nucleos"),
                        corazonesMax = Entero(d, "corazonesMax"),
                        mejoras = Lista(d, "mejoras"),
                        chips = Lista(d, "chips"),
                    }, null);
                }));
            });
        }

        /// <summary>Crea el documento del jugador con su nombre y correo (al registrarse).</summary>
        public static void CrearPerfil(string nombre, string correo, Action<bool, string> listo)
        {
            Escribir(new Dictionary<string, object>
            {
                ["nombre"] = nombre ?? "",
                ["correo"] = correo ?? "",
                ["creado"] = DateTime.UtcNow,
            }, listo, true);
        }

        private static void Escribir(Dictionary<string, object> campos, Action<bool, string> listo, bool aunqueInvitado = false)
        {
            if (!(aunqueInvitado ? CuentaJugador.Configurado && CuentaJugador.Instancia.HaySesion : Disponible))
            {
                listo?.Invoke(false, "No hay sesión iniciada");
                return;
            }
            var cuenta = CuentaJugador.Instancia;
            cuenta.ObtenerToken(token =>
            {
                if (token == null) { listo?.Invoke(false, "No se pudo validar la sesión"); return; }
                cuenta.StartCoroutine(Firestore.Escribir(Ruta, token, campos, (ok, error) => listo?.Invoke(ok, error)));
            });
        }

        private static string Texto(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out object v) && v != null ? v.ToString() : "";

        private static int Entero(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out object v) && v != null ? (int)Convert.ToInt64(v) : 0;

        private static List<string> Lista(Dictionary<string, object> d, string k)
        {
            var r = new List<string>();
            if (d.TryGetValue(k, out object v) && v is List<object> l)
                foreach (object x in l) if (x != null) r.Add(x.ToString());
            return r;
        }
    }
}
