using UnityEngine;

public class hitEffect : MonoBehaviour {
    public Animator animator;
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void OnCollisionEnter2D (Collision2D collision)
    {
        print("hit1");
        if (collision.gameObject.CompareTag("ball"))
        {
            print("hit");
            animator.SetTrigger("hit");
        }
    }
}
