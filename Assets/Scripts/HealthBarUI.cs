using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour {
    public Health target;
    public Slider slider;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    [Header("Mostrar solo al danar (estilo AC/Ghost)")]
    [Tooltip("Si esta activo, la barra arranca oculta y aparece recien cuando el enemigo recibe dano. Se re-oculta tras unos segundos sin golpes.")]
    public bool ocultarHastaDanar = true;

    [Tooltip("Segundos visible tras el ultimo golpe antes de volver a ocultarse.")]
    public float tiempoVisible = 4f;

    Camera _cam; Transform _t;
    CanvasGroup _grupo;
    float _ocultarEn;

    void Awake() {
        _cam = Camera.main;
        _t = transform;

        // Un CanvasGroup nos deja desvanecer toda la barra de una.
        // Si no hay, lo agregamos solo.
        if (slider) {
            _grupo = slider.GetComponent<CanvasGroup>();
            if (!_grupo) _grupo = slider.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void OnEnable() {
        if (!target || !slider) return;
        slider.minValue = 0f;
        slider.maxValue = target.maxHp;
        slider.value   = target.maxHp;

        // Arranca oculta si corresponde: enemigo a full vida y en paz.
        if (_grupo) _grupo.alpha = ocultarHastaDanar ? 0f : 1f;

        target.OnHealthChanged += HandleChanged;
        HandleChanged(target.Current, target.maxHp);
    }

    void OnDisable() {
        if (target) target.OnHealthChanged -= HandleChanged;
    }

    void LateUpdate() {
        if (!target) return;
        _t.position = target.transform.position + worldOffset;
        if (_cam) _t.forward = (_t.position - _cam.transform.position).normalized;

        // Desvanecer la barra tras unos segundos sin recibir golpes.
        if (_grupo && ocultarHastaDanar && _grupo.alpha > 0f && Time.time >= _ocultarEn)
            _grupo.alpha = Mathf.MoveTowards(_grupo.alpha, 0f, Time.deltaTime * 2f);
    }

    void HandleChanged(int hp, int max) {
        bool recibioDano = hp < slider.value; // bajo la vida respecto de antes

        slider.maxValue = max;
        slider.value = hp;
        slider.gameObject.SetActive(hp > 0);

        // Al recibir dano: mostrar la barra y reiniciar el temporizador.
        if (_grupo && ocultarHastaDanar && recibioDano) {
            _grupo.alpha = 1f;
            _ocultarEn = Time.time + tiempoVisible;
        }
    }
}