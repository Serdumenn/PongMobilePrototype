using UnityEngine;

public enum FieldSide
{
    Bottom,
    Top,
    Left,
    Right
}

public static class FieldSideExtensions
{
    public static Vector2 Inward(this FieldSide side)
    {
        return side switch
        {
            FieldSide.Top => Vector2.down,
            FieldSide.Left => Vector2.right,
            FieldSide.Right => Vector2.left,
            _ => Vector2.up
        };
    }

    public static bool MovesHorizontally(this FieldSide side)
    {
        return side == FieldSide.Bottom || side == FieldSide.Top;
    }

    public static Vector2 FromBottomFrame(this FieldSide side, Vector2 direction)
    {
        return side switch
        {
            FieldSide.Top => new Vector2(direction.x, -direction.y),
            FieldSide.Left => new Vector2(direction.y, direction.x),
            FieldSide.Right => new Vector2(-direction.y, direction.x),
            _ => direction
        };
    }
}
