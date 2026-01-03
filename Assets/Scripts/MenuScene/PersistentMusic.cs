using UnityEngine;

public class PersistentMusic : MonoBehaviour
{
    public static PersistentMusic Instance { get; private set; }
    public AudioSource MusicSource { get; private set; }

    [Header("Behavior")]
    public bool dontDestroyOnLoad = true;
    public bool playIfNotPlaying = true;

    void Awake()
    {
        MusicSource = GetComponent<AudioSource>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (MusicSource != null && playIfNotPlaying && !MusicSource.isPlaying)
            MusicSource.Play();
    }
}