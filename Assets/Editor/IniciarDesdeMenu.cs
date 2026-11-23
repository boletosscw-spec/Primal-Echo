using UnityEditor;
using UnityEditor.SceneManagement;

// Al darle Play en el editor, el juego siempre arranca desde el menú de inicio (con el video de
// Monster Byte Studios), aunque tengas abierto un nivel. Para probar un nivel directo usa
// "Elegir nivel" en el menú, o pon Activo = false.
[InitializeOnLoad]
public static class IniciarDesdeMenu
{
    private const bool Activo = true;
    private const string Menu = "Assets/Scenes/Menu_Inicio.unity";

    static IniciarDesdeMenu()
    {
        EditorApplication.delayCall += Aplicar;
    }

    private static void Aplicar()
    {
        EditorSceneManager.playModeStartScene = Activo ? AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu) : null;
    }
}
