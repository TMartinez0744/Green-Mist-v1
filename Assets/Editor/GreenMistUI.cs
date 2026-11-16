using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menu "Green Mist > 8. Interfaz (pausa y vida)".
//
// Arma, toda por codigo, la barra de vida de Onji y el menu de pausa dentro
// de un Canvas propio:
//
//   GM_UI_Canvas                (Canvas nuevo, escala con la pantalla, referencia 1920x1080)
//     GM_UI_Vida                (la barra de vida, abajo a la izquierda)          -> ThinHealthBar
//       GM_UI_Vida_Borde / _Fondo / _Estela / _Relleno
//     GM_UI_Pausa               (el menu de pausa, queda APAGADO)                 -> PauseMenuView
//       GM_UI_Pausa_Fondo       (el oscurecido de toda la pantalla)
//       GM_UI_Pausa_Degradado   (un poco mas oscuro del lado izquierdo, donde va el texto)
//       GM_UI_Pausa_Columna     (PAUSED, RESUME, MAIN MENU, QUIT, MUSIC, la ayuda)
//
// Los colores, la letra, la flecha de pincel y las piezas (opcion, slider,
// barra) NO estan aca: estan en GreenMistUIEstilo.cs, que comparten todos los
// menus de interfaz para que las pantallas se vean como un mismo juego.
//
// POR QUE UN CANVAS APARTE: el "Canvas" de la escena esta en Constant Pixel Size
// (todo medido en pixeles fijos), asi que en otra resolucion las cosas se
// corren o quedan chicas. Este Canvas escala con el alto de la pantalla y se
// ve igual en 16:9 y 16:10. Se dibuja DEBAJO del Canvas viejo (Sort Order -1).
// El fundido a negro (FadeCanvas, 999) tapa todo.
//
// LO VIEJO NO SE BORRA: "PauseMenuPanel" ya estaba apagado y ahi queda;
// "PlayerHealthSlider" (la barra roja) se APAGA. Para volver atras:
// Green Mist > 8b. Volver a la interfaz vieja.
//
// SE PUEDE CORRER LAS VECES QUE QUIERAS: no duplica nada.
//   - Las conexiones (botones, slider, referencias) se rehacen siempre.
//   - Los tamaños, colores y posiciones se escriben cuando se CREA cada objeto.
//     Si ya existe, se respeta lo que hayas ajustado a mano...
//   - ...salvo cuando cambia el ESTILO de fabrica (GreenMistUIEstilo.VersionEstilo).
//     La barra y el panel de pausa guardan con que version se armaron (campo
//     "Style Version" de ThinHealthBar y de PauseMenuView). Si es mas vieja que
//     la actual, este menu les vuelve a poner TODO de fabrica una sola vez, lo
//     avisa en la Console y anota la version nueva. Desde ahi se respeta otra
//     vez lo que toques.
//   - Para forzar los valores de fabrica a mano: pone Style Version en 0 y
//     corre el menu (o borra "GM_UI_Canvas" de la Hierarchy y corre el menu).
public static class GreenMistUI
{
    const string NombreCanvas = GreenMistUIEstilo.NombreCanvas;
    const string NombreVida = "GM_UI_Vida";
    const string NombrePausa = "GM_UI_Pausa";
    const string NombrePausaVieja = "PauseMenuPanel";

    const string TituloUndo = GreenMistUIEstilo.TituloUndo;

    // ---------- Medidas (en unidades de un Canvas de 1920 x 1080) ----------
    const float ColumnaDesdeIzquierda = 0.12f;   // 0.12 = la columna de la pausa empieza al 12% del ancho

    // La barra de vida de Onji. El tamaño es el de la parte que se llena; el marco sobresale un poco.
    static readonly Vector2 VidaPosicion = new Vector2(88f, 72f);   // desde la esquina de abajo a la izquierda
    static readonly Vector2 VidaTamano = new Vector2(340f, 12f);    // largo x alto

