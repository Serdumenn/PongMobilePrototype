using UnityEngine;

public class PaddleVisualTilt : MonoBehaviour
{
    [Header("Assign")]
    public Transform parentTransform;

    [Header("Tilt")]
    public float maxTiltDeg = 18f;
    public float speedToMax = 8f;
    public float smooth = 15f;

    private float previousX;

    void Reset()
    {
        parentTransform = transform.parent;
    }

    void Start()
    {
        if (parentTransform == null) parentTransform = transform.parent;
        if (parentTransform != null) previousX = parentTransform.position.x;
    }

    void LateUpdate()
    {
        if (parentTransform == null) return;

        // Manually compute horizontal velocity (works with Kinematic bodies)
        float currentX = parentTransform.position.x;
        float vx = (currentX - previousX) / Mathf.Max(Time.deltaTime, 0.0001f);
        previousX = currentX;

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