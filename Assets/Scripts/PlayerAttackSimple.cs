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

    [Header("Arranque")]
    [Tooltip("Ignora el input de ataque justo despues de salir de un menu. Sin esto, el clic con el que apretaste PLAY llega al primer frame de juego y Onji pega solo.")]
    public float ignorarInputAlDespausar = 0.2f;

    float ultimoDespausado;
    float timeScalePrevio = 1f;
    float inicioPausa;
    const float pausaMinimaMenu = 0.15f; // mas largo que cualquier hit stop

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

        // Tambien cubre el arranque de la escena, no solo el despause.
        ultimoDespausado = Time.unscaledTime;
        timeScalePrevio = Time.timeScale;
    }

    void Update()
    {
        if (lightCd > 0f) lightCd -= Time.deltaTime;
        if (heavyCd > 0f) heavyCd -= Time.deltaTime;

        // El jugador existe mientras estas en el menu, asi que el clic con
        // el que apretaste PLAY cae en el primer frame de juego y se lee
        // como un golpe. Tus menus marcan la pausa con timeScale = 0, asi
        // que detectamos el 0 -> 1 y damos un respiro antes de leer input.
        // Registramos cuando empieza cada pausa para medir cuanto duro.
        if (timeScalePrevio > 0f && Time.timeScale == 0f)
            inicioPausa = Time.unscaledTime;

        // Solo cuenta como "salir de un menu" si la pausa fue larga. El hit
        // stop tambien pone timeScale en 0, pero dura 0.06 s: sin este
        // filtro, cada golpe que conectaba bloqueaba el siguiente 0.2 s.
        if (timeScalePrevio == 0f && Time.timeScale > 0f
            && (Time.unscaledTime - inicioPausa) > pausaMinimaMenu)
            ultimoDespausado = Time.unscaledTime;
        timeScalePrevio = Time.timeScale;

        // Dos candados:
        // 1) Con el juego pausado no se lee input. Update() sigue corriendo
        //    con timeScale 0, asi que sin esto el clic del boton PLAY se
        //    leia como golpe estando todavia en el menu.
        // 2) Y un respiro despues de despausar, por el clic que cae justo
        //    en el borde entre menu y juego.
        bool puedeAtacar = Time.timeScale > 0f
                           && (Time.unscaledTime - ultimoDespausado) >= ignorarInputAlDespausar;

        bool lightPressed = puedeAtacar && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z));
        bool heavyPressed = puedeAtacar && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X));
        bool ultPressed   = puedeAtacar && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.C));

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
