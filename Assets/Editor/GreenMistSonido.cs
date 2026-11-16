using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
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
//   Assets/Audio/SFX/Pasos        (pasos sueltos; se usan SOLO 2, ver "PasosElegidos" mas abajo)
//   Assets/Audio/SFX/Impacto      (3 o 4 golpes de madera)
//   Assets/Audio/Ambiente/Grillos (1 loop)
//   Assets/Audio/Ambiente/Agua    (1 loop)
// Copia los archivos ahi (con cualquier nombre) y VOLVE A CORRER ESTE MENU: los
// encuentra solo y los conecta. Sin clips todo queda armado pero mudo, sin errores.
//
// Se puede correr las veces que quieras. NO duplica nada y NO pisa lo que hayas
// ajustado a mano (volumenes, zancadas, distancias): eso se pone solo la primera
// vez. Lo que si rehace siempre son las conexiones y la lista de clips.
//
// TRES EXCEPCIONES a proposito (las tres avisan en la Console cuando pasan):
//   - Pasos: si las zancadas siguen con los valores viejos (0.95 / 1.5 / 0.8, que
//     sonaban mucho mas rapido que la animacion) se cambian por las que dan el
//     mismo ritmo que la animacion. Si las habias tocado a mano, no se tocan.
//   - Pasos: UNA sola vez se bajan los cuatro volumenes (sonaban demasiado
//     fuerte). Queda anotado en el propio componente (un numero escondido,
//     "tuningVersion"), asi que las proximas veces ya no se tocan.
//   - Agua: la primera vez que corre esta version (se nota porque el objeto
//     todavia no tiene el filtro "Audio Low Pass Filter") y cada vez que CAMBIA
//     el clip, se ponen el volumen, el tono y el filtro que le van a ese clip.
//     Mientras el clip sea el mismo, lo que ajustes a mano queda como lo dejaste.
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

    // ---------- Pasos: cuales archivos se usan ----------
    // De todos los archivos que haya en Assets/Audio/SFX/Pasos se usan SOLO estos,
    // contados en orden alfabetico (1 = el primero de la carpeta). Con dos suenan
    // alternados, un sonido por pie; con cinco se notaba un patron raro.
    // PARA CAMBIARLOS: cambia los numeros (por ejemplo { 2, 4 }, o { 1 } para usar
    // uno solo) y volve a correr el menu. Los archivos que no se usan no se borran.
    static readonly int[] PasosElegidos = { 1, 2 };

    // ---------- Pasos: volumen ----------
    // Bajitos a proposito. Unity sube al maximo el volumen de los clips de pasos
    // al importarlos (mono + "Normalize"), mientras que los grillos y el agua
    // suenan muy suaves. Con los valores viejos (0.5 / 0.75 / 0.25 / 0.9) los pasos
    // tapaban todo. Estos son mas o menos un 35% de aquellos (unos 9 decibeles
    // menos) y mantienen el orden: agachado < caminando < corriendo < aterrizaje.
    const float VolumenPasoCaminando = 0.18f;
    const float VolumenPasoCorriendo = 0.26f;
    const float VolumenPasoAgachado = 0.09f;
    const float VolumenPasoAterrizaje = 0.32f;

    // La marca que se anota en FootstepAudio.tuningVersion cuando ya se bajaron
    // los volumenes. La escena guardada con la version vieja la tiene en 0.
    const int VersionVolumenPasos = 1;

    // ---------- Pasos: ritmo de las animaciones de Onji ----------
    // Segundos entre un paso y el siguiente. Salen de medir los clips que usa
    // PlayerAnimator (un ciclo = dos pasos):
    //   Standard Walk     35 cuadros a 30 por segundo = 1.167 s  ->  0.583 s por paso
    //   Running           21 cuadros                  = 0.700 s  ->  0.350 s por paso
    //   Crouched Walking  31 cuadros                  = 1.033 s  ->  0.517 s por paso
    const float SegundosPorPasoCaminando = 0.5833f;
    const float SegundosPorPasoCorriendo = 0.35f;
    const float SegundosPorPasoAgachado = 0.5167f;

    // Las zancadas que traia la primera version de FootstepAudio. Con ellas
    // sonaba un paso cada 0.38 s caminando: un 50% mas rapido que los pies.
    const float ZancadaViejaCaminando = 0.95f;
    const float ZancadaViejaCorriendo = 1.5f;
    const float ZancadaViejaAgachado = 0.8f;

    // ---------- Agua: como suena segun el clip ----------
    // Un estanque quieto casi no hace ruido. Si el clip es de agua CORRIENDO
    // (un arroyo) lo mandamos bien al fondo: bajito, mas grave y sin los agudos,
    // que son los que suenan a "chorro". Queda un murmullo suave.
    const float VolumenAguaCorriendo = 0.15f;   // antes 0.6
    const float TonoAguaCorriendo = 0.85f;      // mas grave y un 15% mas lento: suena a agua mas quieta
    const float FiltroAguaCorriendo = 1000f;    // Hz. Lo que esta por arriba se apaga (el salpicado vive entre 2000 y 8000)

    // Si el clip ya es de agua calma (orilla de lago, ranas, estanque de noche)
    // no hace falta apagarle los agudos ni bajarle el tono. El volumen SI va
    // bajo: Unity sube solo el volumen de los clips que pasa a mono (como este,
    // opcion "Normalize" del importador), asi que cualquier clip nuevo llega
    // fuerte, mucho mas que los grillos. Mejor que arranque suave y lo subas vos
    // con "Max Volume" si hace falta.
    const float VolumenAguaCalma = 0.12f;
    const float TonoAguaCalma = 1f;
    const float FiltroAguaCalma = 6000f;

    // Hasta donde se oye, en "radios del estanque" medidos desde el centro.
    // El oido es la camara, que va unos metros detras de Onji.
    const float AguaDistanciaLlena = 0.5f;   // dentro de medio radio: volumen entero (parado en el puente)
    const float AguaDistanciaMuda = 2.5f;    // a 2.5 radios (~16 m) ya no se oye. Antes 4.5 (~29 m)

    // Palabras en el nombre del archivo que delatan agua corriendo.
    static readonly string[] PalabrasAguaCorriendo =
        { "brook", "stream", "creek", "river", "waterfall", "fountain", "arroyo", "cascada", "chorro" };

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

        //                                carpeta         esLoop  mono   avisar si sobran
        AudioClip[] todosLosPasos = BuscarClips(CarpetaPasos,  false, true,  false);
        AudioClip[] clipsImpacto = BuscarClips(CarpetaImpacto, false, true,  false);
        AudioClip[] clipsGrillos = BuscarClips(CarpetaGrillos, true,  false, true);  // 2D: se deja en estereo
        AudioClip[] clipsAgua    = BuscarClips(CarpetaAgua,    true,  true,  false); // 3D: mono alcanza

        // De todos los pasos de la carpeta se usan solo los de "PasosElegidos".
        // 'resumenPasos' es el texto para la Console con los nombres de los archivos.
        string resumenPasos;
        AudioClip[] clipsPasos = ElegirClipsDePasos(todosLosPasos, out resumenPasos);

        // El agua elige su clip aparte: si hay varios prefiere el de agua calma.
        AudioClip clipAgua = ElegirClipDeAgua(clipsAgua);

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
        string cambiosPasos = "";
        if (jugador == null)
        {
            Debug.LogWarning("[Green Mist] No encontré el objeto raíz '" + NombreJugador + "': no pude armar los pasos ni el impacto.");
        }
        else
        {
            cambiosPasos = ArmarPasos(jugador, clipsPasos, panelMenu, panelGameOver);
            cajasConectadas = ArmarImpacto(jugador, clipsImpacto);
        }

        // ---------- 4) Grillos y agua (en el pueblo) ----------
        TimeOfDay hora = GreenMistEditorUtil.BuscarTimeOfDay();
        Transform aldea = GreenMistEditorUtil.Aldea();
        Transform ambiente = GreenMistEditorUtil.ContenedorAmbiente();

        string avisoHora = "";
        string cambiosAgua = "";
        if (aldea != null && ambiente != null)
        {
            AmbienceLoop grillos = ArmarGrillos(ambiente, clipsGrillos, hora, panelMenu, panelGameOver);
            cambiosAgua = ArmarEstanque(aldea, ambiente, clipAgua, panelMenu, panelGameOver);

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
        if (clipsPasos.Length == 0) faltan += "\n  - " + CarpetaPasos + "  (2 pasos sueltos alcanzan)";
        if (clipsImpacto.Length == 0) faltan += "\n  - " + CarpetaImpacto + "  (3 o 4 golpes de madera)";
        if (clipsGrillos.Length == 0) faltan += "\n  - " + CarpetaGrillos + "  (1 loop de grillos)";
        if (clipsAgua.Length == 0) faltan += "\n  - " + CarpetaAgua + "  (1 loop de agua)";
        if (faltan != "")
        {
            Debug.LogWarning("[Green Mist] Sonido: estas carpetas todavía están VACÍAS, así que eso queda mudo (sin errores):"
                             + faltan + "\nCopiá ahí los archivos (.wav u .ogg) y volvé a correr 'Green Mist > 7. Sonido'.");
        }

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Sonido listo. Pasos: " + resumenPasos + ". Impacto: " + clipsImpacto.Length
                  + " clips en " + cajasConectadas + " hitbox. Grillos: " + NombreLoop(clipsGrillos)
                  + ". Agua: " + (clipAgua != null ? "'" + clipAgua.name + "'" : "sin clip todavía") + "."
                  + cambiosPasos + cambiosAgua + avisoHora + " Guardá la escena con Ctrl+S.");
    }

    // ------------------------------------------------------------------
    // Pasos
    // ------------------------------------------------------------------
    // Devuelve un texto con lo que cambio (para el resumen de la Console), o "" si nada.
    static string ArmarPasos(GameObject jugador, AudioClip[] clips, GameObject panelMenu, GameObject panelGameOver)
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

        bool componenteNuevo = jugador.GetComponent<FootstepAudio>() == null;
        FootstepAudio pasos = GreenMistEditorUtil.ObtenerOAgregar<FootstepAudio>(jugador);
        Undo.RecordObject(pasos, TituloUndo);
        pasos.controller = jugador.GetComponent<CharacterController>();
        pasos.movement = jugador.GetComponent<ThirdPersonMovement>();
        pasos.animator = jugador.GetComponentInChildren<Animator>(true);
        pasos.source = fuente;
        if (panelMenu != null) pasos.mainMenuPanel = panelMenu;
        if (panelGameOver != null) pasos.gameOverPanel = panelGameOver;

        // La lista de clips se rehace SIEMPRE (son los de "PasosElegidos").
        int clipsAntes = pasos.clips != null ? pasos.clips.Length : 0;
        if (clips.Length > 0) pasos.clips = clips;   // carpeta vacia: no se borra lo que hubiera

        string cambios = "";

        // Volumenes de los pasos. Se ponen UNA sola vez: la primera vez que corre
        // esta version del menu (la marca tuningVersion todavia esta en 0). Despues
        // queda anotado y no se vuelven a tocar: lo que ajustes a mano, queda.
        if (pasos.tuningVersion < VersionVolumenPasos)
        {
            if (componenteNuevo)
            {
                // Escena nueva: se ponen directamente, sin avisar nada.
                pasos.walkVolume = VolumenPasoCaminando;
                pasos.runVolume = VolumenPasoCorriendo;
                pasos.crouchVolume = VolumenPasoAgachado;
                pasos.landVolume = VolumenPasoAterrizaje;
            }
            else
            {
                // Escena que ya tenia los pasos armados con los volumenes fuertes.
                float caminarAntes = pasos.walkVolume;
                float correrAntes = pasos.runVolume;
                float agachadoAntes = pasos.crouchVolume;
                float aterrizajeAntes = pasos.landVolume;

                // Mathf.Min = el mas chico de los dos. Asi solo se BAJAN: si alguno
                // ya lo habias bajado a mano todavia mas, queda el tuyo.
                pasos.walkVolume = Mathf.Min(caminarAntes, VolumenPasoCaminando);
                pasos.runVolume = Mathf.Min(correrAntes, VolumenPasoCorriendo);
                pasos.crouchVolume = Mathf.Min(agachadoAntes, VolumenPasoAgachado);
                pasos.landVolume = Mathf.Min(aterrizajeAntes, VolumenPasoAterrizaje);

                bool bajoAlguno = pasos.walkVolume < caminarAntes || pasos.runVolume < correrAntes
                               || pasos.crouchVolume < agachadoAntes || pasos.landVolume < aterrizajeAntes;
                if (bajoAlguno)
                {
                    cambios += " PASOS (ajuste de UNA sola vez): bajé los volúmenes porque sonaban muy fuerte. Caminando "
                               + caminarAntes.ToString("0.00") + " -> " + pasos.walkVolume.ToString("0.00")
                               + ", corriendo " + correrAntes.ToString("0.00") + " -> " + pasos.runVolume.ToString("0.00")
                               + ", agachado " + agachadoAntes.ToString("0.00") + " -> " + pasos.crouchVolume.ToString("0.00")
                               + ", aterrizaje " + aterrizajeAntes.ToString("0.00") + " -> " + pasos.landVolume.ToString("0.00")
                               + ". No toqué 'Phase Offset' ni ningún otro ajuste. Este menú NO los vuelve a cambiar:"
                               + " si querés otro nivel, movelos en PlayerRoot > Footstep Audio > Volumen.";
                }
                if (clips.Length > 0 && clipsAntes > clips.Length)
                {
                    cambios += " PASOS: la lista pasó de " + clipsAntes + " clips a " + clips.Length
                               + " (los archivos siguen todos en la carpeta, no borré nada).";
                }
            }

            pasos.tuningVersion = VersionVolumenPasos;   // anotado: ya esta hecho
        }

        // Zancadas del plan B (pasos por distancia). Solo se corrigen si siguen
        // EXACTAMENTE con los valores viejos: si las tocaste a mano, quedan.
        // La zancada nueva es "velocidad x segundos por paso de la animacion",
        // asi el plan B lleva el mismo ritmo que los pies.
        bool zancadasViejas = Mathf.Approximately(pasos.walkStride, ZancadaViejaCaminando)
                           && Mathf.Approximately(pasos.runStride, ZancadaViejaCorriendo)
                           && Mathf.Approximately(pasos.crouchStride, ZancadaViejaAgachado);
        if (zancadasViejas)
        {
            // Velocidades reales de la escena (si faltara el script, las que tiene hoy).
            float caminar = pasos.movement != null ? pasos.movement.walkSpeed : 2.5f;
            float correr = pasos.movement != null ? pasos.movement.runSpeed : 5f;
            float agachado = pasos.movement != null ? pasos.movement.crouchSpeed : 1.5f;

            pasos.walkStride = Zancada(caminar, SegundosPorPasoCaminando);
            pasos.runStride = Zancada(correr, SegundosPorPasoCorriendo);
            pasos.crouchStride = Zancada(agachado, SegundosPorPasoAgachado);

            cambios += " PASOS: corregí las zancadas viejas (0.95 / 1.5 / 0.8) a "
                       + pasos.walkStride.ToString("0.00") + " / " + pasos.runStride.ToString("0.00") + " / "
                       + pasos.crouchStride.ToString("0.00") + " m (mismo ritmo que la animación). Igual ahora los pasos"
                       + " salen de la animación ('Sync With Animation'); las zancadas son solo el plan B.";
        }

        // Lista de estados donde Onji camina: si quedo vacia se pone la de este Animator.
        if (pasos.walkStates == null || pasos.walkStates.Length == 0)
            pasos.walkStates = new[] { "Locomotion", "CrouchWalk" };

        EditorUtility.SetDirty(pasos);

        if (pasos.controller == null)
            Debug.LogWarning("[Green Mist] PlayerRoot no tiene CharacterController: los pasos no van a sonar.");

        RevisarEstadosDeCaminar(pasos);
        return cambios;
    }

    // De todos los clips de la carpeta de pasos devuelve solo los que dice
    // "PasosElegidos" (arriba de todo). 'resumen' queda con el texto para la
    // Console: cuales se usan y cuales quedaron sin usar.
    static AudioClip[] ElegirClipsDePasos(AudioClip[] todos, out string resumen)
    {
        if (todos.Length == 0)
        {
            resumen = "sin clips todavía";
            return todos;
        }

        List<AudioClip> elegidos = new List<AudioClip>();
        string numerosMalos = "";
        foreach (int numero in PasosElegidos)
        {
            // Los numeros se cuentan desde 1; en la lista 'todos' se cuenta desde 0.
            if (numero < 1 || numero > todos.Length)
            {
                numerosMalos += " " + numero;
                continue;
            }
            AudioClip clip = todos[numero - 1];
            if (!elegidos.Contains(clip)) elegidos.Add(clip);   // el mismo numero dos veces cuenta una sola
        }

        if (numerosMalos != "")
        {
            Debug.LogWarning("[Green Mist] Pasos: en 'PasosElegidos' (arriba de todo en GreenMistSonido.cs) hay números que no "
                             + "existen:" + numerosMalos + ". En " + CarpetaPasos + " hay " + todos.Length
                             + " archivos, así que valen del 1 al " + todos.Length + ".");
        }

        // Si no quedo ninguno (lista vacia o todos los numeros mal): los dos primeros.
        if (elegidos.Count == 0)
        {
            for (int i = 0; i < todos.Length && i < 2; i++) elegidos.Add(todos[i]);
        }

        // Texto para la Console. Cada archivo va con su numero, asi es facil elegir otros.
        string usados = "";
        string sinUsar = "";
        for (int i = 0; i < todos.Length; i++)
        {
            string nombre = "[" + (i + 1) + "] '" + todos[i].name + "'";
            if (elegidos.Contains(todos[i])) usados += (usados == "" ? "" : " y ") + nombre;
            else sinUsar += (sinUsar == "" ? "" : ", ") + nombre;
        }

        resumen = "uso " + elegidos.Count + " de los " + todos.Length + " archivos de la carpeta: " + usados;
        if (elegidos.Count == 2) resumen += " (suenan alternados, uno por pie)";
        if (sinUsar != "")
            resumen += ". Sin usar: " + sinUsar + " (para probar otros, cambiá los números de 'PasosElegidos' arriba de todo en GreenMistSonido.cs)";

        return elegidos.ToArray();
    }

    // Metros por paso para que, a esa velocidad, suene un paso cada 'segundosPorPaso'.
    // Redondeado a 2 decimales y dentro de lo que deja el slider del Inspector (0.3 a 3).
    static float Zancada(float velocidad, float segundosPorPaso)
    {
        float metros = Mathf.Round(velocidad * segundosPorPaso * 100f) / 100f;
        return Mathf.Clamp(metros, 0.3f, 3f);
    }

    // Avisa si algun nombre de 'Walk States' no existe en el Animator de Onji.
    // (Si ninguno existe, FootstepAudio usa solo el plan B por distancia.)
    static void RevisarEstadosDeCaminar(FootstepAudio pasos)
    {
        if (pasos.animator == null)
        {
            Debug.LogWarning("[Green Mist] No encontré el Animator de Onji: los pasos van a sonar por distancia (plan B), sin seguir la animación.");
            return;
        }

        // Solo se puede revisar si es un Animator Controller comun (no un Override).
        AnimatorController control = pasos.animator.runtimeAnimatorController as AnimatorController;
        if (control == null || control.layers.Length == 0) return;

        AnimatorStateMachine maquina = control.layers[0].stateMachine;
        int encontrados = 0;
        string faltan = "";
        foreach (string nombre in pasos.walkStates)
        {
            if (string.IsNullOrEmpty(nombre)) continue;
            if (ExisteEstado(maquina, nombre)) encontrados++;
            else faltan += " '" + nombre + "'";
        }

        if (faltan != "")
        {
            Debug.LogWarning("[Green Mist] FootstepAudio > Walk States: no existe en la capa 0 de '" + control.name + "' el estado:" + faltan
                             + ". Escribilo igual que en la ventana Animator." + (encontrados == 0
                                 ? " Como no coincide NINGUNO, los pasos van a sonar por distancia (plan B)." : ""));
        }
    }

    // Busca un estado por nombre, tambien dentro de las sub-maquinas.
    static bool ExisteEstado(AnimatorStateMachine maquina, string nombre)
    {
        if (maquina == null) return false;
        foreach (ChildAnimatorState hijo in maquina.states)
            if (hijo.state != null && hijo.state.name == nombre) return true;
        foreach (ChildAnimatorStateMachine sub in maquina.stateMachines)
            if (ExisteEstado(sub.stateMachine, nombre)) return true;
        return false;
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
    // Devuelve un texto con lo que cambio (para el resumen de la Console), o "" si nada.
    static string ArmarEstanque(Transform aldea, Transform ambiente, AudioClip clip,
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
            // en todo el mapa. Las distancias se ponen mas abajo.
            fuente.rolloffMode = AudioRolloffMode.Linear;

            // Spread abre el sonido entre los dos parlantes. Sin esto, parado en el
            // puente (arriba de la fuente) el agua saltaria de un oido al otro al girar.
            fuente.spread = 100f;
        }

        AudioClip clipAnterior = fuente.clip;
        if (clip != null) fuente.clip = clip;

        // El filtro que apaga los agudos. Ademas sirve de MARCA: si el objeto
        // todavia no lo tiene, es que quedo armado con la version vieja (la del
        // "chorro") y hay que actualizarle los valores una vez.
        AudioLowPassFilter filtro = go.GetComponent<AudioLowPassFilter>();
        bool filtroNuevo = filtro == null;
        if (filtroNuevo) filtro = Undo.AddComponent<AudioLowPassFilter>(go);
        Undo.RecordObject(filtro, TituloUndo);

        bool loopNuevo = go.GetComponent<AmbienceLoop>() == null;
        AmbienceLoop loop = GreenMistEditorUtil.ObtenerOAgregar<AmbienceLoop>(go);
        Undo.RecordObject(loop, TituloUndo);
        if (loopNuevo) loop.followTimeOfDay = false;   // el agua suena igual a cualquier hora

        string cambios = "";

        // (1) Hasta donde se oye. Solo la primera vez que corre esta version.
        // Con radio 6.5: volumen entero a ~3 m del centro (sobre el puente) y mudo
        // a ~16 m. Antes era 4 m y 29 m: se oia desde demasiado lejos para ser un
        // estanque quieto.
        if (filtroNuevo)
        {
            fuente.minDistance = radio * AguaDistanciaLlena;
            fuente.maxDistance = radio * AguaDistanciaMuda;
            cambios += " AGUA: ahora se oye solo cerca del estanque (entera a " + fuente.minDistance.ToString("0.0")
                       + " m del centro, muda a " + fuente.maxDistance.ToString("0.0") + " m).";
        }

        // (2) Volumen, tono y filtro segun el clip. La primera vez que corre esta
        // version, y cada vez que el clip CAMBIA (lo que le iba al arroyo no le va
        // a un clip de agua calma). Si el clip es el mismo de antes no se toca
        // nada: lo que ajustaste a mano queda.
        bool cambioClip = clip != null && clip != clipAnterior;
        if ((filtroNuevo || cambioClip) && fuente.clip != null)
        {
            bool corriendo = EsAguaCorriendo(fuente.clip);
            float volumenAntes = loop.maxVolume;

            loop.maxVolume = corriendo ? VolumenAguaCorriendo : VolumenAguaCalma;
            fuente.pitch = corriendo ? TonoAguaCorriendo : TonoAguaCalma;
            filtro.cutoffFrequency = corriendo ? FiltroAguaCorriendo : FiltroAguaCalma;
            filtro.lowpassResonanceQ = 1f;   // 1 = sin resaltar ninguna frecuencia

            cambios += " AGUA: '" + fuente.clip.name + "' es agua " + (corriendo ? "CORRIENDO (arroyo)" : "calma")
                       + ", le puse volumen " + loop.maxVolume.ToString("0.00") + " (estaba en " + volumenAntes.ToString("0.00")
                       + "), tono " + fuente.pitch.ToString("0.00") + " y filtro de agudos en "
                       + filtro.cutoffFrequency.ToString("0") + " Hz."
                       + (corriendo ? " Queda como un murmullo de fondo; para un estanque de verdad conviene cambiar el clip por uno de agua calma."
                                    : " Arranca bajito a propósito: si casi no se oye (o queda fuerte), mové 'Max Volume' en '"
                                      + NombreEstanque + "' > Ambience Loop.");
        }
        else if (filtroNuevo)
        {
            // Todavia no hay clip: dejamos listo lo del agua calma para cuando llegue.
            loop.maxVolume = VolumenAguaCalma;
            filtro.cutoffFrequency = FiltroAguaCalma;
            filtro.lowpassResonanceQ = 1f;
        }

        loop.source = fuente;
        if (panelMenu != null) loop.mainMenuPanel = panelMenu;
        if (panelGameOver != null) loop.gameOverPanel = panelGameOver;

        EditorUtility.SetDirty(fuente);
        EditorUtility.SetDirty(filtro);
        EditorUtility.SetDirty(loop);
        return cambios;
    }

    // True si el nombre del clip suena a agua que corre (arroyo, cascada...).
    static bool EsAguaCorriendo(AudioClip clip)
    {
        string nombre = clip.name.ToLowerInvariant();
        foreach (string palabra in PalabrasAguaCorriendo)
            if (nombre.Contains(palabra)) return true;
        return false;
    }

    // Que clip usa el estanque cuando la carpeta tiene mas de uno: el primero
    // (por orden alfabetico) que NO sea de agua corriendo. Asi alcanza con copiar
    // el clip nuevo de agua calma al lado del arroyo viejo y volver a correr el menu.
    static AudioClip ElegirClipDeAgua(AudioClip[] clips)
    {
        if (clips.Length == 0) return null;

        AudioClip elegido = clips[0];
        foreach (AudioClip clip in clips)
        {
            if (!EsAguaCorriendo(clip))
            {
                elegido = clip;
                break;
            }
        }

        if (clips.Length > 1)
        {
            Debug.LogWarning("[Green Mist] En " + CarpetaAgua + " hay " + clips.Length + " clips y el estanque usa uno solo: '"
                             + elegido.name + "' (se prefiere el que NO es de arroyo/cascada; si hay varios así, el primero por "
                             + "orden alfabético). Cuando te guste cómo quedó, borrá los otros desde la ventana Project de Unity.");
        }
        return elegido;
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
    // 'avisarSiSobran': para los loops que usan un solo clip, avisa si hay mas de uno.
    static AudioClip[] BuscarClips(string carpeta, bool esLoop, bool mono, bool avisarSiSobran)
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

        if (avisarSiSobran && clips.Count > 1)
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
