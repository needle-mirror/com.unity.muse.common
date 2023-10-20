using System;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class ItemRequest
    {
        public string access_token;

        public ItemRequest(string accessToken)
        {
            access_token = accessToken;
        }
    }
}
