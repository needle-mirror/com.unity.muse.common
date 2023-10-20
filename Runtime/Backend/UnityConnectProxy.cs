using System;
using System.Reflection;

namespace Unity.Muse.Common
{
    internal class UnityConnectProxy
    {
        private object unityConnect;
        private MethodInfo userIdMethod;
        private MethodInfo userNameMethod;
        private MethodInfo accessTokenMethod;
        private MethodInfo showLoginMethod;
        private static UnityConnectProxy s_Instance;

        public static UnityConnectProxy instance  {get { return s_Instance; } }

        static UnityConnectProxy()
        {
            s_Instance = new UnityConnectProxy();
        }

        public UnityConnectProxy()
        {
            unityConnect = Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor").GetProperty("instance").GetValue(null);
            userIdMethod = unityConnect.GetType().GetMethod("GetUserId");
            userNameMethod = unityConnect.GetType().GetMethod("GetUserName");
            accessTokenMethod = unityConnect.GetType().GetMethod("GetAccessToken");
            showLoginMethod = unityConnect.GetType().GetMethod("ShowLogin");
        }

        public string GetUserId()
        {
            return (string)userIdMethod.Invoke(unityConnect, null);
        }

        public string GetUserName()
        {
            return (string)userNameMethod.Invoke(unityConnect, null);
        }

        public string GetAccessToken()
        {
            return (string)accessTokenMethod.Invoke(unityConnect, null);
        }

        public void ShowLogin()
        {
            showLoginMethod.Invoke(unityConnect, null);
        }
    }
}