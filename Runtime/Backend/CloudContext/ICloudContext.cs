namespace Unity.Muse.Common
{
    public interface ICloudContext
    {
        public delegate void Callback();
        void RegisterNextFrameCallback(Callback cb);
        void RegisterForTickCallback(Callback cb);
        void UnregisterForTickCallback(Callback cb);

        double TimeSinceStartup { get; }
    }
}
