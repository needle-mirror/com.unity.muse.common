using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class StatusResponse : Response
    {
        public StatusResponseItem[] results;
    }

    [Serializable]
    public struct StatusResponseItem
    {
        public string guid;
        public string status;
        public float progress;
    }
}
