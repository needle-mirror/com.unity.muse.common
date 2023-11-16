using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Application = UnityEngine.Device.Application;
using Button = Unity.AppUI.UI.Button;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Muse.Common.Account
{
    static class AccountUtility
    {
        static Dictionary<VisualElement, Modal> s_SubscriptionDialog = new();

        public static void StartTrial()
        {
            Application.OpenURL("https://store.unity.com/configure-plan/unity-muse-trial");
        }

        public static void StartSubscription()
        {
            Application.OpenURL("https://store.unity.com/configure-plan/unity-muse");
        }

        public static void ViewPricing()
        {
            Application.OpenURL("https://unity.com/products/muse");
        }

        public static void GoToAccount()
        {
            var organizationId = AccountInfo.Instance.Organization?.Id;
            if (string.IsNullOrEmpty(organizationId))
                Application.OpenURL("https://id.unity.com/account/edit");
            else
                Application.OpenURL($"https://id.unity.com/organizations/{organizationId}");
        }

        public static bool ShowSubscriptionDialog(Model model)
        {
            // Only show the dialog if the user doesn't have any generations and is not subscribed
            var hasAnyGenerations = model.AssetsData?.Any() ?? false;
            return !hasAnyGenerations && !AccountInfo.Instance.IsSubscribed;
        }

        public static void TryDisplaySubscriptionDialog(Model model, VisualElement parent)
        {
            if (!model)
                return;
            // Only show the dialog if the user doesn't have any generations and is not subscribed
            if (!ShowSubscriptionDialog(model))
                return;
            if (!UnityConnectUtils.GetIsLoggedIn())
                return;

            DisplaySubscriptionDialog(parent);
        }

        public static void DisplaySubscriptionDialog(VisualElement parent)
        {
            if (s_SubscriptionDialog.TryGetValue(parent, out var previousDialog))
            {
                previousDialog?.Dismiss();
                s_SubscriptionDialog.Remove(parent);
            }

            // Ensure entitlements will be updated.
            AccountInfo.Instance.ShouldCheckEntitlementsOnFocus = true;
            AccountStatus.instance.entitlementsChecked = false;

            var dialog = new AlertDialog();
            dialog.AddToClassList("muse-subscription-dialog");
            dialog.AddToClassList("muse-subscription-dialog-start");
            dialog.AddToClassList("muse-subscription-message");
            dialog.size = Size.L;

            var descriptionContainer = new VisualElement {name = "muse-dialog-description-container"};
            var museLogo = new Image();
            museLogo.style.backgroundImage = new StyleBackground(ResourceManager.Load<Texture2D>(PackageResources.museLogo));
            descriptionContainer.Add(museLogo);

            var description = new VisualElement {name = "muse-dialog-description-group"};
            description.Add(new Text {text = TextContent.subTitle, name="muse-description-title"});
            description.Add(new Text {text = TextContent.subDescription1, name = "muse-description-secondary", enableRichText = true});
            description.Add(new Text {text = TextContent.subDescription2, name="muse-description-primary"});
            description.Add(new Text {text = TextContent.subDescription3, name="muse-description-3"});
            descriptionContainer.Add(description);

            dialog.contentContainer.Add(descriptionContainer);

            var viewPlan = new Button { title = TextContent.subViewPlan };
            viewPlan.quiet = true;
            viewPlan.pickingMode = PickingMode.Position;
            viewPlan.AddToClassList("appui-dialog__cancel-action");
            viewPlan.clicked += ViewPricing;
            viewPlan.style.marginLeft = 0;

            var subscribe = new Button { title = TextContent.subStart };
            subscribe.AddToClassList("appui-dialog__primary-action");
            subscribe.clicked += StartTrial;

            dialog.actionContainer.style.justifyContent = Justify.SpaceBetween;
            dialog.actionContainer.Add(viewPlan);
            dialog.actionContainer.Add(subscribe);

            var modal = Modal.Build(parent, dialog);
            s_SubscriptionDialog[parent] = modal;
            modal.SetKeyboardDismiss(false);
            modal.view.AddToClassList("muse-subscription-modal");
            modal.view.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));
            // Changing passMask and using `--background-color` on the modal's ExVisualElement is necessary to have
            // a larger border radius on the dialog then the default. Otherwise ghosting on the dialog's edges occurs.
            if (modal.view.contentContainer is ExVisualElement exVisualElement)
                exVisualElement.passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.BackgroundColor | ExVisualElement.Passes.OutsetShadows;
            modal.Show();
        }

        public static void TryDisplaySubscriptionStartedDialog(VisualElement parent)
        {
            Modal subscriptionStartedDialog = null;

            var dialog = new AlertDialog();
            dialog.AddToClassList("muse-subscription-dialog");
            dialog.AddToClassList("muse-subscription-dialog-started");
            dialog.AddToClassList("muse-subscription-message");
            dialog.size = Size.L;

            var descriptionContainer = new VisualElement {name = "muse-dialog-description-container"};
            var museLogo = new Image();
            museLogo.style.backgroundImage = new StyleBackground(ResourceManager.Load<Texture2D>(PackageResources.museLogo));
            descriptionContainer.Add(museLogo);

            var description = new VisualElement {name = "muse-dialog-description-group"};
            description.Add(new Text {text = TextContent.subStartedTitle, name="muse-description-title"});
            var descriptionStart = new Text {text = TextContent.subStartedDesc1, name = "muse-description-primary"};
            descriptionStart.AddToClassList("muse-description-content-start");
            description.Add(descriptionStart);
            var descriptionEnd = new Text {text = TextContent.subStartedDesc2, name = "muse-description-secondary"};
            descriptionEnd.AddToClassList("muse-description-content-end");
            description.Add(descriptionEnd);
            description.Add(new Text {text = TextContent.subStartedDesc3, name="muse-description-primary"});
            descriptionContainer.Add(description);

            dialog.contentContainer.Add(descriptionContainer);

            var viewPlan = new Button { title = TextContent.subViewPlan };
            viewPlan.quiet = true;
            viewPlan.pickingMode = PickingMode.Position;
            viewPlan.AddToClassList("appui-dialog__cancel-action");
            viewPlan.clicked += ViewPricing;
            viewPlan.style.marginLeft = 0;

            var subscribe = new Button { title = TextContent.subStartedPrimary };
            subscribe.AddToClassList("appui-dialog__primary-action");
            subscribe.clicked += () => subscriptionStartedDialog?.Dismiss();

            dialog.actionContainer.style.justifyContent = Justify.SpaceBetween;
            dialog.actionContainer.Add(viewPlan);
            dialog.actionContainer.Add(subscribe);

            subscriptionStartedDialog = Modal.Build(parent, dialog);
            subscriptionStartedDialog.view.AddToClassList("muse-subscription-modal");
            // Changing passMask and using `--background-color` on the modal's ExVisualElement is necessary to have
            // a larger border radius on the dialog then the default. Otherwise ghosting on the dialog's edges occurs.
            if (subscriptionStartedDialog.view.contentContainer is ExVisualElement exVisualElement)
                exVisualElement.passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.BackgroundColor | ExVisualElement.Passes.OutsetShadows;
            subscriptionStartedDialog.Show();
        }

        public static void DismissSubscriptionDialog(VisualElement dialogParent)
        {
            if (s_SubscriptionDialog.TryGetValue(dialogParent, out var dialog))
            {
                dialog?.Dismiss();
                s_SubscriptionDialog.Remove(dialogParent);
            }
        }

        public static Modal DisplaySigninDialog(VisualElement parent)
        {
            var dialog = new AlertDialog();
            dialog.AddToClassList("muse-subscription-dialog");
            dialog.AddToClassList("muse-subscription-message");
            dialog.size = Size.L;

            var descriptionContainer = new VisualElement {name = "muse-dialog-description-container"};
            var museLogo = new Image();
            museLogo.style.backgroundImage = new StyleBackground(ResourceManager.Load<Texture2D>(PackageResources.museLogo));
            descriptionContainer.Add(museLogo);

            var description = new VisualElement {name = "muse-dialog-description-group"};
            description.Add(new Text {text = TextContent.signinTitle, name="muse-description-title"});
            description.Add(new Text {text = TextContent.signinDescription, name="muse-description-primary"});
            descriptionContainer.Add(description);

            dialog.contentContainer.Add(descriptionContainer);

            var subscribe = new Button { title = TextContent.signinAccept };
            subscribe.AddToClassList("appui-dialog__primary-action");
#if UNITY_EDITOR
            subscribe.clicked += () => CloudProjectSettings.ShowLogin();
#endif
            dialog.actionContainer.style.justifyContent = Justify.FlexEnd;
            dialog.actionContainer.Add(subscribe);

            var signinDialog = Modal.Build(parent, dialog);
            signinDialog.SetKeyboardDismiss(false);
            signinDialog.view.AddToClassList("muse-subscription-modal");
            // Changing passMask and using `--background-color` on the modal's ExVisualElement is necessary to have
            // a larger border radius on the dialog then the default. Otherwise ghosting on the dialog's edges occurs.
            if (signinDialog.view.contentContainer is ExVisualElement exVisualElement)
                exVisualElement.passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.BackgroundColor | ExVisualElement.Passes.OutsetShadows;
            signinDialog.Show();

            return signinDialog;
        }

        public static void UpdateMusePackages()
        {
#if UNITY_EDITOR
            UnityEditor.PackageManager.UI.Window.Open("com.unity.muse.common");
#endif
        }
    }
}