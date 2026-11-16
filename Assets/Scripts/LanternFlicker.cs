using UnityEngine;

// Hace titilar las luces de un farol o brasero como si fueran fuego.
//
// Va en la RAIZ del prefab: busca solo todas las Light de sus hijos
// (incluidas las Point Lights extra agregadas en la escena).
// Solo cambia la intensidad, nunca el color, asi respeta la temperatura de color.
// A proposito NO es [ExecuteAlways]: en modo edicion no toca nada.
[DisallowMultipleComponent]
public class LanternFlicker : MonoBehaviour
{
    [Header("Titileo")]
    [Tooltip("Cuanto se aleja de la intensidad original. 0.3 = sube y baja hasta un 30%.")]
    [Range(0f, 1f)] public float amount = 0.3f;

    [Tooltip("Velocidad de la oscilacion lenta (la llama que respira).")]
    public float speed = 2.2f;

    [Tooltip("Cuanto pesa el chisporroteo rapido. 0 = llama calma, 1 = muy nerviosa.")]
    [Range(0f, 1f)] public float crackle = 0.25f;

    [Tooltip("Velocidad del chisporroteo rapido.")]
    public float crackleSpeed = 8f;

    Light[] lights;          // todas las luces de este farol
    float[] baseIntensity;   // la intensidad original de cada una
    float seed;              // desfase propio, para que no titilen todos a la vez

    void Awake()
    {
        // true = incluye las que estan en GameObjects apagados; las guardamos
        // por si se prenden despues, pero mientras esten apagadas no las tocamos.
        lights = GetComponentsInChildren<Light>(true);

        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseIntensity[i] = lights[i].intensity;

        seed = Random.value * 1000f;
    }

    void Update()
    {
        // Tiempo REAL (unscaled): la llama sigue titilando aunque el juego tenga
        // el tiempo frenado. El menu de inicio corre con Time.timeScale en 0 y
        // con Time.time los faroles quedaban congelados, como en una foto.
        // (Por lo mismo tambien titilan detras de la pausa y durante el hit stop.)
        float t = Time.unscaledTime;

        // PerlinNoise devuelve un valor suave entre 0 y 1.
        float lento = Mathf.PerlinNoise(seed, t * speed);
        float rapido = Mathf.PerlinNoise(seed + 37.7f, t * crackleSpeed);
        float ruido = Mathf.Lerp(lento, rapido, crackle);

        // El Perlin de Unity casi nunca llega a 0 ni a 1: se mueve entre 0.2 y 0.75
        // (promedio ~0.47). Lo estiramos para que la onda use todo el rango -1..1.
        float onda = Mathf.Clamp((ruido - 0.47f) * 3f, -1f, 1f);

        // Factor alrededor de 1 (con amount 0.3: entre 0.7 y 1.3).
        float factor = Mathf.Max(0f, 1f + onda * amount);

        for (int i = 0; i < lights.Length; i++)
        {
            Light luz = lights[i];
            if (luz == null || !luz.enabled || !luz.gameObject.activeInHierarchy) continue;
            luz.intensity = baseIntensity[i] * factor;
        }
    }

    void OnDisable()
    {
        // Al apagar el componente, cada luz vuelve a su intensidad original.
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensity[i];
    }
}
