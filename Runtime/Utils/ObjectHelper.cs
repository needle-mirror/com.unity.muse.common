using UnityEngine;

namespace Unity.Muse.Common
{
    public static class ObjectHelper
    {
        public static void SafeDestroy(this Object @object)
        {
            if(@object == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(@object);
            else
                Object.DestroyImmediate(@object);
        }
    }
}
