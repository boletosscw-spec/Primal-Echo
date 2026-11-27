using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PrimalEcho
{
    /// <summary>
    /// Lector y escritor de JSON mínimo para las respuestas de Firebase.
    /// Objetos → Dictionary&lt;string, object&gt;, arreglos → List&lt;object&gt;, números → double.
    /// </summary>
    public static class JsonSimple
    {
        public static object Leer(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int i = 0;
            object v = Valor(json, ref i);
            return v;
        }

        public static string Escapar(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ------------------------------------------------------------------ lectura

        private static void Espacios(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static object Valor(string s, ref int i)
        {
            Espacios(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return Objeto(s, ref i);
            if (c == '[') return Arreglo(s, ref i);
            if (c == '"') return Cadena(s, ref i);
            if (c == 't' && Sigue(s, i, "true")) { i += 4; return true; }
            if (c == 'f' && Sigue(s, i, "false")) { i += 5; return false; }
            if (c == 'n' && Sigue(s, i, "null")) { i += 4; return null; }
            return Numero(s, ref i);
        }

        private static bool Sigue(string s, int i, string palabra) =>
            string.CompareOrdinal(s, i, palabra, 0, palabra.Length) == 0;

        private static Dictionary<string, object> Objeto(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++; // {
            Espacios(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (i < s.Length)
            {
                Espacios(s, ref i);
                string clave = Cadena(s, ref i);
                Espacios(s, ref i);
                i++; // :
                d[clave] = Valor(s, ref i);
                Espacios(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                i++; // }
                break;
            }
            return d;
        }

        private static List<object> Arreglo(string s, ref int i)
        {
            var l = new List<object>();
            i++; // [
            Espacios(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (i < s.Length)
            {
                l.Add(Valor(s, ref i));
                Espacios(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                i++; // ]
                break;
            }
            return l;
        }

        private static string Cadena(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // "
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber));
                        i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            return sb.ToString();
        }

        private static object Numero(string s, ref int i)
        {
            int inicio = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            double.TryParse(s.Substring(inicio, i - inicio), NumberStyles.Float, CultureInfo.InvariantCulture, out double d);
            return d;
        }
    }
}
