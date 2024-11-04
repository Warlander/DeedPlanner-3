using System;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.SpritePacker.Editor
{
    public static class SpritePackerUtilities
    {
        public static Texture2D CreateNewTextureFromSprite(Sprite sprite)
        {
            Texture2D parentTexture = sprite.texture;

            int parentTextureStartX = (int)sprite.rect.x;
            int parentTextureStartY = (int)sprite.rect.y;
            int newTextureWidth = (int) sprite.rect.width;
            int newTextureHeight = (int) sprite.rect.height;
            Texture2D newTexture = new Texture2D(newTextureWidth, newTextureHeight);
            
            Color[] pixels = parentTexture.GetPixels(parentTextureStartX, parentTextureStartY, newTextureWidth, newTextureHeight);
            newTexture.SetPixels(pixels);

            return newTexture;
        }
        
        public static ISpriteEditorDataProvider GetSpriteDataProvider(TextureImporter textureImporter)
        {
            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
            dataProvider.InitSpriteEditorDataProvider();
            return dataProvider;
        }

        public static TextureImporter SaveAndImportSpriteTexture(string path, Texture2D texture, SpriteImportMode spriteImportMode)
        {
            byte[] packedTextureBytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, packedTextureBytes);
            string assetDatabasePath = GetAssetDatabasePath(path);
            AssetDatabase.ImportAsset(assetDatabasePath);
            
            TextureImporter importer = (TextureImporter) TextureImporter.GetAtPath(assetDatabasePath);
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = spriteImportMode;
            importer.SaveAndReimport();
            
            return importer;
        }
        
        public static string GetAssetDatabasePath(string fullPath)
        {
            int indexOfAssets = fullPath.IndexOf("Assets", StringComparison.Ordinal);
            if (indexOfAssets < 0)
            {
                return fullPath;
            }

            return fullPath.Substring(indexOfAssets, fullPath.Length - indexOfAssets);
        }

        public static Texture2D CopyToReadableTexture(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            readableTexture.SetPixels(pixels);
            readableTexture.Apply();

            return readableTexture;
        }
    }
}