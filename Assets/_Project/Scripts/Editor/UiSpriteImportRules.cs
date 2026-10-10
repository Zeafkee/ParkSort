using UnityEditor;
using UnityEngine;

namespace ParkSort.EditorTools
{
    public sealed class UiSpriteImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Art/UI/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.spritePixelsPerUnit = 100f;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            if (assetPath.EndsWith("_9s.png"))
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out _);
                float border = Mathf.Floor(width / 3f);
                importer.spriteBorder = new Vector4(border, border, border, border);
            }
        }
    }
}
