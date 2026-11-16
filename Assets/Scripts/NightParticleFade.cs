using UnityEngine;

// Prende o apaga de a poco un grupo de particulas segun la hora de TimeOfDay.
//
// Va en el objeto padre de los emisores (por ejemplo "GreenMist_Luciernagas").
// Modo normal (luciernagas): de noche (hora baja) se ven enteras; entre "fullUntil"
// y "offFrom" se van transparentando; desde "offFrom" quedan apagadas del todo.
// Con "dayOnly" tildado (petalos) es al reves: apagadas de noche, enteras de dia.
//
// Se apagan bajando el ALPHA del material y no el color: con un material aditivo
// el alpha es lo que decide cuanta luz suma, asi que en 0 desaparecen de verdad.
// Para no tocar el archivo .mat del proyecto, en Awake cada emisor recibe su copia
// propia del material (eso hace "renderer.material").
//
// A proposito NO es [ExecuteAlways]: en modo edicion no toca nada. Se prueba en Play.
[DisallowMultipleComponent]
public class NightParticleFade : MonoBehaviour
{
    [Tooltip("El TimeOfDay de la escena. Si queda vacio lo busca solo.")]
    public TimeOfDay timeOfDay;

    [Tooltip("Destildado = solo de noche (luciernagas). Tildado = solo de dia (petalos): se da vuelta el efecto.")]
    public bool dayOnly = false;

    [Tooltip("Donde EMPIEZA el cambio. Modo noche: hasta esta hora estan al maximo. Modo dia: hasta esta hora estan apagadas.")]
    [Range(0f, 1f)] public float fullUntil = 0.45f;

    [Tooltip("Donde TERMINA el cambio. Modo noche: desde esta hora estan apagadas. Modo dia: desde esta hora estan al maximo.")]
    [Range(0f, 1f)] public float offFrom = 0.75f;

    [Tooltip("Fuerza maxima. Bajalo si de noche brillan demasiado.")]
    [Range(0f, 1f)] public float maxStrength = 1f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    ParticleSystemRenderer[] renderers;
    Material[] materials;   // la copia del material de cada emisor
    Color[] baseColor;      // el color que tenia cada material al empezar
    float lastStrength;     // lo ultimo que se aplico (para no reescribir si no cambio)
    bool applied;

    void Awake()
    {
        if (timeOfDay == null) timeOfDay = FindFirstObjectByType<TimeOfDay>();

        // true = incluye emisores que esten apagados, por si se prenden despues.
        renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);

        materials = new Material[renderers.Length];
        baseColor = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material original = renderers[i].sharedMaterial;
            if (original == null || !original.HasProperty(BaseColorId)) continue;

            // ".material" crea una copia solo para este emisor. El .mat del proyecto no se toca.
            materials[i] = renderers[i].material;
            baseColor[i] = original.GetColor(BaseColorId);
        }
    }

    void OnEnable()
    {
        applied = false;   // fuerza a aplicar en el primer Update
    }

    void Update()
    {
        if (materials == null) return;

        // Sin TimeOfDay se comporta como de noche (hora 0).
        float hora = timeOfDay != null ? timeOfDay.time : 0f;

        // Los dos deslizadores se pueden mover por separado y quedar cruzados
        // (fullUntil mayor que offFrom). Se ordenan aca para que nunca termine
        // al reves (apagadas de noche y prendidas al amanecer).
        float desde = Mathf.Min(fullUntil, offFrom);
        float hasta = Mathf.Max(fullUntil, offFrom);

        // 0 = todavia es de noche, 1 = ya es de dia.
        float apagado;
        if (hasta - desde < 0.0001f)
            apagado = hora >= hasta ? 1f : 0f;                 // iguales: se apagan de golpe a esa hora
        else
            apagado = Mathf.InverseLerp(desde, hasta, hora);   // 0 antes de "desde", 1 despues de "hasta"

        // "apagado" crece hacia el dia: las de noche usan lo contrario, las de dia eso mismo.
        float fuerza = maxStrength * (dayOnly ? apagado : 1f - apagado);

        if (applied && Mathf.Approximately(fuerza, lastStrength)) return;

        Aplicar(fuerza);
        lastStrength = fuerza;
        applied = true;
    }

    void OnDisable()
    {
        // Al apagar el componente, las particulas vuelven a verse enteras.
        if (materials == null) return;
        Aplicar(1f);
    }

    void Aplicar(float fuerza)
    {
        // Con fuerza 0 no se verian igual: apagar el renderer ahorra dibujarlas.
        bool visible = fuerza > 0.001f;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null || renderers[i] == null) continue;

            Color c = baseColor[i];
            c.a *= fuerza;
            materials[i].SetColor(BaseColorId, c);

            if (renderers[i].enabled != visible) renderers[i].enabled = visible;
        }
    }

    void OnDestroy()
    {
        // Las copias de material las creamos nosotros: hay que destruirlas a mano.
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
            if (materials[i] != null) Destroy(materials[i]);
    }
}