    [MenuItem("Green Mist/8. Interfaz (pausa y vida)", false, 108)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Undo.SetCurrentGroupName(TituloUndo);

        // ---------- 1) Lo que ya hay en la escena ----------
        PauseMenu pausa = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        PlayerDeath muerte = Object.FindFirstObjectByType<PlayerDeath>(FindObjectsInactive.Include);
        PlayerHealthHUD hudViejo = Object.FindFirstObjectByType<PlayerHealthHUD>(FindObjectsInactive.Include);

        GameObject panelMenu = BuscarPanelMenuPrincipal(muerte);
        GameObject panelGameOver = muerte != null ? muerte.gameOverPanel : null;
        Health vidaJugador = BuscarVidaJugador(muerte, hudViejo);

        // ---------- 2) El Canvas nuevo ----------
        // Sort Order -1: debajo del Canvas viejo (0) y del fundido a negro (999).
        bool canvasNuevo;
        GameObject raiz = GreenMistUIEstilo.CanvasEscalado(NombreCanvas, -1, out canvasNuevo);

        // Si venis de una version anterior de "8b. Volver a la interfaz vieja", el Canvas quedo apagado.
        Prender(raiz);

        // ---------- 3) Barra de vida y menu de pausa ----------
        bool vidaRestilada;
        ThinHealthBar barra = ArmarVida(raiz.transform, out vidaRestilada);

        bool pausaNueva, pausaRestilada;
        PauseMenuItem opcionResume, opcionMenu, opcionQuit;
        PauseMenuView vista = ArmarPausa(raiz.transform, out pausaNueva, out pausaRestilada,
                                         out opcionResume, out opcionMenu, out opcionQuit);
        GameObject panelNuevo = vista.gameObject;

        // ---------- 4) Conexiones (esto se rehace siempre) ----------
        GreenMistUIEstilo.Registrar(barra);
        barra.target = vidaJugador;
        barra.pausePanel = panelNuevo;
        if (panelMenu != null) barra.mainMenuPanel = panelMenu;
        if (panelGameOver != null) barra.gameOverPanel = panelGameOver;

        if (pausa != null)
        {
            GreenMistUIEstilo.Registrar(pausa);
            pausa.pausePanel = panelNuevo;   // el panel viejo ya estaba apagado y nadie lo vuelve a prender
            if (panelMenu != null) pausa.mainMenuPanel = panelMenu;
            if (panelGameOver != null) pausa.gameOverPanel = panelGameOver;
            if (vidaJugador != null) pausa.playerHealth = vidaJugador;

            // Las tres opciones llaman a metodos de PauseMenu. Quedan guardadas en la
            // escena, igual que si las hubieras arrastrado a mano al "On Click".
            GreenMistUIEstilo.ConectarClic(opcionResume, pausa.Resume);
            GreenMistUIEstilo.ConectarClic(opcionMenu, pausa.GoToMainMenu);
            GreenMistUIEstilo.ConectarClic(opcionQuit, pausa.Quit);
        }
        else
        {
            Debug.LogWarning("[Green Mist] No encontré el componente PauseMenu (suele estar en 'UIManager'). "
                             + "El menú de pausa nuevo quedó armado pero nadie lo abre: agregá PauseMenu a UIManager y volvé a correr este menú.");
        }

        if (vista.musicSlider != null)
        {
            // El slider llama a PauseMenuView.SetMusicVolume (que usa MusicManager.Instance).
            // Por las dudas se saca cualquier conexion directa al MusicManager de la escena:
            // esa referencia queda muerta despues de recargar la escena.
            GreenMistUIEstilo.QuitarConexiones(vista.musicSlider, vista.musicSlider.onValueChanged, typeof(MusicManager));
            GreenMistUIEstilo.ConectarSlider(vista.musicSlider, vista.SetMusicVolume);
        }

        // ---------- 5) Lo viejo se apaga (no se borra) ----------
        bool apagueBarraVieja = false;
        if (hudViejo != null && hudViejo.slider != null && hudViejo.slider.gameObject.activeSelf)
        {
            Undo.RecordObject(hudViejo.slider.gameObject, TituloUndo);
            hudViejo.slider.gameObject.SetActive(false);
            apagueBarraVieja = true;
        }

        // El menu de pausa queda apagado en la escena: lo prende PauseMenu con Esc.
        if (pausaNueva) panelNuevo.SetActive(false);

        // ---------- 6) Chequeos ----------
        if (vidaJugador == null)
            Debug.LogWarning("[Green Mist] No encontré el componente Health de Onji (en 'PlayerRoot'): la barra de vida nueva no va a mostrar nada.");
        if (panelMenu == null || panelGameOver == null)
            Debug.LogWarning("[Green Mist] PlayerDeath (en PlayerRoot) no tiene asignado Main Menu Panel o Game Over Panel. "
                             + "Sin eso se podría abrir la pausa encima de esos menús. Asignalos en PlayerDeath y volvé a correr este menú.");
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            Debug.LogWarning("[Green Mist] No hay ningún EventSystem en la escena: el mouse no va a funcionar en los menús. "
                             + "Creá uno con GameObject > UI > Event System.");

        GreenMistEditorUtil.MarcarEscenaSucia();

        // Que se restilo (solo pasa una vez por cada cambio de estilo).
        string restilado = "";
        if (vidaRestilada || pausaRestilada)
        {
            restilado = " ESTILO NUEVO (versión " + GreenMistUIEstilo.VersionEstilo + ") aplicado a "
                        + (vidaRestilada ? "la barra de vida (más gruesa, con marco, relleno carmesí y estela clara)" : "")
                        + (vidaRestilada && pausaRestilada ? " y a " : "")
                        + (pausaRestilada ? "el menú de pausa (acento jade y flecha de pincel en vez de la rayita)" : "")
                        + ": tamaños, colores y posiciones volvieron a los valores de fábrica. Desde ahora se respeta otra vez lo que ajustes a mano.";
        }

        Debug.Log("[Green Mist] Interfaz lista: barra de vida '" + NombreVida + "' y menú de pausa '" + NombrePausa
                  + "' dentro de '" + NombreCanvas + "'." + restilado
                  + (apagueBarraVieja ? " Apagué la barra roja vieja (PlayerHealthSlider)." : "")
                  + " Para volver atrás: Green Mist > 8b. Guardá la escena con Ctrl+S.");
    }

