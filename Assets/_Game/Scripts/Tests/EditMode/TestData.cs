using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class TestData
{
    public const string ClassicModePath = "Assets/_Game/Data/Modes/mode_classic.asset";
    public const string RushModePath = "Assets/_Game/Data/Modes/mode_rush.asset";
    public const string CatalogPath = "Assets/_Game/Data/CosmeticCatalog.asset";

    public static GameModeDefinition LoadMode(string path)
    {
        var mode = AssetDatabase.LoadAssetAtPath<GameModeDefinition>(path);
        if (mode == null) throw new InvalidOperationException($"Missing mode asset: {path}");
        return mode;
    }

    public static CosmeticCatalog LoadCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CatalogPath);
        if (catalog == null) throw new InvalidOperationException($"Missing catalog asset: {CatalogPath}");
        return catalog;
    }

    public static CosmeticItem Item(string id, CosmeticCategory category, UnlockKind unlock, int unlockValue = 0, string productId = "")
    {
        var item = ScriptableObject.CreateInstance<CosmeticItem>();
        var so = new SerializedObject(item);
        so.FindProperty("<Id>k__BackingField").stringValue = id;
        so.FindProperty("<Category>k__BackingField").enumValueIndex = (int)category;
        so.FindProperty("<Unlock>k__BackingField").enumValueIndex = (int)unlock;
        so.FindProperty("<UnlockValue>k__BackingField").intValue = unlockValue;
        so.FindProperty("<ProductId>k__BackingField").stringValue = productId;
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    public static string TempFile(string name)
    {
        string dir = Path.Combine(Path.GetTempPath(), "PingiTests");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        Delete(path);
        return path;
    }

    public static void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }
}
