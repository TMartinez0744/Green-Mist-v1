using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleKnockback : MonoBehaviour {
    public float force = 6f;        // intensidad del empuje
    public float upForce = 2f;      // empuje vertical leve

    Rigidbody rb;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void ApplyKnockback(Vector3 origin) {
        // dirección desde el golpe hacia el dummy
        Vector3 dir = (transform.position - origin).normalized;
        dir.y = 0.4f; // agrega un poco de elevación
        rb.AddForce(dir * force, ForceMode.VelocityChange);
    }
}