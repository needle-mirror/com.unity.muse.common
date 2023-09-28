using System;

namespace Unity.Muse.Common
{
    [Serializable]
    class ControlNetGenerateRequest: TextToImageItemRequest
    {
        public string guid;

        public string canny_base64;

        public ControlNetGenerateRequest(string sourceGuid, string sourceBase64, string prompt, ImageVariationSettingsRequest settings, string accessToken)
            : base(prompt, (TextToImageRequest)settings, accessToken)
        {
            guid = sourceGuid;
            canny_base64 = sourceBase64;
        }
    }
}