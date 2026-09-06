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
    public float freezeDelay = 0.8f; // tiempo para que se vea la anim

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
        yield return new WaitForSecondsRealtime(freezeDelay);

        if (gameOverPanel) gameOverPanel.SetActive(true);

        // pausar el juego (UI sigue funcionando)
        Time.timeScale = 0f;
    }

    // ===== BOTONES (tienen que ser public void sin parámetros) =====

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;

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
