// ==================================================================================
// Utility: EmbeddedResourceLoader
//
// Provides a helper method for reading embedded resources (e.g., files packed 
// into the mod’s DLL) as raw byte arrays at runtime.
//
// Key Features:
// - LoadResourceBytes(string p_resourceName):
//      • Retrieves the currently executing assembly via `Assembly.GetExecutingAssembly()`.
//      • Attempts to open an embedded resource stream with the given name.
//      • If found, copies the contents to a `MemoryStream` and returns it as a byte array.
//      • Logs an error if the resource cannot be found and returns null.
// - Use Cases:
//      • Loading bundled assets (images, text files, configs) without shipping 
//        separate external files.
//      • Useful for mods that want to keep everything self-contained within the DLL.
// - Error Handling:
//      • Gracefully handles missing resources with a descriptive log message.
//
// Usage:
// Embed files into your mod assembly (e.g., as “Embedded Resource” in project settings), 
// then call `EmbeddedResourceLoader.LoadResourceBytes("Namespace.FileName.ext")` to 
// retrieve them at runtime.
// ==================================================================================

using System.IO;
using UnityEngine;
using System.Reflection;

public static class EmbeddedResourceLoader
{
    public static byte[] LoadResourceBytes(string p_resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using (Stream stream = assembly.GetManifestResourceStream(p_resourceName))
        {
            if (stream == null)
            {
                Debug.LogError($"[EmbeddedResourceLoader] Resource not found: {p_resourceName}");
                return null;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}