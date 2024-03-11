using System;
using UnityEngine.Serialization;

namespace Unity.Muse.Common.Account
{
    class TrialForm
    {
        public bool startTrial;

        public OrganizationInfo organization;
        public LegalConsentRequest legalConsent = new();
    }
}
