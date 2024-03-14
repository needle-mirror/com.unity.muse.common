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

        public event Action OnOrganizationChanged;
        public event Action OnOrganizationListChanged;
        public event Action OnLegalConsentChanged;
        public event Action OnReady;

        public bool IsEntitled => Organization is {IsEntitled: true};
        public bool IsExpired => Organization is {IsExpired: true};
        public bool RequestSeat;
        public bool IsReady => AccountStatus.instance.entitlementsChecked && AccountStatus.instance.legalConsentChecked;

        public List<OrganizationInfo> NotEntitledOrganizations => Organizations?
            .Where(org => org.Status == SubscriptionStatus.NotEntitled).ToList();

        void RefreshReady()
        {
            if (IsReady)
                OnReady?.Invoke();
        }

        public LegalConsentInfo LegalConsent
        {
            get => GlobalPreferences.legalConsent;
            set
            {
                var changed = GlobalPreferences.legalConsent != value;
                GlobalPreferences.legalConsent = value;
                if (changed)
                    OnLegalConsentChanged?.Invoke();
            }
        }

        /// <summary>
        /// List of entitled organizations
        /// </summary>
        public List<OrganizationInfo> Organizations
        {
            get => GlobalPreferences.organizations;
            internal set
            {
                GlobalPreferences.organizations = value;
                OnOrganizationListChanged?.Invoke();
                RefreshOrganization();
            }
        }

        void RefreshOrganization()
        {
#if UNITY_EDITOR
            var projectOrgId = UnityEditor.CloudProjectSettings.organizationId;
#endif
            // Try to select the most appropriate organization
            // In order of preference:
            //      Use entitled project organization
            //      Use any entitled organization
            //      Use not entitled project organization
            //      Use the first organization
            Organization = Organizations
                .OrderByDescending(org => org.Id == Organization?.Id)
                .ThenByDescending(org => org.IsEntitled && org.Id == projectOrgId)
                .ThenByDescending(org => org.IsEntitled)
                .ThenByDescending(org => !org.IsEntitled && org == Organization)
                .ThenByDescending(org => !org.IsEntitled && org.Id == projectOrgId)
                .FirstOrDefault();
        }

        /// <summary>
        /// Currently selected organization
        ///
        /// May not be entitled.
        /// </summary>
        public OrganizationInfo Organization
        {
            get => GlobalPreferences.organization;
            internal set
            {
                var changed = GlobalPreferences.organization != value;  // Record comparison will check for different fields by default
                GlobalPreferences.organization = value;
                if (changed)
                {
                    GlobalPreferences.trialDialogShown = false;
                    OnOrganizationChanged?.Invoke();
                    UpdateUsage();
                }
            }
        }

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

        bool m_UpdatingEntitlements;
        bool m_UpdatingLegalConsent;

        public void UpdateEntitlements(Action done = null)
        {
            if (!UnityConnectUtils.GetIsLoggedIn())
                return;
            if (m_UpdatingEntitlements)
                return;

            m_UpdatingEntitlements = true;
            GenerativeAIBackend.GetEntitlements((result, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    // This can happen if the token or request failed. In which case we should consider no entitlements.
                    Organizations = new();
                }
                else
                {
                    AccountStatus.instance.entitlementsChecked = true;
                    Organizations = result.orgs.ToList();
                    ShouldCheckEntitlementsOnFocus = !IsEntitled;  // Stop checking on focus if we are entitled
                    RefreshReady();
                }

                m_UpdatingEntitlements = false;
                done?.Invoke();
            });
        }

        public void UpdateLegalConsent(Action done = null)
        {
            if (!UnityConnectUtils.GetIsLoggedIn())
                return;
            if (m_UpdatingLegalConsent)
                return;

            m_UpdatingLegalConsent = true;
            GenerativeAIBackend.GetLegalConsent((result, error) =>
            {
                if (string.IsNullOrEmpty(error))
                {
                    AccountStatus.instance.legalConsentChecked = true;
                    LegalConsent = new()
                    {
                        content_usage_data_training = result.content_usage_data_training,
                        privacy_policy_gen_ai = result.privacy_policy_gen_ai,
                        terms_of_service_legal_info = result.terms_of_service_legal_info,
                        user_id = result.user_id
                    };
                    RefreshReady();
                }

                m_UpdatingLegalConsent = false;
                done?.Invoke();
            });
        }

        public void UpdateAccountInformation(Action done = null)
        {
            bool entitlementsDone = false, legalCheckDone = false;
            void OnDone()
            {
                if (entitlementsDone && legalCheckDone)
                    done?.Invoke();
            }

            UpdateEntitlements(() =>
            {
                entitlementsDone = true;
                OnDone();
            });
            UpdateLegalConsent(() =>
            {
                legalCheckDone = true;
                OnDone();
            });
        }

        public void UpdateUsage()
        {
            if (!UnityConnectUtils.GetIsLoggedIn())
                return;
            if (Organization is null or {IsEntitled: false} or {IsExpired: true})
                return;

            GenerativeAIBackend.GetUsage((result, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                    return;

                Usage = new UsageInfo {used = result.points_used, total = result.points_balance + result.points_used};
            });
        }
    }
}
