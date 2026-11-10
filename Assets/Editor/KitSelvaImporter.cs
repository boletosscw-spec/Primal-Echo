using UnityEditor;
using UnityEngine;

namespace PrimalEcho.Editor
{
    /// <summary>
    /// Ajustes de importación de los fondos parallax:
    ///  · BosqueCaricatura (CraftPix, vectorial 1920x1080): Bilinear + mipmaps (se ve suave al reducirse, sin parpadeo).
    ///  · TemploPixel/Capas y KitSelva/Tiles…: pixel art → Point.
    /// Todo a 32 PPU y sin compresión.
    /// </summary>
    public sealed class KitSelvaImporter : AssetPostprocessor
    {
        public const string Root = "Assets/Sprites/Fondos/KitSelva/";
        public const string TempleRoot = "Assets/Sprites/Fondos/TemploPixel/Capas/";
        public const string ForestRoot = "Assets/Sprites/Fondos/BosqueCaricatura/Imagenes/";

        private struct Wanted { public FilterMode filter; public bool mips; }

        private static bool TryGetWanted(string path, out Wanted w)
        {
            w = default;
            if (path.StartsWith(ForestRoot)) { w.filter = FilterMode.Bilinear; w.mips = true; return true; }
            if (path.StartsWith(TempleRoot)) { w.filter = FilterMode.Point; return true; }
            if (path.StartsWith(Root) && !path.StartsWith(Root + "Vistas/"))
            {
                bool illustration = path.StartsWith(Root + "Parallax/") || path.StartsWith(Root + "Corregidas/");
                w.filter = illustration ? FilterMode.Bilinear : FilterMode.Point;
                return true;
            }
            return false;
        }

        // ¿Hay que reimportar para que tome los ajustes?
        public static bool NeedsReimport(TextureImporter imp, string path)
        {
            if (!TryGetWanted(path, out var w)) return false;
            return imp.textureType != TextureImporterType.Sprite || !Mathf.Approximately(imp.spritePixelsPerUnit, 32f)
                || imp.filterMode != w.filter || imp.mipmapEnabled != w.mips
                || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.maxTextureSize < 2048;
        }

        private void OnPreprocessTexture()
        {
            if (!TryGetWanted(assetPath, out var w)) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = w.filter;
            imp.mipmapEnabled = w.mips;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            if (imp.maxTextureSize < 2048) imp.maxTextureSize = 2048; // las capas miden 1920 px: que no se encojan
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.npotScale = TextureImporterNPOTScale.None;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            st.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(st);
        }
    }
}
