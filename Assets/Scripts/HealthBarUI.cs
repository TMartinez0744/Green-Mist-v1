using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour {
    public Health target;
    public Slider slider;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    Camera _cam; Transform _t;

    void Awake() { _cam = Camera.main; _t = transform; }

    void OnEnable() {
        if (!target || !slider) return;
        // init visible (si no, queda en 0 y no ves nada)
        slider.minValue = 0f;
        slider.maxValue = target.maxHp;
        slider.value   = target.maxHp;
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
    }

    void HandleChanged(int hp, int max) {
        slider.maxValue = max;
        slider.value = hp;
        slider.gameObject.SetActive(hp > 0);
    }
}