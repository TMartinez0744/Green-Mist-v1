using UnityEngine;
using System.Collections.Generic;

// Genera su propia malla circular y la ondula.
//
// El Plane de Unity no servia por dos motivos: es cuadrado (las esquinas
// se asomaban fuera del estanque) y tiene solo 121 vertices (las olas
// salian como esquirlas). Este arma un disco de ~640 vertices.
[RequireComponent(typeof(MeshFilter))]
public class WaterWaves : MonoBehaviour
{
    [Header("Forma")]
    [Tooltip("Radio del agua en metros. El estanque mide 6.5 de radio.")]
    public float radio = 6.5f;

    [Tooltip("Anillos concentricos. Mas = olas mas suaves.")]
    [Range(4, 40)] public int anillos = 20;

    [Tooltip("Divisiones alrededor. Mas = borde mas redondo.")]
    [Range(8, 64)] public int segmentos = 32;

    [Header("Oleaje")]
    [Tooltip("Altura de la ola en metros.")]
    public float amplitud = 0.03f;

    [Tooltip("Que tan rapido se mueve el agua.")]
    public float velocidad = 0.5f;

    [Tooltip("Mas alto = olas mas chicas y juntas.")]
    public float escalaOnda = 1f;

    Mesh mesh;
    Vector3[] baseVerts;
    Vector3[] verts;
    float[] atenuacion;   // 1 en el centro, 0 en la orilla

    void Start()
    {
        ConstruirDisco();
    }

    void ConstruirDisco()
    {
        var puntos = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        puntos.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int r = 1; r <= anillos; r++)
        {
            float rad = radio * r / anillos;
            for (int s = 0; s < segmentos; s++)
            {
                float ang = 2f * Mathf.PI * s / segmentos;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                puntos.Add(new Vector3(x, 0f, z));
                uvs.Add(new Vector2(x / (radio * 2f) + 0.5f, z / (radio * 2f) + 0.5f));
            }
        }

        // Abanico del centro al primer anillo.
        for (int s = 0; s < segmentos; s++)
        {
            int sig = (s + 1) % segmentos;
            tris.Add(0);
            tris.Add(1 + sig);
            tris.Add(1 + s);
        }

        // Anillos siguientes, de a quads partidos en dos triangulos.
        for (int r = 2; r <= anillos; r++)
        {
            int adentro = 1 + (r - 2) * segmentos;
            int afuera = 1 + (r - 1) * segmentos;
            for (int s = 0; s < segmentos; s++)
            {
                int sig = (s + 1) % segmentos;
                tris.Add(adentro + s); tris.Add(adentro + sig); tris.Add(afuera + sig);
                tris.Add(adentro + s); tris.Add(afuera + sig); tris.Add(afuera + s);
            }
        }

        mesh = new Mesh { name = "Agua (disco)" };
        mesh.SetVertices(puntos);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        baseVerts = mesh.vertices;
        verts = new Vector3[baseVerts.Length];

        // La ola se apaga hacia la orilla, asi el borde no se levanta
        // por encima de las piedras del estanque.
        atenuacion = new float[baseVerts.Length];
        for (int i = 0; i < baseVerts.Length; i++)
        {
            float d = new Vector2(baseVerts[i].x, baseVerts[i].z).magnitude / radio;
            atenuacion[i] = 1f - Mathf.SmoothStep(0f, 1f, d);
        }
    }

    void Update()
    {
        if (mesh == null) return;

        float t = Time.time * velocidad;

        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 v = baseVerts[i];

            float ola = Mathf.Sin(v.x * escalaOnda + t)
                      + Mathf.Sin(v.z * escalaOnda * 1.3f + t * 0.8f);

            v.y = ola * amplitud * 0.5f * atenuacion[i];
            verts[i] = v;
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }
}
