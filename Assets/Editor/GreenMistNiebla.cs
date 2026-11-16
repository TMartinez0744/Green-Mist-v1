using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 3. Niebla verde".
//
// Crea (o actualiza) una capa baja de niebla hecha con PARTICULAS, pegada al suelo y
// que se mueve despacio. NO toca la niebla de la escena (RenderSettings): esa es de TimeOfDay.
//
// Donde: mas densa en el sando y en las dos franjas de bambu, fina sobre la calle.
// Todo cuelga de "Assets/GreenMist_Ambiente/GreenMist_Niebla".
//
// El color y la densidad los maneja en Play el componente MistParticleTint de ese objeto
// (sigue el color de la niebla de la escena tirado al verde, y se afina al amanecer).
//
// Si lo corres de nuevo: los emisores se rearman con estos valores (pisa lo que hayas
// cambiado a mano en ellos) y el COLOR de MistParticleTint vuelve al de las constantes de abajo.
// Las horas y densidades de MistParticleTint si se respetan.
public static class GreenMistNiebla
{
    const string NombreGrupo = "GreenMist_Niebla";
    const string Prefijo = "Niebla_";

    // Cuantas particulas VIVAS tiene cada zona. Total: 100 + 18 + 40 + 45 + 8 x 11 = 291 (tope acordado: 350).
    const int CantSando = 100;
    const int CantSandoSur = 18;          // donde patrullan los enemigos: menos y mas transparente
    const int CantCalle = 40;
    const int CantBambuSur = 45;
    const int TramosBambuNorte = 8;
    const int CantPorTramoNorte = 11;

    // Altura del CENTRO de los copos sobre el suelo. Como miden ~2.3 m de alto, la niebla
    // queda entre el suelo y el pecho, mas rala arriba (el borde del copo es transparente).
    const float AlturaCentro = 0.8f;

    // Color de la niebla. El menu ESCRIBE estos valores en MistParticleTint cada vez que corre.
    // Es un verde grisaceo y apagado: el anterior, mas saturado, quedaba fluo contra la noche.
    static readonly Color VerdeNiebla = new Color(0.16f, 0.30f, 0.24f);
    const float TironAlVerde = 0.40f;   // 0 = igual a la niebla de la escena, 1 = el verde de arriba
    const float BrilloNiebla = 1.0f;

