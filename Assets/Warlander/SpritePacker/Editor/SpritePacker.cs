using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Warlander.SpritePacker.Editor
{
    public static class SpritePacker
    {
        public static void PackSprites(SpritePackerSettings settings)
        {
            List<Texture2D> allTextures = new List<Texture2D>();
            List<Texture2D> texturesToCleanup = new List<Texture2D>();
            Dictionary<int, SpriteData> spriteDataToUse = new Dictionary<int, SpriteData>();
            
            if (settings.Atlas)
            {
                if (!settings.Atlas.isReadable)
                {
                    EditorUtility.DisplayDialog("Sprite packing error",
                        "Texture is not readable - please set \"Read/Write\" enabled in advanced settings in texture inspector.",
                        "OK");
                    return;
                }
                
                string atlasPath = AssetDatabase.GetAssetPath(settings.Atlas);
                if (string.IsNullOrEmpty(atlasPath))
                {
                    EditorUtility.DisplayDialog("Sprite packing error",
                        "Atlas texture not found in project assets.",
                        "OK");
                }
                
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
                Sprite[] sprites = allAssets.Where(asset => asset is Sprite).Cast<Sprite>().ToArray();

                foreach (Sprite atlasSprite in sprites)
                {
                    Texture2D tempTextureFromSprite = SpritePackerUtilities.CreateNewTextureFromSprite(atlasSprite);
                    allTextures.Add(tempTextureFromSprite);
                    texturesToCleanup.Add(tempTextureFromSprite);
                }
                
                TextureImporter atlasImporter = (TextureImporter) TextureImporter.GetAtPath(atlasPath);
                ISpriteEditorDataProvider atlasDataProvider = SpritePackerUtilities.GetSpriteDataProvider(atlasImporter);
                SpriteRect[] atlasSpriteRects = atlasDataProvider.GetSpriteRects();
                
                // Finish this
                //spriteDataToUse.
                for (int i = 0; i < atlasSpriteRects.Length; i++)
                {
                    SpriteRect spriteRect = atlasSpriteRects[i];
                    spriteDataToUse.Add(i, new SpriteData(spriteRect.spriteID, spriteRect.name));
                }
            }
            
            allTextures.AddRange(settings.TexturesToPack);
            
            Texture2D packedTexture = new Texture2D(0, 0);
            Rect[] rects = packedTexture.PackTextures(allTextures.ToArray(), 4);

            foreach (Texture2D textureToCleanup in texturesToCleanup)
            {
                Object.DestroyImmediate(textureToCleanup);
            }

            Texture2D readableTexture = SpritePackerUtilities.CopyToReadableTexture(packedTexture);
            Object.DestroyImmediate(packedTexture);

            string outputPath = SpritePackerUtilities.GetAssetDatabasePath(settings.OutputPath);
            TextureImporter importer = SpritePackerUtilities.SaveAndImportSpriteTexture(outputPath, readableTexture, SpriteImportMode.Multiple);
            ISpriteEditorDataProvider dataProvider = SpritePackerUtilities.GetSpriteDataProvider(importer);

            Vector2 packedTextureSize = new Vector2(readableTexture.width, readableTexture.height);
            SpriteRect[] spriteRects = new SpriteRect[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                Rect rect = rects[i];
                Rect scaledRect = new Rect(rect.position * packedTextureSize, rect.size * packedTextureSize);
                SpriteRect spriteRect = new SpriteRect();
                spriteRect.rect = scaledRect;
                spriteRect.spriteID = GetOrGenerateSpriteGUID(spriteDataToUse, i);
                spriteRect.name = GetOrGenerateSpriteName(spriteDataToUse, i);
                spriteRects[i] = spriteRect;
            }

            dataProvider.SetSpriteRects(spriteRects);
            
            var spriteNameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            List<SpriteNameFileIdPair> spriteNameFileIdPairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < spriteRects.Length; i++)
            {
                SpriteRect spriteRect = spriteRects[i];
                spriteNameFileIdPairs.Add(new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
            }
            spriteNameFileIdDataProvider.SetNameFileIdPairs(spriteNameFileIdPairs);
            
            dataProvider.Apply();
        }

        private static GUID GetOrGenerateSpriteGUID(Dictionary<int, SpriteData> preexistingSpriteData, int index)
        {
            if (preexistingSpriteData.ContainsKey(index))
            {
                return preexistingSpriteData[index].GUID;
            }
            else
            {
                return GUID.Generate();
            }
        }

        private static string GetOrGenerateSpriteName(Dictionary<int, SpriteData> preexistingSpriteData, int index)
        {
            if (preexistingSpriteData.ContainsKey(index))
            {
                return preexistingSpriteData[index].Name;
            }
            else
            {
                return index.ToString();
            }
        }
        
    }
}