using System;
using System.Collections.Generic;

namespace Unity.Muse.Common
{
    [Serializable]
    class StatusRequest : ItemRequest
    {
        public List<string> guids;
        public StatusRequest(List<string> guids, string accessToken) : base(accessToken)
        {
            this.guids = guids;
        }
    }
}
