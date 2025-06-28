using System.IO;
using System.Reflection;
using UnityEngine;
using static UnityEngine.ImageConversion;

namespace TransitTube_Overlay_Mod
{
    internal class Util
    {
        public static void RegisterEmbeddedIcon(string resourceName, string iconKey)
        {
            if (Assets.Sprites.ContainsKey(iconKey)) { return; }

            Assembly assembly = Assembly.GetExecutingAssembly();
            if (assembly == null)
            {
                Debug.LogError("[TransitTube_Overlay_Mod] Assembly is null.");
                return;
            }


            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError($"[TransitTube_Overlay_Mod] Resource not found: {resourceName}");
                    return;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);

                    byte[] data = ms.ToArray();
                    if (data == null || data.Length == 0)
                    {
                        Debug.LogError("[TransitTube_Overlay_Mod] Image data is null or empty.");
                        return;
                    }

                    Texture2D tex = new Texture2D(256, 256);
                    bool loaded = tex.LoadImage(data);
                    if (!loaded)
                    {
                        Debug.LogError("[TransitTube_Overlay_Mod] Failed to load image from data.");
                        return;
                    }

                    Sprite sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );

                    Assets.Sprites.Add(iconKey, sprite);
                    Debug.Log($"[TransitTube_Overlay_Mod] Custom sprite registered as '{iconKey}'.");
                }
            }
        }
    }
}
