using System;
using Unity.AppUI.UI;

namespace Unity.Muse.Common.Account
{
    class StartTrialDialog : AccountDialog
    {
        public Action OnAccept;

        public StartTrialDialog()
        {
            AddToClassList("muse-subscription-dialog-start");

            dialogDescription.Add(new Text {text = TextContent.subTitle, name="muse-description-title"});
            dialogDescription.Add(new Text {text = TextContent.subDescription1, name = "muse-description-secondary", enableRichText = true});

            AddCancelButton(TextContent.subViewPlan, AccountLinks.ViewPricing);
            AddPrimaryButton(TextContent.subStart, () => OnAccept?.Invoke());

            // Check entitlements on focus as long as the trial dialogs are shown.
            AccountInfo.Instance.ShouldCheckEntitlementsOnFocus = true;
        }
    }
}
