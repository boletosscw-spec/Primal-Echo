using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrimalEcho.Editor
{
    public static partial class JungleLevelBuilder
    {
        private const string ValleyScene = "Nivel_01_Valle_del_Eco";
        private const string ValleyArtRoot = "Ambientacion_Valle";
        private const string ValleyRelicsFolder = "Assets/Sprites/Environment/Relics";
        private const string ValleySceneryFolder = "Assets/Sprites/Environment/Backgrounds";

        private static float[] ParseValleyValues(string value, int count)
        {
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != count) throw new FormatException("Coordenadas incompletas: " + value);
            return Array.ConvertAll(parts, part => float.Parse(part, CultureInfo.InvariantCulture));
        }

        private static void TintValleyTerrain(Vector4 span, params Tilemap[] layers)
        {
            var stone = new Color(0.72f, 0.82f, 0.98f);
            foreach (var layer in layers)
            {
                layer.CompressBounds();
                foreach (var cell in layer.cellBounds.allPositionsWithin)
                {
                    float x = cell.x + 0.5f;
                    if (x <= span.x || x >= span.w || !layer.HasTile(cell)) continue;
                    float inside = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(span.x, span.y, x)) *
                        (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(span.z, span.w, x)));
                    layer.RemoveTileFlags(cell, TileFlags.LockColor);
                    layer.SetColor(cell, Color.Lerp(Color.white, stone, inside));
                }
            }
        }

        private static void BuildValleyArt(Map map, GameObject parallax, Transform lights)
        {
            if (parallax == null || Camera.main == null)
                throw new InvalidOperationException("El Valle necesita una cámara principal y Fondo_Parallax.");

            var span = map.ValleyCave.Value;
            if (!(0f <= span.x && span.x < span.y && span.y < span.z && span.z < span.w && span.w <= map.W))
                throw new FormatException("La transición de cueva necesita cuatro límites crecientes dentro del mapa.");

            const string materialPath = MapsFolder + "/Fondo_Cueva_Valle.mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(MapsFolder + "/Fondo_Cueva_Valle.shader");
            if (shader == null) throw new InvalidOperationException("Falta el shader del fondo de la cueva.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetVector("_CaveSpan", span);
            EditorUtility.SetDirty(material);

            var cam = Camera.main;
            foreach (var (name, order, speed) in new[]
            {
                ("Cueva_Lejos", -59, 0.04f),
                ("Cueva_Media", -56, 0.16f),
                ("Cueva_Cerca", -53, 0.37f),
            })
            {
                var sprite = LoadKitSprite($"{CaveArt}/{name}.png");
                if (sprite == null) throw new InvalidOperationException("Falta la capa " + name);
                var sr = Visual(parallax.transform, sprite, order);
                sr.name = name;
                sr.sharedMaterial = material;
                sr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 20f);
                sr.gameObject.AddComponent<BackdropLayer>().Configure(speed, 0.03f, false);
            }

            // El fondo se mezcla por coordenadas del mundo. CaveAmbience solo indica
            // cuándo bajar la luz en el tramo interior, sin aplicar máscaras a las capas.
            var cave = GetOrAdd<CaveAmbience>(parallax);
            cave.Configure(new[] { Rect.MinMaxRect(span.y, 0f, span.z, map.H) },
                Array.Empty<SpriteRenderer>(), Array.Empty<SpriteRenderer>());
            var darkness = GetOrAdd<CaveDarkness>(cam.gameObject);
            SetField(darkness, "maxDarkness", 0.34f);
            SetField(darkness, "fadeSeconds", 1.1f);
            SetField(darkness, "lightRadius", 5.6f);
            SetField(darkness, "darkRadius", 12f);

            var root = new GameObject(ValleyArtRoot).transform;
            foreach (var (name, x, y, width) in map.ValleyRelics)
            {
                bool skyline = name == "BarrioEnRuinas" || name == "TorreDeLasRaices";
                string folder = skyline ? ValleySceneryFolder : ValleyRelicsFolder;
                var sprite = LoadKitSprite($"{folder}/{name}.png");
                if (sprite == null || !(width > 0f))
                    throw new FormatException($"Reliquia del Valle inválida: {name}, ancho {width}");

                var sr = Visual(root, sprite, skyline ? -75 : -7);
                sr.name = name;
                float scale = width / sprite.bounds.size.x;
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                sr.transform.position = new Vector3(x - sprite.bounds.center.x * scale,
                    y - sprite.bounds.min.y * scale, 0f);
                if (skyline)
                {
                    sr.color = new Color(0.48f, 0.66f, 0.70f, 1f);
                }
                else if (name == "CristalesEnergia" || name == "HongosDeLuz" || name == "ReleAncestral" ||
                         name == "TerminalAncestral")
                {
                    var tint = name == "TerminalAncestral"
                        ? new Color(1f, 0.59f, 0.28f)
                        : new Color(0.27f, 0.86f, 0.92f);
                    float radius = Mathf.Clamp(width, 2.3f, 5f);
                    Light(lights, "Brillo_" + name, new Vector3(x, y + 1.4f, 0f), tint, 0.65f, radius);
                }
            }
        }
    }
}