    // Deja todo como antes: PauseMenu vuelve a usar el panel viejo, se prende la
    // barra roja vieja y se apaga la barra nueva (no se borra nada).
    //
    // A proposito NO apaga "GM_UI_Canvas" entero: en ese Canvas pueden vivir
    // pantallas de otros menus Green Mist (menu principal, derrota) y se
    // apagarian con el. El panel de pausa nuevo ya esta apagado y, como PauseMenu
    // deja de apuntarle, nadie lo vuelve a prender.
    [MenuItem("Green Mist/8b. Volver a la interfaz vieja", false, 109)]
    public static void Restaurar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Undo.SetCurrentGroupName(TituloUndo + " (volver a la vieja)");

        GameObject raiz = GreenMistEditorUtil.BuscarRaiz(NombreCanvas);

        // 1) PauseMenu vuelve a abrir el panel viejo.
        PauseMenu pausa = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        GameObject panelViejo = BuscarPorNombre(NombrePausaVieja, raiz);
        if (pausa != null && panelViejo != null)
        {
            Undo.RecordObject(pausa, TituloUndo);
            pausa.pausePanel = panelViejo;
            EditorUtility.SetDirty(pausa);
        }
        else
        {
            Debug.LogWarning("[Green Mist] No encontré PauseMenu o el panel viejo '" + NombrePausaVieja
                             + "'. Arrastrá a mano el panel que quieras al campo Pause Panel de PauseMenu (en UIManager).");
        }

        // 2) Vuelve la barra roja vieja.
        PlayerHealthHUD hudViejo = Object.FindFirstObjectByType<PlayerHealthHUD>(FindObjectsInactive.Include);
        if (hudViejo != null && hudViejo.slider != null && !hudViejo.slider.gameObject.activeSelf)
        {
            Undo.RecordObject(hudViejo.slider.gameObject, TituloUndo);
            hudViejo.slider.gameObject.SetActive(true);
        }

