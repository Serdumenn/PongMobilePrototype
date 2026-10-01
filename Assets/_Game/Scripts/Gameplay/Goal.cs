using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Goal : MonoBehaviour
{
    [SerializeField] private FieldSide PlaySide = FieldSide.Bottom;

    private Collider2D area;

    public FieldSide Side => PlaySide;
    public bool IsOpen => Area.isTrigger;

    private Collider2D Area => area != null ? area : area = GetComponent<Collider2D>();

    public void SetOpen(bool open)
    {
        Area.isTrigger = open;
    }
}
