using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthHUD : MonoBehaviour
{
    public Health target;
    public Slider slider;

    void Awake()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.GetComponent<Health>();
        }
    }

    void OnEnable()
    {
        if (!target || !slider) return;

        slider.minValue = 0;
        slider.maxValue = target.maxHp;
        slider.value = target.Current;

        target.OnHealthChanged += HandleChanged;
        HandleChanged(target.Current, target.maxHp);
    }

    void OnDisable()
    {
        if (target) target.OnHealthChanged -= HandleChanged;
    }

    void HandleChanged(int hp, int max)
    {
        slider.maxValue = max;
        slider.value = hp;
    }
}