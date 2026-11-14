using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 5. Pétalos de sakura".
//
// Crea (o actualiza) pétalos que caen de la copa de cada sakura y una deriva liviana
// a lo largo del sando. Pocos (290 en total, tope 300), chicos y con desvanecido.
//
// Usan el material de particulas de URP CON LUZ (Particles/Simple Lit, transparente):
// asi los faroles los iluminan calidos y de noche quedan oscuros como el resto de la escena.
// Con el material sin luz (Unlit) brillarian rosa como un cartel de neon.
//
// Todo cuelga de "Assets/GreenMist_Ambiente/GreenMist_Petalos". Los emisores NO son hijos
// de los arboles (que tienen escalas raras): se ubican midiendo la caja de dibujado de cada arbol.
//
// Si lo corres de nuevo, los emisores se rearman con estos valores (y se reubican si moviste un arbol).
public static class GreenMistPetalos
{
    const string NombreGrupo = "GreenMist_Petalos";
    const string Prefijo = "Petalos_";

    // Particulas VIVAS. Total = 180 (repartidas entre los arboles) + 110 = 290 (tope acordado: 300).
    const int CantArbolesEnTotal = 180;
    const int CantSando = 110;

    // Velocidad media de caida (m/s). Se usa para calcular cuanto tiene que vivir un petalo
    // para llegar de la copa al suelo.
    const float CaidaMedia = 0.4f;

