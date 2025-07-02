using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.ImageConversion;

namespace TransitTubeOverlay
{
    internal class Utils
    {
        public static void RegisterEmbeddedIcon(string resourceName, string iconKey)
        {
            if (Assets.Sprites.ContainsKey(iconKey)) { return; }

            Assembly assembly = Assembly.GetExecutingAssembly();
            if (assembly == null)
            {
                Debug.LogError("[TransitTubeOverlay] Assembly is null.");
                return;
            }


            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError($"[TransitTubeOverlay] Resource not found: {resourceName}");
                    return;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);

                    byte[] data = ms.ToArray();
                    if (data == null || data.Length == 0)
                    {
                        Debug.LogError("[TransitTubeOverlay] Image data is null or empty.");
                        return;
                    }

                    Texture2D tex = new Texture2D(256, 256);
                    bool loaded = tex.LoadImage(data);
                    if (!loaded)
                    {
                        Debug.LogError("[TransitTubeOverlay] Failed to load image from data.");
                        return;
                    }

                    Sprite sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );

                    Assets.Sprites.Add(iconKey, sprite);
                    Debug.Log($"[TransitTubeOverlay] Custom sprite registered as '{iconKey}'.");
                }
            }
        }

        public static void RegisterAllStrings()
        {
            RegisterStrings(typeof(STRINGS), "STRINGS");
        }

        private static void RegisterStrings(Type type, string path)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(String))
                {
                    string key = $"{path}.{field.Name}";
                    RuntimeHelpers.RunClassConstructor(field.DeclaringType.TypeHandle);
                    string value = field.GetValue(null)?.ToString();
                    Strings.Add(key, value);
                }
            }

            foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public))
            {
                RegisterStrings(nestedType, $"{path}.{nestedType.Name}");
            }
        }

        public static bool isModLoaded(string modName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == modName);
        }
    }
}
