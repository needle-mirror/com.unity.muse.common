using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class GuidResponse : Response
    {
        public string guid;
        public uint seed;
    }
}
