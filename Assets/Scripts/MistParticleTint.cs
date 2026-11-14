using UnityEngine;

// Le da color y densidad a la niebla verde de particulas.
//
// Va en el objeto "GreenMist_Niebla" (el padre de todos los emisores de niebla).
// Cada frame mira dos cosas:
//   - el color de la niebla de la escena (RenderSettings.fogColor, que maneja TimeOfDay)
//     y lo tira hacia el verde: asi los copos lejanos se funden con la niebla de fondo;
//   - la hora (TimeOfDay.time): hacia el amanecer la niebla se afina.
//
// Solo LEE RenderSettings, nunca lo escribe (eso es cosa de TimeOfDay).
//
// Por que no alcanza con cambiar el color de las particulas: los copos viven ~20 segundos
// y el color de inicio solo afecta a los que nacen. Por eso se cambia el color del
// MATERIAL, que afecta a todos al instante. Para no tocar el archivo .mat del proyecto,
// en Awake cada emisor recibe su copia propia del material (eso hace "renderer.material").
//
// A proposito NO es [ExecuteAlways]: en modo edicion no toca nada. Se prueba en Play.
[DisallowMultipleComponent]
public class MistParticleTint : MonoBehaviour
{
    [Tooltip("El TimeOfDay de la escena. Si queda vacio lo busca solo.")]
    public TimeOfDay timeOfDay;

    [Header("Color")]
    [Tooltip("El verde hacia el que se tira el color de la niebla de la escena.")]
    public Color greenTint = new Color(0.20f, 0.52f, 0.36f);

    [Tooltip("Cuanto se tira hacia el verde. 0 = igual a la niebla de la escena, 1 = el verde puro de arriba.")]
    [Range(0f, 1f)] public float greenPull = 0.55f;

    [Tooltip("Brillo de la niebla. 1 = normal. Subilo si de noche casi no se ve; bajalo si parece que brilla sola. Tiene un tope interno para que nunca dispare el bloom: pasado ese tope, subirlo no cambia nada.")]
    [Range(0.2f, 2.5f)] public float brightness = 1f;

    [Header("Densidad segun la hora")]
    [Tooltip("Densidad de noche. 1 = toda la niebla, 0 = invisible.")]
    [Range(0f, 1f)] public float nightDensity = 1f;

    [Tooltip("Densidad al amanecer (cuando la hora llega a 'thinUntil').")]
    [Range(0f, 1f)] public float dawnDensity = 0.4f;

    [Tooltip("Hasta esta hora la niebla esta con la densidad de noche.")]
    [Range(0f, 1f)] public float thinFrom = 0.5f;

    [Tooltip("Desde esta hora la niebla queda con la densidad de amanecer.")]
    [Range(0f, 1f)] public float thinUntil = 1f;

    // Tope de brillo de la niebla. De noche el bloom tiene el umbral en 0.75, pero su
    // borde suave ya empieza a actuar cerca de 0.55. Con 0.54 la niebla queda siempre
    // por debajo y no se pone a brillar como una luz, por mas que subas "brightness".
    const float MaxChannel = 0.54f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    Material[] materials;   // la copia del material de cada emisor
    float[] baseAlpha;      // el alpha que tenia cada material al empezar
    Color lastColor;        // lo ultimo que se aplico (para no reescribir si no cambio)
    bool applied;

    // El color de la niebla de particulas a partir de la niebla de la escena.
    // Es static para que el menu del editor pueda usar la misma cuenta al crear el material.
    public static Color MixColor(Color sceneFog, Color green, float pull, float bright)
    {
        Color c = Color.Lerp(sceneFog, green, pull) * bright;
        c.r = Mathf.Max(c.r, 0f);
        c.g = Mathf.Max(c.g, 0f);
        c.b = Mathf.Max(c.b, 0f);

        // Si el canal mas brillante pasa del tope, se oscurece el color ENTERO (los tres
        // canales juntos). Recortar cada canal por separado le cambiaria el tono al verde.
        float masBrillante = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        if (masBrillante > MaxChannel)
        {
            float escala = MaxChannel / masBrillante;
            c.r *= escala;
            c.g *= escala;
            c.b *= escala;
        }

        c.a = 1f;
        return c;
    }

    void Awake()
    {
        if (timeOfDay == null) timeOfDay = FindFirstObjectByType<TimeOfDay>();

        // true = incluye emisores que esten apagados, por si se prenden despues.
        ParticleSystemRenderer[] renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);

        materials = new Material[renderers.Length];
        baseAlpha = new float[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material original = renderers[i].sharedMaterial;
            if (original == null || !original.HasProperty(BaseColorId)) continue;

            // ".material" crea una copia solo para este emisor. El .mat del proyecto no se toca.
            materials[i] = renderers[i].material;
            baseAlpha[i] = original.GetColor(BaseColorId).a;
        }
    }

    void OnEnable()
    {
        applied = false;   // fuerza a aplicar el color en el primer Update
    }

    void Update()
    {
        if (materials == null) return;

        // Sin TimeOfDay se comporta como de noche (hora 0).
        float hora = timeOfDay != null ? timeOfDay.time : 0f;

        // Los dos deslizadores se pueden mover por separado y quedar cruzados
        // (thinFrom mayor que thinUntil). Se ordenan aca para que la niebla nunca
        // termine al reves (fina de noche y densa al amanecer).
        float desde = Mathf.Min(thinFrom, thinUntil);
        float hasta = Mathf.Max(thinFrom, thinUntil);

        // 0 = noche, 1 = amanecer.
        float haciaAmanecer;
        if (hasta - desde < 0.0001f)
            haciaAmanecer = hora >= hasta ? 1f : 0f;                 // iguales: cambia de golpe a esa hora
        else
            haciaAmanecer = Mathf.InverseLerp(desde, hasta, hora);   // 0 antes de "desde", 1 despues de "hasta"

        float densidad = Mathf.Lerp(nightDensity, dawnDensity, haciaAmanecer);

        Color color = MixColor(RenderSettings.fogColor, greenTint, greenPull, brightness);
        color.a = densidad;

        // Si nada cambio desde el frame anterior, no hay nada que hacer.
        if (applied && color == lastColor) return;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            Color c = color;
            c.a = baseAlpha[i] * densidad;
            materials[i].SetColor(BaseColorId, c);
        }

        lastColor = color;
        applied = true;
    }

    void OnDestroy()
    {
        // Las copias de material las creamos nosotros: hay que destruirlas a mano.
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
            if (materials[i] != null) Destroy(materials[i]);
    }
}
