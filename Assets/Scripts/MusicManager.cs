using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;

    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponentInChildren<AudioSource>();
    }

    void Start()
    {
        PlayMenuMusic(); // <- como tu versión de ayer
    }

    public void PlayMenuMusic()     => PlayClip(menuMusic);
    public void PlayGameplayMusic() => PlayClip(gameplayMusic);
    public void PlayBossMusic()     => PlayClip(bossMusic);

    void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        // si ya es el mismo clip Y está sonando, no hagas nada
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void SetVolume(float v)
    {
        if (audioSource) audioSource.volume = v;
    }
}