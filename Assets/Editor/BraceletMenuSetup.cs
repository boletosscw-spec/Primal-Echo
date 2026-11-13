using PrimalEcho;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BraceletMenuSetup
{
    [MenuItem("Primal Echo/Brazalete/Agregar al Kael de esta escena")]
    public static void Attach()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (!player) { Debug.LogWarning("Brazalete: abre una escena con Kael."); return; }
        if (!player.GetComponent<BraceletProgress>()) Undo.AddComponent<BraceletProgress>(player.gameObject);
        if (!player.TryGetComponent(out BraceletMenu menu)) menu = Undo.AddComponent<BraceletMenu>(player.gameObject);
        var serialized = new SerializedObject(menu);
        serialized.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BraceletSettings>(
            "Assets/Resources/Brazalete/ConfiguracionBrazalete.asset");
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeGameObject = player.gameObject;
    }
}
