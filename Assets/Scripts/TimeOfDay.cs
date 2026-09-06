using UnityEngine;
using UnityEngine.Rendering;

// Un solo valor (time) maneja sol, niebla y ambiente.
// 0 = noche cerrada, 1 = amanecer.
[ExecuteAlways]
public class TimeOfDay : MonoBehaviour
{
    [Header("Hora")]
    [Range(0f, 1f)] public float time = 0f;

    [Header("Sol / luna")]
    public Light sun;
    public Color sunColorNight = new Color(0.55f, 0.62f, 0.85f);
    public Color sunColorDawn  = new Color(1f, 0.85f, 0.70f);
    public float sunIntensityNight = 0.35f;
    public float sunIntensityDawn  = 1.10f;
    public float sunElevationNight = 35f;   // luna alta: luz pareja y suave
    public float sunElevationDawn  = 6f;    // sol bajo: sombras largas
    public float sunAzimuthNight = -30f;
    public float sunAzimuthDawn  = 95f;

    [Header("Niebla")]
    public bool controlFog = true;
    public Color fogColorNight = new Color(0.09f, 0.13f, 0.13f);
    public Color fogColorDawn  = new Color(0.62f, 0.60f, 0.58f);
    public float fogStartNight = 15f;
    public float fogStartDawn  = 40f;
    public float fogEndNight = 160f;   // niebla cerrada
    public float fogEndDawn  = 420f;   // se abre al amanecer

    [Header("Ambiente")]
    public bool controlAmbient = true;
    public Color ambientSkyNight     = new Color(0.10f, 0.13f, 0.18f);
    public Color ambientSkyDawn      = new Color(0.45f, 0.45f, 0.50f);
    public Color ambientEquatorNight = new Color(0.07f, 0.10f, 0.11f);
    public Color ambientEquatorDawn  = new Color(0.35f, 0.33f, 0.32f);
    public Color ambientGroundNight  = new Color(0.03f, 0.04f, 0.04f);
    public Color ambientGroundDawn   = new Color(0.15f, 0.14f, 0.13f);

    float _applied = -1f;

    void OnEnable()  { _applied = -1f; Apply(); }
    void OnValidate() { _applied = -1f; }

    void Update()
    {
        // Solo reaplica si cambió, para no ensuciar la escena en cada frame del editor.
        if (Mathf.Approximately(time, _applied)) return;
        Apply();
    }

    public void Apply()
    {
        float t = time;

        if (sun)
        {
            sun.color     = Color.Lerp(sunColorNight, sunColorDawn, t);
            sun.intensity = Mathf.Lerp(sunIntensityNight, sunIntensityDawn, t);
            sun.transform.rotation = Quaternion.Euler(
                Mathf.Lerp(sunElevationNight, sunElevationDawn, t),
                Mathf.Lerp(sunAzimuthNight,   sunAzimuthDawn,   t),
                0f);
        }

        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(fogColorNight, fogColorDawn, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(fogStartNight, fogStartDawn, t);
            RenderSettings.fogEndDistance   = Mathf.Lerp(fogEndNight,   fogEndDawn,   t);
        }

        if (controlAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = Color.Lerp(ambientSkyNight,     ambientSkyDawn,     t);
            RenderSettings.ambientEquatorColor = Color.Lerp(ambientEquatorNight, ambientEquatorDawn, t);
            RenderSettings.ambientGroundColor  = Color.Lerp(ambientGroundNight,  ambientGroundDawn,  t);
        }

        _applied = t;
    }
}
