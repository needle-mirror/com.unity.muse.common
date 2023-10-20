using System;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;
using  Unity.Muse.Common.Editor;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using UnityEngine.WSA;

namespace Unity.Muse.Common.Editor.Settings
{
    internal static class MusePreferences
    {
        const string k_MusePreferencesKey = "muse";
        internal const string assetsRoot = "Assets";
        internal static string keyDeleteWithoutWarning => $"{k_MusePreferencesKey}.deleteWithoutWarning";
        internal static string keySpriteAssetGeneratedFolderPath => $"{k_MusePreferencesKey}.spriteAssetGeneratedPath";
        internal static string keyTextureAssetGeneratedFolderPath => $"{k_MusePreferencesKey}.textureAssetGeneratedPath";

        public static bool deleteWithoutWarning
        {
            get => MuseSettingsManager.Get<bool>(keyDeleteWithoutWarning, SettingsScope.User);
            set => MuseSettingsManager.Set(keyDeleteWithoutWarning, value, SettingsScope.User);
        }

        public static string spriteAssetGeneratedPath
        {
            get => MuseSettingsManager.Get<string>(keySpriteAssetGeneratedFolderPath, SettingsScope.User);
            set => MuseSettingsManager.Set(keySpriteAssetGeneratedFolderPath, value, SettingsScope.User);
        }

        public static string textureAssetGeneratedFolderPath
        {
            get => MuseSettingsManager.Get<string>(keyTextureAssetGeneratedFolderPath, SettingsScope.User);
            set => MuseSettingsManager.Set(keyTextureAssetGeneratedFolderPath, value, SettingsScope.User);
        }

        public static string GetMuseAssetGeneratedFolderPathFromMode(string currentMode)
        {
            var directory = assetsRoot;

            if (currentMode == "TextToImage" && IsValidMuseGeneratedPath(textureAssetGeneratedFolderPath))
            {
                directory = textureAssetGeneratedFolderPath;
            }
            else if (currentMode == "TextToSprite" && IsValidMuseGeneratedPath(spriteAssetGeneratedPath))
            {
                directory = spriteAssetGeneratedPath;
            }

            return directory;
        }

        internal static bool IsValidMuseGeneratedPath(string museAssetPath)
        {
            return !string.IsNullOrWhiteSpace(museAssetPath) && museAssetPath.StartsWith(assetsRoot) && Directory.Exists(museAssetPath);
        }
    }

    class MuseSettingsWindow : EditorWindow
    {
        [UserSetting]
        static MuseSetting<bool> s_DeleteWithoutWarning = new (MusePreferences.keyDeleteWithoutWarning, MusePreferences.deleteWithoutWarning, SettingsScope.User);

        [UserSetting]
        static MuseSetting<string> s_SpriteAssetGeneratedFolderPath = new (MusePreferences.keySpriteAssetGeneratedFolderPath, MusePreferences.assetsRoot, SettingsScope.User);

        [UserSetting]
        static MuseSetting<string> s_TextureAssetGeneratedFolderPath = new (MusePreferences.keyTextureAssetGeneratedFolderPath, MusePreferences.assetsRoot, SettingsScope.User);

        [UserSettingBlock("General")]
        static void SavePreferencesChanges(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            s_DeleteWithoutWarning.value = SettingsGUILayout.SearchableToggle("Delete Generations Without Warning", MusePreferences.deleteWithoutWarning, searchContext);

            if (EditorGUI.EndChangeCheck())
                MuseSettingsManager.Save();
        }

#if MUSE_SPRITE_ENABLED
        static bool s_ShowInvalidPathSpriteLabel;
        static bool s_WindowReloadsSprite = true;

        [UserSettingBlock("Muse Sprite")]
        static void SavePreferencesMuseSpriteChanges(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Default Path for Muse Sprite Assets", GUILayout.ExpandWidth(false), GUILayout.MinWidth(247));
            var spritePathTemp = s_SpriteAssetGeneratedFolderPath.value;
            s_SpriteAssetGeneratedFolderPath.value = EditorGUILayout.TextField(s_SpriteAssetGeneratedFolderPath.value, GUILayout.MinWidth(5), GUILayout.MaxWidth(500));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var folderToOpen = MusePreferences.assetsRoot;
                if (Directory.Exists(s_SpriteAssetGeneratedFolderPath.value))
                {
                    folderToOpen = s_SpriteAssetGeneratedFolderPath.value;
                }

                var path = EditorUtility.SaveFolderPanel("Save Muse Sprite Asset Default Folder", folderToOpen, "");

                if(!string.IsNullOrWhiteSpace(path))
                {
                    SetValidPath(path, s_SpriteAssetGeneratedFolderPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (s_WindowReloadsSprite || (GUI.changed && spritePathTemp != s_SpriteAssetGeneratedFolderPath.value))
            {
                s_ShowInvalidPathSpriteLabel = !MusePreferences.IsValidMuseGeneratedPath(s_SpriteAssetGeneratedFolderPath.value);
            }

            if (s_ShowInvalidPathSpriteLabel)
            {
                EditorGUILayout.LabelField("Path is not valid");
            }

            if (EditorGUI.EndChangeCheck())
                MuseSettingsManager.Save();

            s_WindowReloadsSprite = false;
        }
#endif

#if MUSE_TEXTURE_ENABLED
        static bool s_ShowInvalidPathTextureLabel;
        static bool s_WindowReloadsTexture = true;

        [UserSettingBlock("Muse Texture")]
        static void SavePreferencesMuseTextureChanges(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Default Path for Muse Texture Assets", GUILayout.ExpandWidth(false), GUILayout.MinWidth(247));
            var texturePathTemp = s_TextureAssetGeneratedFolderPath.value;
            s_TextureAssetGeneratedFolderPath.value = EditorGUILayout.TextField(s_TextureAssetGeneratedFolderPath.value, GUILayout.MinWidth(5), GUILayout.MaxWidth(500));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var folderToOpen = MusePreferences.assetsRoot;
                if (Directory.Exists(s_TextureAssetGeneratedFolderPath.value))
                {
                    folderToOpen = s_TextureAssetGeneratedFolderPath.value;
                }

                var path = EditorUtility.SaveFolderPanel("Save Muse Texture Asset Default Folder", folderToOpen, "");

                if(!string.IsNullOrWhiteSpace(path))
                {
                    SetValidPath(path, s_TextureAssetGeneratedFolderPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (s_WindowReloadsTexture || (GUI.changed && texturePathTemp != s_TextureAssetGeneratedFolderPath.value))
            {
                s_ShowInvalidPathTextureLabel = !MusePreferences.IsValidMuseGeneratedPath(s_TextureAssetGeneratedFolderPath.value);
            }

            if (s_ShowInvalidPathTextureLabel)
            {
                EditorGUILayout.LabelField("Path is not valid");
            }

            if (EditorGUI.EndChangeCheck())
                MuseSettingsManager.Save();

            s_WindowReloadsTexture = false;
        }
#endif

        static void SetValidPath(string fullPath, MuseSetting<string> pathSetting)
        {
            var assetsPath = GetPathRelativeToRoot(fullPath);
            if (string.IsNullOrWhiteSpace(assetsPath))
            {
                assetsPath = MusePreferences.assetsRoot;
            }

            pathSetting.value = assetsPath;
        }

        static string GetPathRelativeToRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.StartsWith(MusePreferences.assetsRoot) ? path : FileUtil.GetProjectRelativePath(path);
        }
    }
}
