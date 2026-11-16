using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    bool arrancando;   // ya se apreto PLAY y el fundido esta en marcha

    void Start()
    {
        if (menuPanel) menuPanel.SetActive(true);
        Time.timeScale = 0f;

        // arranca sin negro
        CanvasGroup fundido = BuscarFundido();
        if (fundido)
        {
            fundido.alpha = 0f;
            fundido.blocksRaycasts = false;
            DejarNegroOpaco(fundido);
        }

        // Musica del menu. La primera vez la pone el propio MusicManager; esto es
        // para cuando se VUELVE al menu recargando la escena (Retry o Main Menu):
        // ahi el MusicManager que sigue vivo todavia tiene la musica del juego.
        MusicManager musica = BuscarMusica();
        if (musica) musica.PlayMenuMusic();
    }

    // FadeCanvas y MusicManager NO se destruyen al recargar la escena
    // (DontDestroyOnLoad). Cada recarga trae una copia nueva de cada uno, y esa
    // copia se borra sola porque ya hay uno vivo. Los campos "fadeCanvas" y
    // "music" de arriba apuntan a esas copias: despues de la primera recarga
    // quedan apuntando a algo que ya no existe. Por eso PLAY dejaba de fundir a
    // negro y de cambiar la musica.
    // Estas dos funciones devuelven siempre el que esta VIVO.

    CanvasGroup BuscarFundido()
    {
        // ScreenFader esta en el mismo objeto FadeCanvas, y ScreenFader.I es el que sobrevive.
        if (ScreenFader.I != null)
        {
            CanvasGroup vivo = ScreenFader.I.GetComponent<CanvasGroup>();
            if (vivo != null) return vivo;
        }
        return fadeCanvas;   // por si la escena no tuviera ScreenFader
    }

    MusicManager BuscarMusica()
    {
        if (MusicManager.Instance != null) return MusicManager.Instance;
        return music;
    }

    // El negro del FadeCanvas es un Image hijo. Este script funde con el
    // CanvasGroup (la transparencia de todo el FadeCanvas), asi que ese Image
    // tiene que estar opaco: si alguien lo dejo transparente, el fundido no se
    // veria. Se llama solo con el CanvasGroup en 0, asi no se nota el cambio.
    void DejarNegroOpaco(CanvasGroup fundido)
    {
        Image negro = fundido.GetComponentInChildren<Image>(true);
        if (negro != null && negro.color.a < 1f)
        {
            Color c = negro.color;
            c.a = 1f;
            negro.color = c;
        }
    }

    public void OnStart()
    {
        // Clic y Enter casi juntos: que no arranquen dos fundidos a la vez.
        if (arrancando) return;
        arrancando = true;

        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        CanvasGroup fundido = BuscarFundido();

        // bloquear clicks
        if (fundido)
        {
            if (fundido.alpha <= 0f) DejarNegroOpaco(fundido);
            fundido.blocksRaycasts = true;
        }

        // fade a negro
        yield return Fade(fundido, 1f);

        // reanudar juego
        Time.timeScale = 1f;
        if (menuPanel) menuPanel.SetActive(false);

        // música: si tenés método de crossfade, llamalo acá
        // si no, al menos cambiá a gameplay
        MusicManager musica = BuscarMusica();
        if (musica) musica.PlayGameplayMusic();

        // si seteaste una escena distinta, cargala
        if (!string.IsNullOrEmpty(gameSceneName) && gameSceneName != SceneManager.GetActiveScene().name)
        {
            SceneManager.LoadScene(gameSceneName);
            yield break;
        }

        // fade desde negro
        yield return Fade(fundido, 0f);
        if (fundido) fundido.blocksRaycasts = false;

        arrancando = false;
    }

    IEnumerator Fade(CanvasGroup fundido, float target)
    {
        if (!fundido) yield break;

        float start = fundido.alpha;
        float t = 0f;

        // ojo: con Time.timeScale=0 usamos unscaledDeltaTime
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            fundido.alpha = Mathf.Lerp(start, target, k);
            yield return null;

            if (!fundido) yield break;   // por las dudas: el FadeCanvas dejo de existir
        }

        fundido.alpha = target;
    }

    public void OnQuit()
    {
        // Con PLAY ya apretado (la pantalla se esta yendo a negro) no se sale.
        if (arrancando) return;

        // En el juego compilado (el .exe) cierra la ventana.
        Application.Quit();

        // Dentro del editor Application.Quit no hace nada, asi que ahi cortamos
        // el Play para que se note que la opcion funciona (igual que PauseMenu).
        #if UNITY_EDITOR
        Time.timeScale = 1f; // no dejar el tiempo frenado al salir de Play
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
