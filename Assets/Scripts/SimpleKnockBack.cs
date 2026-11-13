using UnityEngine;
using UnityEngine.AI;

// Empuje al recibir un golpe.
//
// Antes usaba fuerza fisica sobre un Rigidbody, pero el enemigo se mueve
// con NavMeshAgent. Rigidbody y Agent peleando por la posicion = el
// deslizamiento sobre hielo. Aca empujamos moviendo al Agent sobre el
// NavMesh, asi el Agent sigue mandando siempre y no hay conflicto.
[RequireComponent(typeof(NavMeshAgent))]
public class SimpleKnockback : MonoBehaviour {
    [Tooltip("Cuantos metros retrocede al ser golpeado.")]
    public float distancia = 1.2f;

    [Tooltip("Que tan rapido retrocede.")]
    public float velocidad = 10f;

    [Tooltip("Cuanto queda aturdido sin poder perseguir, en segundos.")]
    public float aturdido = 0.25f;

    NavMeshAgent agent;
    Vector3 objetivo;
    float finEmpuje;
    bool empujando;

    public bool EstaAturdido => Time.time < finEmpuje;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    public void ApplyKnockback(Vector3 origin) {
        Vector3 dir = (transform.position - origin);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        // Clampeamos el destino al NavMesh: si hay una pared o un borde
        // atras, no lo empujamos a traves ni fuera del piso caminable.
        Vector3 deseado = transform.position + dir * distancia;
        if (NavMesh.SamplePosition(deseado, out var hit, distancia + 0.5f, NavMesh.AllAreas))
            objetivo = hit.position;
        else
            objetivo = transform.position;

        finEmpuje = Time.time + aturdido;
        empujando = true;

        // El Agent no persigue mientras lo empujamos.
        if (agent.isOnNavMesh) agent.isStopped = true;
    }

    void Update() {
        if (!empujando) return;

        transform.position = Vector3.MoveTowards(
            transform.position, objetivo, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, objetivo) < 0.05f || !EstaAturdido) {
            empujando = false;
            // Reenganchamos el Agent en la posicion final para que no
            // teletransporte al retomar la persecucion.
            if (agent.isOnNavMesh) {
                agent.Warp(transform.position);
                agent.isStopped = false;
            }
        }
    }
}
