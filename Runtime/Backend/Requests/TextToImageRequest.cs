using System;
using System.Collections.Generic;

namespace Unity.Muse.Common
{
    [Serializable]
    internal struct TextToImageRequest
    {
        public string negative_prompt;
        public bool seamless;
        public uint seed;
        public int model;
        public uint width;
        public uint height;

        public TextToImageRequest(string negative_prompt, bool seamless, uint seed, int model, uint width, uint height)
        {
            this.negative_prompt = negative_prompt;
            this.seamless = seamless;
            this.seed = seed;
            this.model = model;
            this.width = width;
            this.height = height;
        }
        
        public static explicit operator TextToImageRequest(ImageVariationSettingsRequest settings)
        {
            return new TextToImageRequest(settings.negative_prompt, settings.seamless, settings.seed, settings.model, settings.width, settings.height);
        }
    }
}
