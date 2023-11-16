using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Unity.Muse.Common
{
    [Serializable]
    class ClientStatusRequest : ItemRequest
    {
        public override string parameters => $"package_name={package_name}&package_version={package_version}&api_version={api_version}";

        public string package_version;
        public string package_name = "com.unity.muse.common";
        public string api_version = GenerativeAIBackend.TexturesUrl.version;

        public ClientStatusRequest()
        {
#if UNITY_EDITOR
            package_version = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.muse.common/package.json").version;
#endif
        }
    }
}
