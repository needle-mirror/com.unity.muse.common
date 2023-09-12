namespace Unity.Muse.Common
{
    public static class ArtifactExtensions
    {
        public static bool IsValid(this Artifact artifact)
        {
            return artifact != null && !string.IsNullOrEmpty(artifact?.Guid);
        }
    }
}
