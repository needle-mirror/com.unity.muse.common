using System;
using UnityEngine;

namespace Unity.Muse.Common
{
    public class DefaultCloudContext : ICloudContext
    {
        public void RegisterNextFrameCallback(ICloudContext.Callback cb) => throw new System.NotImplementedException();
        public void RegisterForTickCallback(ICloudContext.Callback cb) => throw new System.NotImplementedException();
        public void UnregisterForTickCallback(ICloudContext.Callback cb) => throw new System.NotImplementedException();

        public double TimeSinceStartup => throw new NotImplementedException();
    }
}
