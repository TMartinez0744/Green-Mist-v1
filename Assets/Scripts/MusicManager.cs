using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Pistas")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;

    [Header("Transicion")]
    [Tooltip("Segundos que tarda en cruzar de una pista a la otra.")]
    public float crossfadeDuration = 2.5f;

    // Dos fuentes para poder solapar las pistas: una baja mientras la otra sube.
    // La segunda se crea sola en Awake, no hay que agregarla en el Inspector.
    AudioSource actual;
    AudioSource reserva;

    // El volumen que eligio el jugador con el slider. Se guarda aparte porque
    // durante el cruce el volumen de cada fuente lo maneja la corrutina.
    float volumenJugador = 1f;
    Coroutine cruce;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        actual = GetComponentInChildren<AudioSource>();
        if (actual == null) actual = gameObject.AddComponent<AudioSource>();

        volumenJugador = actual.volume;
        actual.playOnAwake = false;
        actual.loop = true;

        reserva = actual.gameObject.AddComponent<AudioSource>();
        reserva.outputAudioMixerGroup = actual.outputAudioMixerGroup;
        reserva.spatialBlend = actual.spatialBlend;
        reserva.priority = actual.priority;
        reserva.playOnAwake = false;
        reserva.loop = true;
        reserva.volume = 0f;
    }

    void Start()
    {
        PlayMenuMusic();
    }

    public void PlayMenuMusic()     => Play(menuMusic);
    public void PlayGameplayMusic() => Play(gameplayMusic);
    public void PlayBossMusic()     => Play(bossMusic);

    public void Play(AudioClip clip)
    {
        if (clip == null || actual == null) return;

        // Ya esta sonando esa pista: no reiniciarla.
        // (MainMenuUI pide la musica del menu en cada recarga de escena.)
        if (actual.clip == clip && actual.isPlaying) return;

        if (cruce != null) StopCoroutine(cruce);

        // Todavia no sonaba nada: entra directo, sin cruce.
        if (!actual.isPlaying)
        {
            actual.clip = clip;
            actual.volume = volumenJugador;
            actual.Play();
            cruce = null;
            return;
        }

        cruce = StartCoroutine(Cruzar(clip));
    }

    IEnumerator Cruzar(AudioClip clip)
    {
        reserva.clip = clip;
        reserva.volume = 0f;
        reserva.Play();

        float t = 0f;
        float desde = actual.volume;

        while (t < crossfadeDuration)
        {
            // unscaledDeltaTime: en el menu Time.timeScale esta en 0.
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / crossfadeDuration);
            actual.volume  = Mathf.Lerp(desde, 0f, k);
            reserva.volume = Mathf.Lerp(0f, volumenJugador, k);
            yield return null;
        }

        actual.Stop();
        actual.clip = null;
        actual.volume = volumenJugador;
        reserva.volume = volumenJugador;

        // La reserva pasa a ser la fuente activa.
        AudioSource tmp = actual;
        actual = reserva;
        reserva = tmp;

        cruce = null;
    }

    public void SetVolume(float v)
    {
        volumenJugador = Mathf.Clamp01(v);

        // Si hay un cruce en marcha, los volumenes los maneja la corrutina.
        if (cruce == null && actual != null) actual.volume = volumenJugador;
    }

    public float GetVolume() => volumenJugador;
}
