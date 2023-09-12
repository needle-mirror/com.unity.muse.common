using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.Muse.Common.Editor
{
    [CustomEditor(typeof(Model))]
    internal class EditorModelAssetEditor : UnityEditor.Editor
    {
        public static readonly string defaultAssetCreationPath = "Assets";

        public static string assetCreationPath = defaultAssetCreationPath;

        Model Target => target as Model;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Open asset in Muse Window", MessageType.Info);
            if (GUILayout.Button("Open in Muse Window"))
            {
                OpenEditorTo(Target);
            }
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var doubleClickedAsset = EditorUtility.InstanceIDToObject(instanceID) as Model;
            if (doubleClickedAsset != null)
            {
                OpenEditorTo(doubleClickedAsset);
                return true;
            }

            return false;
        }

        static void OpenEditorTo(Model asset)
        {
            MuseEditor window = null;
            var windows = GetAllInstances<MuseEditor>();
            foreach (var genWindow in windows)
            {
                if (genWindow.CurrentModel != asset) continue;

                window = genWindow;
                break;
            }

            if (window == null)
            {
                window = CreateInstance<MuseEditor>();
                window.SetContext(asset);
            }

            window.Show();
            window.Focus();
        }

        public static void OpenWindowForMode(string mode)
        {
            var modeIndex = ModesFactory.GetModeIndexFromKey(mode);
            if (modeIndex == -1)
                return;

            var model = CreateInstance<Model>();
            model.ModeChanged(modeIndex);
            AssetDatabase.CreateAsset(model, AssetDatabase.GenerateUniqueAssetPath(Path.Combine(assetCreationPath,
                TextContent.defaultAssetName(ModesFactory.GetModeData(mode)?.title ?? "Muse Generator") + ".asset")));
            EditorGUIUtility.PingObject(model);

            OpenEditorTo(model);
        }

        public static T[] GetAllInstances<T>() where T : EditorWindow
        {
            return Resources.FindObjectsOfTypeAll<T>().Where(window => window.GetType() == typeof(T)).ToArray();
        }
    }
}
