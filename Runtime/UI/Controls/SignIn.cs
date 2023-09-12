using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.Common
{
    public class SignIn : ExVisualElement, IControl
    {
#if UNITY_EDITOR
        bool m_Initialized;
        Button m_SignInBtn;

        ChangeInfo m_Info = new ChangeInfo();
        Model m_CurrentModel;
        MainUI m_MainUI;
        bool? m_LoggedIn;

        public SignIn()
        {
            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));
        }

        public void SetModel(Model model)
        {
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

            m_SignInBtn = this.Q<Button>("SignInBtn");
            m_SignInBtn.clicked += () =>
            {
                UnityConnectProxy.instance.ShowLogin();
            };
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
        public new class UxmlFactory : UxmlFactory<SignIn, UxmlTraits> { }


    }
}
