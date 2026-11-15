using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pingi/Shop Content", fileName = "ShopContent")]
public sealed class ShopContent : ScriptableObject
{
    [field: Header("Sets and seasons")]
    [field: SerializeField] public List<CosmeticSet> Sets { get; private set; } = new List<CosmeticSet>();
    [field: SerializeField] public List<SeasonDef> Seasons { get; private set; } = new List<SeasonDef>();

    [field: Header("Showcase")]
    [field: SerializeField] public int ShowcaseSize { get; private set; } = 3;
    [field: SerializeField, Range(0f, 0.9f)] public float AdDiscount { get; private set; } = 0.3f;

    [field: Header("Surprise box")]
    [field: SerializeField] public int BoxPrice { get; private set; } = 500;
    [field: SerializeField] public int BoxCommon { get; private set; } = 70;
    [field: SerializeField] public int BoxRare { get; private set; } = 25;
    [field: SerializeField] public int BoxEpic { get; private set; } = 5;
}
