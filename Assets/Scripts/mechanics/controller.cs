using UnityEngine;
using UnityEngine.UI;

public class controller : MonoBehaviour
{
    public Button left, right;
    public bool direction;   // true = left, false = right (mevcut UI event’lerin bozulmaması için bıraktım)
    public bool moving;
    public float speed = 5f;
    public float limit = 7f;

    private float moveDir;   // -1, 0, +1

    void OnEnable()
    {
        moving = false;
        moveDir = 0f;
    }

    void Update()
    {
        if (!moving || Mathf.Approximately(moveDir, 0f))
            return;

        float delta = moveDir * speed * Time.deltaTime;
        float clampedX = Mathf.Clamp(transform.position.x + delta, -limit, limit);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    public void goLeft()
    {
        moving = true;
        direction = true;
        moveDir = -1f;
    }

    public void goRight()
    {
        moving = true;
        direction = false;
        moveDir = 1f;
    }

    public void stop()
    {
        moving = false;
        moveDir = 0f;
    }
}
