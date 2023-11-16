using System;
using System.Linq;
using Unity.AppUI.UI;
using Unity.Muse.Common.Account;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.Common
{
    class AccountDropdown : VisualElement
    {
        readonly VisualElement m_DialogRoot;
        Popover m_SubscriptionStartModal;

        public AccountDropdown(VisualElement dialogRoot)
        {
            m_DialogRoot = dialogRoot;

            var dropdown = new Button {title = TextContent.museTitle};
            dropdown.hierarchy.Insert(0, new Image
            {
                image = ResourceManager.Load<Texture2D>(PackageResources.accountDropdownIcon),
                name = "muse-dropdown-logo",
                scaleMode = ScaleMode.StretchToFill
            });
            dropdown.hierarchy.Add(new Icon {iconName = "caret-down"});

            Add(dropdown);

            dropdown.clicked += ShowMuseAccountSettings;
        }

        void ShowMuseAccountSettings()
        {
            Popover modal = null;

            void GoToAccountClick()
            {
                modal?.Dismiss();
                AccountUtility.GoToAccount();
            }

            var content = new VisualElement();
            content.AddToClassList("muse-account-settings");

            if (AccountInfo.Instance.Organizations.Count > 1)
            {
                var organizationDropdown = new Dropdown {name = "muse-account-organization-dropdown"};
                organizationDropdown.bindItem = (item, i) => item.label = AccountInfo.Instance.Organizations[i].Label;
                organizationDropdown.sourceItems = AccountInfo.Instance.Organizations;
                organizationDropdown.value = new int[] { };
                var selected = AccountInfo.Instance.Organizations.FindIndex(org => org.Id == AccountInfo.Instance.Organization?.Id);
                if (selected != -1)
                    organizationDropdown.value = new[] {selected};
                organizationDropdown.RegisterValueChangedCallback(evt =>
                    AccountInfo.Instance.Organization = AccountInfo.Instance.Organizations[evt.newValue.FirstOrDefault()]);
                content.Add(organizationDropdown);
            }

            var usageGroup = new VisualElement {name = "muse-account-usage-group"};

            var isUsageExceeded = AccountInfo.Instance.Usage.CanExceed &&
                AccountInfo.Instance.Usage.used > AccountInfo.Instance.Usage.total;
            if (isUsageExceeded)
            {
                var usageExceeded = new Text {name = "muse-usage-exceeded", text = TextContent.subUsageExceeded};
                usageGroup.Add(usageExceeded);
            }

            var usageRow = new VisualElement {name = "muse-account-usage-row"};
            var usageLabel = new Text {text = TextContent.subUsageUsed};
            usageRow.Add(usageLabel);
            var usage = new Text {text = AccountInfo.Instance.Usage.Label};
            usage.AddToClassList("usage");
            usageRow.Add(usage);

            usageGroup.Add(usageRow);

            if (isUsageExceeded)
            {
                var bar = new Image
                {
                    image = ResourceManager.Load<Texture2D>(PackageResources.accountUsageExceededBar),
                    scaleMode = ScaleMode.StretchToFill
                };
                bar.AddToClassList("muse-usage-progress");
                usageGroup.Add(bar);
            }
            else
            {
                var usageProgress = new LinearProgress
                {
                    value = AccountInfo.Instance.Usage.Progress,
                    variant = Progress.Variant.Determinate
                };
                usageProgress.AddToClassList("muse-usage-progress");
                usageProgress.colorOverride = new Color(0.9215f, 0.2549f, 0.47843f);
                usageGroup.Add(usageProgress);
            }

            content.Add(usageGroup);

            var goToAccountRow = new VisualElement();
            goToAccountRow.AddToClassList("row");
            goToAccountRow.Add(new Text {text = TextContent.goToMuseAccount});
            goToAccountRow.Add(new IconButton
            {
                icon = "arrow-square-out",
                quiet = true,
                clickable = new Pressable(GoToAccountClick)
            });
            goToAccountRow.AddManipulator(new Pressable(GoToAccountClick));

            content.Add(goToAccountRow);

            modal = Popover.Build(this, content);
            modal.SetAnchor(this);
            modal.SetPlacement(PopoverPlacement.Bottom);
            modal.Show();
        }

        public void ShowSubscriptionStartMessage()
        {
            if (panel == null)
                return;
            if (AccountInfo.Instance.SubscriptionStartDisplayed)
                return;

            AccountInfo.Instance.SubscriptionStartDisplayed = true;

            var message = new VisualElement();
            message.AddToClassList("muse-subscription-message");
            message.AddToClassList("muse-subscription-start-message");

            var titleRow = new VisualElement {name = "muse-message-title-row"};
            titleRow.Add(new Text {text = TextContent.subStartTitle, name = "muse-message-title"});
            titleRow.Add(new IconButton {icon = "x", quiet = true, clickable = new Pressable(() => m_SubscriptionStartModal?.Dismiss())});
            message.Add(titleRow);

            message.Add(new Text {text = TextContent.subStartDescription, name = "muse-message-description", enableRichText = true});
            message.Add(new Button(() =>
            {
                m_SubscriptionStartModal?.Dismiss();
                AccountUtility.TryDisplaySubscriptionStartedDialog(m_DialogRoot);
            })
            {
                name = "muse-message-learn-more-button",
                title = TextContent.subStartLearnMore,
                variant = ButtonVariant.Accent
            });

            m_SubscriptionStartModal = Popover.Build(this, message);
            m_SubscriptionStartModal.view.AddToClassList("muse-subscription-start-popover");
            m_SubscriptionStartModal.SetAnchor(this);
            m_SubscriptionStartModal.SetPlacement(PopoverPlacement.Bottom);
            // Changing passMask and using `--background-color` on the modal's ExVisualElement is necessary to have
            // a larger border radius on the dialog then the default. Otherwise ghosting on the dialog's edges occurs.
            if (m_SubscriptionStartModal.view.Q<ExVisualElement>("appui-popover__shadow-element") is ExVisualElement exVisualElement)
                exVisualElement.passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.BackgroundColor | ExVisualElement.Passes.OutsetShadows;
            m_SubscriptionStartModal.Show();
        }
    }
}
