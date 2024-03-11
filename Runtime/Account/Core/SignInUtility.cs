using System;
using Unity.Muse.Common.Account;
using UnityEditor;
using UnityEngine;

namespace Unity.Muse.Common
{
    class SignInUtility
    {
        static SignInUtility s_SignInUtility;
        public static SignInUtility Instance => s_SignInUtility ??= new SignInUtility();

        [InitializeOnLoadMethod]
        static void Start() => s_SignInUtility ??= new SignInUtility();

        /// <summary>
        /// LoggedIn value is only valid if Ready is true, otherwise it is unknown
        /// </summary>
        public SignInState SignInState
        {
            get
            {
                if (m_LoggedIn is null)
                    return SignInState.NotReady;

                return m_LoggedIn.Value ? SignInState.SignedIn : SignInState.SignedOut;
            }
        }

        bool? m_LoggedIn;
        readonly ChangeInfo m_Info = new();

        SignInUtility()
        {
            RefreshSignIn();
            m_Info.eventDelegate = UnityConnectUtils.RegisterConnectStateChangedEvent(_ => RefreshSignIn());
        }

        void RefreshSignIn()
        {
            if (!UnityConnectUtils.GetIsUserInfoReady())
            {
                EditorApplication.delayCall += RefreshSignIn;
                return;
            }

            var loggedIn = UnityConnectUtils.GetIsLoggedIn();

            if (m_LoggedIn is not null && m_LoggedIn.Value == loggedIn)
                return;
            m_LoggedIn = loggedIn;

            AccountController.Refresh();
        }
    }
}
