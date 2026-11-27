using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace PrimalEcho
{
    /// <summary>
    /// Cuenta del jugador con Firebase Authentication (correo y contraseña) usando la API REST,
    /// porque el SDK oficial de Firebase para Unity no está pensado para builds de PC.
    /// Es la misma cuenta que usan la PWA y la app de Android.
    /// La sesión se recuerda entre partidas (refresh token en PlayerPrefs).
    /// </summary>
    public sealed class CuentaJugador : MonoBehaviour
    {
        private const string UrlAuth = "https://identitytoolkit.googleapis.com/v1/accounts:";
        private const string UrlToken = "https://securetoken.googleapis.com/v1/token?key=";
        private const string PrefSesion = "PrimalEcho_Sesion_v1";

        [Serializable]
        private sealed class Sesion
        {
            public string uid = "", correo = "", nombre = "", refreshToken = "";
        }

        private static CuentaJugador instancia;
        private Sesion sesion;
        private string idToken = "";
        private float tokenVence;

        /// <summary>El jugador eligió "Jugar sin cuenta": no se guarda nada en la nube.</summary>
        public static bool ModoInvitado { get; set; }

        public static CuentaJugador Instancia
        {
            get
            {
                if (instancia != null) return instancia;
                var go = new GameObject("Cuenta Jugador");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<CuentaJugador>();
                return instancia;
            }
        }

        public static bool Configurado => FirebaseConfig.Actual.Configurado;
        public bool HaySesion => sesion != null && !string.IsNullOrEmpty(sesion.uid);
        public string Uid => HaySesion ? sesion.uid : "";
        public string Correo => HaySesion ? sesion.correo : "";
        public string Nombre => !HaySesion ? "" : string.IsNullOrEmpty(sesion.nombre) ? sesion.correo : sesion.nombre;

        /// <summary>Se dispara al entrar o salir de la cuenta.</summary>
        public static event Action CambioSesion;

        private void Awake()
        {
            if (instancia != null && instancia != this) { Destroy(gameObject); return; }
            instancia = this;
            DontDestroyOnLoad(gameObject);
            try { sesion = JsonUtility.FromJson<Sesion>(PlayerPrefs.GetString(PrefSesion, "{}")); }
            catch (ArgumentException) { sesion = null; }
            if (sesion != null && string.IsNullOrEmpty(sesion.refreshToken)) sesion = null;
        }

        // ------------------------------------------------------------------ acciones

        public void IniciarSesion(string correo, string contrasena, Action<bool, string> listo)
        {
            string cuerpo = "{\"email\":" + JsonSimple.Escapar(correo.Trim()) +
                            ",\"password\":" + JsonSimple.Escapar(contrasena) +
                            ",\"returnSecureToken\":true}";
            StartCoroutine(PedirAuth("signInWithPassword", cuerpo, (ok, datos, error) =>
            {
                if (!ok) { listo(false, error); return; }
                Guardar(datos);
                listo(true, "Bienvenido, " + Nombre);
            }));
        }

        public void CrearCuenta(string nombre, string correo, string contrasena, Action<bool, string> listo)
        {
            string cuerpo = "{\"email\":" + JsonSimple.Escapar(correo.Trim()) +
                            ",\"password\":" + JsonSimple.Escapar(contrasena) +
                            ",\"returnSecureToken\":true}";
            StartCoroutine(PedirAuth("signUp", cuerpo, (ok, datos, error) =>
            {
                if (!ok) { listo(false, error); return; }
                Guardar(datos);
                sesion.nombre = nombre.Trim();
                GuardarSesion();

                // El nombre va en el perfil de Firebase y en el documento del jugador en Firestore.
                string perfil = "{\"idToken\":" + JsonSimple.Escapar(idToken) +
                                ",\"displayName\":" + JsonSimple.Escapar(sesion.nombre) +
                                ",\"returnSecureToken\":false}";
                StartCoroutine(PedirAuth("update", perfil, (ok2, _, __) => { }));
                GuardadoNube.CrearPerfil(sesion.nombre, sesion.correo, (ok3, ___) => { });
                listo(true, "Cuenta creada. Bienvenido, " + Nombre);
            }));
        }

        public void RecuperarContrasena(string correo, Action<bool, string> listo)
        {
            string cuerpo = "{\"requestType\":\"PASSWORD_RESET\",\"email\":" + JsonSimple.Escapar(correo.Trim()) + "}";
            StartCoroutine(PedirAuth("sendOobCode", cuerpo, (ok, _, error) =>
                listo(ok, ok ? "Te enviamos un correo para cambiar tu contraseña" : error)));
        }

        public void CerrarSesion()
        {
            sesion = null;
            idToken = "";
            tokenVence = 0f;
            PlayerPrefs.DeleteKey(PrefSesion);
            PlayerPrefs.Save();
            CambioSesion?.Invoke();
        }

        /// <summary>Entrega un token válido (lo renueva si ya venció). Null si no hay sesión o no hay red.</summary>
        public void ObtenerToken(Action<string> listo)
        {
            if (!HaySesion || !Configurado) { listo(null); return; }
            if (!string.IsNullOrEmpty(idToken) && Time.realtimeSinceStartup < tokenVence) { listo(idToken); return; }
            StartCoroutine(Renovar(listo));
        }

        // ------------------------------------------------------------------ red

        private IEnumerator PedirAuth(string accion, string cuerpo, Action<bool, Dictionary<string, object>, string> listo)
        {
            if (!Configurado) { listo(false, null, "Firebase todavía no está configurado"); yield break; }
            using (var req = new UnityWebRequest(UrlAuth + accion + "?key=" + FirebaseConfig.Actual.apiKey, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(cuerpo));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 15;
                yield return req.SendWebRequest();

                var datos = JsonSimple.Leer(req.downloadHandler.text) as Dictionary<string, object>;
                if (req.result == UnityWebRequest.Result.Success) { listo(true, datos, null); yield break; }
                if (req.result == UnityWebRequest.Result.ConnectionError) { listo(false, datos, "Sin conexión a internet"); yield break; }
                listo(false, datos, Traducir(datos));
            }
        }

        private IEnumerator Renovar(Action<string> listo)
        {
            var form = new WWWForm();
            form.AddField("grant_type", "refresh_token");
            form.AddField("refresh_token", sesion.refreshToken);
            using (var req = UnityWebRequest.Post(UrlToken + FirebaseConfig.Actual.apiKey, form))
            {
                req.timeout = 15;
                yield return req.SendWebRequest();
                var datos = JsonSimple.Leer(req.downloadHandler.text) as Dictionary<string, object>;
                if (req.result != UnityWebRequest.Result.Success || datos == null)
                {
                    // 400 = la sesión ya no es válida (contraseña cambiada o cuenta borrada).
                    if (req.responseCode == 400) CerrarSesion();
                    listo(null);
                    yield break;
                }
                idToken = Texto(datos, "id_token");
                sesion.refreshToken = Texto(datos, "refresh_token", sesion.refreshToken);
                tokenVence = Time.realtimeSinceStartup + Segundos(Texto(datos, "expires_in")) - 60f;
                GuardarSesion();
                listo(idToken);
            }
        }

        private void Guardar(Dictionary<string, object> datos)
        {
            sesion = new Sesion
            {
                uid = Texto(datos, "localId"),
                correo = Texto(datos, "email"),
                nombre = Texto(datos, "displayName"),
                refreshToken = Texto(datos, "refreshToken"),
            };
            idToken = Texto(datos, "idToken");
            tokenVence = Time.realtimeSinceStartup + Segundos(Texto(datos, "expiresIn")) - 60f;
            ModoInvitado = false;
            GuardarSesion();
            CambioSesion?.Invoke();
        }

        private void GuardarSesion()
        {
            PlayerPrefs.SetString(PrefSesion, JsonUtility.ToJson(sesion));
            PlayerPrefs.Save();
        }

        private static string Texto(Dictionary<string, object> d, string clave, string porDefecto = "")
        {
            return d != null && d.TryGetValue(clave, out object v) && v != null ? v.ToString() : porDefecto;
        }

        private static float Segundos(string s) => float.TryParse(s, out float v) ? v : 3600f;

        private static string Traducir(Dictionary<string, object> datos)
        {
            string codigo = "";
            if (datos != null && datos.TryGetValue("error", out object e) && e is Dictionary<string, object> err)
                codigo = Texto(err, "message");
            if (codigo.StartsWith("WEAK_PASSWORD")) return "La contraseña debe tener al menos 6 caracteres";
            if (codigo.StartsWith("TOO_MANY_ATTEMPTS")) return "Demasiados intentos. Espera un momento";
            switch (codigo)
            {
                case "EMAIL_EXISTS": return "Ese correo ya tiene una cuenta";
                case "INVALID_EMAIL": return "El correo no es válido";
                case "EMAIL_NOT_FOUND":
                case "INVALID_PASSWORD":
                case "INVALID_LOGIN_CREDENTIALS": return "Correo o contraseña incorrectos";
                case "USER_DISABLED": return "Esta cuenta está desactivada";
                case "MISSING_PASSWORD": return "Escribe tu contraseña";
                case "OPERATION_NOT_ALLOWED": return "Activa el inicio con correo en la consola de Firebase";
                default: return string.IsNullOrEmpty(codigo) ? "No se pudo conectar con el servidor" : "Error: " + codigo;
            }
        }
    }
}
