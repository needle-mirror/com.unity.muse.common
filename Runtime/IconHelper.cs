using UnityEngine;

namespace Unity.Muse.Common
{
    internal static class IconHelper
    {
        static Texture2D s_WindowIcon;
        static Texture2D s_AssetIcon;

        public const string assetIconPath = "Packages/com.unity.muse.common/Editor/Resources/Icons/MuseAssetIcon.png";

        public static Texture2D windowIcon
        {
            get
            {
                #if UNITY_EDITOR
                if (s_WindowIcon == null)
                    s_WindowIcon = UnityEditor.EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_Muse") : Resources.Load<Texture2D>("Icons/Muse");
                #endif
                return s_WindowIcon;
            }
        }

        public static Texture2D assetIcon
        {
            get
            {
                if (s_AssetIcon == null)
                    s_AssetIcon = Resources.Load<Texture2D>("Icons/MuseAssetIcon");
                return s_AssetIcon;
            }
        }
    }
}
