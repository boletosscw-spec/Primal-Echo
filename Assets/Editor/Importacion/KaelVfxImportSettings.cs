using UnityEditor;
using UnityEngine;

// Importa las hojas de Assets/Resources/KaelVFX y Assets/Resources/Centinelas como pixel art nítido (sin compresión ni suavizado).
public sealed class KaelVfxImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith("Assets/Resources/KaelVFX/") && !path.StartsWith("Assets/Resources/Centinelas/")
            && !path.StartsWith("Assets/Resources/CentinelaRex/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
