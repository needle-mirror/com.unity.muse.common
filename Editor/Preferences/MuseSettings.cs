using System;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Unity.Muse.Common.Editor.Settings
{
    public static class MusePreferences
    {
        const string k_MusePreferencesKey = "muse";
        internal static string keyDeleteWithoutWarning => $"{k_MusePreferencesKey}.deleteWithoutWarning";

        public static bool deleteWithoutWarning
        {
            get => MuseSettingsManager.Get<bool>(keyDeleteWithoutWarning, SettingsScope.User);
            set => MuseSettingsManager.Set(keyDeleteWithoutWarning, value, SettingsScope.User);
        }
    }

    class MuseSettingsWindow : EditorWindow
    {
        [UserSetting]
        static MuseSetting<bool> s_DeleteWithoutWarning = new (MusePreferences.keyDeleteWithoutWarning, MusePreferences.deleteWithoutWarning, SettingsScope.User);

        [UserSettingBlock("General")]
        static void SavePreferencesChanges(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            s_DeleteWithoutWarning.value = SettingsGUILayout.SearchableToggle("Delete Generations Without Warning", MusePreferences.deleteWithoutWarning, searchContext);

            if (EditorGUI.EndChangeCheck())
                MuseSettingsManager.Save();
        }
    }
}
