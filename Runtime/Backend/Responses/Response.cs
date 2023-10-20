using System;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class Response
    {
        public bool success;
        public string error;

        public virtual bool HasErrors()
        {
            return !success;
        }
    }
}
