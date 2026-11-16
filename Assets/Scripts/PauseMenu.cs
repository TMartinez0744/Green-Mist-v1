using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;

    [Header("Cuando NO se puede pausar (lo conecta Green Mist > 8. Interfaz)")]
    [Tooltip("Panel del menu principal. Mientras esta a la vista, Esc no abre la pausa. Vacio = no se chequea.")]
    public GameObject mainMenuPanel;

    [Tooltip("Panel de game over. Mientras esta a la vista, Esc no abre la pausa. Vacio = no se chequea.")]
    public GameObject gameOverPanel;

    [Tooltip("La vida de Onji. Si llego a 0 (esta muriendo), Esc no abre la pausa. Vacio = no se chequea.")]
    public Health playerHealth;

    [Header("SFX")]
    public AudioSource uiSource;
    public AudioClip paperSfx;

    [Range(0.8f, 1.2f)] public float openPitch = 1.0f;
    [Range(0.8f, 1.2f)] public float closePitch = 0.95f;

    [Header("Paper trim")]
    [Tooltip("Segundo desde donde empieza el sonido real (saltea el silencio)")]
    public float paperStartTime = 1.0f;

    bool isPaused;
    bool seVioGameOver;   // el cartel de game over ya aparecio (termino la secuencia de muerte)
    bool volviendoAlMenu; // ya se eligio MAIN MENU: la escena se recarga en el proximo frame

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
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
            else if (PuedePausar()) Pause();
        }

        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) seVioGameOver = true;

        // La pausa manda. Si apretas Esc justo durante un hit stop (el golpe
        // congela el tiempo 0.06 s y despues lo vuelve a poner en 1), el juego
        // seguia corriendo con el menu de pausa abierto. Esto lo vuelve a frenar.
        if (isPaused && Time.timeScale != 0f) Time.timeScale = 0f;
    }

    // Esc no abre la pausa encima del menu principal ni del game over: antes se
    // podia, y al "reanudar" el juego arrancaba solo detras del menu.
    // Tampoco mientras Onji se esta muriendo (desde que la vida llega a 0 hasta
    // que aparece el cartel de game over).
    bool PuedePausar()
    {
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy) return false;
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return false;
        if (playerHealth != null && playerHealth.Current <= 0 && !seVioGameOver) return false;

        // Ni durante el fundido a negro de PLAY (la pantalla todavia esta aclarando).
        // Ese fundido vive en FadeCanvas, que NO se destruye al recargar la escena:
        // si se pudiera pausar ahi y salir al menu principal, quedaria clavado a
        // medias, oscureciendo la pantalla y tapando los clics para siempre.
        if (ScreenFader.I != null)
        {
            CanvasGroup fundido = ScreenFader.I.GetComponent<CanvasGroup>();
            if (fundido != null && (fundido.blocksRaycasts || fundido.alpha > 0.01f)) return false;
        }

        return true;
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);

        PlayPaper(openPitch);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);

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

    // ===== BOTONES (tienen que ser public void sin parametros) =====

    // Boton MAIN MENU. Vuelve al menu principal RECARGANDO la escena, igual
    // que el Retry de PlayerDeath: la escena siempre arranca en el menu
    // principal (MainMenuUI.Start), y asi Onji, los enemigos y el mundo
    // vuelven a cero en vez de quedar por la mitad detras del menu.
    //
    // A proposito NO apaga el panel de pausa ni pone Time.timeScale en 1:
    // Unity recarga la escena recien en el frame siguiente, y si no ese
    // ultimo frame mostraba el juego andando sin menu (y escondia el cursor)
    // mientras cargaba. Asi queda la pausa a la vista hasta que aparece el
    // menu principal. El panel se destruye solo con la recarga y
    // MainMenuUI.Start deja el tiempo en 0, como en cada arranque.
    public void GoToMainMenu()
    {
        if (volviendoAlMenu) return;   // ya se pidio: no recargar dos veces (clic + Enter a la vez)
        volviendoAlMenu = true;

        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Boton QUIT. Los botones de la escena buscan un metodo con este nombre.
    public void Quit()
    {
        QuitGame();
    }

    public void QuitGame()
    {
        // En el juego compilado (el .exe) cierra la ventana.
        Application.Quit();

        // Dentro del editor Application.Quit no hace nada, asi que ahi
        // cortamos el Play para que se note que el boton funciona.
        #if UNITY_EDITOR
        isPaused = false;
        Time.timeScale = 1f; // igual que PlayerDeath.Quit: no dejar el tiempo frenado al salir de Play
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
