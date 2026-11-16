using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 5. Pétalos de sakura".
//
// Crea (o actualiza) pétalos que caen de la copa de cada sakura y una deriva liviana
// a lo largo del sando. Pocos (290 en total, tope 300), chicos y con desvanecido.
// SOLO DE DIA (NightParticleFade en modo "dayOnly") y en RAFAGAS: no caen todo el tiempo.
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

    // SOLO DE DIA. El menu ESCRIBE estas horas en NightParticleFade (modo "dayOnly") cada vez que corre.
    // Apagados de noche hasta 0.45; van apareciendo y desde 0.70 estan enteros.
    const float ApagadosHasta = 0.45f;
    const float EnterosDesde = 0.70f;

    // RAFAGAS: los petalos no caen todo el tiempo. Cada emisor repite un ciclo de estos segundos
    // (cada uno un poco distinto, asi no van todos a la vez) y solo suelta petalos en la primera
    // parte del ciclo. Con FraccionRafaga = 1 caerian siempre.
    const float CicloRafaga = 36f;
    const float FraccionRafaga = 0.45f;

    // En que parte de su ciclo cae la rafaga de cada arbol (0 = al principio, 1 = al final).
    // Todas distintas, para que no suelten petalos todos a la vez. El primero usa 1: asi al
    // arrancar el juego ya hay petalos en el aire.
    static readonly float[] PosicionesRafaga = { 1f, 0.3f, 0.65f, 0.05f };

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

        // El mismo componente que apaga las luciernagas de dia, pero dado vuelta: petalos solo de dia.
        TimeOfDay hora = GreenMistEditorUtil.BuscarTimeOfDay();
        NightParticleFade dia = GreenMistEditorUtil.ObtenerOAgregar<NightParticleFade>(grupo);
        Undo.RecordObject(dia, "Green Mist: pétalos");
        dia.timeOfDay = hora;
        dia.dayOnly = true;
        dia.fullUntil = ApagadosHasta;
        dia.offFrom = EnterosDesde;
        EditorUtility.SetDirty(dia);

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
                                  new Vector3(-0.20f, -0.50f, -0.15f), new Vector3(0.20f, -0.30f, 0.15f),
                                  CicloRafaga + 5f * arboles, PosicionesRafaga[arboles % PosicionesRafaga.Length]);
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
                          new Vector3(0.15f, -0.30f, -0.10f), new Vector3(0.45f, -0.12f, 0.10f),
                          CicloRafaga + 8f, 0.8f);
        total += CantSando;

        int borrados = GreenMistParticulas.BorrarSobrantes(padre, Prefijo, usados);
        GreenMistParticulas.VistaPrevia(padre);

        AssetDatabase.SaveAssets();
        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Pétalos listos: " + arboles + " sakuras + la deriva del sando, " + total
                  + " partículas (tope 300) en '" + GreenMistEditorUtil.NombreAmbiente + "/" + NombreGrupo + "'"
                  + (borrados > 0 ? ", " + borrados + " emisores viejos borrados" : "") + ".");

        // Aviso util: son solo de dia, asi que de noche parece que el menu no hizo nada.
        if (hora != null && hora.time <= ApagadosHasta)
            Debug.LogWarning("[Green Mist] Ojo: los pétalos son solo de día. Ahora TimeOfDay.time = " + hora.time.ToString("0.00")
                             + " y recién aparecen después de " + ApagadosHasta.ToString("0.00")
                             + ", así que en Play NO se van a ver. Subí 'Time' del objeto TimeOfDay a 0.8 para verlos.");
    }

    // La curva de una rafaga a lo largo del ciclo (0 = empieza el ciclo, 1 = termina).
    // Vale 0 (no nace nada), sube rapido a 1, se queda ahi y vuelve a 0.
    // "posicion" dice en que parte del ciclo cae la rafaga: 0 = al principio, 1 = al final.
    // Con 1 la rafaga termina justo cuando el ciclo vuelve a empezar, asi que al arrancar
    // el juego ese emisor ya tiene petalos en el aire.
    static AnimationCurve CurvaRafaga(float posicion)
    {
        float f = Mathf.Clamp(FraccionRafaga, 0.1f, 1f);
        if (f >= 0.99f) return AnimationCurve.Constant(0f, 1f, 1f);   // sin rafagas: caen siempre

        float inicio = (1f - f) * Mathf.Clamp01(posicion);
        float fin = inicio + f;
        float rampa = f * 0.15f;

        List<Keyframe> puntos = new List<Keyframe>();
        if (inicio > 0.001f) puntos.Add(new Keyframe(0f, 0f));
        puntos.Add(new Keyframe(inicio, 0f));
        puntos.Add(new Keyframe(inicio + rampa, 1f));
        puntos.Add(new Keyframe(fin - rampa, 1f));
        puntos.Add(new Keyframe(fin, 0f));
        if (fin < 0.999f) puntos.Add(new Keyframe(1f, 0f));
        return new AnimationCurve(puntos.ToArray());
    }

    // Los valores de un emisor de petalos: chicos, que caen despacio girando sobre si mismos,
    // y que vienen en rafagas ("ciclo" = cada cuantos segundos se repite la rafaga;
    // "posicionRafaga" = en que parte del ciclo cae, de 0 a 1, para que no vayan todos juntos).
    static void ConfigurarPetalos(ParticleSystem ps, Material mat, Vector3 caja, int cantidad,
                                  float vidaMin, float vidaMax, Vector3 velMin, Vector3 velMax,
                                  float ciclo, float posicionRafaga)
    {
        GreenMistParticulas.ConfigurarBase(ps, cantidad, vidaMin, vidaMax);

        ParticleSystem.MainModule main = ps.main;

        // Rafagas: la cantidad que nace por segundo sigue una curva a lo largo del ciclo.
        main.duration = ciclo;
        float nacenPorSegundo = cantidad / ((vidaMin + vidaMax) * 0.5f);
        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = new ParticleSystem.MinMaxCurve(nacenPorSegundo, CurvaRafaga(posicionRafaga));

        // Por defecto Unity PAUSA un sistema en loop cuando no esta en pantalla. Entre rafagas
        // estos emisores quedan vacios y Unity los daria por "fuera de pantalla": el reloj de la
        // rafaga se frenaria. Con esto siguen corriendo siempre (son pocos petalos, no pesa).
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.16f);   // 9 a 16 cm (antes 4.5 a 8: casi no se veian)

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
