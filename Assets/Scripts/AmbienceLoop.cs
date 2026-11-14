using UnityEngine;

// Un sonido de ambiente en loop (los grillos, el agua del estanque) que solo
// se escucha JUGANDO: en el menu principal y en la pantalla de game over se calla.
//
// Lo unico que hace es mover el volumen del AudioSource:
//   - En menu principal / game over: baja a 0 y frena el loop.
//   - Jugando: sube hasta maxVolume.
//   - En PAUSA (Esc): NO se corta, queda mas bajito (pauseVolume). El mundo sigue
//     ahi atras del menu de pausa. Pone pauseVolume en 0 si lo queres en silencio.
//   - Si followTimeOfDay esta tildado (grillos): ademas baja hacia el amanecer.
//
// Los fundidos usan tiempo REAL (unscaledDeltaTime): el juego frena el tiempo en
// los menus y 0.06 s en cada golpe, y con el tiempo del juego el fundido se
// trabaria o haria "pop".
//
// Para el agua, la distancia la resuelve el propio AudioSource (3D, Min/Max Distance).
// Mientras el AudioSource no tenga clip no hace nada (ni errores).
// A proposito NO es [ExecuteAlways]: en modo edicion no toca nada.
[DisallowMultipleComponent]
public class AmbienceLoop : MonoBehaviour
{
    [Header("Referencias (las pone el menu Green Mist > 7. Sonido)")]
    [Tooltip("El AudioSource de ESTE objeto, con el clip del loop. Su Volume lo maneja este script.")]
    public AudioSource source;

    [Tooltip("Panel del menu principal. Mientras esta a la vista, silencio.")]
    public GameObject mainMenuPanel;

    [Tooltip("Panel de game over. Mientras esta a la vista, silencio.")]
    public GameObject gameOverPanel;

    [Header("Volumen")]
    [Tooltip("Volumen jugando (y de noche cerrada, si sigue la hora).")]
    [Range(0f, 1f)] public float maxVolume = 0.35f;

    [Tooltip("Segundos que tarda en aparecer o desaparecer. Va en tiempo real, no se frena con la pausa.")]
    [Range(0.05f, 5f)] public float fadeTime = 1.5f;

    [Tooltip("Cuanto queda sonando con el juego en pausa. 1 = igual que jugando, 0 = silencio.")]
    [Range(0f, 1f)] public float pauseVolume = 0.35f;

    [Header("Hora del dia (para los grillos)")]
    [Tooltip("Tildado: el volumen depende de TimeOfDay.time. Destildado: suena igual a cualquier hora.")]
    public bool followTimeOfDay = false;

    [Tooltip("El TimeOfDay de la escena. Si queda vacio lo busca solo.")]
    public TimeOfDay timeOfDay;

    [Tooltip("Hasta esta hora (0 = noche cerrada) suena al maximo.")]
    [Range(0f, 1f)] public float fullUntil = 0.5f;

    [Tooltip("Desde esta hora (1 = amanecer) ya llego al volumen de amanecer.")]
    [Range(0f, 1f)] public float offFrom = 0.95f;

    [Tooltip("Cuanto queda al amanecer, como parte de maxVolume. 0 = se callan del todo.")]
    [Range(0f, 1f)] public float dawnVolume = 0f;

    // El juego pone timeScale en 0 tanto en la pausa como 0.06 s en cada golpe.
    // Si lleva congelado mas que esto, es una pausa de verdad.
    const float PausaMinima = 0.25f;

    float compuerta;         // 0 = cerrado (silencio), 1 = abierto. Es lo que se funde.
    float tiempoCongelado;   // segundos reales seguidos con timeScale en 0

    void Awake()
    {
        if (followTimeOfDay && timeOfDay == null) timeOfDay = FindFirstObjectByType<TimeOfDay>();

        // Arranca siempre callado: la escena abre en el menu principal.
        compuerta = 0f;
        if (source != null)
        {
            source.loop = true;
            source.volume = 0f;
            if (source.isPlaying) source.Stop();
        }
    }

    void Update()
    {
        // Sin fuente o sin clip (todavia no bajaste los archivos): nada que hacer.
        if (source == null || source.clip == null || !source.isActiveAndEnabled) return;

        // Tiempo real, con tope: despues de un tiron (cargar la escena) no pega un salto.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        if (Time.timeScale == 0f) tiempoCongelado += dt;
        else tiempoCongelado = 0f;

        // A donde tiene que ir la compuerta.
        float objetivo = 1f;
        if (EnMenu()) objetivo = 0f;
        else if (tiempoCongelado > PausaMinima) objetivo = pauseVolume;

        compuerta = Mathf.MoveTowards(compuerta, objetivo, dt / Mathf.Max(fadeTime, 0.01f));

        float volumen = maxVolume * compuerta * FactorHora();
        source.volume = volumen;

        // Con volumen 0 frenamos el loop para no gastar una voz de audio de gusto.
        if (volumen > 0.0005f)
        {
            if (!source.isPlaying) Arrancar();
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }

    void OnDisable()
    {
        compuerta = 0f;
        tiempoCongelado = 0f;
        if (source != null)
        {
            source.volume = 0f;
            source.Stop();
        }
    }

    bool EnMenu()
    {
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy) return true;
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return true;
        return false;
    }

    // 1 de noche, bajando hasta dawnVolume hacia el amanecer. Si no sigue la hora, siempre 1.
    float FactorHora()
    {
        if (!followTimeOfDay || timeOfDay == null) return 1f;

        // InverseLerp da 0 antes de fullUntil y 1 despues de offFrom.
        float amanecer = Mathf.InverseLerp(fullUntil, offFrom, timeOfDay.time);
        return Mathf.Lerp(1f, dawnVolume, amanecer);
    }

    void Arrancar()
    {
        source.Play();

        // Empieza en un punto al azar del loop, asi no arranca siempre igual.
        // No hace "click" porque en este momento el volumen es casi 0.
        int muestras = source.clip.samples;
        if (muestras > 1) source.timeSamples = Random.Range(0, muestras - 1);
    }
}
