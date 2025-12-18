using UnityEngine;
using UnityEngine.Audio;

public class PlayerAttackSimple : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public WeaponHitbox weaponHitbox;
    public Renderer debugRenderer; // opcional

    [Header("Animator state names")]
    public string lightState = "Light";
    public string heavyState = "Heavy";
    public string ultimateState = "Ultimate";

    [Header("Cooldowns")]
    public float lightCooldown = 0.25f;
    public float heavyCooldown = 0.6f;
    float lightCd, heavyCd;

    [Header("SFX")]
    public AudioSource sfxSource;          // AudioSource en PlayerRoot (PlayOnAwake off)
    public AudioClip bambooWhoosh;         // tu mp3
    [Tooltip("Tiempo (seg) a reproducir desde el inicio del clip. Ej: 1.0 para cortar un clip de 6s.")]
    public float whooshDuration = 1.0f;
    [Range(0f, 1f)] public float whooshVolume = 0.8f;

    [Header("Optional: Only play whoosh when equipped")]
    public WeaponSwitcher weaponSwitcher;  // arrastralo si querés chequear equipado

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>(); // si lo ponés en el mismo GO
    }

    void Update()
    {
        if (lightCd > 0f) lightCd -= Time.deltaTime;
        if (heavyCd > 0f) heavyCd -= Time.deltaTime;

        bool lightPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z);
        bool heavyPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X);
        bool ultPressed   = Input.GetKeyDown(KeyCode.R)  || Input.GetKeyDown(KeyCode.C);

        if (lightPressed && lightCd <= 0f)
        {
            lightCd = lightCooldown;
            Flash(Color.green);
            PlayWhoosh();
            animator.CrossFadeInFixedTime(lightState, 0.05f);
        }

        if (heavyPressed && heavyCd <= 0f)
        {
            heavyCd = heavyCooldown;
            Flash(Color.blue);
            PlayWhoosh();
            animator.CrossFadeInFixedTime(heavyState, 0.05f);
        }

        if (ultPressed)
        {
            Flash(Color.red);
            PlayWhoosh();
            animator.CrossFadeInFixedTime(ultimateState, 0.05f);
        }
    }

    void PlayWhoosh()
    {
        if (!sfxSource || !bambooWhoosh) return;

        // opcional: solo si está equipado
        // (si no querés esto, borrá este if)
        if (weaponSwitcher != null)
        {
            // Si tu WeaponSwitcher no expone isEquipped, fijate abajo: te digo cómo agregarlo.
            var equipped = weaponSwitcher.IsEquipped;
            if (!equipped) return;
        }

        // Si no tenés editor para recortar, lo cortamos por código:
        // reproducimos el clip y lo frenamos a whooshDuration.
        sfxSource.Stop();
        sfxSource.clip = bambooWhoosh;
        sfxSource.volume = whooshVolume;
        sfxSource.loop = false;

        double start = AudioSettings.dspTime;
        sfxSource.PlayScheduled(start);
        sfxSource.SetScheduledEndTime(start + whooshDuration);
    }

    void Flash(Color c)
    {
        if (!debugRenderer) return;
        var m = debugRenderer.material;
        var old = m.color;
        m.color = c;
        Invoke(nameof(ResetColor), 0.12f);

        void ResetColor() { m.color = old; }
    }

    // Si usás Animation Events para abrir/cerrar hitbox, estos quedan igual:
    public void OnAttackStart() { weaponHitbox?.EnableHitbox(); }
    public void OnAttackEnd()   { weaponHitbox?.DisableHitbox(); }
}
