using UnityEngine;

public sealed class TargetTrigger : MonoBehaviour
{
    private TargetBoard owner;
    private int index;

    public void Setup(TargetBoard board, int slot)
    {
        owner = board;
        index = slot;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.Touched(index, other);
    }
}
