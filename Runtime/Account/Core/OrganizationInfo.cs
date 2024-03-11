using System;

namespace Unity.Muse.Common.Account
{
    [Serializable]
    record OrganizationInfo
    {
        public string org_id;
        public string org_name;
        public string status;

        public string Label => org_name;
        public string Id => org_id;

        public SubscriptionStatus Status => SubscriptionStatusUtils.FromString(status);
        public bool IsExpired => Status is SubscriptionStatus.SubscriptionExpired or SubscriptionStatus.TrialExpired;
        public bool IsEntitled => this is {Status: SubscriptionStatus.FreeTrial} or {Status: SubscriptionStatus.Entitled};
    }
}
