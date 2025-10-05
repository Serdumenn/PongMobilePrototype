using UnityEngine;

public class textAnim : MonoBehaviour {
    public float giggleAmount = 5f;
    public float giggleSpeed = 10f;
    private Vector3 originalPosition;
    private float timeOffset;
    
    void Start()
    {
        originalPosition = transform.position;
        timeOffset = Random.Range(0f, 100f);
    }
    void Update()
    {
        float xOffset = Mathf.Sin(Time.time * giggleSpeed + timeOffset) * giggleAmount;
        transform.position = originalPosition + new Vector3(xOffset, 0f, 0f);
    }
}