    [MenuItem("Green Mist/3. Niebla verde", false, 103)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Transform aldea = GreenMistEditorUtil.Aldea();
        if (aldea == null) return;
        Transform ambiente = GreenMistEditorUtil.ContenedorAmbiente();
        if (ambiente == null) return;

        // ---------- 1) El grupo y el componente que le da color ----------
        GameObject grupo = GreenMistEditorUtil.BuscarOCrearHijo(ambiente, NombreGrupo);
        Undo.RecordObject(grupo.transform, "Green Mist: niebla");
        grupo.transform.localPosition = Vector3.zero;      // asi sus hijos usan las coordenadas del pueblo
        grupo.transform.localRotation = Quaternion.identity;
        grupo.transform.localScale = Vector3.one;

        MistParticleTint tinte = GreenMistEditorUtil.ObtenerOAgregar<MistParticleTint>(grupo);
        Undo.RecordObject(tinte, "Green Mist: niebla");
        tinte.timeOfDay = GreenMistEditorUtil.BuscarTimeOfDay();
        tinte.greenTint = VerdeNiebla;
        tinte.greenPull = TironAlVerde;
        tinte.brightness = BrilloNiebla;
        EditorUtility.SetDirty(tinte);

        // ---------- 2) Textura y material ----------
        Texture2D textura = GreenMistParticulas.TexturaNiebla();
        if (textura == null) return;

        // Color inicial del material: el mismo que calcularia MistParticleTint ahora.
        // (Solo se LEE la niebla de la escena.) En Play el componente lo actualiza solo.
        Color color = MistParticleTint.MixColor(RenderSettings.fogColor, tinte.greenTint, tinte.greenPull, tinte.brightness);

        Material mat = GreenMistParticulas.MaterialParticulas(
            "GM_Niebla",
            false,          // sin luz (Particles/Unlit): es barato y no se quema con los faroles
            textura, color,
            false,          // transparencia normal, NO aditiva: asi nunca dispara el bloom
            false,
            1.5f,           // se desvanece en el ultimo metro y medio antes de tocar suelo, paredes o personajes
            1.5f, 5f,       // invisible a menos de 1.5 m de la camara, entera desde 5 m: no tapa a Onji
            GreenMistParticulas.ColaNiebla);
        if (mat == null) return;

        // ---------- 3) Los emisores ----------
        List<string> usados = new List<string>();
        Transform padre = grupo.transform;
        int total = 0;

        // Sando: desde donde se une con la calle (Z 594) hasta antes de la zona de enemigos (Z 516). X 20 a 42.
        total += ZonaPlana(padre, usados, mat, "Niebla_Sando", 31f, 555f, 22f, 78f, CantSando, 0.085f, 0.17f);

        // Sando sur (Z 490 a 516): aca patrullan los enemigos, asi que va mas rala y mas transparente.
        // No baja de Z 490 porque mas al sur el terreno empieza a subir.
        total += ZonaPlana(padre, usados, mat, "Niebla_Sando_Sur", 31f, 503f, 22f, 26f, CantSandoSur, 0.06f, 0.12f);

        // Calle: una capa fina entre las dos filas de casas. X 15 a 105.
        total += ZonaPlana(padre, usados, mat, "Niebla_Calle", 60f, 608.2f, 90f, 9f, CantCalle, 0.05f, 0.10f);

        // Bambu sur: franja plana. X 42 a 122, Z 589 a 596.
        total += ZonaPlana(padre, usados, mat, "Niebla_Bambu_Sur", 82f, 592.75f, 80f, 7f, CantBambuSur, 0.085f, 0.17f);

        // Bambu norte: esta sobre una ladera. Un solo emisor plano quedaria enterrado o flotando,
        // asi que van varios chicos, cada uno apoyado e inclinado segun el terreno de su tramo.
        float anchoTramo = (124f - 8f) / TramosBambuNorte;
        for (int i = 0; i < TramosBambuNorte; i++)
        {
            string nombre = "Niebla_Bambu_Norte_" + (i + 1);
            ParticleSystem ps = GreenMistParticulas.PrepararEmisor(padre, nombre, usados);
            ConfigurarNiebla(ps, mat, new Vector3(anchoTramo, 0.6f, 7.5f), CantPorTramoNorte, 0.085f, 0.17f, 4f, 6.5f);
            ApoyarEnLadera(ps.transform, aldea, 8f + anchoTramo * (i + 0.5f), 625.9f, anchoTramo * 0.5f, 3.75f);
            total += CantPorTramoNorte;
        }

        int borrados = GreenMistParticulas.BorrarSobrantes(padre, Prefijo, usados);
        GreenMistParticulas.VistaPrevia(padre);

        AssetDatabase.SaveAssets();
        GreenMistEditorUtil.MarcarEscenaSucia();

        if (Terrain.activeTerrain == null)
            Debug.LogWarning("[Green Mist] No hay un Terrain activo: la niebla del bambú norte quedó a la altura de la calle "
                             + "en vez de seguir la ladera. Revisala a mano (objetos 'Niebla_Bambu_Norte_N').");

        Debug.Log("[Green Mist] Niebla verde lista: " + usados.Count + " emisores y " + total
                  + " partículas (tope 350) en '" + GreenMistEditorUtil.NombreAmbiente + "/" + NombreGrupo + "'"
                  + (borrados > 0 ? ", " + borrados + " emisores viejos borrados" : "")
                  + ". El color y la densidad se ven en Play (componente MistParticleTint);"
                  + " su color se volvió a poner en los valores del menú.");

        // Aviso util: si la hora guardada en la escena es de dia, la niebla esta en su version fina de amanecer.
        // (Min: el componente ordena los dos umbrales, asi que empieza a afinarse desde el menor de los dos.)
        if (tinte.timeOfDay != null && tinte.timeOfDay.time > Mathf.Min(tinte.thinFrom, tinte.thinUntil))
            Debug.LogWarning("[Green Mist] Ojo: la niebla se afina al amanecer. Ahora TimeOfDay.time = "
                             + tinte.timeOfDay.time.ToString("0.00") + ", así que en Play se va a ver rala. "
                             + "Bajá 'Time' del objeto TimeOfDay a 0 o 0.5 para verla entera.");
    }

    // Un emisor sobre suelo plano. cx, cz = centro; tx, tz = tamaño de la zona (coordenadas locales del pueblo).
    static int ZonaPlana(Transform padre, List<string> usados, Material mat, string nombre,
                         float cx, float cz, float tx, float tz, int cantidad, float alfaMin, float alfaMax)
    {
        ParticleSystem ps = GreenMistParticulas.PrepararEmisor(padre, nombre, usados);
        ps.transform.localPosition = new Vector3(cx, GreenMistParticulas.SueloLocalY + AlturaCentro, cz);
        ConfigurarNiebla(ps, mat, new Vector3(tx, 0.6f, tz), cantidad, alfaMin, alfaMax, 6f, 9f);
        return cantidad;
    }

