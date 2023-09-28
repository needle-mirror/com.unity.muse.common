using System;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Unity.Muse.Common.Editor.Settings
{
    static class MuseSettingsProvider
    {
        const string k_PreferencesPath = "Preferences/Muse";

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            var provider = new UserSettingsProvider(k_PreferencesPath,
                MuseSettingsManager.instance,
                new[] { typeof(MuseSettingsProvider).Assembly });

            return provider;
        }
    }
}
