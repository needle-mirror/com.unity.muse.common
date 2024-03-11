using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Account
{
    class MuseProjectSettings : Panel
    {
        OrganizationDropdown m_OrganizationDropdown;
        Delegate m_ConnectChanged;
        bool m_SignedIn;
        readonly Text m_SignedOutWarning;
        readonly Text m_SelectOrgText;

        public MuseProjectSettings()
        {
            AddToClassList("muse-project-settings-panel");

            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.museTheme));
            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));

            Add(new Text {name = "muse-settings-header", text = TextContent.projectSettingsTitle});
            Add(new Text {name = "muse-settings-blurb", text = TextContent.subDescription1});
            m_SignedOutWarning = new Text {name = "muse-settings-sign-in-warning", text = TextContent.projectSettingsSignedOut};
            Add(m_SignedOutWarning);
            m_SelectOrgText = new Text {text = TextContent.projectSettingsOrgDesc};
            Add(m_SelectOrgText);
            RefreshOrganizations();
            Refresh();

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                AccountInfo.Instance.OnOrganizationChanged += RefreshOrganizations;
                AccountInfo.Instance.OnOrganizationListChanged += RefreshOrganizations;
                m_ConnectChanged = UnityConnectUtils.RegisterConnectStateChangedEvent(ConnectChanged);
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                AccountInfo.Instance.OnOrganizationChanged -= RefreshOrganizations;
                AccountInfo.Instance.OnOrganizationListChanged -= RefreshOrganizations;
                UnityConnectUtils.UnregisterConnectStateChangedEvent(m_ConnectChanged);
            });
        }

        void ConnectChanged(object obj)
        {
            Refresh();
        }

        void Refresh()
        {
            m_SignedIn = SignInUtility.Instance.SignInState == SignInState.SignedIn;
            m_SignedOutWarning.style.display = m_SignedIn ? DisplayStyle.None : DisplayStyle.Flex;
            m_SelectOrgText.SetEnabled(m_SignedIn);
            m_OrganizationDropdown.SetEnabled(m_SignedIn);
        }

        void RefreshOrganizations()
        {
            if (m_OrganizationDropdown != null)
                Remove(m_OrganizationDropdown);

            m_OrganizationDropdown = new OrganizationDropdown(org =>
            {
                AccountInfo.Instance.Organization = org;
            }, "muse-account-label-not-entitled");
            m_OrganizationDropdown.AddToClassList("muse-account-organization-dropdown");
            m_OrganizationDropdown.SetEnabled(m_SignedIn);
            Add(m_OrganizationDropdown);
        }
    }
}
