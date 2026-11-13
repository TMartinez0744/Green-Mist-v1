using System.Collections.Generic;
using UnityEngine;

// Hitbox del arma de Onji. Se enciende por ventana de animacion
// (AttackHitboxWindow decide cuando) y mientras esta encendida golpea
// a todo enemigo que tenga adentro.
public class WeaponHitbox : MonoBehaviour {
    [Header("Dano")]
    public int damage = 10;
    public string enemyTag = "Enemy";

    [Header("Texto flotante (opcional)")]
    public GameObject floatingTextPrefab;

    [Header("Hit stop")]
    [Tooltip("Segundos que se congela el tiempo al conectar un golpe. 0 = desactivado.")]
    public float hitStop = 0.06f;

    Collider col;

    // Enemigos ya golpeados en ESTA ventana. Se limpia cada vez que la
    // ventana abre, asi cada golpe pega una sola vez por enemigo pero
    // el proximo golpe puede volver a pegarle.
    readonly HashSet<Health> yaGolpeados = new HashSet<Health>();

    void Awake() {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false; // se enciende al atacar
    }

    public void EnableHitbox() {
        col.enabled = true;
        yaGolpeados.Clear(); // nueva ventana => todos pueden recibir de nuevo
    }

    public void DisableHitbox() {
        col.enabled = false;
    }

    // OnTriggerStay, no Enter: el hitbox se prende a mitad del golpe, asi
    // que el enemigo casi siempre YA esta adentro del volumen cuando abre
    // la ventana. Con Enter no se registraba nada salvo que te acercaras
    // justo durante los pocos frames de la ventana (el "hay que pegar en
    // un lugar justo"). Stay chequea cada frame de fisica.
    void OnTriggerStay(Collider other) {
        if (!col.enabled) return;

        // El tag "Enemy" esta en el prefab padre, pero el collider que
        // tocamos suele ser un hijo. Por eso subimos al root.
        Transform root = other.transform.root;
        if (!root.CompareTag(enemyTag)) return;

        Health h = root.GetComponent<Health>();
        if (h == null) h = root.GetComponentInChildren<Health>();
        if (h == null) return;

        // Ya le pegamos en esta ventana: no repetir.
        if (yaGolpeados.Contains(h)) return;
        yaGolpeados.Add(h);

        h.Take(damage);

        // Texto flotante, knockback y hit stop SOLO cuando confirmamos
        // que le pegamos a un enemigo de verdad. Antes esto estaba fuera
        // del filtro y saltaba "-10" contra arboles y paredes.
        SpawnDamageText(root, h);

        var kb = root.GetComponent<SimpleKnockback>();
        if (kb == null) kb = root.GetComponentInChildren<SimpleKnockback>();
        if (kb != null) kb.ApplyKnockback(transform.position);

        if (hitStop > 0f) HitStop.Freeze(hitStop);
    }

    void SpawnDamageText(Transform root, Health h) {
        if (!floatingTextPrefab) return;

        Vector3 pos = root.position + Vector3.up * 2.5f;
        var go = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
        go.name = $"DmgText_{root.name}";
        go.GetComponent<FloatingDamage>()?.Setup(damage, Color.red);
    }
}
