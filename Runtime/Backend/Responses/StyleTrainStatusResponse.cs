using System;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class StyleTrainStatusResponse : Response
    {
        public string status;
        public TrainStatusVersion[] versions;
    }

    [Serializable]
    internal struct TrainStatusVersion
    {
        public string guid;
        public int version;
        public string[] sample_images; // guids
    }
}