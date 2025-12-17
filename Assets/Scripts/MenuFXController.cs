using UnityEngine;

public class MenuFXController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;     // MainMenuPanel
    [SerializeField] private ParticleSystem[] systems; // todos los particle systems (si el prefab tiene varios)

    private bool lastState;

    void Awake()
    {
        if (systems == null || systems.Length == 0)
            systems = GetComponentsInChildren<ParticleSystem>(true);

        lastState = menuPanel != null && menuPanel.activeSelf;
        Apply(lastState, clear: true);
    }

    void Update()
    {
        if (menuPanel == null) return;

        bool now = menuPanel.activeSelf;
        if (now == lastState) return;

        Apply(now, clear: true);
        lastState = now;
    }

    private void Apply(bool show, bool clear)
    {
        gameObject.SetActive(true); // aseguramos que el objeto exista

        foreach (var ps in systems)
        {
            if (ps == null) continue;

            if (show)
            {
                ps.gameObject.SetActive(true);
                ps.Play(true);
            }
            else
            {
                // corta y limpia
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        // Si querés ocultar TODO el MenuFX cuando no hay menú:
        if (!show) gameObject.SetActive(false);
    }
}