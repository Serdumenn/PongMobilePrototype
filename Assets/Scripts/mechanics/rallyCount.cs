using UnityEngine;
using UnityEngine.UI;

public class rallyCount : MonoBehaviour {
    public int count;
    public Text rallyText;
    public Animator animator;
    public ParticleSystem effect;
    public float minRally, maxRally;
    public float startRate, maxRate;

    void Start ()
    {
        resetCount();
    }

    public void changeColor()
    {
        float t = Mathf.Clamp01((count - minRally) / maxRally);
        rallyText.color = Color.Lerp(Color.white, Color.red, t);
    }

    public void AddRally()
    {
        count += 1;
        if (count >= minRally)
        {   
            rallyText.text = ("RALLY " + count + "X!");
            animator.SetTrigger("count");
            changeColor();

            var emissionModule = effect.emission;
            float emissionRate = Mathf.Lerp(startRate, maxRate, (count - minRally) / maxRally);
            emissionModule.rateOverTime = emissionRate;
        }
    }

    public void resetCount()
    {
        rallyText.text = "";
        count = 0;
        rallyText.color = Color.white;
        effect.Clear();
        var emissionModule = effect.emission;
        emissionModule.rateOverTime = startRate;
    }
}
