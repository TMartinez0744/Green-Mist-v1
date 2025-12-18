using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;

    [Header("SFX")]
    public AudioSource uiSource;
    public AudioClip paperSfx;

    [Range(0.8f, 1.2f)] public float openPitch = 1.0f;
    [Range(0.8f, 1.2f)] public float closePitch = 0.95f;

    [Header("Paper trim")]
    [Tooltip("Segundo desde donde empieza el sonido real (saltea el silencio)")]
    public float paperStartTime = 1.0f;

    bool isPaused;

    void Start()
    {
        pausePanel.SetActive(false);
        isPaused = false;

        // seguridad
        if (uiSource)
        {
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f; // 2D UI sound
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);

        PlayPaper(openPitch);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);

        PlayPaper(closePitch);
    }

    void PlayPaper(float pitch)
    {
        if (!uiSource || !paperSfx) return;

        uiSource.Stop();
        uiSource.pitch = pitch;
        uiSource.clip = paperSfx;

        //recorte del primer segundo 
        uiSource.time = Mathf.Clamp(
            paperStartTime,
            0f,
            paperSfx.length - 0.01f
        );

        uiSource.Play();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}