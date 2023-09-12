using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.Muse.Common
{
    public static class TextureUtils
    {
        public static void SafeDestroy(this RenderTexture texture)
        {
            if(texture == null)
                return;

            texture.Release();

            ((Object)texture).SafeDestroy();
        }

        public static Texture2D ToTexture2D(this RenderTexture rTex)
        {
            // Save current RenderTexture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = rTex;

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(rTex.width, rTex.height, rTex.graphicsFormat, TextureCreationFlags.None);

            // Copy pixels to texture
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();

            // Restore previously active RenderTexture
            RenderTexture.active = currentActiveRT;

            return tex;
        }
    }
}
