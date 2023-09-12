using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class StyleTrainStatusResponse : Response
    {
        public string status;
        public TrainStatusVersion[] versions;
    }

    [Serializable]
    public struct TrainStatusVersion
    {
        public string guid;
        public int version;
        public string[] sample_images; // guids
    }
}