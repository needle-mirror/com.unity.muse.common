using System;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class GuidResponse : Response
    {
        public string guid;
        public uint seed;
    }
}
