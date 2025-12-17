using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlayMenuMusic();
    }

    void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void PlayMenuMusic() => PlayClip(menuMusic);
    public void PlayGameplayMusic() => PlayClip(gameplayMusic);
    public void PlayBossMusic() => PlayClip(bossMusic);

    public void SetVolume(float v)
    {
        audioSource.volume = v;
    }
}
