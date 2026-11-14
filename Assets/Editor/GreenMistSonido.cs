using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 7. Sonido".
//
// Arma todo el sonido nuevo del juego:
//   - Pasos:   hijo "GM_Audio_Pasos" de PlayerRoot + componente FootstepAudio en PlayerRoot.
//   - Impacto: hijo "GM_Audio_Impacto" de PlayerRoot, conectado al WeaponHitbox del baston.
//   - Grillos: "GM_Audio_Grillos" (2D, baja hacia el amanecer segun TimeOfDay).
//   - Agua:    "GM_Audio_Estanque" (3D, parado en el centro del estanque).
//
// LOS CLIPS LOS BAJAS VOS. Este menu crea 4 carpetas vacias:
//   Assets/Audio/SFX/Pasos        (5 o 6 pasos sueltos)
//   Assets/Audio/SFX/Impacto      (3 o 4 golpes de madera)
//   Assets/Audio/Ambiente/Grillos (1 loop)
//   Assets/Audio/Ambiente/Agua    (1 loop)
// Copia los archivos ahi (con cualquier nombre) y VOLVE A CORRER ESTE MENU: los
// encuentra solo y los conecta. Sin clips todo queda armado pero mudo, sin errores.
//
// Se puede correr las veces que quieras. NO duplica nada y NO pisa lo que hayas
// ajustado a mano (volumenes, zancadas, distancias): eso se pone solo la primera
// vez. Lo que si rehace siempre son las conexiones y la lista de clips.
public static class GreenMistSonido
{
    const string CarpetaPasos = "Assets/Audio/SFX/Pasos";
    const string CarpetaImpacto = "Assets/Audio/SFX/Impacto";
    const string CarpetaGrillos = "Assets/Audio/Ambiente/Grillos";
    const string CarpetaAgua = "Assets/Audio/Ambiente/Agua";

    const string NombreJugador = "PlayerRoot";
    const string NombrePasos = "GM_Audio_Pasos";
    const string NombreImpacto = "GM_Audio_Impacto";
    const string NombreGrillos = "GM_Audio_Grillos";
    const string NombreEstanque = "GM_Audio_Estanque";
    const string NombreAgua = "Agua pond";

    // Marca que se deja en el importador de cada clip ya configurado, para no
    // volver a pisarle los ajustes de importacion cada vez que corre el menu.
    const string MarcaImportado = "GreenMistSonido";

    const string TituloUndo = "Green Mist: sonido";

