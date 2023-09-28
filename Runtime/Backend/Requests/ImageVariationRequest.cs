using System;

namespace Unity.Muse.Common
{
    [Serializable]
    class ImageVariationRequest: TextToImageItemRequest
    {
        public string guid;

        public ImageVariationRequest(string sourceGuid, string prompt, ImageVariationSettingsRequest settings, string accessToken)
            : base(prompt, (TextToImageRequest)settings, accessToken)
        {
            guid = sourceGuid;
        }
    }

    [Serializable]
    class ImageVariationBase64Request : TextToImageItemRequest
    {
        public string image_base64; 
        
        public ImageVariationBase64Request(string imageB64, string prompt, ImageVariationSettingsRequest settings, string accessToken)
            : base(prompt, (TextToImageRequest)settings, accessToken)
        {
            image_base64 = imageB64;
        }
    }
}