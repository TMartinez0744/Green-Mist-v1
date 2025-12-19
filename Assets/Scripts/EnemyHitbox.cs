using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    public int damage = 10;
    public string playerTag = "Player";

    Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false;
    }

    public void EnableHitbox()  => col.enabled = true;
    public void DisableHitbox() => col.enabled = false;

    void OnTriggerEnter(Collider other)
    {
        if (!col.enabled) return;

        // Subimos al root del objeto que tocamos
        var root = other.transform.root;

        // Filtramos por tag en el ROOT (no en el collider hijo)
        if (!root.CompareTag(playerTag)) return;

        // Buscamos Health en el root o en hijos (PlayerRoot lo tiene)
        var h = root.GetComponent<Health>();
        if (h == null) h = root.GetComponentInChildren<Health>();

        if (h != null) h.Take(damage);
    }
}