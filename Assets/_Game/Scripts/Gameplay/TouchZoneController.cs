using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TouchZoneController : IPaddleController
{
    private readonly FieldSide side;
    private readonly Func<Vector2, bool> zone;
    private readonly bool lockFinger;
    private int lockedTouch = -1;

    public TouchZoneController(FieldSide side, float zoneRatio)
        : this(side, p => InZone(side, zoneRatio, new Vector2(Screen.width, Screen.height), p), false)
    {
    }

    public TouchZoneController(FieldSide side, Func<Vector2, bool> zone, bool lockFinger)
    {
        this.side = side;
        this.zone = zone;
        this.lockFinger = lockFinger;
    }

    public bool TryGetTarget(Paddle paddle, Camera camera, out float axisPosition)
    {
        axisPosition = 0f;
        if (camera == null) return false;
        if (!TryGetPress(out Vector2 screen)) return false;

        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
        axisPosition = side.MovesHorizontally() ? world.x : world.y;
        return true;
    }

    public static bool InZone(FieldSide side, float ratio, Vector2 screenSize, Vector2 position)
    {
        return side switch
        {
            FieldSide.Top => position.y > screenSize.y * (1f - ratio),
            FieldSide.Left => position.x < screenSize.x * ratio,
            FieldSide.Right => position.x > screenSize.x * (1f - ratio),
            _ => position.y < screenSize.y * ratio
        };
    }

    public static FieldSide NearestEdge(Rect court, Vector2 position)
    {
        float bottom = position.y - court.yMin;
        float top = court.yMax - position.y;
        float left = position.x - court.xMin;
        float right = court.xMax - position.x;

        FieldSide nearest = FieldSide.Bottom;
        float best = bottom;
        if (top < best) { best = top; nearest = FieldSide.Top; }
        if (left < best) { best = left; nearest = FieldSide.Left; }
        if (right < best) nearest = FieldSide.Right;
        return nearest;
    }

    private bool TryGetPress(out Vector2 position)
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            if (lockFinger && TryLocked(touchscreen, out position)) return true;

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed) continue;

                position = touch.position.ReadValue();
                if (!lockFinger)
                {
                    if (zone(position)) return true;
                    continue;
                }

                if (!zone(touch.startPosition.ReadValue())) continue;

                lockedTouch = touch.touchId.ReadValue();
                return true;
            }
        }

        lockedTouch = -1;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            position = mouse.position.ReadValue();
            if (zone(position)) return true;
        }

        position = default;
        return false;
    }

    private bool TryLocked(Touchscreen touchscreen, out Vector2 position)
    {
        position = default;
        if (lockedTouch < 0) return false;

        foreach (var touch in touchscreen.touches)
        {
            if (!touch.press.isPressed || touch.touchId.ReadValue() != lockedTouch) continue;

            position = touch.position.ReadValue();
            return true;
        }

        lockedTouch = -1;
        return false;
    }
}
