using System;
using Unity.AppUI.UI;
using Unity.Muse.Common.Account;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.Common
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    partial class AccountDropdown : VisualElement
    {
#if ENABLE_UXML_TRAITS
        internal new class UxmlFactory : UxmlFactory<AccountDropdown, UxmlTraits> { }
#endif

        Popover m_SubscriptionStartModal;

        public AccountDropdown()
        {
            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));

            var dropdown = new Button
            {
                title = TextContent.museTitle,
                leadingIcon = "muse-logo",
                trailingIcon = "caret-down--fill"
            };
            dropdown.AddToClassList("muse-account-dropdown");

            Add(dropdown);

            dropdown.clicked += ShowMuseAccountSettings;
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
            ShowSubscriptionStartMessage();
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            AccountInfo.Instance.OnOrganizationChanged += ShowSubscriptionStartMessage;
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            AccountInfo.Instance.OnOrganizationChanged -= ShowSubscriptionStartMessage;
        }

        void ShowMuseAccountSettings()
        {
            Popover modal = null;

            void GoToAccountClick()
            {
                modal?.Dismiss();
                AccountUtility.GoToMuseAccount();
            }

            var content = new VisualElement();
            content.AddToClassList("muse-account-settings");

            var hasNeverUsedMuse = AccountInfo.Instance is {IsEntitled: false, IsExpired: false};
            var controller = AccountController.Get(this);
            if (hasNeverUsedMuse && controller is {allowNoAccount: true})
            {
                var startTrial = new Button {title = TextContent.tryMuse};
                startTrial.pickingMode = PickingMode.Position;
                startTrial.style.marginLeft = 0;
                startTrial.AddToClassList("account-dropdown-start-subscription");
                startTrial.quiet = true;
                startTrial.clicked += () =>
                {
                    modal?.Dismiss();
                    GlobalPreferences.trialDialogShown = false;
                    controller.StateChanged();
                };
                content.Add(startTrial);
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
            var usage = new Text {text = AccountInfo.Instance.Usage.Label, tooltip = AccountInfo.Instance.Usage.Tooltip};
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

            if (!hasNeverUsedMuse)
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
            if (!AccountInfo.Instance.IsEntitled)
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
                AccountController.SetAllStates(AccountState.TrialStarted);
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
