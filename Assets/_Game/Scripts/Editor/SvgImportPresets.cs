using UnityEditor;
using UnityEditor.Presets;

public sealed class SvgImportPresets : AssetPostprocessor
{
    private static readonly (string folder, string preset)[] Rules =
    {
        ("Assets/_Game/Art/UI/Icons/", "Assets/_Game/Settings/Presets/SVG_UI_Icon.preset"),
        ("Assets/_Game/Art/Sprites/", "Assets/_Game/Settings/Presets/SVG_Sprite.preset"),
    };

    private void OnPreprocessAsset()
    {
        if (!assetImporter.importSettingsMissing) return;
        if (!assetPath.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase)) return;

        foreach (var (folder, presetPath) in Rules)
        {
            if (!assetPath.StartsWith(folder, System.StringComparison.Ordinal)) continue;

            var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            if (preset != null && preset.CanBeAppliedTo(assetImporter)) preset.ApplyTo(assetImporter);
            return;
        }
    }
}
