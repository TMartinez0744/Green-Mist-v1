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

    [Header("Sonido de impacto")]
    [Tooltip("AudioSource propio del golpe (el hijo 'GM_Audio_Impacto' de PlayerRoot). Lo pone el menu Green Mist > 7. Sonido.")]
    public AudioSource impactSource;

    [Tooltip("Variantes del golpe del baston (3 o 4 queda bien). Se elige una al azar sin repetir la anterior. Vacio = no suena nada.")]
    public AudioClip[] impactClips;

    [Tooltip("Volumen del golpe del baston cuando conecta.")]
    [Range(0f, 1f)] public float impactVolume = 0.85f;

    [Tooltip("Cuanto cambia el tono entre golpe y golpe. 0.08 = hasta un 8% mas agudo o mas grave.")]
    [Range(0f, 0.3f)] public float impactPitchVariation = 0.08f;

    int ultimoImpacto = -1;           // para no repetir el mismo clip dos veces seguidas
    float momentoUltimoImpacto = -1f; // para no apilar dos sonidos en el mismo instante

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

        // Sonido del golpe, apenas se confirma y ANTES del hit stop de abajo.
        PlayImpact();

        // Texto flotante, knockback y hit stop SOLO cuando confirmamos
        // que le pegamos a un enemigo de verdad. Antes esto estaba fuera
        // del filtro y saltaba "-10" contra arboles y paredes.
        SpawnDamageText(root, h);

        var kb = root.GetComponent<SimpleKnockback>();
        if (kb == null) kb = root.GetComponentInChildren<SimpleKnockback>();
        if (kb != null) kb.ApplyKnockback(transform.position);

        if (hitStop > 0f) HitStop.Freeze(hitStop);
    }

    // Suena una variante al azar del golpe. Si falta la fuente o no hay clips, no hace nada.
    void PlayImpact() {
        if (impactSource == null || !impactSource.isActiveAndEnabled) return;
        if (impactClips == null || impactClips.Length == 0) return;

        // Si el mismo golpe agarra a dos enemigos a la vez, que suene uno solo:
        // dos sonidos iguales pegados suenan al doble de fuerte.
        // Tiempo real (unscaled) porque el hit stop congela el tiempo del juego.
        if (Time.unscaledTime - momentoUltimoImpacto < 0.05f) return;
        momentoUltimoImpacto = Time.unscaledTime;

        // Elegir un clip al azar que no sea el mismo de recien.
        int cantidad = impactClips.Length;
        int elegido;
        if (cantidad == 1) {
            elegido = 0;
        } else if (ultimoImpacto < 0 || ultimoImpacto >= cantidad) {
            elegido = Random.Range(0, cantidad);
        } else {
            // Sorteamos entre los demas: uno menos, y salteamos el anterior.
            elegido = Random.Range(0, cantidad - 1);
            if (elegido >= ultimoImpacto) elegido++;
        }
        ultimoImpacto = elegido;

        AudioClip clip = impactClips[elegido];
        if (clip == null) return;

        impactSource.pitch = 1f + Random.Range(-impactPitchVariation, impactPitchVariation);
        impactSource.PlayOneShot(clip, impactVolume * Random.Range(0.9f, 1f));
    }

    void SpawnDamageText(Transform root, Health h) {
        if (!floatingTextPrefab) return;

        Vector3 pos = root.position + Vector3.up * 2.5f;
        var go = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
        go.name = $"DmgText_{root.name}";
        go.GetComponent<FloatingDamage>()?.Setup(damage, Color.red);
    }
}
