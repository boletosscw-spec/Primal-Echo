// Pega esta carpeta dentro de Assets/PrimalEcho/Escena02_Nemesis.
// Unity 6+ | Menu Tools > Primal Echo > Configurar Nemesis (Pixel Art + Clips)
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class NemesisClipGenerator
{
    private const string FramesPath = "Assets/PrimalEcho/Escena02_Nemesis/02_Sprites";
    private const string OutPath = "Assets/PrimalEcho/Escena02_Nemesis/06_Unity/Clips";

    [MenuItem("Tools/Primal Echo/Configurar Nemesis (Pixel Art + Clips)")]
    public static void Generate()
    {
        string[] sequences = { "01_Azul_Idle", "02_Azul_Dialogo", "03_Transformacion", "04_Rojo_Dialogo" };
        if (!AssetDatabase.IsValidFolder(OutPath)) {
            Directory.CreateDirectory(OutPath);
            AssetDatabase.Refresh();
        }
        foreach (string sequence in sequences) {
            string folder = FramesPath + "/" + sequence;
            if (!AssetDatabase.IsValidFolder(folder)) {
                Debug.LogError("Carpeta no encontrada: " + folder);
                continue;
            }
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileName(p).StartsWith("frame_") && p.EndsWith(".png"))
                .OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var keys = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < paths.Length; i++) {
                var ti = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                if (ti != null) {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = 96;
                    ti.filterMode = FilterMode.Point;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.SaveAndReimport();
                }
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                keys.Add(new ObjectReferenceKeyframe {time = i / 12f, value = sp});
            }
            if (keys.Count == 0) continue;
            // Duplicate final frame at the end of the duration for full frame visibility.
            var clip = new AnimationClip {frameRate = 12f};
            var binding = new EditorCurveBinding {
                path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = sequence != "03_Transformacion";
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string clipPath = OutPath + "/" + sequence + ".anim";
            AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log("NÉMESIS creado: " + clipPath + " (" + keys.Count + " cuadros)");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Primal Echo", "Clips de NÉMESIS generados en 06_Unity/Clips.", "OK");
    }
}
#endif
