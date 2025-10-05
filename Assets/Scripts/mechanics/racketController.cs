using UnityEngine;

public class racketController : MonoBehaviour
{
    public float speed = 10f;         // Speed of the racket movement
    public float rotationFactor = 5f; // How much it rotates based on speed
    public float smoothRotation = 5f; // Smoothing factor for rotation
    public float racketLength = 2f;   // Raketin uzunluğu

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

                // Dokunulan noktayı sınırlar içinde tut
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
            // Hızı hesapla
            velocityX = (transform.position.x - lastPosition.x) / Time.deltaTime;

            // Rotasyonu hıza göre uygula (Mevcut rotasyon mekaniği korunuyor)
            float targetRotation = -velocityX * rotationFactor;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetRotation), Time.deltaTime * smoothRotation);

            lastPosition = transform.position;
        }
        else
        {
            // Dokunma yoksa rotasyonu sıfırla
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * smoothRotation);
        }
    }

    void CalculateLimit()
    {
        float screenHalfWidth = Camera.main.orthographicSize * Screen.width / Screen.height;
        screenLimit = screenHalfWidth - (racketLength / 2); // Raketin yarısını çıkartarak ekran dışına taşmasını önlüyoruz.
    }
}
