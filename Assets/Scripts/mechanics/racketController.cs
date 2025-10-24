using UnityEngine;

public class racketController : MonoBehaviour
{
    public float speed = 10f;
    public float rotationFactor = 5f;
    public float smoothRotation = 5f;
    public float racketLength = 2f;

    private Vector3 lastPosition;
    private float velocityX;
    private bool isTouching = false;
    private float screenLimit;

    void Start()
    {
        lastPosition = transform.position;
        CalculateLimit();
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 0));

            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                isTouching = true;

                float clampedX = Mathf.Clamp(touchPos.x, -screenLimit, screenLimit);
                transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isTouching = false;
            }
        }

        if (isTouching)
        {
            velocityX = (transform.position.x - lastPosition.x) / Time.deltaTime;

            float targetRotation = -velocityX * rotationFactor;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetRotation), Time.deltaTime * smoothRotation);

            lastPosition = transform.position;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * smoothRotation);
        }
    }

    void CalculateLimit()
    {
        float screenHalfWidth = Camera.main.orthographicSize * Screen.width / Screen.height;
        screenLimit = screenHalfWidth - (racketLength / 2);
    }
}