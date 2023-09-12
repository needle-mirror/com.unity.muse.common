using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class StyleTrainStatusRequest : GuidItemRequest
    {
        public StyleTrainStatusRequest(string accessToken, string guid) : base(guid, accessToken)
        {
            this.guid = guid;
        }
    }
}
