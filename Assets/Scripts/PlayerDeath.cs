using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("Refs")]
    public Health health;
    public Animator animator;

    [Header("Animator")]
    public string dieTrigger = "Die";

    [Header("UI")]
    public GameObject gameOverPanel;
    public GameObject mainMenuPanel;

    bool dead;

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!animator) animator = GetComponentInChildren<Animator>();
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

        // reproducir anim aunque después pauses
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetTrigger(dieTrigger);

        Invoke(nameof(ShowGameOver), 0.8f);
    }

    void ShowGameOver()
    {
        Time.timeScale = 0f;
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // === BOTONES ===

    public void MainMenu()
    {
        Time.timeScale = 1f;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
