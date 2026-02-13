using UnityEngine;

public class PaddleVisualTilt : MonoBehaviour
{
    [Header("Assign")]
    public Rigidbody2D parentRb;

    [Header("Tilt")]
    public float maxTiltDeg = 18f;
    public float speedToMax = 8f;
    public float smooth = 15f;

    void Reset()
    {
        parentRb = GetComponentInParent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (!parentRb) return;

        float vx = parentRb.linearVelocity.x;
        float t = Mathf.Clamp(vx / Mathf.Max(0.01f, speedToMax), -1f, 1f);
        float targetZ = -t * maxTiltDeg;

        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZ);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, smooth * Time.deltaTime);
    }

    public void ResetTiltInstant()
    {
        transform.localRotation = Quaternion.identity;
    }
}