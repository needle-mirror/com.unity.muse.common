using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Muse.Common
{
    internal static class Preferences
    {
        public const string keyPrefix = "Unity.Muse.Common.Preferences.";

        static Dictionary<string, string> s_PlayerPreferences = new()
        {
            { nameof(resultsTraySize), keyPrefix + nameof(resultsTraySize) },
            { nameof(autoSave), keyPrefix + nameof(autoSave) }
        };

#if UNITY_EDITOR
        [UnityEditor.MenuItem("internal:Muse/Clear Preferences", false)]
#endif
        public static void ClearAll()
        {
            foreach (var (_, key) in s_PlayerPreferences)
            {
                if (PlayerPrefs.HasKey(key))
                    PlayerPrefs.DeleteKey(key);
            }

            Session.ClearAllSessionKeys();
        }

        public static float resultsTraySize
        {
            get => PlayerPrefs.GetFloat(s_PlayerPreferences[nameof(resultsTraySize)], 1.0f);
            set => PlayerPrefs.SetFloat(s_PlayerPreferences[nameof(resultsTraySize)], value);
        }

        public static bool autoSave
        {
            get => PlayerPrefs.GetInt(s_PlayerPreferences[nameof(autoSave)], 1) == 1;
            set => PlayerPrefs.SetInt(s_PlayerPreferences[nameof(autoSave)], value ? 1 : 0);
        }

        /// <summary>
        /// Preferences that last only for one session.
        /// </summary>
        internal static class Session
        {
            public const string sessionKeyPrefix = "Unity.Muse.Common.SessionPreferences.";
            static Dictionary<string, string> s_SessionPlayerPreferences = new()
            {
                { nameof(deleteWithoutWarning), sessionKeyPrefix + nameof(deleteWithoutWarning) }
            };

            [RuntimeInitializeOnLoadMethod]
            [Preserve]
#if UNITY_EDITOR
            [UnityEditor.InitializeOnLoadMethod]
#endif
            public static void Init()
            {
                ClearAllSessionKeys();
            }

            public static void ClearAllSessionKeys()
            {
                foreach (var (_, key) in s_SessionPlayerPreferences)
                {
                    if (PlayerPrefs.HasKey(key))
                        PlayerPrefs.DeleteKey(key);
                }
            }

            public static bool deleteWithoutWarning
            {
                get => PlayerPrefs.GetInt(s_SessionPlayerPreferences[nameof(deleteWithoutWarning)], 0) == 1;
                set => PlayerPrefs.SetInt(s_SessionPlayerPreferences[nameof(deleteWithoutWarning)], value ? 1 : 0);
            }

            public static CanvasControlScheme canvasControlScheme
            {
                get => (CanvasControlScheme)PlayerPrefs.GetInt(s_SessionPlayerPreferences[nameof(canvasControlScheme)], (int)CanvasControlScheme.Modern);
                set => PlayerPrefs.SetInt(s_SessionPlayerPreferences[nameof(canvasControlScheme)], (int)value);
            }
        }
    }
}
