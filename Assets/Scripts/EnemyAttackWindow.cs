using UnityEngine;

public class EnemyAttackWindow : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public EnemyHitbox hitbox;

    [Header("State name (Layer 0)")]
    public string attackState = "Attack"; // el nombre del state en el Animator

    [Header("Hit window")]
    [Range(0f, 1f)] public float start = 0.35f;
    [Range(0f, 1f)] public float end   = 0.55f;

    bool _on;

    void Reset()
    {
        animator = GetComponent<Animator>();
        hitbox = GetComponentInChildren<EnemyHitbox>();
    }

    void Update()
    {
        if (!animator || !hitbox) return;

        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(attackState))
        {
            if (_on) { _on = false; hitbox.DisableHitbox(); }
            return;
        }

        float t = info.normalizedTime % 1f;
        bool should = t >= Mathf.Min(start, end) && t <= Mathf.Max(start, end);

        if (should != _on)
        {
            _on = should;
            if (_on) hitbox.EnableHitbox();
            else     hitbox.DisableHitbox();
        }
    }
}