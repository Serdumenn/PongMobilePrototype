using UnityEngine;

public sealed class PowerToken : MonoBehaviour
{
    public PowerUps Owner { get; set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Owner != null) Owner.Touched(other);
    }
}
