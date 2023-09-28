using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Unity.Muse.Common.Editor.Settings
{
    class MuseSetting<T> : UserSetting<T>
    {
        public MuseSetting(string key, T value, SettingsScope scope = SettingsScope.Project)
            : base(MuseSettingsManager.instance, key, value, scope)
        {}

        MuseSetting(UnityEditor.SettingsManagement.Settings settings, string key, T value, SettingsScope scope = SettingsScope.Project)
            : base(settings, key, value, scope) { }
    }
}
