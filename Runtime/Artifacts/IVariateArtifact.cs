using System;

namespace Unity.Muse.Common
{
    public interface IVariateArtifact
    {
        public void Variate(Model model, int variationNbr = 4);
    }
}
