using System;
using UnityEngine;

public class Health : MonoBehaviour {
    public int maxHp = 100;
    public bool destroyOnDeath = true;

    public event Action<int,int> OnHealthChanged;
    public event Action OnDied;

    public int Current => _hp;
    int _hp;

    void Awake() {
        _hp = maxHp;
        OnHealthChanged?.Invoke(_hp, maxHp); // <-- importante: inicializa la UI
    }

    public void Take(int dmg) {
        if (dmg <= 0 || _hp <= 0) return;
        _hp = Mathf.Max(_hp - dmg, 0);
        OnHealthChanged?.Invoke(_hp, maxHp);
        if (_hp == 0) {
            OnDied?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
            else enabled = false;
        }
    }
}