using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class GuidItemRequest : ItemRequest
    {
        public string guid;
        public GuidItemRequest(string guid, string accessToken) : base(accessToken)
        {
            this.guid = guid;
        }
    }
}
