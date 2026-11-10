using UnityEditor;
using UnityEngine;

namespace PrimalEcho.Editor
{
    /// <summary>
    /// Ajustes de importación del kit de escenario (Assets/Sprites/Fondos/KitSelva).
    /// Parallax y Corregidas: Bilinear (son ilustración, no pixel art puro).
    /// Tiles, Atlas y Recortes: Point. Todo a 32 PPU, sin compresión y sin mipmaps.
    /// </summary>
    public sealed class KitSelvaImporter : AssetPostprocessor
    {
        public const string Root = "Assets/Sprites/Fondos/KitSelva/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || assetPath.StartsWith(Root + "Vistas/")) return;
            var imp = (TextureImporter)assetImporter;
            bool illustration = assetPath.StartsWith(Root + "Parallax/") || assetPath.StartsWith(Root + "Corregidas/");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = illustration ? FilterMode.Bilinear : FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.npotScale = TextureImporterNPOTScale.None;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            st.spriteMeshType = SpriteMeshType.FullRect; // sin recortes raros en los bordes transparentes
            imp.SetTextureSettings(st);
        }
    }
}
