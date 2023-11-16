using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Unity.Muse.Common.Editor.Settings
{
    /// <summary>
    /// This class will act as a manager for the <see cref="Settings"/> singleton.
    /// </summary>
    static class MuseSettingsManager
    {
        const string k_PackageName = "com.unity.muse.common";
        static UnityEditor.SettingsManagement.Settings  s_Instance;

        internal static UnityEditor.SettingsManagement.Settings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new UnityEditor.SettingsManagement.Settings(k_PackageName);
                }

                return s_Instance;
            }
        }

        // The rest of this file is just forwarding the various setting methods to the instance.

        internal static void Save()
        {
            instance.Save();
        }

        internal static T Get<T>(string key, SettingsScope scope = SettingsScope.Project, T fallback = default(T))
        {
            return instance.Get<T>(key, scope, fallback);
        }

        internal static void Set<T>(string key, T value, SettingsScope scope = SettingsScope.Project)
        {
            instance.Set<T>(key, value, scope);
        }

        internal static bool ContainsKey<T>(string key, SettingsScope scope = SettingsScope.Project)
        {
            return instance.ContainsKey<T>(key, scope);
        }

        internal static void DeleteKey<T>(string key, SettingsScope scope = SettingsScope.Project)
        {
            instance.DeleteKey<T>(key, scope);
        }
    }
}
