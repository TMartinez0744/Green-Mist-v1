using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    [Header("Refs")]
    public Health health;
    public Animator animator;

    [Header("Animator")]
    public string dieTrigger = "Die";

    [Header("Timing")]
    [Tooltip("Cuanto se ve la animacion de morir antes de empezar a fundir a negro.")]
    public float verAnimacion = 1.6f;

    [Tooltip("Cuanto tarda el fundido a negro, estilo Dark Souls.")]
    public float fundidoNegro = 1.5f;

    [Tooltip("Pausa en negro total antes de mostrar DEFEAT.")]
    public float esperaEnNegro = 0.6f;

    [Header("UI")]
    public GameObject gameOverPanel;   // DeathMenu / GameOverPanel
    public GameObject mainMenuPanel;   // MainMenuPanel (en la misma escena)

    [Header("Disable On Death")]
    public MonoBehaviour[] disableOnDeath;
    public CharacterController characterController;

    bool dead;

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!characterController) characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (health) health.OnDied += HandleDied;
    }

    void OnDisable()
    {
        if (health) health.OnDied -= HandleDied;
    }

    void HandleDied()
    {
        if (dead) return;
        dead = true;

        // cortar control
        if (disableOnDeath != null)
            foreach (var b in disableOnDeath)
                if (b) b.enabled = false;

        if (characterController) characterController.enabled = false;

        // disparar anim
        if (animator)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime; // clave si después pausás
            animator.ResetTrigger(dieTrigger);
            animator.SetTrigger(dieTrigger);
        }

        StartCoroutine(DeathFlow());
    }

    IEnumerator DeathFlow()
    {
        // 1. Dejar que se vea caer. El juego sigue corriendo: la camara
        //    queda sobre el cuerpo, los enemigos se quedan ahi.
        yield return new WaitForSecondsRealtime(verAnimacion);

        // 2. Fundido lento a negro, estilo Dark Souls. El fader corre en
        //    tiempo real, asi que no importa si despues pausamos.
        if (ScreenFader.I != null)
            yield return ScreenFader.I.FadeTo(1f, fundidoNegro);
        else
            yield return new WaitForSecondsRealtime(fundidoNegro);

        // 3. Un respiro en negro total antes del texto.
        yield return new WaitForSecondsRealtime(esperaEnNegro);

        // 4. Recien ahora aparece DEFEAT, ya sobre la pantalla negra.
        if (gameOverPanel) gameOverPanel.SetActive(true);

        // Pausar el juego. La UI sigue andando con unscaled time.
        Time.timeScale = 0f;
    }

    // ===== BOTONES (tienen que ser public void sin parámetros) =====

    public void Retry()
    {
        Time.timeScale = 1f;
        // Sacamos el negro antes de recargar, si no la escena nueva
        // arranca a oscuras.
        if (ScreenFader.I != null) ScreenFader.I.FadeTo(0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        if (ScreenFader.I != null) ScreenFader.I.FadeTo(0f);

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);

        // opcional: si querés que no quede el player “vivo” atrás
        // gameObject.SetActive(false);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
