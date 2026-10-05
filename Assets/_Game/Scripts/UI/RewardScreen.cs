using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RewardScreen : UIScreen
{
    private readonly VisualElement art;
    private readonly Label title;
    private readonly Label text;

    public CosmeticItem Item { get; private set; }

    public RewardScreen(VisualElement root, Action<CosmeticItem> onEquip, Action onLater) : base(root)
    {
        art = root.Q("art");
        title = root.Q<Label>("title");
        text = root.Q<Label>("text");

        Bind("equip-button", () => onEquip?.Invoke(Item));
        Bind("later-button", onLater);
    }

    public void Present(CosmeticItem item)
    {
        Item = item;

        title.text = item.Category switch
        {
            CosmeticCategory.Ball => Loc.T("New costume!"),
            CosmeticCategory.Paddle => Loc.T("New paddle!"),
            _ => Loc.T("New theme!")
        };
        text.text = item.Unlock == UnlockKind.Streak
            ? Loc.T("{0} days in a row!\n{1} is unlocked.", item.UnlockValue, item.Title)
            : Loc.T("You reached {0} points.\n{1} is unlocked.", item.UnlockValue, item.Title);

        art.Clear();
        art.EnableInClassList("dialog__art--paddle", item.Category == CosmeticCategory.Paddle);
        art.EnableInClassList("dialog__art--theme", item.Category == CosmeticCategory.Theme);

        Sprite sprite = item.Category == CosmeticCategory.Ball ? item.Happy : item.PaddleSprite;
        art.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
        art.style.backgroundColor = item.Category == CosmeticCategory.Theme ? new StyleColor(item.BackgroundColor) : new StyleColor(StyleKeyword.Null);
        if (item.Category == CosmeticCategory.Theme)
        {
            art.style.borderTopLeftRadius = art.style.borderTopRightRadius = 120;
            art.style.borderBottomLeftRadius = art.style.borderBottomRightRadius = 120;
        }

        Show();
        AudioManager.PlayOne(Sfx.Unlock);
    }
}
