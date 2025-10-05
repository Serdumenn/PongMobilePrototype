using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ball : MonoBehaviour {
    [Header("GENERAL SETTINGS")]
	public float startSpeed = 5f;
    public float minSpeed = 5f;
    public float maxSpeed = 20f;
    public float speedIncrease = 0.5f;
    public float spinFactor = 0.2f;
    private Rigidbody2D rb;
    private Vector2 lastVelocity;
	private bool isWaiting;
    [HideInInspector]
    public bool coroutineRunning;
    bool racket;
	[Space(25)]
	[Header("OTHER")]
    [HideInInspector]
	public SpriteRenderer spriteRenderer;
	private score scoreManager;
	private Text startText;
    private GameObject textObject;
    [SerializeField]
	private AudioSource[] sfx;
    private float lastCollisionTime;
    private float collisionCooldown = 0.05f;
    [Header ("ROUND END")]
    private rallyCount rallyscript;
    public GameObject rallyObject;
    public ParticleSystem[] effects;
    public bool waitForTapActive;
    public float roundStarterTime;
    void Start()
    {
        rallyscript = rallyObject.transform.GetComponent<rallyCount>();
        GameObject textObject = GameObject.FindGameObjectWithTag("text");
        startText = textObject.GetComponent<Text>();
        spriteRenderer = transform.GetComponent<SpriteRenderer>();
        scoreManager = transform.GetComponent<score>();
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine("ResetBallWithCountdown");
    }
    void FixedUpdate()
    {
        lastVelocity = rb.linearVelocity; // Stores the last velocity for use in collisions.

        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed); // Limits speed to maxSpeed.

        if (rb.linearVelocity.magnitude < minSpeed) // Ensures minimum speed.
        {
            rb.linearVelocity = rb.linearVelocity.normalized * minSpeed;
        }
    }
    // this CollisionEnter2D checks when ball hit something solid
    void OnCollisionEnter2D (Collision2D collision)
    {
        sfx[1].Play();

        if (Time.time - lastCollisionTime < collisionCooldown) return; // Prevent double reflection
        lastCollisionTime = Time.time;

        if (collision.gameObject.CompareTag("racket")) // If collision with racket.
        {
            rallyscript.AddRally();

            float hitPoint = (transform.position.x - collision.transform.position.x) / collision.collider.bounds.size.x; // Determines impact point.
            float randomFactor = Random.Range(-0.2f, 0.2f); // Adds randomness to prevent looping.
            Vector2 newDirection = new Vector2(hitPoint + randomFactor, 1).normalized; // Sets new direction.
            float angle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg; // Calculates angle.
            angle = Mathf.Clamp(angle, 25f, 155f);
            newDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            rb.linearVelocity = newDirection * (lastVelocity.magnitude + speedIncrease);
            if (rb.linearVelocity.magnitude < minSpeed) // Ensure minimum speed
            {
                rb.linearVelocity = rb.linearVelocity.normalized * minSpeed;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.Reflect(lastVelocity, collision.contacts[0].normal); // Reflects ball on impact.
        }
    }
    // this ColliderEnter2D checks when ball hit something not solid
    void OnTriggerEnter2D (Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Goal1"))
        {
            scoreManager.ChangeScore(true);
            StartCoroutine("ResetBallWithCountdown");
        }
        if (collider.gameObject.CompareTag("Goal2"))
        {
            scoreManager.ChangeScore(false);
            StartCoroutine("ResetBallWithCountdown");
        }
    }
    public IEnumerator ResetBallWithCountdown ()
	{
        rallyscript.resetCount();
        coroutineRunning = true;
		isWaiting = true;

        // disable sprite
		spriteRenderer.enabled = false; 
		transform.position = Vector2.zero; rb.linearVelocity = Vector2.zero;
        
        // ıF wait for tap is active round will start when tapped
        if (waitForTapActive == true)
        {
            startText.text = ("TAP TO START!");
            yield return StartCoroutine(WaitForTap());
        }
        // COUNTDOWN FROM 3 TO 1 THEN GO!
        else
        {
            for (int i = 3; i > 0; i--)
            {
                startText.text = i.ToString();
                yield return new WaitForSeconds((roundStarterTime / 2));
            }

            startText.text = "GO!";
            yield return new WaitForSeconds(roundStarterTime);
        }

        startText.text = ("");
        // enable sprite
		spriteRenderer.enabled = true; 

        // ball pyhsics
        float randomAngle = Random.Range(30f, 90f); // Selects a random angle between 30 and 90 degrees.
        float angleInRadians = randomAngle * Mathf.Deg2Rad;
        float directionX = Mathf.Cos(angleInRadians);
        float directionY = Mathf.Sign(Random.Range(-1f, 1f)) * Mathf.Sin(angleInRadians); // Randomizes Y-axis movement direction.
        Vector2 direction = new Vector2(directionX, directionY).normalized; // Normalized direction vector.
        rb.linearVelocity = direction * startSpeed; // Launches the ball with start speed.

        // make values false
		isWaiting = false;
        coroutineRunning = false;
	}
    // WAIT FOR TAP
    IEnumerator WaitForTap()
    {
        while (Input.touchCount > 0)
        {
            yield return null;
        }

        while (Input.touchCount == 0)
        {
            yield return null;
        }

        while (Input.touchCount > 0)
        {
            yield return null;
        }
    }
}
