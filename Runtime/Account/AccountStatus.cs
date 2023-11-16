using System;
using UnityEngine.Serialization;

namespace Unity.Muse.Common.Account
{
    /// <summary>
    /// Keeps properties that only need to be checked once per editor session.
    /// </summary>
    [Serializable]
    class AccountStatus
    {
        static AccountStatus s_Instance;
        public static AccountStatus instance => s_Instance ??= new();

        public bool usageChecked;
        public bool statusChecked;
        public bool entitlementsChecked;        // Used to avoid checking entitlements on every domain reload.
        public ClientStatusResponse status = new();
    }
}
