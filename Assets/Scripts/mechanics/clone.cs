using UnityEngine;
using System.Collections;

public class clone : MonoBehaviour {
    [Header ("General")]
    public float lifeTime;
    public Vector2 direction;
    public float speed;
    score scoreManager;
    public GameObject ball;
    public bool who;
    void Awake()
    {
        ball = GameObject.FindWithTag("ball");
        scoreManager = ball.transform.GetComponent<score>(); 
        StartCoroutine("Kill");
    }
    public void FixedUpdate ()
    {
        transform.Translate(direction.normalized * speed * Time.fixedDeltaTime);
    }
    void OnCollisionEnter2D (Collision2D collision)
    {
        AudioSource source = transform.GetComponent<AudioSource>();
        if(collision.transform.CompareTag("wall"))
        {
            source.Play();
            direction.x = -direction.x;
        }
        if(collision.transform.CompareTag("racket1"))
        {
            source.Play();
            direction.y = -direction.y;
        }
        if(collision.transform.CompareTag("racket2"))
        {
            source.Play();
            direction.y = -direction.y;
        }
    }
    void OnTriggerEnter2D (Collider2D collider)
    {
        if (who)
        {
            if(collider.transform.CompareTag("roof"))
            {
                scoreManager.ChangeScore(false);
            }
        }
        else
        {
            if(collider.transform.CompareTag("bottom"))
            {
                scoreManager.ChangeScore(true);
            }
        }
    }
    public IEnumerator Kill ()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }
}
