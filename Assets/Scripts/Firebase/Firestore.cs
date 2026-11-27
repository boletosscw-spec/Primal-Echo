using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace PrimalEcho
{
    /// <summary>
    /// Lectura y escritura de documentos de Cloud Firestore con la API REST.
    /// Convierte entre Dictionary&lt;string, object&gt; y el formato de valores con tipo de Firestore.
    /// Tipos admitidos: string, int/long, float/double, bool, DateTime, listas y diccionarios.
    /// </summary>
    public static class Firestore
    {
        private static string Base =>
            "https://firestore.googleapis.com/v1/projects/" + FirebaseConfig.Actual.projectId + "/databases/(default)/documents/";

        /// <summary>Lee un documento. Entrega null en "datos" si no existe todavía.</summary>
        public static IEnumerator Leer(string ruta, string token, Action<bool, Dictionary<string, object>, string> listo)
        {
            using (var req = UnityWebRequest.Get(Base + ruta))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.timeout = 15;
                yield return req.SendWebRequest();

                if (req.responseCode == 404) { listo(true, null, null); yield break; }
                if (req.result != UnityWebRequest.Result.Success) { listo(false, null, Error(req)); yield break; }

                var doc = JsonSimple.Leer(req.downloadHandler.text) as Dictionary<string, object>;
                var campos = doc != null && doc.TryGetValue("fields", out object f) ? f as Dictionary<string, object> : null;
                listo(true, campos != null ? DesdeCampos(campos) : new Dictionary<string, object>(), null);
            }
        }

        /// <summary>
        /// Crea o actualiza un documento. Solo se modifican los campos enviados;
        /// los demás (por ejemplo, el nombre) se quedan como estaban.
        /// </summary>
        public static IEnumerator Escribir(string ruta, string token, Dictionary<string, object> campos, Action<bool, string> listo)
        {
            var url = new StringBuilder(Base + ruta);
            char sep = '?';
            foreach (string clave in campos.Keys)
            {
                url.Append(sep).Append("updateMask.fieldPaths=").Append(UnityWebRequest.EscapeURL(clave));
                sep = '&';
            }

            string cuerpo = "{\"fields\":" + ACampos(campos) + "}";
            using (var req = new UnityWebRequest(url.ToString(), "PATCH"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(cuerpo));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.timeout = 15;
                yield return req.SendWebRequest();
                listo(req.result == UnityWebRequest.Result.Success, req.result == UnityWebRequest.Result.Success ? null : Error(req));
            }
        }

        private static string Error(UnityWebRequest req)
        {
            if (req.result == UnityWebRequest.Result.ConnectionError) return "Sin conexión a internet";
            if (req.responseCode == 403) return "Firestore rechazó el acceso (revisa las reglas de seguridad)";
            Debug.LogWarning("Firestore " + req.responseCode + ": " + req.downloadHandler?.text);
            return "Error de Firestore (" + req.responseCode + ")";
        }

        // ------------------------------------------------------------------ a Firestore

        private static string ACampos(Dictionary<string, object> campos)
        {
            var sb = new StringBuilder("{");
            bool primero = true;
            foreach (var par in campos)
            {
                if (!primero) sb.Append(',');
                primero = false;
                sb.Append(JsonSimple.Escapar(par.Key)).Append(':').Append(AValor(par.Value));
            }
            return sb.Append('}').ToString();
        }

        private static string AValor(object v)
        {
            if (v == null) return "{\"nullValue\":null}";
            if (v is string s) return "{\"stringValue\":" + JsonSimple.Escapar(s) + "}";
            if (v is bool b) return "{\"booleanValue\":" + (b ? "true" : "false") + "}";
            if (v is int || v is long) return "{\"integerValue\":\"" + Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture) + "\"}";
            if (v is float || v is double) return "{\"doubleValue\":" + Convert.ToDouble(v).ToString("R", CultureInfo.InvariantCulture) + "}";
            if (v is DateTime t) return "{\"timestampValue\":\"" + t.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) + "\"}";
            if (v is Dictionary<string, object> m) return "{\"mapValue\":{\"fields\":" + ACampos(m) + "}}";
            if (v is IEnumerable lista)
            {
                var sb = new StringBuilder("{\"arrayValue\":{\"values\":[");
                bool primero = true;
                foreach (object x in lista)
                {
                    if (!primero) sb.Append(',');
                    primero = false;
                    sb.Append(AValor(x));
                }
                return sb.Append("]}}").ToString();
            }
            return "{\"stringValue\":" + JsonSimple.Escapar(v.ToString()) + "}";
        }

        // ------------------------------------------------------------------ desde Firestore

        private static Dictionary<string, object> DesdeCampos(Dictionary<string, object> campos)
        {
            var r = new Dictionary<string, object>();
            foreach (var par in campos) r[par.Key] = DesdeValor(par.Value as Dictionary<string, object>);
            return r;
        }

        private static object DesdeValor(Dictionary<string, object> v)
        {
            if (v == null) return null;
            foreach (var par in v)
            {
                switch (par.Key)
                {
                    case "stringValue": return par.Value as string;
                    case "booleanValue": return par.Value is bool b && b;
                    case "integerValue": return long.TryParse(par.Value?.ToString(), out long l) ? l : 0L;
                    case "doubleValue": return Convert.ToDouble(par.Value, CultureInfo.InvariantCulture);
                    case "timestampValue":
                        return DateTime.TryParse(par.Value?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime t) ? (object)t : null;
                    case "mapValue":
                        var mapa = par.Value as Dictionary<string, object>;
                        return mapa != null && mapa.TryGetValue("fields", out object f) && f is Dictionary<string, object> fc
                            ? DesdeCampos(fc) : new Dictionary<string, object>();
                    case "arrayValue":
                        var lista = new List<object>();
                        if (par.Value is Dictionary<string, object> arr && arr.TryGetValue("values", out object vals) && vals is List<object> items)
                            foreach (object x in items) lista.Add(DesdeValor(x as Dictionary<string, object>));
                        return lista;
                    case "nullValue": return null;
                }
            }
            return null;
        }
    }
}
