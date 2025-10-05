using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class slider : MonoBehaviour {
    [Range(1f, 10f)]
    public float speed = 5f;
    [Range(1f, 10f)]
    public float boundaryLimit = 7f;
    public GameObject upperPaddle;
    public GameObject lowerPaddle;
    private float halfScreenHeight;
    public float racketHalfWidth;
    void Awake() 
    {
        halfScreenHeight = Screen.height / 2f;
        SetBoundaryLimit();
    }

    void Update() 
    {
        HandlePlayerMovement();
    }

    void HandlePlayerMovement() {
        for (int i = 0; i < Input.touchCount; i++) {
            Touch touch = Input.GetTouch(i);
            Vector2 touchWorldPosition = Camera.main.ScreenToWorldPoint(touch.position);

            if (touch.position.y > halfScreenHeight) {
                MovePaddle(upperPaddle, touchWorldPosition);
            } else if (touch.position.y < halfScreenHeight) {
                MovePaddle(lowerPaddle, touchWorldPosition);
            }
        }
    }

    void MovePaddle(GameObject paddle, Vector2 touchWorldPosition)
{
    // Lock the Y position of the paddle
    Vector2 targetPosition = new Vector2(
        Mathf.Clamp(touchWorldPosition.x, -boundaryLimit, boundaryLimit),
        paddle.transform.position.y
    );

    // Directly set the paddle position for snappy movement
    paddle.transform.position = targetPosition;
}
    public IEnumerator smallRacket (float duration)
    {
        yield return new WaitForSeconds(duration);
        upperPaddle.transform.localScale = new Vector3(1, 0.25f, 1);
        lowerPaddle.transform.localScale = new Vector3(1, 0.25f, 1);
    }
    void SetBoundaryLimit()
    {
        // Convert screen width to world units
        float screenHalfWidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        boundaryLimit = screenHalfWidth; // Use half the screen width as the boundary
        boundaryLimit = boundaryLimit - racketHalfWidth;
    }
}
