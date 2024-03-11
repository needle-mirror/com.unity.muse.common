using System;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;
using Toggle = Unity.AppUI.UI.Toggle;

namespace Unity.Muse.Common.Account
{
    class DataOpInDialog : AccountDialog
    {
        public Action<bool> OnAccept;
        readonly Button m_PrimaryAction;

        public DataOpInDialog()
        {
            AddToClassList("muse-subscription-dialog-data-opt-in");

            var header = new Text {text = TextContent.subDataTitle, name = "muse-description-title"};
            var description2 = new Text {text = TextContent.subDataDescription2, name = "muse-description-secondary", enableRichText = true};
            description2.AddToClassList("muse-description-section");
            dialogDescription.Add(header);

            var optInGroup = new VisualElement();
            optInGroup.AddToClassList("muse-opt-in-group");
            optInGroup.AddToClassList("muse-description-section");
            var usageOptIn = new Toggle {value = true};
            optInGroup.Add(usageOptIn);
            optInGroup.Add(new Text {text = TextContent.subDataLegalOptInMessage, enableRichText = true});

            dialogDescription.Add(optInGroup);
            dialogDescription.Add(description2);

            AddCancelButton(TextContent.subDataReadPolicy, AccountLinks.PrivacyNotice);
            m_PrimaryAction = AddPrimaryButton(TextContent.subDataClose, () => OnAccept?.Invoke(usageOptIn.value), false);
        }

        public override void SetProcessing()
        {
            m_PrimaryAction.SetEnabled(false);
        }
    }
}
