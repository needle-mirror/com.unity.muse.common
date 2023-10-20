using System;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class StatusResponse : Response
    {
        public StatusResponseItem[] results;
    }

    [Serializable]
    internal struct StatusResponseItem
    {
        public string guid;
        public string status;
        public float progress;
    }
}
