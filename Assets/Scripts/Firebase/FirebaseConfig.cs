using System;
using UnityEngine;

namespace PrimalEcho
{
    /// <summary>
    /// Datos del proyecto de Firebase. Se leen de Assets/Resources/Firebase/firebase_config.json.
    /// Mientras ese archivo tenga los valores de ejemplo, el juego funciona sin cuenta (modo invitado).
    /// </summary>
    [Serializable]
    public sealed class FirebaseConfig
    {
        public string apiKey = "";
        public string projectId = "";

        private const string Ruta = "Firebase/firebase_config";
        private static FirebaseConfig actual;

        public static FirebaseConfig Actual
        {
            get
            {
                if (actual != null) return actual;
                actual = new FirebaseConfig();
                TextAsset archivo = Resources.Load<TextAsset>(Ruta);
                if (archivo != null)
                {
                    try { JsonUtility.FromJsonOverwrite(archivo.text, actual); }
                    catch (ArgumentException e) { Debug.LogWarning("Firebase: firebase_config.json no es válido. " + e.Message); }
                }
                else Debug.LogWarning("Firebase: falta Resources/" + Ruta + ".json");
                return actual;
            }
        }

        /// <summary>True cuando ya se pegaron la API key y el ID del proyecto reales.</summary>
        public bool Configurado =>
            !string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("PEGA_") &&
            !string.IsNullOrWhiteSpace(projectId) && !projectId.StartsWith("PEGA_");
    }
}
