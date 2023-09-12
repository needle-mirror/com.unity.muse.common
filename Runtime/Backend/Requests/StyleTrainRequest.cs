using System;

namespace Unity.Muse.Common
{
    [Serializable]
    public class StyleTrainRequest : ItemRequest
    {
        public string guid;
        public string name;
        public string[] training_images;

        public StyleTrainRequest(string accessToken, string guid, string name, string[] texturesData)
            : base(accessToken)
        {
            this.guid = guid;
            this.name = name;
            training_images = texturesData;
        }
    }
}