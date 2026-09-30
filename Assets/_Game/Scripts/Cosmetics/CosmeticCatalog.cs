using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pingi/Cosmetic Catalog", fileName = "CosmeticCatalog")]
public sealed class CosmeticCatalog : ScriptableObject
{
    public const string PassProductId = "pingi_pass";
    public const string RemoveAdsProductId = "remove_ads";

    [field: SerializeField] public List<CosmeticItem> Items { get; private set; } = new List<CosmeticItem>();

    public CosmeticItem Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Items.Count; i++)
            if (Items[i] != null && Items[i].Id == id) return Items[i];
        return null;
    }

    public CosmeticItem FindByProduct(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return null;
        for (int i = 0; i < Items.Count; i++)
            if (Items[i] != null && Items[i].ProductId == productId) return Items[i];
        return null;
    }

    public List<CosmeticItem> InCategory(CosmeticCategory category)
    {
        var result = new List<CosmeticItem>();
        for (int i = 0; i < Items.Count; i++)
            if (Items[i] != null && Items[i].Category == category) result.Add(Items[i]);
        return result;
    }

    public CosmeticItem DefaultFor(CosmeticCategory category)
    {
        for (int i = 0; i < Items.Count; i++)
            if (Items[i] != null && Items[i].Category == category && Items[i].Unlock == UnlockKind.Free) return Items[i];
        return null;
    }
}
