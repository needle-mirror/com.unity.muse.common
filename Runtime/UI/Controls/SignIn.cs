using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;
#if UNITY_EDITOR
using Unity.Muse.Common.Account;
using UnityEditor;
#endif
namespace Unity.Muse.Common
{
    internal class SignIn : ExVisualElement, IControl
    {
#if UNITY_EDITOR
        bool m_Initialized;

        ChangeInfo m_Info = new ChangeInfo();
        Model m_CurrentModel;
        MainUI m_MainUI;
        bool? m_LoggedIn;
        Modal m_SigninDialog;

        public SignIn()
        {
            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            RefreshSignInDialog();
        }

        void RefreshSignInDialog()
        {
            if (m_LoggedIn is not null && !m_LoggedIn.Value)
            {
                m_SigninDialog ??= AccountUtility.DisplaySigninDialog(this);
            }
            else
            {
                m_SigninDialog?.Dismiss();
                m_SigninDialog = null;
            }
        }

        public void SetModel(Model model)
        {
            if (model == null)
                return;
            Init();
            m_CurrentModel = model;
            RefreshSignIn();
        }

        public void UpdateView()
        {
            throw new NotImplementedException();
        }

        void Init()
        {
            if(m_Initialized) return;
            m_Initialized = true;

            RefreshSignInDialog();
            m_Info.eventDelegate = UnityConnectUtils.RegisterConnectStateChangedEvent(connectInfo => StateSignOnChange(m_Info));
        }

        void RefreshSignIn()
        {
            var loggedIn = UnityConnectUtils.GetIsLoggedIn();
            if (m_LoggedIn is not null && m_LoggedIn.Value == loggedIn)
                return;
            m_LoggedIn = loggedIn;
            m_CurrentModel.LoggedInStateChanged(loggedIn);
            parent.style.display = loggedIn ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshSignInDialog();
        }

        void StateSignOnChange(object parameters)
        {
            RefreshSignIn();
        }
        #else
        public void SetModel(Model model)
        {
        }

        public void UpdateView()
        {
            throw new NotImplementedException();
        }

        #endif
        internal new class UxmlFactory : UxmlFactory<SignIn, UxmlTraits> { }


    }
}
