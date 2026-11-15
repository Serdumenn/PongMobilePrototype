using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ShopContentBuilder
{
    private const string SpecPath = "ArtSource/shop_items.json";
    private const string ItemFolder = "Assets/_Game/Data/Cosmetics";
    private const string CatalogPath = "Assets/_Game/Data/CosmeticCatalog.asset";
    private const string ContentPath = "Assets/_Game/Data/ShopContent.asset";
    private const string Balls = "Assets/_Game/Art/Sprites/Skins/Balls/";
    private const string Paddles = "Assets/_Game/Art/Sprites/Skins/Paddles/";
    private const string Fx = "Assets/_Game/Art/Sprites/Fx/";
    private const string Icons = "Assets/_Game/Art/Sprites/Shop/";
    private const int CommonUpTo = 300;
    private static readonly string[] Expressions = { "idle", "happy", "sad", "wow" };

    [Serializable]
    private sealed class Spec
    {
        public Entry[] faces;
        public Entry[] paddles;
        public Entry[] themes;
        public Entry[] trails;
        public Entry[] hits;
        public Entry[] parties;
        public Entry[] frames;
        public SetEntry[] sets;
        public SeasonEntry[] seasons;
    }

    [Serializable]
    private sealed class Entry
    {
        public string id;
        public string art;
        public string name;
        public string tier;
        public string unlock;
        public string season;
        public int gold;
        public string accent;
        public string burst;
        public bool sparkle;
        public string bg;
        public string a;
        public string b;
        public string ui;
        public string head;
        public string tail;
        public bool rainbow;
        public float width;
        public string shape;
        public float rate;
        public float[] drift;
        public string[] colors;
        public int count;
        public float speed;
        public float gravity;
        public string style;
    }

    [Serializable]
    private sealed class SetEntry
    {
        public string id;
        public string name;
        public string[] items;
        public string reward;
    }

    [Serializable]
    private sealed class SeasonEntry
    {
        public string id;
        public string name;
        public int[] start;
        public int[] end;
        public string[] items;
    }

    [MenuItem("Pingi/Build Shop Content")]
    private static void BuildFromMenu()
    {
        Debug.Log(Build());
    }

    public static string Build()
    {
        var spec = JsonUtility.FromJson<Spec>(File.ReadAllText(SpecPath));
        var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CatalogPath);
        if (spec == null || catalog == null) return "spec or catalog missing";

        var made = new Dictionary<string, CosmeticItem>();
        var missing = new List<string>();

        foreach (var e in spec.faces)
            made[e.id] = Make(e, CosmeticCategory.Ball, so =>
            {
                Sprite("Idle", so, Balls + "spr_ball_" + e.art + "_idle.svg", missing);
                Sprite("Happy", so, Balls + "spr_ball_" + e.art + "_happy.svg", missing);
                Sprite("Sad", so, Balls + "spr_ball_" + e.art + "_sad.svg", missing);
                Sprite("Wow", so, Balls + "spr_ball_" + e.art + "_wow.svg", missing);
                P(so, "Accent").colorValue = Hex(e.accent);
                Sprite("Burst", so, string.IsNullOrEmpty(e.burst) ? null : Fx + e.burst + ".svg", missing);
                P(so, "Sparkle").boolValue = e.sparkle;
            });

        foreach (var e in spec.paddles)
            made[e.id] = Make(e, CosmeticCategory.Paddle, so => Sprite("PaddleSprite", so, Paddles + "spr_paddle_" + e.art + ".svg", missing));

        foreach (var e in spec.themes)
            made[e.id] = Make(e, CosmeticCategory.Theme, so =>
            {
                P(so, "BackgroundColor").colorValue = Hex(e.bg);
                P(so, "BlobPrimary").colorValue = Hex(e.a);
                P(so, "BlobSecondary").colorValue = Hex(e.b);
                P(so, "UiClass").stringValue = e.ui ?? "";
            });

        foreach (var e in spec.trails)
            made[e.id] = Make(e, CosmeticCategory.Trail, so =>
            {
                Icon(so, e, missing);
                P(so, "TrailHead").colorValue = Hex(e.head);
                P(so, "TrailTail").colorValue = Hex(e.tail);
                P(so, "TrailRainbow").boolValue = e.rainbow;
                P(so, "TrailWidth").floatValue = e.width > 0f ? e.width : 1f;
                Sprite("TrailShape", so, string.IsNullOrEmpty(e.shape) ? null : Fx + e.shape + ".svg", missing);
                P(so, "TrailShapeRate").floatValue = e.rate;
                P(so, "TrailDrift").vector2Value = e.drift != null && e.drift.Length == 2 ? new Vector2(e.drift[0], e.drift[1]) : Vector2.zero;
            });

        foreach (var e in spec.hits)
            made[e.id] = Make(e, CosmeticCategory.Hit, so =>
            {
                Icon(so, e, missing);
                Sprite("HitShape", so, string.IsNullOrEmpty(e.shape) ? null : Fx + e.shape + ".svg", missing);
                Colors(so, e);
                P(so, "HitCount").intValue = e.count;
                P(so, "HitSpeed").floatValue = e.speed > 0f ? e.speed : 1f;
                P(so, "HitGravity").floatValue = e.gravity;
            });

        foreach (var e in spec.parties)
            made[e.id] = Make(e, CosmeticCategory.Celebration, so =>
            {
                Icon(so, e, missing);
                P(so, "Style").enumValueIndex = (int)Parse(e.style, CelebrationStyle.Confetti);
                Colors(so, e);
                Sprite("CelebrationShape", so, string.IsNullOrEmpty(e.shape) ? null : Fx + e.shape + ".svg", missing);
            });

        foreach (var e in spec.frames)
            made[e.id] = Make(e, CosmeticCategory.Frame, so =>
            {
                Icon(so, e, missing);
                P(so, "FrameClass").stringValue = e.ui ?? "";
            });

        int added = 0;
        foreach (var item in made.Values)
        {
            if (catalog.Items.Contains(item)) continue;
            catalog.Items.Add(item);
            added++;
        }

        int tiered = 0;
        foreach (var item in catalog.Items)
        {
            if (item == null || made.ContainsKey(item.Id) || item.GoldPrice <= 0 || item.Tier != CosmeticTier.None) continue;
            var so = new SerializedObject(item);
            P(so, "Tier").enumValueIndex = (int)(item.GoldPrice <= CommonUpTo ? CosmeticTier.Common : CosmeticTier.Rare);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);
            tiered++;
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);

        var content = AssetDatabase.LoadAssetAtPath<ShopContent>(ContentPath);
        if (content == null)
        {
            content = ScriptableObject.CreateInstance<ShopContent>();
            AssetDatabase.CreateAsset(content, ContentPath);
        }

        content.Sets.Clear();
        foreach (var s in spec.sets)
            content.Sets.Add(new CosmeticSet(s.id, s.name, Items(s.items, catalog, missing), Find(s.reward, catalog, missing)));

        content.Seasons.Clear();
        foreach (var s in spec.seasons)
            content.Seasons.Add(new SeasonDef(s.id, s.name, s.start[0], s.start[1], s.end[0], s.end[1], Items(s.items, catalog, missing)));

        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssetIfDirty(content);

        string report = $"items {made.Count}, added {added}, tiered {tiered}, sets {content.Sets.Count}, seasons {content.Seasons.Count}";
        return missing.Count == 0 ? report : report + ", missing: " + string.Join(", ", missing);
    }

    private static CosmeticItem Make(Entry e, CosmeticCategory category, Action<SerializedObject> fill)
    {
        string path = ItemFolder + "/cos_" + e.id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<CosmeticItem>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<CosmeticItem>();
            AssetDatabase.CreateAsset(item, path);
        }

        var so = new SerializedObject(item);
        P(so, "Id").stringValue = e.id;
        P(so, "Category").enumValueIndex = (int)category;
        P(so, "DisplayName").stringValue = e.name;
        P(so, "Unlock").enumValueIndex = (int)Parse(e.unlock, UnlockKind.Gold);
        P(so, "UnlockValue").intValue = 0;
        P(so, "ProductId").stringValue = "";
        P(so, "GoldPrice").intValue = e.gold;
        P(so, "Tier").enumValueIndex = (int)Parse(e.tier, CosmeticTier.None);
        P(so, "SeasonId").stringValue = e.season ?? "";
        fill(so);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssetIfDirty(item);
        return item;
    }

    private static void Colors(SerializedObject so, Entry e)
    {
        var colors = P(so, "Colors");
        int length = e.colors != null ? e.colors.Length : 0;
        colors.arraySize = length;
        for (int i = 0; i < length; i++) colors.GetArrayElementAtIndex(i).colorValue = Hex(e.colors[i]);
    }

    private static void Icon(SerializedObject so, Entry e, List<string> missing)
    {
        Sprite("Icon", so, Icons + "spr_icon_" + e.id + ".svg", missing);
    }

    private static void Sprite(string field, SerializedObject so, string path, List<string> missing)
    {
        Sprite sprite = null;
        if (!string.IsNullOrEmpty(path))
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) missing.Add(path);
        }
        P(so, field).objectReferenceValue = sprite;
    }

    private static List<CosmeticItem> Items(string[] ids, CosmeticCatalog catalog, List<string> missing)
    {
        var list = new List<CosmeticItem>();
        if (ids == null) return list;
        foreach (var id in ids)
        {
            var item = Find(id, catalog, missing);
            if (item != null) list.Add(item);
        }
        return list;
    }

    private static CosmeticItem Find(string id, CosmeticCatalog catalog, List<string> missing)
    {
        var item = catalog.Find(id);
        if (item == null) missing.Add(id);
        return item;
    }

    private static SerializedProperty P(SerializedObject so, string name)
    {
        return so.FindProperty("<" + name + ">k__BackingField");
    }

    private static T Parse<T>(string value, T fallback) where T : struct
    {
        return !string.IsNullOrEmpty(value) && Enum.TryParse(value, out T parsed) ? parsed : fallback;
    }

    private static Color Hex(string value)
    {
        return !string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.white;
    }
}
