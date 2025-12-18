using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] CanvasGroup fadeCanvas; // CanvasGroup en FadeCanvas
    [SerializeField] float fadeDuration = 0.35f;

    [Header("Scene (opcional)")]
    [SerializeField] string gameSceneName = ""; // dejalo vacío si NO vas a cambiar de escena

    [Header("Audio (opcional)")]
    [SerializeField] MusicManager music;

    void Start()
    {
        if (menuPanel) menuPanel.SetActive(true);
        Time.timeScale = 0f;

        // arranca sin negro
        if (fadeCanvas)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    public void OnStart()
    {
        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        // bloquear clicks
        if (fadeCanvas) fadeCanvas.blocksRaycasts = true;

        // fade a negro
        yield return Fade(1f);

        // reanudar juego
        Time.timeScale = 1f;
        if (menuPanel) menuPanel.SetActive(false);

        // música: si tenés método de crossfade, llamalo acá
        // si no, al menos cambiá a gameplay
        if (music) music.PlayGameplayMusic();

        // si seteaste una escena distinta, cargala
        if (!string.IsNullOrEmpty(gameSceneName) && gameSceneName != SceneManager.GetActiveScene().name)
        {
            SceneManager.LoadScene(gameSceneName);
            yield break;
        }

        // fade desde negro
        yield return Fade(0f);
        if (fadeCanvas) fadeCanvas.blocksRaycasts = false;
    }

    IEnumerator Fade(float target)
    {
        if (!fadeCanvas) yield break;

        float start = fadeCanvas.alpha;
        float t = 0f;

        // ojo: con Time.timeScale=0 usamos unscaledDeltaTime
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            fadeCanvas.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }

        fadeCanvas.alpha = target;
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}
