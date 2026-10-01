using System.IO;
using UnityEngine;

public static class SaveLocation
{
    private static string overrideRoot;

    public static string Root => string.IsNullOrEmpty(overrideRoot) ? Application.persistentDataPath : overrideRoot;

    public static string PathFor(string fileName)
    {
        return Path.Combine(Root, fileName);
    }

    public static void Override(string root)
    {
        overrideRoot = root;
    }
}
