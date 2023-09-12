using System;

namespace Unity.Muse.Common
{
    [Serializable]
    class DownloadImageRequest : GuidItemRequest
    {
        public DownloadImageRequest(string guid, string accessToken) : base(guid, accessToken)
        {
        }
    }
}
