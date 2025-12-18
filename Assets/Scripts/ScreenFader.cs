using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader I;

    [SerializeField] Image fadeImage;
    [SerializeField] float defaultDuration = 0.6f;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (!fadeImage) fadeImage = GetComponentInChildren<Image>(true);
    }

    public Coroutine FadeTo(float targetAlpha, float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        return StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    IEnumerator FadeRoutine(float target, float dur)
    {
        Color c = fadeImage.color;
        float start = c.a;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, t / dur);
            fadeImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, target);
    }
}
