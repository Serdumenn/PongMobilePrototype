using UnityEngine;
using UnityEngine.UI;

public class controller : MonoBehaviour {
    public Button left, right;
    public bool direction;
    public bool moving;
    public float speed;
    public float limit;
    void FixedUpdate()
    {
        if (transform.position.x < limit) transform.position = new Vector3(limit, transform.position.y, 0);
        if (transform.position.x > -limit) transform.position = new Vector3(-limit, transform.position.y, 0);
        if (direction && moving)
        {
            transform.Translate(-Vector3.right * speed * Time.deltaTime);
        }
        if (!direction && moving) 
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
        
    }
    public void goLeft()
    {
        moving = true;
        direction = true;
    }
    public void stop()
    {
        moving = false;
    }
    public void goRight()
    {
        moving = true;
        direction = false;
    }
}
