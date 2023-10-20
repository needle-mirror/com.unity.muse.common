namespace Unity.Muse.Common
{
    internal static class ArtifactExtensions
    {
        public static bool IsValid(this Artifact artifact)
        {
            return artifact != null && !string.IsNullOrEmpty(artifact?.Guid);
        }
    }
}
