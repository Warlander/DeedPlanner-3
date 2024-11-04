using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEditor.VersionControl;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.SpritePacker.Editor
{
    public class SpritePackerEditorWindow : EditorWindow
    {

        [SerializeField] private Texture2D _atlas;
        [SerializeField] private bool _overrideAtlas;
        [SerializeField] private DefaultAsset _outputFolder;
        [SerializeField] private string _outputName;
        [SerializeField] private Texture2D[] _spritesToPack = Array.Empty<Texture2D>();
        
        private SerializedObject _selfSerializedObject;
        private SerializedProperty _atlasProperty;
        private SerializedProperty _overrideAtlasProperty;
        private SerializedProperty _spritesToPackProperty;
        private SerializedProperty _outputFolderProperty;
        private SerializedProperty _outputNameProperty;
        
        [MenuItem("Window/Warlander/Sprite Packer")]
        public static void OpenSpritePackerWindow()
        {
            EditorWindow window = GetWindow<SpritePackerEditorWindow>();
            window.titleContent = new GUIContent("Sprite Packer");
            window.Show();
        }

        private void OnEnable()
        {
            _selfSerializedObject = new SerializedObject(this);
            _atlasProperty = _selfSerializedObject.FindProperty(nameof(_atlas));
            _overrideAtlasProperty = _selfSerializedObject.FindProperty(nameof(_overrideAtlas));
            _outputFolderProperty = _selfSerializedObject.FindProperty(nameof(_outputFolder));
            _outputNameProperty = _selfSerializedObject.FindProperty(nameof(_outputName));
            _spritesToPackProperty = _selfSerializedObject.FindProperty(nameof(_spritesToPack));
        }
        
        private void OnGUI()
        {
            EditorGUILayout.PropertyField(_atlasProperty, new GUIContent("Atlas to extend (optional)"));
            if (_atlas)
            {
                EditorGUILayout.PropertyField(_overrideAtlasProperty, new GUIContent("Override Atlas"));
            }
            if (!_overrideAtlas)
            {
                EditorGUILayout.PropertyField(_outputFolderProperty, new GUIContent("Output Folder"));
                EditorGUILayout.PropertyField(_outputNameProperty, new GUIContent("Atlas Name"));
            }
            
            EditorGUILayout.PropertyField(_spritesToPackProperty, new GUIContent("Sprites to pack"), true);
            _selfSerializedObject.ApplyModifiedProperties();
            
            Texture2D[] readableSprites = new Texture2D[_spritesToPack.Length];
            for (int i = 0; i < readableSprites.Length; i++)
            {
                Texture2D originalSprite = _spritesToPack[i];
                if (originalSprite.isReadable)
                {
                    readableSprites[i] = originalSprite;
                    continue;
                }
                
                Texture2D assetDatabaseSprite = new Texture2D(originalSprite.width, originalSprite.height, originalSprite.format, false);
                string originalTexturePath = AssetDatabase.GetAssetPath(originalSprite);
                assetDatabaseSprite.LoadImage(File.ReadAllBytes(originalTexturePath));
                readableSprites[i] = assetDatabaseSprite;
            }
            
            if (GUILayout.Button("Pack Sprites"))
            {
                SpritePackerSettings spritePackerSettings = new SpritePackerSettings();
                spritePackerSettings.TexturesToPack = readableSprites;
                spritePackerSettings.OutputPath = GetOutputDirectory();
                spritePackerSettings.Atlas = _atlas;
                SpritePacker.PackSprites(spritePackerSettings);
            }
        }

        private string GetOutputDirectory()
        {
            if (_overrideAtlas)
            {
                return AssetDatabase.GetAssetPath(_atlas);
            }
            else
            {
                return $"{AssetDatabase.GetAssetPath(_outputFolder)}/{_outputName}.png";
            }
        }
    }
}