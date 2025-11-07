using UnityEngine;

public class AttackHitboxWindow : MonoBehaviour {
    [Header("Refs")]
    public Animator animator;
    public WeaponHitbox weaponHitbox;

    [Header("State names (Layer 0)")]
    public string lightState = "Light";
    public string heavyState = "Heavy";
    public string ultimateState = "Ultimate";

    [System.Serializable]
    public struct Window {
        [Range(0f,1f)] public float start; // tiempo normalizado (0..1)
        [Range(0f,1f)] public float end;   // tiempo normalizado (0..1)
    }

    [Header("Light windows (dos golpes)")]
    public Window lightHit1 = new Window { start = 0.30f, end = 0.44f };
    public Window lightHit2 = new Window { start = 0.58f, end = 0.72f };

    [Header("Heavy window")]
    public Window heavyHit = new Window { start = 0.40f, end = 0.60f };

    [Header("Ultimate window")]
    public Window ultimateHit = new Window { start = 0.45f, end = 0.70f };

    bool _on;

    void Reset() {
        animator = GetComponent<Animator>();
        weaponHitbox = GetComponentInChildren<WeaponHitbox>();
    }

    void Update() {
        var info = animator.GetCurrentAnimatorStateInfo(0);
        float t = info.normalizedTime % 1f;

        bool shouldBeOn = false;

        if (info.IsName(lightState)) {
            shouldBeOn = In(lightHit1, t) || In(lightHit2, t);
        } else if (info.IsName(heavyState)) {
            shouldBeOn = In(heavyHit, t);
        } else if (info.IsName(ultimateState)) {
            shouldBeOn = In(ultimateHit, t);
        }

        if (shouldBeOn != _on) {
            _on = shouldBeOn;
            if (_on) weaponHitbox.EnableHitbox();
            else     weaponHitbox.DisableHitbox();
        }
    }

    bool In(Window w, float t) => t >= Mathf.Min(w.start, w.end) && t <= Mathf.Max(w.start, w.end);
}