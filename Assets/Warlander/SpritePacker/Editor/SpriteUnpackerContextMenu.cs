using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.SpritePacker.Editor
{
    public static class SpriteUnpackerContextMenu
    {
        private const string ExtensionToUse = ".png";
        
        [MenuItem("Assets/Warlander/Unpack Sprites", true)]
        private static bool UnpackSpritesValidation()
        {
            if (Selection.count > 1 || Selection.count == 0)
            {
                return false;
            }

            Texture2D texture = Selection.activeObject as Texture2D;
            if (!texture)
            {
                return false;
            }
            
            string assetPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite[] sprites = allAssets.Where(asset => asset is Sprite).Cast<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                return false;
            }
            
            return true;
        }
        
        [MenuItem("Assets/Warlander/Unpack Sprites")]
        private static void UnpackSprites()
        {
            Texture2D texture = Selection.activeObject as Texture2D;
            if (!texture.isReadable)
            {
                EditorUtility.DisplayDialog("Sprite packing error", "Texture is not readable - please set \"Read/Write\" enabled in advanced settings in texture inspector.", "OK");
                return;
            }
            
            string assetPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite[] sprites = allAssets.Where(asset => asset is Sprite).Cast<Sprite>().ToArray();
            
            DirectoryInfo parentDirectory = Directory.GetParent(assetPath);
            string parentDirectoryPath = parentDirectory.FullName.Replace("\\", "/");
            string[] filesInDirectory = parentDirectory.GetFiles()
                .Select(file => file.Name)
                .Where(file => !file.Contains(".meta"))
                .ToArray();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                float progress = (float) i / sprites.Length;
                string title = $"Unpacking Sprites [{i + 1}/{sprites.Length}]";
                string info = $"Unpacking sprite {sprite.name}";
                
                EditorUtility.DisplayProgressBar(title, info, progress);
                UnpackSprite(sprite, parentDirectoryPath, filesInDirectory);
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private static void UnpackSprite(Sprite sprite, string directory, string[] filesInDirectory)
        {
            Texture2D newTexture = SpritePackerUtilities.CreateNewTextureFromSprite(sprite);

            string fileName = FindFreeFileName(sprite.name, filesInDirectory);
            string finalPath = $"{directory}/{fileName}";
            TextureImporter importer = SpritePackerUtilities.SaveAndImportSpriteTexture(finalPath, newTexture, SpriteImportMode.Single);
            Object.DestroyImmediate(newTexture);

            ISpriteEditorDataProvider dataProvider = SpritePackerUtilities.GetSpriteDataProvider(importer);
            
            SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
            spriteRects[0].spriteID = GUID.Generate();
            
            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
        }
        
        private static string FindFreeFileName(string fileName, string[] filesInDirectory)
        {
            string fileNameToText = $"{fileName}.{ExtensionToUse}";

            int attempt = 1;
            while (true)
            {
                if (!filesInDirectory.Contains(fileNameToText))
                {
                    return fileNameToText;
                }
                
                fileNameToText = $"{fileNameToText} ({attempt}).{ExtensionToUse}";
            }
        }
    }
}