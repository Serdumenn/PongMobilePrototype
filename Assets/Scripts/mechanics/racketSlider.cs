using UnityEngine;

public class racketSlider : MonoBehaviour
{
    public Transform racket; // Raketin transform'u
    public float moveSpeed = 15f; // Hareket hızı
    public float rotationSpeed = 100f; // Rotasyon hızı
    public float minX = -5f, maxX = 5f; // Hareket sınırları
    public float minRotation = -45f, maxRotation = 45f; // Rotasyon sınırları
    public float minHeight = -2f, maxHeight = 2f; // Yükseklik sınırları
    public float heightStep = 0.5f; // Kademe büyüklüğü
    public float heightLerpSpeed = 5f; // Geçiş hızı
    public float touchSensitivity = 10f; // Dokunma hassasiyeti
    public float heightThreshold = 0.5f; // Yükseklik değişim eşiği
    public Joystick rotationJoystick; // Sanal joystick referansı (Eğim için kullanılıyor)
    public Joystick movementJoystick; // Hareket için kullanılıyor
    
    private float targetHeight;
    
    void Start()
    {
        targetHeight = racket.position.y;
    }
    
    void Update()
    {
        HandleMovement();
        HandleRotation();
        //HandleHeight();
    }

    void HandleMovement()
    {
        float horizontalInput = movementJoystick.Horizontal; // Joystick'in X ekseni girdisi
        float verticalInput = movementJoystick.Vertical; // Joystick'in Y ekseni girdisi

        float targetX = Mathf.Lerp(racket.position.x, Mathf.Lerp(minX, maxX, (horizontalInput + 1f) / 2f), Time.deltaTime * moveSpeed);
        racket.position = new Vector3(targetX, racket.position.y, racket.position.z);
    }

    void HandleRotation()
    {
        float rotationInput = rotationJoystick.Vertical; // Joystick X ekseni girdisi
        float currentRotation = racket.localEulerAngles.z;

        if (currentRotation > 180f) currentRotation -= 360f;

        if (Mathf.Abs(rotationInput) < 0.1f) 
        {
            currentRotation = Mathf.Lerp(currentRotation, 0f, Time.deltaTime * rotationSpeed);
        }
        else
        {
            currentRotation += rotationInput * rotationSpeed * Time.deltaTime;
        }

        currentRotation = Mathf.Clamp(currentRotation, minRotation, maxRotation);
        racket.rotation = Quaternion.Euler(0, 0, currentRotation);
    }
    
    void HandleHeight()
    {
        float heightInput = movementJoystick.Vertical; // Joystick'in Y ekseni girdisi

        float stepHeight = (maxHeight - minHeight) / 3f;
        float idleHeight = (maxHeight + minHeight) / 2f;

        if (Mathf.Abs(heightInput) < heightThreshold)
        {
            targetHeight = Mathf.Lerp(targetHeight, idleHeight, Time.deltaTime * heightLerpSpeed);
        }
        else if (heightInput > heightThreshold) 
        {
            if (targetHeight < minHeight + stepHeight) 
                targetHeight = minHeight + stepHeight;
            else if (targetHeight < minHeight + 2 * stepHeight)
                targetHeight = minHeight + 2 * stepHeight;
            else if (targetHeight < maxHeight)
                targetHeight = maxHeight;
        }
        else if (heightInput < -heightThreshold) 
        {
            if (targetHeight > maxHeight - stepHeight)
                targetHeight = maxHeight - stepHeight;
            else if (targetHeight > maxHeight - 2 * stepHeight)
                targetHeight = maxHeight - 2 * stepHeight;
            else if (targetHeight > minHeight)
                targetHeight = minHeight;
        }
        racket.position = new Vector3(racket.position.x, Mathf.Lerp(racket.position.y, targetHeight, Time.deltaTime * heightLerpSpeed), racket.position.z);
    }
}
