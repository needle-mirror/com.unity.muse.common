using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.Common.Account
{
    class AccountDialog : AlertDialog
    {
        public ScrollView dialogDescription;
        public VisualElement primaryActionContainer = new();
        public VisualElement cancelActionContainer = new();

        public AccountDialog()
        {
            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));

            AddToClassList("muse-subscription-dialog");
            AddToClassList("muse-subscription-message");
            size = Size.L;

            var descriptionContainer = new VisualElement {name = "muse-dialog-description-container"};
            var logoContainer = new VisualElement  {name = "muse-dialog-logo-container"};
            var museLogo = new Image();
            museLogo.scaleMode = ScaleMode.StretchToFill;
            museLogo.image = ResourceManager.Load<Texture2D>(PackageResources.museLogo);
            museLogo.AddToClassList("muse-dialog-logo");
            logoContainer.Add(museLogo);
            descriptionContainer.Add(logoContainer);

            dialogDescription = new ScrollView {name = "muse-dialog-description-group"};
            dialogDescription.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            descriptionContainer.Add(dialogDescription);

            actionContainer.AddToClassList("muse-dialog-action-container");
            primaryActionContainer.AddToClassList("muse-dialog-secondary-action-container");
            cancelActionContainer.AddToClassList("muse-dialog-secondary-action-container");
            actionContainer.Add(cancelActionContainer);
            actionContainer.Add(primaryActionContainer);

            base.contentContainer.Add(descriptionContainer);
        }

        void SetQuiet(Button button)
        {
            button.quiet = true;
            button.pickingMode = PickingMode.Position;
            button.AddToClassList("appui-dialog__cancel-action");
        }

        public Button AddPrimaryButton(string text, Action clicked, bool isPrimary = true)
        {
            var button = new Button { title = text };
            if (isPrimary)
                button.AddToClassList("appui-dialog__primary-action");
            else
                SetQuiet(button);
            button.clicked += clicked;
            primaryActionContainer.Add(button);

            return button;
        }

        public Button AddCancelButton(string text, Action clicked)
        {
            var button = new Button { title = text };
            SetQuiet(button);
            button.clicked += clicked;
            cancelActionContainer.Add(button);

            return button;
        }

        /// <summary>
        /// Set to disabled while processing
        /// </summary>
        public virtual void SetProcessing()
        {
            primaryButton.SetEnabled(false);
        }

        public Modal CreateModal(VisualElement target)
        {
            var modal = Modal.Build(target, this);
            modal.view.AddToClassList("muse-subscription-modal");
            modal.SetKeyboardDismiss(false);
            modal.view.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.accountStyleSheet));

            // Changing passMask and using `--background-color` on the modal's ExVisualElement is necessary to have
            // a larger border radius on the dialog then the default. Otherwise ghosting on the dialog's edges occurs.
            if (modal.view.contentContainer is ExVisualElement exVisualElement)
                exVisualElement.passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.BackgroundColor | ExVisualElement.Passes.OutsetShadows;

            return modal;
        }
    }
}
