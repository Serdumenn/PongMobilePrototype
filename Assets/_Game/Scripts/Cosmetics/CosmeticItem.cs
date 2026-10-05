using UnityEngine;

public enum CosmeticCategory
{
    Ball,
    Paddle,
    Theme
}

public enum UnlockKind
{
    Free,
    Score,
    Ads,
    PassOnly,
    Streak
}

[CreateAssetMenu(menuName = "Pingi/Cosmetic Item", fileName = "cos_item")]
public sealed class CosmeticItem : ScriptableObject
{
    [field: Header("Identity")]
    [field: SerializeField] public string Id { get; private set; }
    [field: SerializeField] public CosmeticCategory Category { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }

    [field: Header("Unlock")]
    [field: SerializeField] public UnlockKind Unlock { get; private set; }
    [field: SerializeField] public int UnlockValue { get; private set; }
    [field: SerializeField] public string ProductId { get; private set; }

    [field: Header("Ball")]
    [field: SerializeField] public Sprite Idle { get; private set; }
    [field: SerializeField] public Sprite Happy { get; private set; }
    [field: SerializeField] public Sprite Sad { get; private set; }

    [field: Header("Paddle")]
    [field: SerializeField] public Sprite PaddleSprite { get; private set; }

    [field: Header("Theme")]
    [field: SerializeField] public Color BackgroundColor { get; private set; } = Color.white;
    [field: SerializeField] public Color BlobPrimary { get; private set; } = Color.white;
    [field: SerializeField] public Color BlobSecondary { get; private set; } = Color.white;
    [field: SerializeField] public string UiClass { get; private set; }

    public bool IsPurchasable => !string.IsNullOrEmpty(ProductId);
    public string Title => Loc.T(DisplayName);

    public Sprite Preview => Category switch
    {
        CosmeticCategory.Ball => Idle,
        CosmeticCategory.Paddle => PaddleSprite,
        _ => null
    };
}