    [MenuItem("Green Mist/5. Pétalos de sakura", false, 105)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Transform aldea = GreenMistEditorUtil.Aldea();
        if (aldea == null) return;
        Transform ambiente = GreenMistEditorUtil.ContenedorAmbiente();
        if (ambiente == null) return;

        // ---------- 1) El grupo ----------
        GameObject grupo = GreenMistEditorUtil.BuscarOCrearHijo(ambiente, NombreGrupo);
        Undo.RecordObject(grupo.transform, "Green Mist: pétalos");
        grupo.transform.localPosition = Vector3.zero;      // asi sus hijos usan las coordenadas del pueblo
        grupo.transform.localRotation = Quaternion.identity;
        grupo.transform.localScale = Vector3.one;

        // ---------- 2) Textura y material ----------
        Texture2D textura = GreenMistParticulas.TexturaPetalo();
        if (textura == null) return;

        Material mat = GreenMistParticulas.MaterialParticulas(
            "GM_Petalos",
            true,           // CON luz (Particles/Simple Lit): los faroles y la luna los iluminan
            textura, Color.white,
            false,          // transparencia normal
            true,           // doble cara: los petalos giran y se ven tambien de atras
            0f,             // sin soft particles: son muy chicos
            0.25f, 0.9f,    // se desvanecen si pasan a menos de 25 cm de la camara
            GreenMistParticulas.ColaPetalos);
        if (mat == null) return;

        // ---------- 3) Un emisor en la copa de cada sakura ----------
        List<string> usados = new List<string>();
        Transform padre = grupo.transform;
        int total = 0;

        List<Transform> sakuras = GreenMistParticulas.BuscarSakuras(aldea);
        float pisoMundo = GreenMistParticulas.SueloPlanoMundoY(aldea);
        int arboles = 0;

        if (sakuras.Count > 0)
        {
            // El presupuesto se reparte: con 4 arboles son 45 petalos cada uno.
            // (Si agregas mas sakuras, a cada uno le tocan menos: el total nunca pasa de 180.)
            int porArbol = Mathf.Max(1, CantArbolesEnTotal / sakuras.Count);

            foreach (Transform arbol in sakuras)
            {
                Bounds caja;
                if (!GreenMistParticulas.LimitesDe(arbol, out caja))
                {
                    Debug.LogWarning("[Green Mist] '" + arbol.name + "' no tiene nada que se dibuje: no le puse pétalos.");
                    continue;
                }

                // Altura VISIBLE del arbol: desde el suelo (no desde el pivote, que esta hundido) hasta la punta.
                float suelo = pisoMundo;
                if (Terrain.activeTerrain != null) suelo = Mathf.Max(suelo, GreenMistEditorUtil.AlturaSueloMundo(caja.center));
                float alto = caja.max.y - suelo;
                if (alto < 1.5f)
                {
                    Debug.LogWarning("[Green Mist] '" + arbol.name + "' casi no asoma del suelo: no le puse pétalos.");
                    continue;
                }

                // La copa: mas o menos el 35% de arriba del arbol, y algo mas angosta que su caja.
                Vector3 centroCopa = new Vector3(caja.center.x, suelo + alto * 0.68f, caja.center.z);
                Vector3 tamCopa = new Vector3(
                    Mathf.Max(2f, caja.size.x * 0.65f),
                    Mathf.Max(0.8f, alto * 0.35f),
                    Mathf.Max(2f, caja.size.z * 0.65f));

                // Vida justa para caer de la copa al suelo (si viviera mas, seguiria cayendo bajo tierra).
                float vida = Mathf.Clamp((centroCopa.y - suelo) / CaidaMedia, 6f, 14f);

                ParticleSystem ps = GreenMistParticulas.PrepararEmisor(padre, Prefijo + arbol.name, usados);
                ps.transform.position = centroCopa;
                ConfigurarPetalos(ps, mat, tamCopa, porArbol, vida * 0.8f, vida * 1.1f,
                                  new Vector3(-0.20f, -0.50f, -0.15f), new Vector3(0.20f, -0.30f, 0.15f));
                total += porArbol;
                arboles++;
            }
        }
        if (arboles == 0)
            Debug.LogWarning("[Green Mist] No encontré árboles 'SAKURA-N' (hijos directos de 'Assets'): solo creé la deriva del sando.");

        // ---------- 4) Deriva liviana a lo largo del sando ----------
        // Una caja larga (X 20 a 42, Z 490 a 590) entre 1 y 4.5 m de altura. Viento suave hacia +X.
        ParticleSystem sando = GreenMistParticulas.PrepararEmisor(padre, "Petalos_Sando", usados);
        sando.transform.localPosition = new Vector3(31f, GreenMistParticulas.SueloLocalY + 2.75f, 540f);
        ConfigurarPetalos(sando, mat, new Vector3(22f, 3.5f, 100f), CantSando, 9f, 13f,
                          new Vector3(0.15f, -0.30f, -0.10f), new Vector3(0.45f, -0.12f, 0.10f));
        total += CantSando;

        int borrados = GreenMistParticulas.BorrarSobrantes(padre, Prefijo, usados);
        GreenMistParticulas.VistaPrevia(padre);

        AssetDatabase.SaveAssets();
        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Pétalos listos: " + arboles + " sakuras + la deriva del sando, " + total
                  + " partículas (tope 300) en '" + GreenMistEditorUtil.NombreAmbiente + "/" + NombreGrupo + "'"
                  + (borrados > 0 ? ", " + borrados + " emisores viejos borrados" : "") + ".");
    }

    // Los valores de un emisor de petalos: chicos, que caen despacio girando sobre si mismos.
    static void ConfigurarPetalos(ParticleSystem ps, Material mat, Vector3 caja, int cantidad,
                                  float vidaMin, float vidaMax, Vector3 velMin, Vector3 velMax)
    {
        GreenMistParticulas.ConfigurarBase(ps, cantidad, vidaMin, vidaMax);

        ParticleSystem.MainModule main = ps.main;
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.08f);   // 4.5 a 8 cm

        // Giro inicial al azar en los TRES ejes: asi no son cartelitos que siempre miran a la camara,
        // sino laminitas que a veces se ven de frente y a veces de canto. (Desde codigo va en radianes.)
        float vuelta = Mathf.PI * 2f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, vuelta);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, vuelta);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, vuelta);

        // Rosa palido, apagado (paleta ukiyo-e). La luz de la escena hace el resto.
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.00f, 0.80f, 0.86f, 1f), new Color(0.94f, 0.66f, 0.76f, 1f));

        GreenMistParticulas.FormaCaja(ps, caja);

        // Caida lenta con algo de viento, y un vaiven de hoja que planea.
        GreenMistParticulas.Velocidad(ps, velMin, velMax);
        GreenMistParticulas.Ruido(ps, new Vector3(0.25f, 0.10f, 0.25f), 0.4f, 0.2f);

        // Aparecen y se van de a poco.
        GreenMistParticulas.Desvanecer(ps, 0.1f, 0.8f);

        // Siguen girando despacio mientras caen (entre -70 y 70 grados por segundo en cada eje).
        float giroMax = 70f * Mathf.Deg2Rad;
        ParticleSystem.RotationOverLifetimeModule giro = ps.rotationOverLifetime;
        giro.separateAxes = true;
        giro.x = new ParticleSystem.MinMaxCurve(-giroMax, giroMax);
        giro.y = new ParticleSystem.MinMaxCurve(-giroMax, giroMax);
        giro.z = new ParticleSystem.MinMaxCurve(-giroMax, giroMax);
        giro.enabled = true;

        ParticleSystem.SizeOverLifetimeModule tam = ps.sizeOverLifetime;
        tam.enabled = false;

        GreenMistParticulas.ConfigurarRenderer(ps, mat, 0.5f);

        // El material con luz necesita la "normal" de cada particula para calcular la iluminacion.
        // Estos son los datos que espera Particles/Simple Lit (lo mismo que pide su Inspector).
        ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend != null)
        {
            rend.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV
            });
        }
    }
}
