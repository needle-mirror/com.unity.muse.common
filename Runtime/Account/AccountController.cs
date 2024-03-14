using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Account
{
    delegate AccountState StateTransition(AccountState toState, AccountState fromState);

    class AccountController : IDisposable
    {
        AccountState m_State = AccountState.Default;

        internal static void SetAllStates(AccountState state)
        {
            RunAll(controller => controller.State = state);
        }

        internal static void RunAll(Action<AccountController> action)
        {
            foreach (var controller in s_Controllers)
                action(controller);
        }

        public AccountState State
        {
            get => m_State;
            set => StateChanged(value);
        }

        TrialForm m_TrialForm;
        bool m_TrialStartProcessing;

        static List<AccountController> s_Controllers = new();
        public static IEnumerable<AccountController> controllers => s_Controllers;

        /// <summary>
        /// Registers an EditorWindow to the account controller.
        ///
        /// This will make the window show the various relevant dialogs (signed in, start trial, data opt-in, etc...)
        ///
        /// The state change logic can be intercepted and modified if desired to add various conditions through an optional
        /// `transition` argument. This method will receive a `fromState` and a `toState` and should return the new desired state.
        ///
        /// eg:
        /// <example>
        /// <code>
        /// AccountController.Register(myEditorWindow, (toState, fromState) =>
        /// {
        ///     // Remove support for sign-in state
        ///     if (fromState == AccountState.SignIn)
        ///         return AccountState.Default;
        ///
        ///     return toState;
        /// });
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="window">The EditorWindow to be registered.</param>
        /// <param name="transition">An option method that can modify the state change logic if desired.</param>
        /// <returns></returns>
        public static AccountController Register(EditorWindow window, StateTransition transition = null, bool allowNoAccount = false)
        {
            var controller = new AccountController(window, false, allowNoAccount);
            controller.OnStateTransition += transition;
            controller.Init();

            return controller;
        }

        public StateTransition OnStateTransition;
        public bool skipInternalStateChange;
        public AccountDropdown AccountDropdown => m_Window.rootVisualElement.Q<AccountDropdown>();
        public bool IsInvalid => m_Window == null || element == null;

        public VisualElement element => m_Window.rootVisualElement.Q<Panel>(); // An arbitrary UI element inside the UI panel.
        readonly EditorWindow m_Window;
        Modal m_Modal;  // The currently displayed modal dialog
        AccountDialog m_Dialog;
        bool m_HasAttached;
        public bool allowNoAccount { get; protected set; }

        public AccountController(EditorWindow window, bool init = true, bool allowNoAccount = false)
        {
            s_Controllers.Add(this);
            this.allowNoAccount = allowNoAccount;

            m_Window = window;
            if (element is null)
                throw new Exception("The window must have a visual element with a Panel type to be apple to display modal dialogs.");

            element.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.museTheme));

            AccountInfo.Instance.OnOrganizationChanged += StateChanged;
            AccountInfo.Instance.OnLegalConsentChanged += StateChanged;
            AccountInfo.Instance.OnReady += StateChanged;
            window.rootVisualElement.RegisterCallback<AttachToPanelEvent>(AttachToPanel);

            if (init)
                Init();
        }

        public virtual void Dispose()
        {
            AccountInfo.Instance.OnOrganizationChanged -= StateChanged;
            AccountInfo.Instance.OnLegalConsentChanged -= StateChanged;
            AccountInfo.Instance.OnReady -= StateChanged;
        }

        public void Init()
        {
            StateChanged();
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            m_HasAttached = true;
            Apply();
        }

        static void Clear(AccountController controller)
        {
            controller.Dispose();
            s_Controllers.Remove(controller);     // Window has been destroyed.
        }

        public static bool IsAnyWindowRegistered()
        {
            foreach (var controller in s_Controllers.ToList())
            {
                if (controller.IsInvalid)
                    Clear(controller);
            }

            return s_Controllers.Any();
        }

        /// <summary>
        /// Get the account controller that a visual element is part of
        /// </summary>
        /// <param name="anyElementInWindow"></param>
        /// <returns></returns>
        public static AccountController Get(VisualElement anyElementInWindow)
        {
            return s_Controllers.Find(accountController => accountController.element == anyElementInWindow.GetFirstAncestorOfType<Panel>());
        }

        /// <summary>
        /// Refresh all editor windows's state
        /// </summary>
        public static void Refresh()
        {
            foreach (var controller in s_Controllers.ToList())
                controller.StateChanged();
        }

        bool ShouldSkipStateChange()
        {
            // Skip any update if the window has been destroyed.
            // Only clear from registered windows if the element in it has at least been attached once.
            if (IsInvalid && m_HasAttached)
                Clear(this);

            return IsInvalid;
        }

        public void StateChanged() => StateChanged(AccountState.Default);

        public void StateChanged(AccountState toState)
        {
            if (ShouldSkipStateChange())
                return;

            var updateToState = skipInternalStateChange ? toState : ChangeStateDefault(toState);// Apply internal state change logic
            m_State = OnStateTransition?.Invoke(updateToState, m_State) ?? updateToState;       // Apply External client's state change logic
            Apply();                                                                            // Apply new state
        }

        /// <summary>
        /// Refresh state
        /// </summary>
        protected virtual AccountState ChangeStateDefault(AccountState toState)
        {
            // Remove this condition and make it controllable in tests fixtures
            if (GenerativeAIBackend.s_IsRunningOnYamato)
                return AccountState.Default;        // Don't show any dialogs when running tests
            if (SignInUtility.Instance.SignInState == SignInState.SignedOut)
                return AccountState.SignIn;         // Always show sign-in dialog if requested
            if (allowNoAccount && GlobalPreferences.trialDialogShown)
                return AccountState.Default;        // Opt-out of the subscription flow if the app is usable without an entitlement.
            if (AccountInfo.Instance.RequestSeat)
                return AccountState.RequestSeat;
            if (!AccountInfo.Instance.IsReady)
            {
                // Failsafe to avoid a potential issue where the user is stuck in the sign-in state
                // This isn't something that should happen, but seem to have.
                // Should consider removing after the logic has been reviewed.
                if (m_State == AccountState.SignIn)
                    return AccountState.Default;
                else
                    return m_State;                     // Don't change state until we have entitlements+legal information
            }
            // Expired subscription do not show the "start trial" dialog
            if ((AccountInfo.Instance.IsEntitled || AccountInfo.Instance.IsExpired) &&
                AccountInfo.Instance.LegalConsent.HasConsented)
            {
                // We should most likely return the toState here, but until we can think through
                // the implications further, keeping this as-is.
                if (toState == AccountState.TrialStarted)
                    return toState;

                return AccountState.Default;
            }

            // Not entitled and not expired
            if (toState == AccountState.Default)
            {
                if (m_TrialStartProcessing)             // Keep in current state if processing trial start
                    return m_State;

                if (!AccountInfo.Instance.LegalConsent.HasConsented)
                    return AccountState.TrialConfirm;

                if (m_State == AccountState.Default)
                    return AccountState.Trial;
                else
                    return m_State;     // Can't change to default, so keep current state
            }

            return toState;
        }

        /// <summary>
        /// Apply current state
        /// </summary>
        protected virtual void Apply()
        {
            if (State == AccountState.Default)
                TryDismissCurrentModal();
            else if (State == AccountState.Trial)
            {
                m_TrialForm = new() {startTrial = !AccountInfo.Instance.IsEntitled};
                DisplayStartTrial();
            }
            else if (State == AccountState.TrialConfirm)
            {
                if (m_TrialForm == null)
                    m_TrialForm = new();    // startTrial will be false since in this case we only need legal consent and opt-in

                DisplayStartTrialConfirm();
            }
            else if (State == AccountState.DataOptIn)
                DisplayDataOptIn();
            else if (State == AccountState.TrialStarted)
                DisplayTrialStarted();
            else if (State == AccountState.SignIn)
                DisplaySignIn();
            else if (State == AccountState.RequestSeat)
                DisplayRequestSeat();
        }

        public void TryDismissCurrentModal()
        {
            if (m_Modal != null)
            {
                m_Modal.Dismiss();
                m_Modal = null;
                m_Dialog = null;
            }
        }

        public virtual void DisplayStartTrial()
        {
            ShowModal(new StartTrialDialog(allowNoAccount)
            {
                OnAccept = () => State = AccountState.TrialConfirm,
                OnClose = () =>
                {
                    GlobalPreferences.trialDialogShown = true;
                    StateChanged();
                }
            });
        }

        public virtual void DisplayStartTrialConfirm() => ShowModal(new StartTrialConfirmDialog
            {
                OnAccept = org =>
                {
                    // Something went wrong to even get here. Try to unstuck from bad situation
                    // Reported once as an issue with call stack but without reproduction
                    if (m_TrialForm == null)
                    {
                        StateChanged();
                        return;
                    }

                    m_TrialForm.organization = org;
                    m_TrialForm.legalConsent.terms_of_service_legal_info = true;
                    m_TrialForm.legalConsent.privacy_policy_gen_ai = true;

                    if (AccountInfo.Instance.LegalConsent.HasConsented)
                        ProcessTrialForm(m_TrialForm);      // Start trial without showing usage opt-in if the user has already consented to the legal terms
                    else
                        State = AccountState.DataOptIn;
                },
                OnClose = () => State = AccountState.Trial
            });
        public virtual void DisplayDataOptIn() => ShowModal(new DataOpInDialog {OnAccept = (usage) =>
            {
                m_TrialForm.legalConsent.content_usage_data_training = usage;
                ProcessTrialForm(m_TrialForm);
            }
        });
        public virtual void DisplayTrialStarted() => ShowModal(new SubscriptionStartedDialog
        {
            OnAccept = () => StateChanged(AccountState.Default)
        });
        public virtual void DisplaySignIn() => ShowModal(new SignInDialog());
        public virtual void DisplayRequestSeat() => ShowModal(new RequestSeatDialog());

        void ShowModal(AccountDialog dialog)
        {
            TryDismissCurrentModal();
            m_Modal = dialog.CreateModal(element);
            m_Dialog = dialog;
            m_Modal.Show();
        }

        void ProcessTrialForm(TrialForm trialForm)
        {
            m_Dialog.SetProcessing();
            m_TrialForm = null;

            void SetLegalConsent(Action done = null)
            {
                m_TrialStartProcessing = false;

                // No need to send it again if the user has already consented
                if (AccountInfo.Instance.LegalConsent.HasConsented)
                {
                    OnProcessTrialFormCompleted(done);
                }
                else
                {
                    AccountStatus.instance.legalConsentChecked = false;
                    GenerativeAIBackend.SetLegalConsent(trialForm.legalConsent, (_, _) => OnProcessTrialFormCompleted(done));
                }
            }

            if (trialForm.startTrial)
            {
                // Ensure entitlements will be updated.
                AccountInfo.Instance.ShouldCheckEntitlementsOnFocus = true;
                AccountStatus.instance.entitlementsChecked = false;

                // Avoids changing the state to another dialog while processing the trial start
                // because of various organization and legal change events
                m_TrialStartProcessing = true;

                GenerativeAIBackend.StartTrial(trialForm.organization.Id, _ => SetLegalConsent(() =>
                {
                    // Switch to trial form's organization if different then current
                    AccountInfo.Instance.Organization = AccountInfo.Instance.Organizations
                        .Find(org => org.Id == trialForm.organization.Id);
                }));
            }
            else
                SetLegalConsent();
        }

        void OnProcessTrialFormCompleted(Action done = null)
        {
            AccountInfo.Instance.UpdateAccountInformation(done);
        }
    }
}