    [MenuItem("Green Mist/7. Sonido", false, 107)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        // ---------- 1) Carpetas y clips ----------
        GreenMistEditorUtil.AsegurarCarpeta(CarpetaPasos);
        GreenMistEditorUtil.AsegurarCarpeta(CarpetaImpacto);
        GreenMistEditorUtil.AsegurarCarpeta(CarpetaGrillos);
        GreenMistEditorUtil.AsegurarCarpeta(CarpetaAgua);

        // Por si copiaste los archivos desde el Explorador y Unity todavia no los vio.
        AssetDatabase.Refresh();

        //                                carpeta         esLoop  mono
        AudioClip[] clipsPasos   = BuscarClips(CarpetaPasos,   false, true);
        AudioClip[] clipsImpacto = BuscarClips(CarpetaImpacto, false, true);
        AudioClip[] clipsGrillos = BuscarClips(CarpetaGrillos, true,  false); // 2D: se deja en estereo
        AudioClip[] clipsAgua    = BuscarClips(CarpetaAgua,    true,  true);  // 3D: mono alcanza

        // ---------- 2) Paneles de menu (para callar todo fuera del juego) ----------
        GameObject jugador = GreenMistEditorUtil.BuscarRaiz(NombreJugador);

        PlayerDeath muerte = jugador != null ? jugador.GetComponent<PlayerDeath>() : null;
        if (muerte == null) muerte = Object.FindFirstObjectByType<PlayerDeath>(FindObjectsInactive.Include);

        GameObject panelMenu = muerte != null ? muerte.mainMenuPanel : null;
        GameObject panelGameOver = muerte != null ? muerte.gameOverPanel : null;
        if (panelMenu == null || panelGameOver == null)
        {
            Debug.LogWarning("[Green Mist] PlayerDeath (en PlayerRoot) no tiene asignado Main Menu Panel o Game Over Panel. "
                             + "Sin eso los grillos y el agua también suenan sobre el menú. Asignalos en PlayerDeath y volvé a correr este menú.");
        }

        // ---------- 3) Pasos e impacto (en Onji) ----------
        int cajasConectadas = 0;
        if (jugador == null)
        {
            Debug.LogWarning("[Green Mist] No encontré el objeto raíz '" + NombreJugador + "': no pude armar los pasos ni el impacto.");
        }
        else
        {
            ArmarPasos(jugador, clipsPasos, panelMenu, panelGameOver);
            cajasConectadas = ArmarImpacto(jugador, clipsImpacto);
        }

        // ---------- 4) Grillos y agua (en el pueblo) ----------
        TimeOfDay hora = GreenMistEditorUtil.BuscarTimeOfDay();
        Transform aldea = GreenMistEditorUtil.Aldea();
        Transform ambiente = GreenMistEditorUtil.ContenedorAmbiente();

        string avisoHora = "";
        if (aldea != null && ambiente != null)
        {
            AmbienceLoop grillos = ArmarGrillos(ambiente, clipsGrillos, hora, panelMenu, panelGameOver);
            ArmarEstanque(aldea, ambiente, clipsAgua, panelMenu, panelGameOver);

            // Aviso util para probar: a la hora guardada en la escena, se oyen los grillos?
            if (hora != null && grillos.followTimeOfDay)
            {
                float amanecer = Mathf.InverseLerp(grillos.fullUntil, grillos.offFrom, hora.time);
                float parte = Mathf.Lerp(1f, grillos.dawnVolume, amanecer);
                if (parte < 0.05f)
                {
                    avisoHora = " OJO: TimeOfDay.time está en " + hora.time.ToString("0.00")
                                + " y a esa hora los grillos están callados (se apagan desde "
                                + grillos.offFrom.ToString("0.00") + "). Para oírlos bajá time a 0.";
                }
            }
        }

        // ---------- 5) Chequeos y resumen ----------
        AudioListener[] oidos = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (oidos.Length != 1)
        {
            Debug.LogWarning("[Green Mist] Hay " + oidos.Length + " AudioListener activos en la escena y tiene que haber exactamente 1 (el de Main Camera).");
        }

        string faltan = "";
        if (clipsPasos.Length == 0) faltan += "\n  - " + CarpetaPasos + "  (5 o 6 pasos sueltos)";
        if (clipsImpacto.Length == 0) faltan += "\n  - " + CarpetaImpacto + "  (3 o 4 golpes de madera)";
        if (clipsGrillos.Length == 0) faltan += "\n  - " + CarpetaGrillos + "  (1 loop de grillos)";
        if (clipsAgua.Length == 0) faltan += "\n  - " + CarpetaAgua + "  (1 loop de agua)";
        if (faltan != "")
        {
            Debug.LogWarning("[Green Mist] Sonido: estas carpetas todavía están VACÍAS, así que eso queda mudo (sin errores):"
                             + faltan + "\nCopiá ahí los archivos (.wav u .ogg) y volvé a correr 'Green Mist > 7. Sonido'.");
        }

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Sonido listo. Pasos: " + clipsPasos.Length + " clips. Impacto: " + clipsImpacto.Length
                  + " clips en " + cajasConectadas + " hitbox. Grillos: " + NombreLoop(clipsGrillos)
                  + ". Agua: " + NombreLoop(clipsAgua) + "." + avisoHora + " Guardá la escena con Ctrl+S.");
    }

    // ------------------------------------------------------------------
    // Pasos
    // ------------------------------------------------------------------
    static void ArmarPasos(GameObject jugador, AudioClip[] clips, GameObject panelMenu, GameObject panelGameOver)
    {
        // La fuente va en un HIJO nuevo: el AudioSource de PlayerRoot es del whoosh
        // del baston (hace Stop() y cambia el clip), cortaria los pasos.
        GameObject go = GreenMistEditorUtil.BuscarOCrearHijo(jugador.transform, NombrePasos);
        bool fuenteNueva;
        AudioSource fuente = PrepararFuente(go, false, out fuenteNueva);
        if (fuenteNueva)
        {
            fuente.spatialBlend = 0f;   // 2D: son los pasos de uno mismo, siempre se oyen igual
            fuente.volume = 1f;         // el volumen real lo decide FootstepAudio en cada paso
        }
        EditorUtility.SetDirty(fuente);

        FootstepAudio pasos = GreenMistEditorUtil.ObtenerOAgregar<FootstepAudio>(jugador);
        Undo.RecordObject(pasos, TituloUndo);
        pasos.controller = jugador.GetComponent<CharacterController>();
        pasos.movement = jugador.GetComponent<ThirdPersonMovement>();
        pasos.animator = jugador.GetComponentInChildren<Animator>(true);
        pasos.source = fuente;
        if (panelMenu != null) pasos.mainMenuPanel = panelMenu;
        if (panelGameOver != null) pasos.gameOverPanel = panelGameOver;
        if (clips.Length > 0) pasos.clips = clips;   // carpeta vacia: no se borra lo que hubiera
        EditorUtility.SetDirty(pasos);

        if (pasos.controller == null)
            Debug.LogWarning("[Green Mist] PlayerRoot no tiene CharacterController: los pasos no van a sonar.");
    }

    // ------------------------------------------------------------------
    // Impacto del baston
    // ------------------------------------------------------------------
    // Devuelve a cuantos WeaponHitbox se les conecto el sonido.
    static int ArmarImpacto(GameObject jugador, AudioClip[] clips)
    {
        // Tambien en un hijo de PlayerRoot. No va en el baston (cuelga de huesos
        // escalados raro) ni en el enemigo (se destruye al morir y cortaria el sonido).
        GameObject go = GreenMistEditorUtil.BuscarOCrearHijo(jugador.transform, NombreImpacto);
        bool fuenteNueva;
        AudioSource fuente = PrepararFuente(go, false, out fuenteNueva);
        if (fuenteNueva)
        {
            fuente.spatialBlend = 0f;   // 2D: el golpe pasa al lado de Onji
            fuente.volume = 1f;         // el volumen real es WeaponHitbox.impactVolume
        }
        EditorUtility.SetDirty(fuente);

        // Los hitbox del jugador: los que cuelgan de PlayerRoot (true = tambien los apagados)
        // y, por las dudas, el que tenga asignado PlayerAttackSimple.
        List<WeaponHitbox> cajas = new List<WeaponHitbox>(jugador.GetComponentsInChildren<WeaponHitbox>(true));
        PlayerAttackSimple ataque = jugador.GetComponent<PlayerAttackSimple>();
        if (ataque != null && ataque.weaponHitbox != null && !cajas.Contains(ataque.weaponHitbox))
            cajas.Add(ataque.weaponHitbox);

        foreach (WeaponHitbox caja in cajas)
        {
            Undo.RecordObject(caja, TituloUndo);
            caja.impactSource = fuente;
            if (clips.Length > 0) caja.impactClips = clips;
            EditorUtility.SetDirty(caja);
        }

        if (cajas.Count == 0)
            Debug.LogWarning("[Green Mist] No encontré ningún WeaponHitbox en PlayerRoot: el impacto del bastón no va a sonar.");

        return cajas.Count;
    }

    // ------------------------------------------------------------------
    // Grillos
    // ------------------------------------------------------------------
    static AmbienceLoop ArmarGrillos(Transform ambiente, AudioClip[] clips, TimeOfDay hora,
                                     GameObject panelMenu, GameObject panelGameOver)
    {
        GameObject go = GreenMistEditorUtil.BuscarOCrearHijo(ambiente, NombreGrillos);
        bool fuenteNueva;
        AudioSource fuente = PrepararFuente(go, true, out fuenteNueva);
        if (fuenteNueva)
        {
            fuente.spatialBlend = 0f;   // 2D: los grillos estan "en todos lados"
            fuente.volume = 0f;         // lo maneja AmbienceLoop (ver maxVolume)
            fuente.priority = 200;      // si faltan voces de audio, que se corte esto antes que un golpe
        }
        if (clips.Length > 0) fuente.clip = clips[0];
        EditorUtility.SetDirty(fuente);

        bool loopNuevo = go.GetComponent<AmbienceLoop>() == null;
        AmbienceLoop loop = GreenMistEditorUtil.ObtenerOAgregar<AmbienceLoop>(go);
        Undo.RecordObject(loop, TituloUndo);
        if (loopNuevo)
        {
            loop.followTimeOfDay = true;    // noche = fuerte, amanecer = se apagan
            loop.maxVolume = 0.3f;
            loop.fullUntil = 0.5f;
            loop.offFrom = 0.95f;
            loop.dawnVolume = 0f;
        }
        loop.source = fuente;
        if (hora != null) loop.timeOfDay = hora;
        if (panelMenu != null) loop.mainMenuPanel = panelMenu;
        if (panelGameOver != null) loop.gameOverPanel = panelGameOver;
        EditorUtility.SetDirty(loop);
        return loop;
    }

    // ------------------------------------------------------------------
    // Agua del estanque
    // ------------------------------------------------------------------
    static void ArmarEstanque(Transform aldea, Transform ambiente, AudioClip[] clips,
                              GameObject panelMenu, GameObject panelGameOver)
    {
        bool objetoNuevo = ambiente.Find(NombreEstanque) == null;
        GameObject go = GreenMistEditorUtil.BuscarOCrearHijo(ambiente, NombreEstanque);

        // Donde esta el agua y cuanto mide. Se le pregunta al objeto "Agua pond";
        // si no esta, se usa la posicion conocida del estanque dentro del pueblo.
        float radio = 6.5f;
        Vector3 centro = aldea.TransformPoint(new Vector3(60f, -30.03f, 545f));
        Transform agua = aldea.Find(NombreAgua);
        if (agua != null)
        {
            centro = agua.position;
            WaterWaves olas = agua.GetComponent<WaterWaves>();
            if (olas != null)
                radio = olas.radio * Mathf.Max(Mathf.Abs(agua.lossyScale.x), Mathf.Abs(agua.lossyScale.z));
        }
        else if (objetoNuevo)
        {
            Debug.LogWarning("[Green Mist] No encontré '" + NombreAgua + "' dentro de 'Assets'. Puse el sonido del agua en la "
                             + "posición conocida del estanque; si lo moviste, mové a mano '" + NombreEstanque + "'.");
        }

        if (objetoNuevo)
        {
            // Solo la primera vez: si despues lo moves a mano, no te lo vuelve a acomodar.
            go.transform.position = centro + Vector3.up * 0.5f;
        }

        bool fuenteNueva;
        AudioSource fuente = PrepararFuente(go, true, out fuenteNueva);
        if (fuenteNueva)
        {
            fuente.spatialBlend = 1f;       // 3D: se oye mas fuerte cuanto mas cerca
            fuente.volume = 0f;             // lo maneja AmbienceLoop (ver maxVolume)
            fuente.priority = 200;

            // Linear: volumen entero hasta minDistance y de ahi baja parejo hasta 0 en
            // maxDistance. Con el modo por defecto (Logarithmic, 1 a 500) se oiria
            // en todo el mapa. Con radio 6.5 queda: entero a ~4 m del centro, mudo a ~29 m
            // (desde la calle no se oye; bajando por el sando aparece suave al pasar
            // frente al estanque, y sobre el puente esta al maximo).
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.minDistance = radio * 0.6f;
            fuente.maxDistance = radio * 4.5f;

            // Spread abre el sonido entre los dos parlantes. Sin esto, parado en el
            // puente (arriba de la fuente) el agua saltaria de un oido al otro al girar.
            fuente.spread = 100f;
        }
        if (clips.Length > 0) fuente.clip = clips[0];
        EditorUtility.SetDirty(fuente);

        bool loopNuevo = go.GetComponent<AmbienceLoop>() == null;
        AmbienceLoop loop = GreenMistEditorUtil.ObtenerOAgregar<AmbienceLoop>(go);
        Undo.RecordObject(loop, TituloUndo);
        if (loopNuevo)
        {
            loop.followTimeOfDay = false;   // el agua suena igual a cualquier hora
            loop.maxVolume = 0.6f;
        }
        loop.source = fuente;
        if (panelMenu != null) loop.mainMenuPanel = panelMenu;
        if (panelGameOver != null) loop.gameOverPanel = panelGameOver;
        EditorUtility.SetDirty(loop);
    }

    // ------------------------------------------------------------------
    // Ayudas
    // ------------------------------------------------------------------

    // Devuelve el AudioSource del objeto (lo agrega si falta). Siempre deja
    // Play On Awake apagado: los sonidos los disparan los scripts, no la escena.
    static AudioSource PrepararFuente(GameObject go, bool loop, out bool nueva)
    {
        AudioSource fuente = go.GetComponent<AudioSource>();
        nueva = fuente == null;
        if (nueva) fuente = Undo.AddComponent<AudioSource>(go);

        Undo.RecordObject(fuente, TituloUndo);
        fuente.playOnAwake = false;
        fuente.loop = loop;
        if (nueva) fuente.dopplerLevel = 0f; // sin efecto Doppler: no queremos que cambie el tono al moverse
        return fuente;
    }

    // Todos los AudioClip de una carpeta (con cualquier nombre), en orden alfabetico.
    // De paso les pone ajustes de importacion razonables la primera vez que los ve.
    static AudioClip[] BuscarClips(string carpeta, bool esLoop, bool mono)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { carpeta });

        List<string> rutas = new List<string>();
        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!rutas.Contains(ruta)) rutas.Add(ruta);
        }
        rutas.Sort(System.StringComparer.OrdinalIgnoreCase);

        List<AudioClip> clips = new List<AudioClip>();
        foreach (string ruta in rutas)
        {
            AjustarImportacion(ruta, esLoop, mono);

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            if (clip == null) continue;
            clips.Add(clip);

            if (esLoop && ruta.ToLowerInvariant().EndsWith(".mp3"))
            {
                Debug.LogWarning("[Green Mist] '" + ruta + "' es un .mp3: los mp3 dejan un huequito de silencio cada vez "
                                 + "que el loop vuelve a empezar. Mejor usá un .wav o un .ogg.");
            }
        }

        if (esLoop && clips.Count > 1)
        {
            Debug.LogWarning("[Green Mist] En " + carpeta + " hay " + clips.Count + " clips y se usa uno solo: '"
                             + clips[0].name + "' (el primero por orden alfabético). Dejá solo el que quieras.");
        }

        return clips.ToArray();
    }

    // Ajustes de importacion, una sola vez por archivo (despues podes cambiarlos a mano).
    static void AjustarImportacion(string ruta, bool esLoop, bool mono)
    {
        AudioImporter importador = AssetImporter.GetAtPath(ruta) as AudioImporter;
        if (importador == null) return;
        if (importador.userData == MarcaImportado) return; // ya lo configuramos antes

        AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
        if (esLoop)
        {
            // Loop largo: queda comprimido en memoria. "Decompress On Load" lo
            // descomprimiria entero en RAM (un minuto de audio son ~10 MB).
            ajustes.loadType = AudioClipLoadType.CompressedInMemory;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = 0.6f;
        }
        else
        {
            // Sonido corto que suena seguido: descomprimido, asi arranca al instante.
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.compressionFormat = AudioCompressionFormat.ADPCM;
        }
        importador.defaultSampleSettings = ajustes;

        // Mono para pasos, golpes y el agua 3D: ocupa la mitad y no pierde nada.
        if (mono) importador.forceToMono = true;

        importador.userData = MarcaImportado;
        importador.SaveAndReimport();
    }

    static string NombreLoop(AudioClip[] clips)
    {
        if (clips.Length == 0) return "sin clip todavía";
        return "'" + clips[0].name + "'";
    }
}
