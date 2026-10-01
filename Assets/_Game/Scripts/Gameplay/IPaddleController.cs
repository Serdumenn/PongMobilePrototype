using UnityEngine;

public interface IPaddleController
{
    bool TryGetTarget(Paddle paddle, Camera camera, out float axisPosition);
}
