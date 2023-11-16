using System;
using UnityEngine;

namespace Unity.Muse.Common.Editor.Settings
{
    class RuntimePreferences : IMusePreferences
    {
        const string sessionKeyPrefix = "Unity.Muse.Common.Preferences";
        string m_KeyPrefix;
        string Key(string key) => $"{m_KeyPrefix}.{key}";

        public RuntimePreferences(string prefix = sessionKeyPrefix)
        {
            m_KeyPrefix = prefix;
        }

        public T Get<T>(string key, PreferenceScope scope = PreferenceScope.User, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(Key(key)))
                Set(key, defaultValue);

            var json = PlayerPrefs.GetString(Key(key));
            return JsonUtility.FromJson<PreferenceDataWrapper<T>>(json).value;
        }

        public void Set<T>(string key, T value, PreferenceScope scope = PreferenceScope.User)
        {
            var wrapper = new PreferenceDataWrapper<T> { value = value };
            var json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(Key(key), json);
        }

        public void Delete<T>(string key, PreferenceScope scope = PreferenceScope.User)
        {
            if (PlayerPrefs.HasKey(Key(key)))
                PlayerPrefs.DeleteKey(Key(key));
        }
    }
}
