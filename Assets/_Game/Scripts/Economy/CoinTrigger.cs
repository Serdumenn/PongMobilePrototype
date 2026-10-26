using UnityEngine;

public sealed class CoinTrigger : MonoBehaviour
{
    private GoldCoins owner;
    private int index;

    public void Setup(GoldCoins coins, int slot)
    {
        owner = coins;
        index = slot;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.Touched(index, other);
    }
}
