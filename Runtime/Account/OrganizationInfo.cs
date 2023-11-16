using System;

namespace Unity.Muse.Common.Account
{
    [Serializable]
    class OrganizationInfo
    {
        public string org_id;
        public string org_name;

        public string Label => org_name;
        public string Id => org_id;
    }
}
