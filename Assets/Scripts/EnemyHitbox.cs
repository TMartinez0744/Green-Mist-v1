using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    public int damage = 10;
    public string playerTag = "Player";

    Collider col;

    // Una vez por ventana de ataque: el hitbox esta prendido varios frames,
    // sin esto le pegaria al jugador cada frame de fisica. Se limpia al abrir.
    Health yaGolpeado;

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false;
    }

    public void EnableHitbox()  { col.enabled = true;  yaGolpeado = null; }
    public void DisableHitbox() { col.enabled = false; }

    // OnTriggerStay, no Enter: el hitbox se prende a mitad del ataque, cuando
    // el jugador YA esta parado enfrente, adentro del volumen. Con Enter no
    // registraba salvo que el jugador entrara justo durante la ventana.
    void OnTriggerStay(Collider other)
    {
        if (!col.enabled) return;

        var root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        var h = root.GetComponent<Health>();
        if (h == null) h = root.GetComponentInChildren<Health>();
        if (h == null) return;

        if (h == yaGolpeado) return; // ya le pegamos en esta ventana
        yaGolpeado = h;

        h.Take(damage);
    }
}