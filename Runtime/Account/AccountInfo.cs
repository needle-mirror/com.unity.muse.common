using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Unity.Muse.Common.Account
{
    class AccountInfo
    {
        static AccountInfo s_Instance;
        public static AccountInfo Instance => s_Instance ??= new();

        public event Action<ClientStatusResponse> OnClientStatusChanged;
        public event Action OnOrganizationChanged;

        public ClientStatusResponse Status
        {
            get => AccountStatus.instance.status;
            internal set
            {
                AccountStatus.instance.status = value;
                OnClientStatusChanged?.Invoke(value);
            }
        }

        public bool IsSubscribed => Organization != null;

        /// <summary>
        /// List of entitled organizations
        /// </summary>
        public List<OrganizationInfo> Organizations
        {
            get => GlobalPreferences.organizations;
            internal set
            {
                GlobalPreferences.organizations = value;

                string defaultOrgId = null;
#if UNITY_EDITOR
                defaultOrgId = UnityEditor.CloudProjectSettings.organizationId; // Prefer project org if it is entitled
#endif
                if (string.IsNullOrEmpty(Organization?.Id) || !value.Exists(org => org.Id == Organization?.Id))
                {
                    Organization =
                        Organizations?.Find(org => !string.IsNullOrEmpty(defaultOrgId) && org?.Id == defaultOrgId) ??
                        Organizations?.FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// Currently selected entitled organization, null if none.
        /// </summary>
        public OrganizationInfo Organization
        {
            get => GlobalPreferences.organization;
            internal set
            {
                var changed = GlobalPreferences.organization != value;
                GlobalPreferences.organization = value;
                if (changed)
                {
                    OnOrganizationChanged?.Invoke();
                    UpdateUsage();
                }
            }
        }

        public bool IsClientUsable => IsSubscribed && !Status.IsDeprecated;

        public bool ShouldCheckEntitlementsOnFocus { get; set; }

        public UsageInfo Usage
        {
            get => GlobalPreferences.usage;
            set => GlobalPreferences.usage = value;
        }

        public bool SubscriptionStartDisplayed
        {
            get => GlobalPreferences.subscriptionStartDisplayed;
            set => GlobalPreferences.subscriptionStartDisplayed = value;
        }

        public void UpdateEntitlements()
        {
            GenerativeAIBackend.GetEntitlements((result, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    // This can happen if the token or request failed. In which case we should consider no entitlements.
                    Organizations = new();
                    return;
                }

                Organizations = result.entitlements;
                AccountStatus.instance.entitlementsChecked = true;
                ShouldCheckEntitlementsOnFocus = Organization is null;  // Stop checking on focus if we are subscribed
            });
        }

        public void UpdateStatus()
        {
            if (AccountStatus.instance.statusChecked)
                return;

            GenerativeAIBackend.GetStatus((result, error) =>
            {
                AccountStatus.instance.statusChecked = true;

                if (!string.IsNullOrEmpty(error))
                    return;

                Status = result;
            });
        }

        public void UpdateUsage()
        {
            if (Organization is null)
                return;

            GenerativeAIBackend.GetUsage((result, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                    return;

                Usage = new UsageInfo {used = result.points_used, total = result.points_balance + result.points_used};
                AccountStatus.instance.usageChecked = true;
            });
        }
    }
}
