using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho.Editor
{
    // Quita los "===" de los nombres de los grupos de la jerarquía ("=== ENTORNO ===" → "Entorno").
    // Se aplica solo al abrir cualquier escena; guarda con Ctrl+S para conservarlo.
    [InitializeOnLoad]
    public static class LimpiarNombresJerarquia
    {
        private static readonly Regex Decorado = new Regex(@"^\s*=+\s*(.*?)\s*=+\s*$");

        static LimpiarNombresJerarquia()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) Limpiar(SceneManager.GetActiveScene());
            };
            EditorSceneManager.sceneOpened += (scene, mode) => Limpiar(scene);
        }

        [MenuItem("Primal Echo/Quitar === de los nombres (escena abierta)")]
        private static void Menu() => Limpiar(SceneManager.GetActiveScene());

        private static void Limpiar(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            int cambios = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var m = Decorado.Match(t.name);
                    if (!m.Success || m.Groups[1].Value.Length == 0) continue;
                    string limpio = CultureInfo.GetCultureInfo("es-MX").TextInfo.ToTitleCase(m.Groups[1].Value.ToLowerInvariant());
                    limpio = limpio.Replace(" Y ", " y ").Replace(" De ", " de ");
                    t.name = limpio;
                    cambios++;
                }
            if (cambios > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Primal Echo] {scene.name}: se quitaron los '===' de {cambios} nombres. Guarda con Ctrl+S.");
            }
        }
    }
}
