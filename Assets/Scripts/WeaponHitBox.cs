using UnityEngine;

public class WeaponHitbox : MonoBehaviour {
    public int damage = 10;
    public GameObject floatingTextPrefab;
    Collider col;

    void Awake() {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false; // se enciende al atacar
    }

    public void EnableHitbox()  { col.enabled = true;  Debug.Log("HITBOX ON"); }
    public void DisableHitbox() { col.enabled = false; Debug.Log("HITBOX OFF"); }

    void OnTriggerEnter(Collider other) {
        if (!col.enabled) return; // por si algo lo deja prendido fuera de tiempo

        if (other.CompareTag("Enemy")) {
            var h = other.GetComponent<Health>();
            if (h != null) {
                h.Take(damage);
                Debug.Log($"HIT a {other.name} por {damage}. HP restante: {h.Current}/{h.maxHp}");
            } else {
                Debug.Log("Enemy sin Health");
            }
        } 
        
       
        

if (floatingTextPrefab) {
    var pos = other.bounds.center + Vector3.up * 2.5f;
    var go  = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
    go.name = $"DmgText_{other.name}";

    // grande para debug (después bajalo a 0.03–0.05)
    go.transform.localScale = Vector3.one * 0.20f;

    // que dibuje por encima
    var mr = go.GetComponent<MeshRenderer>();
    if (mr) mr.sortingOrder = 5000;

    // por si el prefab quedó con valores raros
    var tmp = go.GetComponent<TMPro.TextMeshPro>();
    if (tmp) { tmp.text = "-" + damage; tmp.fontSize = 8; tmp.color = Color.red; }

    go.GetComponent<FloatingDamage>()?.Setup(damage, Color.red);
    Debug.Log($"💥 DmgText @ {pos} scale={go.transform.localScale}");
}


        var kb = other.GetComponent<SimpleKnockback>();
        if (kb != null)
            kb.ApplyKnockback(transform.position);
    }
}