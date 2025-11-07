using UnityEngine;

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

    void Update()
    {
        if (lightCd > 0f) lightCd -= Time.deltaTime;
        if (heavyCd > 0f) heavyCd -= Time.deltaTime;

        bool lightPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z);
        bool heavyPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X);
        bool ultPressed   = Input.GetKeyDown(KeyCode.R)  || Input.GetKeyDown(KeyCode.C);

        if (lightPressed && lightCd <= 0f) {
            lightCd = lightCooldown;
            Flash(Color.green);
            animator.CrossFadeInFixedTime(lightState, 0.05f);
        }
        if (heavyPressed && heavyCd <= 0f) {
            heavyCd = heavyCooldown;
            Flash(Color.blue);
            animator.CrossFadeInFixedTime(heavyState, 0.05f);
        }
        if (ultPressed) {
            Flash(Color.red);
            animator.CrossFadeInFixedTime(ultimateState, 0.05f);
        }
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

    public void OnAttackStart() { weaponHitbox?.EnableHitbox(); }
    public void OnAttackEnd()   { weaponHitbox?.DisableHitbox(); }
}