using System;
using System.Collections.Generic;
using Unity.Muse.Common.Account;
using Unity.Muse.Common.Editor.Settings;
using UnityEngine;

namespace Unity.Muse.Common
{
    /// <summary>
    /// Standardized preferences accessor for Editor and Runtime.
    /// </summary>
    static class GlobalPreferences
    {
        static IMusePreferences s_Preferences;

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        public static void Init() => Init(new RuntimePreferences());
#endif

        public static void Init(IMusePreferences preferences)
        {
            s_Preferences = preferences;
        }

        public static void Delete<T>(string preferenceName, PreferenceScope scope = PreferenceScope.User)
        {
            s_Preferences.Delete<T>(preferenceName, scope);
        }

        /// <summary>
        /// Last-fetched list of organizations
        /// </summary>
        public static List<OrganizationInfo> organizations
        {
            get => s_Preferences.Get<List<OrganizationInfo>>(nameof(organizations), defaultValue: new());
            set => s_Preferences.Set(nameof(organizations), value);
        }

        /// <summary>
        /// Last selected organization, null if none.
        /// </summary>
        public static OrganizationInfo organization
        {
            get => s_Preferences.Get<OrganizationInfo>(nameof(organization));
            set => s_Preferences.Set(nameof(organization), value);
        }

        /// <summary>
        /// Current usage information for the current user
        /// </summary>
        public static UsageInfo usage
        {
            get => s_Preferences.Get(nameof(usage), defaultValue: new UsageInfo());
            set => s_Preferences.Set(nameof(usage), value);
        }

        /// <summary>
        /// If the subscriptStart message has been displayed or not (should be displayed only once per user lifetime)
        /// </summary>
        public static bool subscriptionStartDisplayed
        {
            get => s_Preferences.Get<bool>(nameof(subscriptionStartDisplayed));
            set => s_Preferences.Set(nameof(subscriptionStartDisplayed), value);
        }
    }
}
