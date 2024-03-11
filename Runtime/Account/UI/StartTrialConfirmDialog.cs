using System;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.Common.Account
{
    class StartTrialConfirmDialog : AccountDialog
    {
        public Action<OrganizationInfo> OnAccept;
        public Action OnClose;
        readonly Button m_PrimaryAction;
        readonly Text m_TermsOfServiceText;
        readonly Text m_PrivacyPolicyText;
        readonly Checkbox m_TermsOfServiceCheck;
        readonly Checkbox m_PrivacyPolicyCheck;
        readonly Button m_CloseButton;

        public StartTrialConfirmDialog()
        {
            AddToClassList("muse-subscription-dialog-start-confirm");

            var dialogTitle = new Text {text = TextContent.subConfirmTitle, name = "muse-description-title"};
            var description1 = new Text {text = TextContent.subConfirmDescription1, name = "muse-description-secondary", enableRichText = true};
            description1.AddToClassList("muse-description-section");
            dialogDescription.Add(dialogTitle);
            dialogDescription.Add(description1);

            var org = AccountInfo.Instance.Organization;
            var hasUsedMused = org is {IsEntitled: true} or {IsExpired: true} or {Status: SubscriptionStatus.FreeTrial};

            var organizationWarning = new Text {name = "muse-description-secondary", text = TextContent.SubConfirmSelectOrganizationWarning(org?.Label)};

            if (!hasUsedMused &&
                AccountInfo.Instance.NotEntitledOrganizations.Count > 1)
            {
                var organizationSelection = new VisualElement {name = "muse-organization-selection"};
                organizationSelection.AddToClassList("muse-description-section");
                organizationSelection.Add(new Text {name = "muse-description-text-primary", text = TextContent.subConfirmSelectOrganization});
                var organizationDropDown = new OrganizationDropdown(
                    AccountInfo.Instance.NotEntitledOrganizations,
                    selection => org = selection);
                org = organizationDropDown.Selected;
                organizationDropDown.AddToClassList("muse-trial-organization-dropdown");
                organizationSelection.Add(organizationDropDown);
                dialogDescription.Add(organizationSelection);

                organizationWarning.text = TextContent.subConfirmSelectOrganizationWarningSimple;
            }

            if (!hasUsedMused)
            {
                organizationWarning.AddToClassList("muse-description-section");
                dialogDescription.Add(organizationWarning);
            }

            AddCancelButton(TextContent.subConfirmLearnMore, AccountLinks.TrialLearnMore);
            m_CloseButton = new Button {title = TextContent.subConfirmClose};
            m_CloseButton.quiet = true;
            m_CloseButton.pickingMode = PickingMode.Position;
            m_CloseButton.AddToClassList("appui-dialog__cancel-action");
            m_CloseButton.clicked += () => OnClose?.Invoke();
            m_CloseButton.style.marginLeft = 0;
            primaryActionContainer.Add(m_CloseButton);
            m_PrimaryAction = AddPrimaryButton(TextContent.subConfirmStart, () => OnAccept?.Invoke(org));
            m_PrimaryAction.SetEnabled(false);

            m_TermsOfServiceCheck = new Checkbox {name = "trial-confirm-checkbox"};
            m_PrivacyPolicyCheck = new Checkbox {name = "trial-confirm-checkbox"};

            void CheckboxCallback(ChangeEvent<CheckboxState> _) =>
                m_PrimaryAction.SetEnabled(
                    org != null &&
                    m_TermsOfServiceCheck.value == CheckboxState.Checked &&
                    m_PrivacyPolicyCheck.value == CheckboxState.Checked);

            m_TermsOfServiceCheck.RegisterValueChangedCallback(CheckboxCallback);
            m_PrivacyPolicyCheck.RegisterValueChangedCallback(CheckboxCallback);

            var termsOfService = new VisualElement {name = "muse-terms-of-service"};
            termsOfService.AddToClassList("muse-dialog-description-group");
            termsOfService.AddToClassList("muse-description-section");
            termsOfService.Add(m_TermsOfServiceCheck);
            m_TermsOfServiceText = new Text {text = TextContent.subConfirmTermsOfService, enableRichText = true};
            m_TermsOfServiceText.RegisterCallback<PointerDownLinkTagEvent>(TermsOfServiceClick);
            m_TermsOfServiceText.RegisterCallback<PointerOverLinkTagEvent>(LinkEnter);
            m_TermsOfServiceText.RegisterCallback<PointerOutLinkTagEvent>(LinkLeave);
            termsOfService.Add(m_TermsOfServiceText);
            dialogDescription.Add(termsOfService);

            var privacyPolicy = new VisualElement {name = "muse-privacy-policy"};
            privacyPolicy.AddToClassList("muse-dialog-description-group");
            privacyPolicy.AddToClassList("muse-description-section");
            privacyPolicy.Add(m_PrivacyPolicyCheck);
            m_PrivacyPolicyText = new Text {text = TextContent.subConfirmPrivacy, enableRichText = true};
            m_PrivacyPolicyText.RegisterCallback<PointerDownLinkTagEvent>(PrivacyPolicyClick);
            m_PrivacyPolicyText.RegisterCallback<PointerOverLinkTagEvent>(LinkEnter);
            m_PrivacyPolicyText.RegisterCallback<PointerOutLinkTagEvent>(LinkLeave);
            privacyPolicy.Add(m_PrivacyPolicyText);
            dialogDescription.Add(privacyPolicy);

            // Customize the copy based on the current context
            if (AccountInfo.Instance.Organization is {Status: SubscriptionStatus.FreeTrial})
            {
                primaryActionContainer.Remove(m_CloseButton);
                dialogTitle.text = TextContent.subConfirmTitleTrial;
                m_PrimaryAction.title = TextContent.subConfirmJoinTrial;
            }
            else if (AccountInfo.Instance.Organization is {IsExpired: true} or {IsEntitled: true})
            {
                primaryActionContainer.Remove(m_CloseButton);
                dialogTitle.text = TextContent.subConfirmTitleSubscribed;
                m_PrimaryAction.title = TextContent.subConfirmStartSubscribed;
            }
        }

        void LinkEnter(PointerOverLinkTagEvent evt)
        {
            m_TermsOfServiceText.AddToClassList("muse-link-hover");
            m_PrivacyPolicyText.AddToClassList("muse-link-hover");
        }

        void LinkLeave(PointerOutLinkTagEvent pointerOutLinkTagEvent)
        {
            m_TermsOfServiceText.RemoveFromClassList("muse-link-hover");
            m_PrivacyPolicyText.RemoveFromClassList("muse-link-hover");
        }

        static void PrivacyPolicyClick(PointerDownLinkTagEvent evt)
        {
            if (evt.linkID == "policy")
                AccountLinks.PrivacyPolicy();
            else if (evt.linkID == "supplemental")
                AccountLinks.PrivacyStatement();
        }

        static void TermsOfServiceClick(PointerDownLinkTagEvent evt)
        {
            if (evt.linkID == "terms")
                AccountLinks.TermsOfService();
            else if (evt.linkID == "legal")
                AccountLinks.LegalInfo();
        }

        public override void SetProcessing()
        {
            m_PrimaryAction.SetEnabled(false);
            m_TermsOfServiceCheck.SetEnabled(false);
            m_PrivacyPolicyCheck.SetEnabled(false);
            m_CloseButton.SetEnabled(false);
        }
    }
}
