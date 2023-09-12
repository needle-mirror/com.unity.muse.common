using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class TextToImageItemRequest : ItemRequest
    {
        public string prompt;

        public TextToImageRequest settings;

        public TextToImageItemRequest(string prompt, TextToImageRequest settings, string accessToken) : base(accessToken)
        {
            this.prompt = prompt;
            this.settings = settings;
        }
    }
}