    // Los valores de un emisor de niebla: pocos copos, grandes, anchos y bajitos, muy transparentes,
    // que viven unos 20 segundos y se arrastran despacio.
    static void ConfigurarNiebla(ParticleSystem ps, Material mat, Vector3 caja, int cantidad,
                                 float alfaMin, float alfaMax, float anchoMin, float anchoMax)
    {
        GreenMistParticulas.ConfigurarBase(ps, cantidad, 16f, 24f);

        ParticleSystem.MainModule main = ps.main;

        // Tamaño distinto por eje: varios metros de ancho y unos dos metros de alto.
        // Sigue siendo mas ancha que alta: eso es lo que la deja "acostada" contra el suelo.
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(anchoMin, anchoMax);
        main.startSizeY = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(1f, 1f);

        main.startRotation3D = false;
        main.startRotation = 0f;               // sin girar: el lado ancho siempre queda horizontal

        // El alpha de cada copo es bajo: la niebla se arma por superposicion de varios.
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, alfaMin), new Color(1f, 1f, 1f, alfaMax));

        GreenMistParticulas.FormaCaja(ps, caja);

        // Una brisa muy lenta hacia +X, con un vaiven suave. Casi nada en vertical.
        GreenMistParticulas.Velocidad(ps, new Vector3(0.04f, 0f, -0.06f), new Vector3(0.18f, 0f, 0.06f));
        GreenMistParticulas.Ruido(ps, new Vector3(0.12f, 0.02f, 0.12f), 0.08f, 0.03f);

        // Aparece en el primer cuarto de su vida y se va en el ultimo tercio: nunca "salta".
        GreenMistParticulas.Desvanecer(ps, 0.25f, 0.7f);

        // Crece un poquito mientras vive, como humo que se abre.
        ParticleSystem.SizeOverLifetimeModule tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.separateAxes = false;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.85f, 1f, 1.15f));

        ParticleSystem.RotationOverLifetimeModule giro = ps.rotationOverLifetime;
        giro.enabled = false;

        // Tope de tamaño en pantalla bien alto (3 = tres pantallas): si no, Unity achica de golpe
        // los copos grandes cuando la camara se les acerca.
        GreenMistParticulas.ConfigurarRenderer(ps, mat, 3f);

        // La mitad de los copos sale espejada, para que no se note que todos usan la misma textura.
        ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend != null) rend.flip = new Vector3(0.5f, 0.5f, 0f);
    }

    // Apoya un emisor sobre el terreno de la ladera y lo inclina igual que la pendiente.
    // x, z = centro del tramo (locales del pueblo); medioX, medioZ = la mitad de su tamaño.
    static void ApoyarEnLadera(Transform emisor, Transform aldea, float x, float z, float medioX, float medioZ)
    {
        Vector3 centro = PuntoSobreSuelo(aldea, x, z);
        Vector3 este = PuntoSobreSuelo(aldea, x + medioX, z);
        Vector3 oeste = PuntoSobreSuelo(aldea, x - medioX, z);
        Vector3 norte = PuntoSobreSuelo(aldea, x, z + medioZ);
        Vector3 sur = PuntoSobreSuelo(aldea, x, z - medioZ);

        // Con dos direcciones apoyadas sobre el suelo, el producto cruz da la "normal":
        // la flecha que sale perpendicular a la pendiente.
        Vector3 normal = Vector3.Cross(norte - sur, este - oeste).normalized;
        if (normal.y < 0f) normal = -normal;
        if (normal.sqrMagnitude < 0.5f) normal = Vector3.up;   // por si las muestras salieron raras

        // Inclinacion maxima de 40 grados, para que un salto del terreno no pare el emisor de canto.
        Quaternion inclinacion = Quaternion.FromToRotation(Vector3.up, normal);
        inclinacion = Quaternion.RotateTowards(Quaternion.identity, inclinacion, 40f);

        float altura = (centro.y + este.y + oeste.y + norte.y + sur.y) / 5f;

        emisor.position = new Vector3(centro.x, altura + AlturaCentro, centro.z);
        emisor.rotation = inclinacion;
    }

    // Un punto del pueblo (x, z locales) llevado a coordenadas de mundo y apoyado en el terreno.
    // Nunca queda por debajo del suelo plano de la calle.
    static Vector3 PuntoSobreSuelo(Transform aldea, float x, float z)
    {
        Vector3 p = aldea.TransformPoint(new Vector3(x, GreenMistParticulas.SueloLocalY, z));
        float piso = GreenMistParticulas.SueloPlanoMundoY(aldea);
        p.y = Mathf.Max(GreenMistEditorUtil.AlturaSueloMundo(p), piso);
        return p;
    }
}
