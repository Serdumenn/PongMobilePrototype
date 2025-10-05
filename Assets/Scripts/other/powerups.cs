using UnityEngine;

public class powerups : MonoBehaviour {
    public enum PowerUpType { Damage, Racket, Life, Split }
    public PowerUpType powerUpType;
    private score Score;
    private slider Slider;
    private GameObject ball;
    private ball ballcs;
    [Header ("Damage Boost")]
    public int damageBoost;
    [Header ("Longer Racket")]
    public float biggerRacketDuration;
    public float expandAmount;
    [Header ("Life")]
    public int extraLiveAmount;
    [Header ("Split")]
    public int cloneCount;
    void Awake ()
    {
        ball = GameObject.FindWithTag("ball");
        Score = ball.GetComponent<score>();
        Slider = ball.GetComponent<slider>();
        ballcs = ball.GetComponent<ball>();
    }
    public void ActivatePowerUp(bool racket) 
    {
        switch (powerUpType) 
        {
            case PowerUpType.Damage:
                damageIncrease(racket);
                break;
            case PowerUpType.Racket:
                biggerRacket(racket);
                break;
            case PowerUpType.Life:
                extraLife(racket);
                break;
            case PowerUpType.Split:
                split(racket);
                break;
        }
    }
    public void damageIncrease (bool racket)
    {
        //Score.extraDamage(racket, damageBoost);
    }
    public void biggerRacket (bool racket)
    {
        //Slider.biggerRacket(racket, biggerRacketDuration, expandAmount);
    }
    public void extraLife (bool racket)
    {
        //Score.getLife(racket, extraLiveAmount);
    }
    public void split (bool racket)
    {
        //ballcs.splitBall(cloneCount, racket);
    }
}
