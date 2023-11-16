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

        ChangeInfo m_Info = new();
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
            if(!m_Initialized) return;
            RefreshSignInDialog();
        }

        void RefreshSignInDialog()
        {
            if (!UnityConnectUtils.GetIsUserInfoReady())
            {
                schedule.Execute(RefreshSignInDialog).ExecuteLater(1000);
                return;
            }

            if (m_LoggedIn is not null && !m_LoggedIn.Value)
            {
                if (m_SigninDialog?.view?.panel is null)
                {
                    try
                    {
                        DismissSigninDialog();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
                m_SigninDialog = AccountUtility.DisplaySigninDialog(this);
            }
            else
                DismissSigninDialog();
        }

        void DismissSigninDialog()
        {
            m_SigninDialog?.Dismiss();
            m_SigninDialog = null;
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
