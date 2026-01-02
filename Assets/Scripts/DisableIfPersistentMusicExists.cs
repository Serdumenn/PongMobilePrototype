using UnityEngine;

public class DisableIfPersistentMusicExists : MonoBehaviour
{
    AudioSource _src;

    void Awake()
    {
        _src = GetComponent<AudioSource>();

        if (PersistentMusic.Instance != null &&
            PersistentMusic.Instance.MusicSource != null &&
            _src != null &&
            _src != PersistentMusic.Instance.MusicSource)
        {
            _src.Stop();
            _src.enabled = false;
        }
    }
}