        // 3) Se apagan la barra nueva y el panel de pausa nuevo (el Canvas queda prendido).
        if (raiz != null)
        {
            Apagar(raiz.transform.Find(NombreVida));
            Apagar(raiz.transform.Find(NombrePausa));
        }

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Volví a la interfaz vieja: PauseMenu usa '" + NombrePausaVieja + "', la barra roja está prendida y '"
                  + NombreVida + "' y '" + NombrePausa + "' quedaron apagados (no se borró nada). "
                  + "Para volver a la nueva: Green Mist > 8. Guardá la escena con Ctrl+S.");
    }

    // ------------------------------------------------------------------
    // Barra de vida
    // ------------------------------------------------------------------
    //   restilada  sale en true si la barra YA existia con un estilo viejo y se le puso el nuevo.
    static ThinHealthBar ArmarVida(Transform canvas, out bool restilada)
    {
        bool nueva;
        RectTransform vida = GreenMistUIEstilo.Hijo(canvas, NombreVida, 0, out nueva);
        Prender(vida.gameObject);   // "8b" la deja apagada

        // El script guarda con que version de estilo se armo la barra.
        ThinHealthBar barra = GreenMistUIEstilo.Componente<ThinHealthBar>(vida.gameObject);
        restilada = !nueva && GreenMistUIEstilo.HayQueRestilar(barra.styleVersion);
        bool aplicar = nueva || restilada;

        if (aplicar)
        {
            // Anclada a la esquina de abajo a la izquierda, con margen generoso.
            GreenMistUIEstilo.Colocar(vida, Vector2.zero, Vector2.zero, Vector2.zero, VidaPosicion, VidaTamano);
        }

        // En el editor se ve; jugando arranca oculta (lo hace ThinHealthBar). Nunca tapa un clic.
        CanvasGroup grupo = GreenMistUIEstilo.Grupo(vida.gameObject, false);

        // Marco, pista, estela y relleno.
        GreenMistUIEstilo.PiezasBarra piezas = GreenMistUIEstilo.Barra(vida, GreenMistUIEstilo.GrosorBordeBarra, aplicar);

        GreenMistUIEstilo.Registrar(barra);
        barra.group = grupo;
        barra.fill = piezas.relleno;
        barra.trail = piezas.estela;
        barra.border = piezas.borde;
        if (aplicar)
        {
            // Los colores que usa el script mientras se juega (los de la escena son solo el punto de partida).
            barra.normalColor = GreenMistUIEstilo.Carmesi;
            barra.lowColor = GreenMistUIEstilo.CarmesiVivo;
            barra.lowBorderColor = GreenMistUIEstilo.BordeAlerta;
            barra.styleVersion = GreenMistUIEstilo.VersionEstilo;
        }
        return barra;
    }

    // ------------------------------------------------------------------
    // Menu de pausa
    // ------------------------------------------------------------------
    //   pausaNueva  sale en true si el panel se creo recien.
    //   restilada   sale en true si el panel YA existia con un estilo viejo y se le puso el nuevo.
    static PauseMenuView ArmarPausa(Transform canvas, out bool pausaNueva, out bool restilada,
                                    out PauseMenuItem opcionResume, out PauseMenuItem opcionMenu, out PauseMenuItem opcionQuit)
    {
        RectTransform panel = GreenMistUIEstilo.Hijo(canvas, NombrePausa, 1, out pausaNueva);

        // El script guarda con que version de estilo se armo el panel.
        PauseMenuView vista = GreenMistUIEstilo.Componente<PauseMenuView>(panel.gameObject);
        restilada = !pausaNueva && GreenMistUIEstilo.HayQueRestilar(vista.styleVersion);
        bool forzar = restilada;   // lo que se cree recien toma el estilo igual, sin forzar

        if (pausaNueva || forzar) GreenMistUIEstilo.Estirar(panel);
        CanvasGroup grupo = GreenMistUIEstilo.Grupo(panel.gameObject, true);

        // ---- Fondo: oscurece toda la pantalla y ataja los clics.
        GreenMistUIEstilo.Oscurecido(panel, "GM_UI_Pausa_Fondo", 0, GreenMistUIEstilo.OscurecidoAlpha, forzar);

        // ---- Degradado: un poco mas oscuro a la izquierda, para que el texto se lea
        //      aunque atras haya un farol prendido.
        GreenMistUIEstilo.SombraIzquierda(panel, "GM_UI_Pausa_Degradado", 1, 0.62f, 0.6f, forzar);

        // ---- Columna: todo el texto. Empieza al 12% del ancho y queda centrada en alto.
        RectTransform columna = GreenMistUIEstilo.Columna(panel, "GM_UI_Pausa_Columna", 2, ColumnaDesdeIzquierda,
                                                          new Vector2(600f, 470f), forzar);

        // Dentro de la columna todo se mide desde ARRIBA a la izquierda; "y" es cuanto baja.
        float y = 0f;

        GreenMistUIEstilo.Etiqueta(columna, "GM_UI_Pausa_Titulo", 0, "PAUSED", y,
                                   GreenMistUIEstilo.TamanoTitulo, GreenMistUIEstilo.EspaciadoTitulo, GreenMistUIEstilo.AcentoClaro, forzar);
        y += 46f;

        GreenMistUIEstilo.Raya(columna, "GM_UI_Pausa_Raya", 1, y, 56f, forzar);
        y += 32f;

        opcionResume = GreenMistUIEstilo.Opcion(columna, "GM_UI_Pausa_Resume", 2, "RESUME", y, forzar);
        y += GreenMistUIEstilo.PasoOpcion;
        opcionMenu = GreenMistUIEstilo.Opcion(columna, "GM_UI_Pausa_MainMenu", 3, "MAIN MENU", y, forzar);
        y += GreenMistUIEstilo.PasoOpcion;
        opcionQuit = GreenMistUIEstilo.Opcion(columna, "GM_UI_Pausa_Quit", 4, "QUIT", y, forzar);
        y += GreenMistUIEstilo.AltoOpcion + 56f;

        GreenMistUIEstilo.Etiqueta(columna, "GM_UI_Pausa_MusicaTitulo", 5, "MUSIC", y, 16f, 40f,
                                   GreenMistUIEstilo.ConAlpha(GreenMistUIEstilo.Hueso, 0.5f), forzar);
        y += 34f;

        // Arranca donde esta el volumen de la musica en la escena. Jugando,
        // PauseMenuView lo vuelve a acomodar cada vez que se abre la pausa.
        Slider slider = GreenMistUIEstilo.SliderFino(columna, "GM_UI_Pausa_Musica", 6, y, 260f,
                                                     GreenMistUIEstilo.VolumenMusicaDeLaEscena(), forzar);
        y += 24f + 42f;

        GreenMistUIEstilo.Etiqueta(columna, "GM_UI_Pausa_Ayuda", 7, "ESC   RESUME", y, 15f, 35f,
                                   GreenMistUIEstilo.ConAlpha(GreenMistUIEstilo.Hueso, 0.35f), forzar);

        // ---- El script que lo anima ----
        GreenMistUIEstilo.Registrar(vista);
        vista.group = grupo;
        vista.items = new[] { opcionResume, opcionMenu, opcionQuit };
        vista.musicSlider = slider;
        if (pausaNueva || restilada) vista.styleVersion = GreenMistUIEstilo.VersionEstilo;
        return vista;
    }

    // ------------------------------------------------------------------
    // Buscar cosas de la escena
    // ------------------------------------------------------------------

    static GameObject BuscarPanelMenuPrincipal(PlayerDeath muerte)
    {
        if (muerte != null && muerte.mainMenuPanel != null) return muerte.mainMenuPanel;

        // Plan B: el mismo panel que usa MainMenuUI. Su campo es privado, asi que
        // lo leemos como lo lee el Inspector (SerializedObject). Solo se LEE.
        MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menu != null)
        {
            SerializedProperty campo = new SerializedObject(menu).FindProperty("menuPanel");
            if (campo != null && campo.objectReferenceValue != null)
                return campo.objectReferenceValue as GameObject;
        }
        return null;
    }

    static Health BuscarVidaJugador(PlayerDeath muerte, PlayerHealthHUD hudViejo)
    {
        GameObject jugador = GreenMistEditorUtil.BuscarRaiz("PlayerRoot");
        if (jugador != null && jugador.GetComponent<Health>() != null) return jugador.GetComponent<Health>();

        if (muerte != null && muerte.GetComponent<Health>() != null) return muerte.GetComponent<Health>();
        if (hudViejo != null && hudViejo.target != null) return hudViejo.target;
        return null;
    }

    // Busca un objeto por nombre en toda la escena (tambien apagados), salteando lo que cuelga de "excluir".
    static GameObject BuscarPorNombre(string nombre, GameObject excluir)
    {
        foreach (GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (raiz == excluir) continue;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t.gameObject;
        }
        return null;
    }

    // Prende un objeto si estaba apagado (con Undo).
    static void Prender(GameObject go)
    {
        if (go == null || go.activeSelf) return;
        Undo.RecordObject(go, TituloUndo);
        go.SetActive(true);
    }

    // Apaga un objeto si estaba prendido (con Undo). No pasa nada si no existe.
    static void Apagar(Transform t)
    {
        if (t == null || !t.gameObject.activeSelf) return;
        Undo.RecordObject(t.gameObject, TituloUndo);
        t.gameObject.SetActive(false);
    }
}
