using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public static class LocFontSetup
{
    private const string Folder = "Assets/_Game/Fonts/";

    private static readonly (string Primary, string Japanese)[] Chains =
    {
        ("fnt_fredoka_medium_sdf", "fnt_mplus_rounded_medium_ja"),
        ("fnt_fredoka_semibold_sdf", "fnt_mplus_rounded_bold_ja"),
        ("fnt_fredoka_bold_sdf", "fnt_mplus_rounded_extrabold_ja")
    };

    private const string Korean = "fnt_jua_ko";

    [MenuItem("Pingi/Rebuild Language Fonts")]
    public static void Rebuild()
    {
        var korean = Ensure(Korean, Folder + "fnt_fredoka_bold_sdf.asset");
        MatchLines(korean, AssetDatabase.LoadAssetAtPath<FontAsset>(Folder + "fnt_fredoka_bold_sdf.asset"));
        var report = new List<string>();

        foreach (var (primaryName, japaneseName) in Chains)
        {
            string primaryPath = Folder + primaryName + ".asset";
            var primary = AssetDatabase.LoadAssetAtPath<FontAsset>(primaryPath);
            if (primary == null)
            {
                report.Add("missing " + primaryPath);
                continue;
            }

            var japanese = Ensure(japaneseName, primaryPath);
            MatchLines(japanese, primary);
            primary.fallbackFontAssetTable = new List<FontAsset> { japanese, korean };
            EditorUtility.SetDirty(primary);
            AssetDatabase.SaveAssetIfDirty(primary);
            report.Add(primaryName + " → " + japaneseName + ", " + Korean);
        }

        Debug.Log("Language fonts ready:\n" + string.Join("\n", report));
    }

    private static void MatchLines(FontAsset fallback, FontAsset primary)
    {
        if (fallback == null || primary == null) return;

        var face = fallback.faceInfo;
        var reference = primary.faceInfo;
        float ratio = face.pointSize / reference.pointSize;
        face.ascentLine = reference.ascentLine * ratio;
        face.descentLine = reference.descentLine * ratio;
        face.lineHeight = reference.lineHeight * ratio;
        fallback.faceInfo = face;
        EditorUtility.SetDirty(fallback);
        AssetDatabase.SaveAssetIfDirty(fallback);
    }

    private static FontAsset Ensure(string name, string templatePath)
    {
        string assetPath = Folder + name + "_sdf.asset";
        var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);
        if (existing != null)
        {
            existing.ClearFontAssetData(true);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(Folder + name + ".ttf");
        var template = AssetDatabase.LoadAssetAtPath<FontAsset>(templatePath);
        int size = template != null ? (int)template.faceInfo.pointSize : 90;
        int padding = template != null ? template.atlasPadding : 9;
        var mode = template != null ? template.atlasRenderMode : UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA;

        var asset = FontAsset.CreateFontAsset(font, size, padding, mode, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = name + "_sdf";
        AssetDatabase.CreateAsset(asset, assetPath);

        var texture = asset.atlasTextures[0];
        texture.name = name + "_sdf Atlas";
        AssetDatabase.AddObjectToAsset(texture, asset);
        asset.material.name = name + "_sdf Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        var serialized = new SerializedObject(asset);
        var clear = serialized.FindProperty("m_ClearDynamicDataOnBuild");
        if (clear != null) clear.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        return asset;
    }
}
