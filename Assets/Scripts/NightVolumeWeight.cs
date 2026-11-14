using UnityEngine;
using UnityEngine.Rendering;

// Prende y apaga un Volume de post-procesado segun la hora de TimeOfDay.
//
// De noche (time bajo) el Volume pesa 1: se ve el bloom y el color de noche.
// Hacia el amanecer el peso baja a 0 y queda solo el Global Volume de siempre.
// Es [ExecuteAlways] para que, al mover "time" en el editor, la Game view
// muestre lo mismo que se va a ver jugando.
[ExecuteAlways]
[RequireComponent(typeof(Volume))]
public class NightVolumeWeight : MonoBehaviour
{
    [Tooltip("El TimeOfDay de la escena. Si queda vacio lo busca solo.")]
    public TimeOfDay timeOfDay;

    [Tooltip("Hasta esta hora el efecto de noche esta al maximo.")]
    [Range(0f, 1f)] public float fullUntil = 0.5f;

    [Tooltip("Desde esta hora el efecto de noche esta apagado del todo.")]
    [Range(0f, 1f)] public float offFrom = 0.95f;

    [Tooltip("Fuerza maxima del efecto. Bajalo si la noche queda muy cargada.")]
    [Range(0f, 1f)] public float maxWeight = 1f;

    Volume volume;

    void OnEnable()
    {
        volume = GetComponent<Volume>();
        if (timeOfDay == null) timeOfDay = FindFirstObjectByType<TimeOfDay>();
        Aplicar();
    }

    void Update()
    {
        Aplicar();
    }

    void Aplicar()
    {
        if (volume == null || timeOfDay == null) return;

        // Ordenamos los dos limites por si los deslizadores quedaron cruzados.
        float desde = Mathf.Min(fullUntil, offFrom);
        float hasta = Mathf.Max(fullUntil, offFrom);

        // "apagado" va de 0 (antes de 'desde') a 1 (despues de 'hasta').
        // Si los dos limites coinciden es un interruptor: prendido antes, apagado despues.
        float apagado;
        if (hasta - desde < 0.0001f) apagado = timeOfDay.time >= hasta ? 1f : 0f;
        else apagado = Mathf.InverseLerp(desde, hasta, timeOfDay.time);
        float peso = maxWeight * (1f - apagado);

        if (!Mathf.Approximately(volume.weight, peso)) volume.weight = peso;
    }
}
