using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DamageFlash : MonoBehaviour {
    public Health target;
    public Color flashColor = new Color(1f, 0.3f, 0.3f);
    public float duration = 0.08f;

    Renderer _r;
    Color _orig;
    Material _mat;

    void Awake() {
        _r = GetComponent<Renderer>();
        _mat = _r.material; // instancia
        _orig = _mat.color;
    }

    void OnEnable() {
        if (target) target.OnHealthChanged += OnChanged;
    }
    void OnDisable() {
        if (target) target.OnHealthChanged -= OnChanged;
    }

    void OnChanged(int hp, int max) {
        StopAllCoroutines();
        StartCoroutine(Flash());
    }

    IEnumerator Flash() {
        _mat.color = flashColor;
        yield return new WaitForSeconds(duration);
        _mat.color = _orig;
    }
}