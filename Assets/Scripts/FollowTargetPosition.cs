using UnityEngine;

// Hace que este objeto siga la POSICION de otro (no su rotacion ni su escala).
//
// Lo usa el emisor de luciernagas que acompaña a Onji: asi hay luciernagas en
// cualquier parte del mapa sin llenar todo el mapa de particulas. Como las
// particulas viven en "World", las que ya nacieron se quedan donde estaban y
// las nuevas nacen alrededor de donde esta Onji ahora.
public class FollowTargetPosition : MonoBehaviour
{
    [Tooltip("A quien seguir (PlayerRoot). Si queda vacio, este objeto no se mueve.")]
    public Transform target;

    [Tooltip("Desplazamiento respecto del objetivo, en metros. Y = altura sobre sus pies.")]
    public Vector3 offset = new Vector3(0f, 1.3f, 0f);

    void LateUpdate()
    {
        // LateUpdate corre despues de que Onji ya se movio en este frame.
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
