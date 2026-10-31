using UnityEngine;

public sealed class ObstacleBody : MonoBehaviour
{
    private FieldObstacles owner;
    private int index;

    public void Setup(FieldObstacles obstacles, int slot)
    {
        owner = obstacles;
        index = slot;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (owner != null) owner.Touched(index, collision);
    }
}
