using System;

namespace Unity.Muse.Common
{
    interface IMusePreferences
    {
        T Get<T>(string key, PreferenceScope scope = PreferenceScope.User, T defaultValue = default);
        void Set<T>(string key, T value, PreferenceScope scope = PreferenceScope.User);
        public void Delete<T>(string key, PreferenceScope scope = PreferenceScope.User);
    }
}
