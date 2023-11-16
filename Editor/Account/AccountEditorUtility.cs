using Unity.Muse.Common.Account;
using Unity.Muse.Common.Editor;
using Unity.Muse.Common.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace Unity.Muse.Common.EditorAccount
{
    static class AccountEditorUtility
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            GlobalPreferences.Init(new EditorPreferences());
            EditorApplication.focusChanged += OnFocusChanged;
            EditorApplication.delayCall += () =>
            {
                if (!AccountStatus.instance.entitlementsChecked)
                    AccountInfo.Instance.UpdateEntitlements();

                if (!AccountStatus.instance.statusChecked) AccountInfo.Instance.UpdateStatus();
            };
            UnityConnectUtils.RegisterUserStateChangedEvent(_ =>
            {
                AccountInfo.Instance.ShouldCheckEntitlementsOnFocus = true;
                AccountStatus.instance.entitlementsChecked = false;

                AccountInfo.Instance.UpdateEntitlements();
            });
        }

        static void OnFocusChanged(bool focus)
        {
            // Don't constantly check subscription if muse is not even being used
            if (!EditorWindow.HasOpenInstances<MuseEditor>())
                return;

            if (focus && AccountInfo.Instance.ShouldCheckEntitlementsOnFocus)
                AccountInfo.Instance.UpdateEntitlements();
        }
    }
}
