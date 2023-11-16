using System;
using UnityEditor;

namespace Unity.Muse.Common.Editor.Settings
{
    class EditorPreferences : IMusePreferences
    {
        const string sessionKeyPrefix = "Unity.Muse.Common.Preferences";
        string m_KeyPrefix;
        string Key(string key) => $"{m_KeyPrefix}.{key}";

        public EditorPreferences(string prefix = sessionKeyPrefix)
        {
            m_KeyPrefix = prefix;
        }

        public T Get<T>(string key, PreferenceScope scope = PreferenceScope.User, T defaultValue = default)
        {
            var value = MuseSettingsManager.Get(Key(key), ToSettingsScope(scope), new PreferenceDataWrapper<T> {value = defaultValue});
            return value.value;
        }

        public void Set<T>(string key, T value, PreferenceScope scope = PreferenceScope.User)
        {
            var wrapper = new PreferenceDataWrapper<T> { value = value };
            MuseSettingsManager.Set(Key(key), wrapper, ToSettingsScope(scope));
        }

        static SettingsScope ToSettingsScope(PreferenceScope scope) {
            if (scope == PreferenceScope.Project)
                return SettingsScope.Project;
            return SettingsScope.User;
        }

        public void Delete<T>(string key, PreferenceScope scope = PreferenceScope.User)
        {
            var settingsScope = ToSettingsScope(scope);
            if (MuseSettingsManager.ContainsKey<PreferenceDataWrapper<T>>(Key(key), settingsScope))
                MuseSettingsManager.DeleteKey<PreferenceDataWrapper<T>>(Key(key), settingsScope);
        }
    }
}
