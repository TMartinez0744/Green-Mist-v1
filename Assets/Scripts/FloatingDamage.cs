using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingDamage : MonoBehaviour {
    public float lifetime = 1.2f;
    public float floatSpeed = 1.5f;
    public Vector3 randomOffset = new Vector3(0.3f, 0.3f, 0.3f);

    private TextMeshPro text;
    private float timer;

    void Awake() {
        text = GetComponent<TextMeshPro>();
        // pequeño offset aleatorio para que no se superpongan
        transform.localPosition += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(0f,            randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z)
        );
    }

    public void Setup(int dmg, Color color) {
        text.text  = $"-{dmg}";
        text.color = color;
    }

    void Update() {
        // flota hacia arriba
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // mira a la cámara (sin espejar)
        var cam = Camera.main;
        if (cam) transform.forward = cam.transform.forward;

        // vida útil
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }
}
