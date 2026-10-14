using UnityEngine;

public sealed class ShieldWall : MonoBehaviour
{
    public PowerUps Owner { get; set; }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Owner != null && collision.collider.GetComponentInParent<Ball>() != null) Owner.ShieldHit();
    }
}
