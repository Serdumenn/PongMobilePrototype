using UnityEngine;
using UnityEngine.UI;

public class rallyCount : MonoBehaviour
{
    public int count;
    public Text rallyText;
    public Animator animator;
    public ParticleSystem effect;
    public float minRally, maxRally;
    public float startRate, maxRate;

    void Start()
    {
        resetCount();
    }

    public void changeColor()
    {
        float denom = Mathf.Max(0.0001f, maxRally);
        float t = Mathf.Clamp01((count - minRally) / denom);
        if (rallyText) rallyText.color = Color.Lerp(Color.white, Color.red, t);
    }

    public void AddRally()
    {
        count += 1;
        if (count >= minRally)
        {
            if (rallyText) rallyText.text = "RALLY " + count + "X!";
            if (animator) animator.SetTrigger("count");
            changeColor();

            if (effect)
            {
                var emissionModule = effect.emission;
                float denom = Mathf.Max(0.0001f, maxRally);
                float t = Mathf.Clamp01((count - minRally) / denom);
                float emissionRate = Mathf.Lerp(startRate, maxRate, t);
                emissionModule.rateOverTime = emissionRate;
            }
        }
    }

    public void resetCount()
    {
        if (rallyText) { rallyText.text = ""; rallyText.color = Color.white; }
        count = 0;
        if (effect)
        {
            effect.Clear();
            var emissionModule = effect.emission;
            emissionModule.rateOverTime = startRate;
        }
    }
}