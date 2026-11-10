using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho.Editor
{
    /// <summary>
    /// Mientras editas el mapa, los enemigos se ven con su diseño ACTUAL (centinelas), igual que en el juego:
    ///   Robot_Centinela / Robot_Pesado → Explorador (el pesado, más grande)
    ///   Avispa → Ala Cian / Ala Ámbar (alternadas de izquierda a derecha, como hace el juego)
    ///   Planta_Carnivora → transparente (en el juego no aparece)
    ///   + una silueta del Centinela Rex donde aparece el jefe (solo se ve al editar).
    /// Solo cambia el dibujo en la escena; el comportamiento en el juego es el mismo.
    /// </summary>
    [InitializeOnLoad]
    public static class VistaEnemigos
    {
        private const string Folder = "Assets/Sprites/Enemigos/Vista";
        private const float Size = 1.4f, EliteScale = 1.3f, FlyerDrop = 0.54f; // mismos valores que Centinelas.cs

        static VistaEnemigos()
        {
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) Apply(SceneManager.GetActiveScene(), true); };
            EditorSceneManager.sceneOpened += (scene, mode) => Apply(scene, true);
            EditorSceneManager.sceneSaving += (scene, path) => Apply(scene, false);
        }

        [MenuItem("Primal Echo/Ver enemigos con su diseño actual (escena abierta)", false, 41)]
        public static void ApplyMenu() => Apply(SceneManager.GetActiveScene(), true);

        private static Sprite Preview(string name, float ppu, float pivotY)
        {
            string path = $"{Folder}/{name}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); imp = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (imp == null) return null;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            bool ok = imp.textureType == TextureImporterType.Sprite && Mathf.Approximately(imp.spritePixelsPerUnit, ppu)
                && imp.filterMode == FilterMode.Point && st.spriteAlignment == (int)SpriteAlignment.Custom
                && Mathf.Approximately(st.spritePivot.y, pivotY) && imp.spriteImportMode == SpriteImportMode.Single;
            if (!ok)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.ReadTextureSettings(st);
                st.spriteAlignment = (int)SpriteAlignment.Custom;
                st.spritePivot = new Vector2(0.5f, pivotY);
                st.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(st);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool Set(SpriteRenderer sr, Sprite sprite, float scale, Vector3 localPos)
        {
            if (sr == null || sprite == null) return false;
            var s = new Vector3(scale, scale, 1f);
            if (sr.sprite == sprite && sr.transform.localScale == s && sr.transform.localPosition == localPos && !sr.flipX) return false;
            sr.sprite = sprite; sr.flipX = false;
            sr.transform.localScale = s;
            sr.transform.localPosition = localPos;
            EditorUtility.SetDirty(sr);
            EditorUtility.SetDirty(sr.transform);
            return true;
        }

        private static void Apply(Scene scene, bool markDirty)
        {
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!scene.name.StartsWith("Nivel_")) return;
            var all = scene.GetRootGameObjects();
            var robots = all.SelectMany(r => r.GetComponentsInChildren<RobotPatrol>(true)).ToList();
            var wasps = all.SelectMany(r => r.GetComponentsInChildren<WaspFlyer>(true)).OrderBy(w => w.transform.position.x).ToList();
            var plants = all.SelectMany(r => r.GetComponentsInChildren<CarnivorousPlant>(true)).ToList();
            if (robots.Count == 0 && wasps.Count == 0 && plants.Count == 0) return;

            Sprite explorer = Preview("Explorador", 56f, 0.23f), cyan = Preview("AlaCian", 56f, 0.25f), amber = Preview("AlaAmbar", 56f, 0.19f);
            bool changed = false;
            foreach (var robot in robots)
            {
                float scale = (robot.name.StartsWith("Robot_Pesado") ? EliteScale : 1f) * Size;
                changed |= Set(robot.GetComponentInChildren<SpriteRenderer>(true), explorer, scale, Vector3.zero);
            }
            for (int i = 0; i < wasps.Count; i++)
                changed |= Set(wasps[i].GetComponentInChildren<SpriteRenderer>(true), i % 2 == 0 ? cyan : amber, Size, new Vector3(0f, -FlyerDrop * Size, 0f));
            foreach (var plant in plants)
            {
                var sr = plant.GetComponentInChildren<SpriteRenderer>(true);
                if (sr == null || Mathf.Approximately(sr.color.a, 0.3f)) continue;
                var c = sr.color; c.a = 0.3f; sr.color = c; // en el juego las plantas no aparecen
                EditorUtility.SetDirty(sr);
                changed = true;
            }

            // Silueta del jefe (solo en el nivel del templo): aparece 9 unidades antes de la meta
            if (scene.name.Contains("Templo"))
            {
                var goal = all.SelectMany(r => r.GetComponentsInChildren<LevelGoal>(true)).FirstOrDefault();
                var old = all.SelectMany(r => r.GetComponentsInChildren<SoloVistaEditor>(true)).FirstOrDefault(v => v.name == "Vista_Centinela_Rex");
                Sprite rex = Preview("Rex", 46f, 24f / 224f);
                if (goal != null && rex != null)
                {
                    var pos = new Vector3(goal.transform.position.x - 9f, goal.transform.position.y - 1.5f, 0f);
                    GameObject go = old != null ? old.gameObject : null;
                    if (go == null)
                    {
                        go = new GameObject("Vista_Centinela_Rex");
                        SceneManager.MoveGameObjectToScene(go, scene);
                        go.AddComponent<SoloVistaEditor>();
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = rex; sr.sortingOrder = 5;
                        changed = true;
                    }
                    if (go.transform.position != pos) { go.transform.position = pos; changed = true; }
                }
            }

            if (changed && markDirty) EditorSceneManager.MarkSceneDirty(scene);
            if (changed) Debug.Log($"[Primal Echo] {scene.name}: enemigos mostrados con su diseño actual ({robots.Count} exploradores, {wasps.Count} voladores).");
        }
    }
}
