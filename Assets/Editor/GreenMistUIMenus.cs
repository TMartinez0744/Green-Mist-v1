using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Estilo = GreenMistUIEstilo;   // para no escribir "GreenMistUIEstilo." en cada linea

// Menu "Green Mist > 9. Menús (inicio y derrota)".
//
// Arma, todo por codigo, el menu de INICIO y la pantalla de DERROTA con el
// mismo estilo que el menu de pausa (los colores, la letra, la flecha de pincel
// y las piezas salen de GreenMistUIEstilo.cs):
//
//   GM_UI_Menus_Canvas          (Canvas nuevo, escala con la pantalla)            -> MenuScreenFollower
//     GM_UI_Inicio              (el menu de inicio)                               -> PauseMenuView
//       GM_UI_Inicio_Fondo      (un oscurecido MUY suave de toda la pantalla)
//       GM_UI_Inicio_Degradado  (sombra de tinta del lado izquierdo, donde va el texto)
//       GM_UI_Inicio_Columna    (WHISPERS OF THE / GREEN MIST, PLAY, QUIT, MUSIC)
//     GM_UI_Derrota             (la pantalla de derrota, queda APAGADA)           -> DefeatScreenView
//       GM_UI_Derrota_Fondo     (el oscurecido total, color tinta)
//       GM_UI_Derrota_Titulo    (la palabra DEFEAT en carmesi y una rayita)
//       GM_UI_Derrota_Opciones  (RETRY, MAIN MENU)                                -> PauseMenuView
//
// LOS INTERRUPTORES NO CAMBIAN: medio juego mira si "MainMenuPanel" esta
// prendido para saber "estamos en el menu" y si "DeathMenu" esta prendido para
// saber "game over". Esos dos objetos siguen ahi y los siguen prendiendo y
// apagando MainMenuUI y PlayerDeath, igual que antes. Lo que tenian adentro
// (el pergamino, los botones) se APAGA, no se borra. El script
// MenuScreenFollower (en GM_UI_Menus_Canvas) mira esos dos interruptores y
// prende o apaga la pantalla nueva que le corresponde a cada uno.
//
// POR QUE UN CANVAS APARTE: el "Canvas" viejo mide todo en pixeles fijos; en
// otra resolucion los menus quedarian chicos o corridos. Este escala con el
// alto de la pantalla. Su Sort Order es 10: se dibuja ENCIMA de la pausa y la
// barra de vida (GM_UI_Canvas, -1) y del Canvas viejo (0), y DEBAJO del fundido
// a negro (FadeCanvas, 999), asi el fundido de PLAY tapa tambien al menu.
//
// SE PUEDE CORRER LAS VECES QUE QUIERAS: no duplica nada. Las conexiones se
// rehacen siempre; los tamaños, colores y posiciones se escriben al CREAR cada
// objeto y despues se respeta lo que ajustes a mano (salvo que cambie el
// estilo de fabrica, igual que en el menu 8: ver "Style Version").
//
// Para volver atras: Green Mist > 9b. Volver a los menús viejos.
public static class GreenMistUIMenus
{
    const string NombreCanvas = "GM_UI_Menus_Canvas";
    const int OrdenCanvas = 10;   // encima de GM_UI_Canvas (-1) y de Canvas (0); debajo de FadeCanvas (999)

    const string NombreInicio = "GM_UI_Inicio";
    const string NombreDerrota = "GM_UI_Derrota";

    // Los interruptores, por si hubiera que buscarlos por nombre.
    const string NombrePanelMenu = "MainMenuPanel";
    const string NombrePanelDerrota = "DeathMenu";

    const string TituloUndo = "Green Mist: menús";

    // ---------- Medidas (en unidades de un Canvas de 1920 x 1080) ----------
    const float ColumnaDesdeIzquierda = 0.12f;   // igual que la pausa: la columna empieza al 12% del ancho

    const float InicioOscurecido = 0.15f;        // oscurecido de TODA la pantalla en el menu de inicio (muy suave)
    // La sombra de la izquierda llega lejos (90% del ancho) pero se pierde suave:
    // debajo de PLAY tapa mas o menos un 60%, al final de "GREEN MIST" un 30%,
    // y la mitad derecha de la pantalla queda casi limpia.
    const float InicioSombraHasta = 0.90f;       // hasta que parte del ancho llega la sombra de la izquierda
    const float InicioSombraAlpha = 0.90f;       // que tan oscura es esa sombra en el borde izquierdo

    const float SubtituloTamano = 34f;           // "WHISPERS OF THE"
    const float SubtituloEspaciado = 55f;
    const float TituloTamano = 100f;             // "GREEN MIST"
    const float TituloEspaciado = 18f;

    const float DerrotaTamano = 120f;            // "DEFEAT"
    const float DerrotaEspaciado = 42f;
    const float DerrotaAlturaTitulo = 110f;      // cuanto mas arriba del centro de la pantalla va DEFEAT
    const float DerrotaAlturaOpciones = -20f;    // donde empiezan las opciones (negativo = debajo del centro)

    [MenuItem("Green Mist/9. Menús (inicio y derrota)", false, 110)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Undo.SetCurrentGroupName(TituloUndo);

        // ---------- 1) Lo que ya hay en la escena ----------
        MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        PlayerDeath muerte = Object.FindFirstObjectByType<PlayerDeath>(FindObjectsInactive.Include);

        GameObject panelMenu = BuscarPanelMenu(menu, muerte);
        GameObject panelDerrota = BuscarPanelDerrota(muerte);

        if (panelMenu == null && panelDerrota == null)
        {
            Debug.LogWarning("[Green Mist] No encontré ni '" + NombrePanelMenu + "' ni '" + NombrePanelDerrota
                             + "' (los paneles que prenden MainMenuUI y PlayerDeath). Sin ellos no hay a qué enganchar los menús nuevos: no cambié nada.");
            return;
        }

        // ---------- 2) El Canvas nuevo ----------
        bool canvasNuevo;
        GameObject raiz = Estilo.CanvasEscalado(NombreCanvas, OrdenCanvas, out canvasNuevo);
        Prender(raiz);   // "9b" lo deja apagado

        MenuScreenFollower seguidor = Estilo.Componente<MenuScreenFollower>(raiz);

        // ---------- 3) Menu de inicio ----------
        bool inicioRestilado = false;
        GameObject pantallaInicio = null;
        if (panelMenu != null)
        {
            bool inicioNuevo;
            PauseMenuItem opcionPlay, opcionQuit;
            PauseMenuView vistaInicio = ArmarInicio(raiz.transform, out inicioNuevo, out inicioRestilado, out opcionPlay, out opcionQuit);
            pantallaInicio = vistaInicio.gameObject;

            if (menu != null)
            {
                // Las mismas funciones que llamaban los botones viejos: PLAY hace el
                // fundido, arranca el tiempo, cambia la musica y apaga el menu.
                Estilo.ConectarClic(opcionPlay, menu.OnStart);
                Estilo.ConectarClic(opcionQuit, menu.OnQuit);
            }
            else
            {
                Debug.LogWarning("[Green Mist] No encontré el componente MainMenuUI (suele estar en 'UIManager'). "
                                 + "El menú de inicio nuevo quedó armado pero PLAY y QUIT no hacen nada: agregá MainMenuUI a UIManager y volvé a correr este menú.");
            }

            if (vistaInicio.musicSlider != null)
            {
                // El slider llama a PauseMenuView.SetMusicVolume, que usa el MusicManager que
                // esta vivo. Una conexion directa al MusicManager de la escena queda muerta
                // despues de recargar la escena (por eso el slider viejo dejaba de andar).
                Estilo.QuitarConexiones(vistaInicio.musicSlider, vistaInicio.musicSlider.onValueChanged, typeof(MusicManager));
                Estilo.ConectarSlider(vistaInicio.musicSlider, vistaInicio.SetMusicVolume);
            }
        }
        else
        {
            Debug.LogWarning("[Green Mist] No encontré '" + NombrePanelMenu + "' (el panel que prende MainMenuUI): no armé el menú de inicio nuevo. "
                             + "Asigná el panel en el campo Menu Panel de MainMenuUI y volvé a correr este menú.");
        }

        // ---------- 4) Pantalla de derrota ----------
        bool derrotaRestilada = false;
        GameObject pantallaDerrota = null;
        if (panelDerrota != null)
        {
            bool derrotaNueva;
            PauseMenuItem opcionRetry, opcionMenu;
            DefeatScreenView vistaDerrota = ArmarDerrota(raiz.transform, muerte, out derrotaNueva, out derrotaRestilada, out opcionRetry, out opcionMenu);
            pantallaDerrota = vistaDerrota.gameObject;

            if (muerte != null)
            {
                Estilo.ConectarClic(opcionRetry, muerte.Retry);
                Estilo.ConectarClic(opcionMenu, muerte.MainMenu);

                Estilo.Registrar(muerte);
                if (muerte.gameOverPanel == null) muerte.gameOverPanel = panelDerrota;
                // La pantalla nueva hace su propio oscurecido: PlayerDeath la prende apenas termina la animacion.
                muerte.panelFadesIn = true;
            }
            else
            {
                Debug.LogWarning("[Green Mist] No encontré el componente PlayerDeath (suele estar en 'PlayerRoot'). "
                                 + "La pantalla de derrota nueva quedó armada pero nadie la muestra y RETRY / MAIN MENU no hacen nada.");
            }
        }
        else
        {
            Debug.LogWarning("[Green Mist] No encontré '" + NombrePanelDerrota + "' (el Game Over Panel de PlayerDeath): no armé la pantalla de derrota nueva. "
                             + "Asigná el panel en PlayerDeath (en PlayerRoot) y volvé a correr este menú.");
        }

        // ---------- 5) El cable entre los interruptores y las pantallas nuevas ----------
        Estilo.Registrar(seguidor);
        if (pantallaInicio != null)
        {
            seguidor.mainMenuPanel = panelMenu;
            seguidor.startScreen = pantallaInicio;
        }
        if (pantallaDerrota != null)
        {
            seguidor.gameOverPanel = panelDerrota;
            seguidor.defeatScreen = pantallaDerrota;
        }

        // ---------- 6) Lo viejo se apaga (no se borra) y queda anotado para "9b" ----------
        int apagados = 0;
        if (pantallaInicio != null) apagados += ApagarLoViejo(panelMenu, seguidor);
        if (pantallaDerrota != null) apagados += ApagarLoViejo(panelDerrota, seguidor);

        // Cada pantalla nueva queda igual que su interruptor: el menu de inicio
        // prendido (asi arranca la escena) y la derrota apagada.
        if (pantallaInicio != null) PonerActivo(pantallaInicio, panelMenu.activeSelf);
        if (pantallaDerrota != null) PonerActivo(pantallaDerrota, panelDerrota.activeSelf);

        // ---------- 7) Chequeos ----------
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            Debug.LogWarning("[Green Mist] No hay ningún EventSystem en la escena: el mouse no va a funcionar en los menús. "
                             + "Creá uno con GameObject > UI > Event System.");

        Canvas canvasMenus = raiz.GetComponent<Canvas>();
        ScreenFader fundido = Object.FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Include);
        Canvas canvasFundido = fundido != null ? fundido.GetComponentInParent<Canvas>() : null;
        if (canvasMenus != null && canvasFundido != null && canvasFundido.rootCanvas.sortingOrder <= canvasMenus.sortingOrder)
            Debug.LogWarning("[Green Mist] El fundido a negro ('" + canvasFundido.rootCanvas.name + "', Sort Order " + canvasFundido.rootCanvas.sortingOrder
                             + ") quedó por debajo de '" + NombreCanvas + "' (Sort Order " + canvasMenus.sortingOrder
                             + "): el fundido de PLAY no va a tapar el menú. Subile el Sort Order al Canvas del fundido o bajáselo a '" + NombreCanvas + "'.");

        // La pausa y la barra de vida (GM_UI_Canvas) tienen que quedar DEBAJO de estos menus.
        GameObject raizPausa = GreenMistEditorUtil.BuscarRaiz(Estilo.NombreCanvas);
        Canvas canvasPausa = raizPausa != null ? raizPausa.GetComponent<Canvas>() : null;
        if (canvasMenus != null && canvasPausa != null && canvasPausa.sortingOrder >= canvasMenus.sortingOrder)
            Debug.LogWarning("[Green Mist] '" + Estilo.NombreCanvas + "' (Sort Order " + canvasPausa.sortingOrder
                             + ") quedó por encima de '" + NombreCanvas + "' (Sort Order " + canvasMenus.sortingOrder
                             + "): la barra de vida se va a dibujar encima del menú de inicio y de la derrota. Bajale el Sort Order a '"
                             + Estilo.NombreCanvas + "' (de fábrica es -1) o subíselo a '" + NombreCanvas + "' (de fábrica es " + OrdenCanvas + ").");

        GreenMistEditorUtil.MarcarEscenaSucia();

        string restilado = "";
        if (inicioRestilado || derrotaRestilada)
        {
            restilado = " ESTILO NUEVO (versión " + Estilo.VersionEstilo + ") aplicado a "
                        + (inicioRestilado ? "el menú de inicio" : "")
                        + (inicioRestilado && derrotaRestilada ? " y a " : "")
                        + (derrotaRestilada ? "la pantalla de derrota" : "")
                        + ": tamaños, colores y posiciones volvieron a los valores de fábrica.";
        }

        Debug.Log("[Green Mist] Menús listos dentro de '" + NombreCanvas + "': "
                  + (pantallaInicio != null ? "menú de inicio '" + NombreInicio + "'" : "")
                  + (pantallaInicio != null && pantallaDerrota != null ? " y " : "")
                  + (pantallaDerrota != null ? "pantalla de derrota '" + NombreDerrota + "' (queda apagada: la prende PlayerDeath)" : "")
                  + "." + restilado
                  + (apagados > 0 ? " Apagué " + apagados + " cosas viejas de '" + NombrePanelMenu + "' y '" + NombrePanelDerrota + "' (no se borró nada)." : "")
                  + " Para volver atrás: Green Mist > 9b. Guardá la escena con Ctrl+S.");
    }

    // Deja todo como antes: vuelve a prender lo viejo que habia dentro de
    // MainMenuPanel y DeathMenu, apaga el Canvas de los menus nuevos (no se
    // borra) y PlayerDeath vuelve a mostrar el panel de golpe, al final.
    [MenuItem("Green Mist/9b. Volver a los menús viejos", false, 111)]
    public static void Restaurar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Undo.SetCurrentGroupName(TituloUndo + " (volver a los viejos)");

        GameObject raiz = GreenMistEditorUtil.BuscarRaiz(NombreCanvas);
        MenuScreenFollower seguidor = raiz != null ? raiz.GetComponent<MenuScreenFollower>() : null;
        PlayerDeath muerte = Object.FindFirstObjectByType<PlayerDeath>(FindObjectsInactive.Include);

        // 1) Vuelve lo viejo.
        int prendidos = 0;
        bool habiaAnotado = seguidor != null
                            && ((seguidor.hiddenOldObjects != null && seguidor.hiddenOldObjects.Length > 0)
                                || (seguidor.hiddenOldGraphics != null && seguidor.hiddenOldGraphics.Length > 0));
        if (habiaAnotado)
        {
            // Exactamente lo que apago el menu 9 (quedo anotado en MenuScreenFollower).
            if (seguidor.hiddenOldObjects != null)
            {
                foreach (GameObject viejo in seguidor.hiddenOldObjects)
                {
                    if (viejo == null || viejo.activeSelf) continue;
                    PonerActivo(viejo, true);
                    prendidos++;
                }
            }
            if (seguidor.hiddenOldGraphics != null)
            {
                foreach (Behaviour imagen in seguidor.hiddenOldGraphics)
                {
                    if (imagen == null || imagen.enabled) continue;
                    Estilo.Registrar(imagen);
                    imagen.enabled = true;
                    prendidos++;
                }
            }

            Estilo.Registrar(seguidor);
            seguidor.hiddenOldObjects = new GameObject[0];
            seguidor.hiddenOldGraphics = new Behaviour[0];
        }
        else if (seguidor == null)
        {
            // No existe quien tenia la lista (por ejemplo, se borro GM_UI_Menus_Canvas
            // a mano): plan B, prendemos todo lo que cuelga directo de los dos paneles viejos.
            MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            prendidos += PrenderTodo(BuscarPanelMenu(menu, muerte));
            prendidos += PrenderTodo(BuscarPanelDerrota(muerte));
        }
        // Si la lista existe pero esta vacia es que "9b" ya se habia corrido (o que
        // "9" no encontro nada para apagar): lo viejo ya esta como estaba y no se
        // prende nada mas. Asi correr "9b" dos veces no cambia nada la segunda vez.

        // 2) Se apaga el Canvas de los menus nuevos entero (adentro solo hay menus nuevos).
        //    Con el Canvas apagado, MenuScreenFollower deja de prender las pantallas.
        if (raiz != null) PonerActivo(raiz, false);

        // 3) PlayerDeath vuelve al comportamiento del menu viejo.
        if (muerte != null && muerte.panelFadesIn)
        {
            Estilo.Registrar(muerte);
            muerte.panelFadesIn = false;
        }

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Volví a los menús viejos: prendí " + prendidos + " cosas de '" + NombrePanelMenu + "' y '" + NombrePanelDerrota
                  + "' y apagué '" + NombreCanvas + "' (no se borró nada). MAIN MENU en la derrota sigue recargando la escena (eso es un arreglo, no cambia). "
                  + "Para volver a los nuevos: Green Mist > 9. Guardá la escena con Ctrl+S.");
    }

    // ------------------------------------------------------------------
    // Menu de inicio
    // ------------------------------------------------------------------
    //   nueva      sale en true si la pantalla se creo recien.
    //   restilada  sale en true si YA existia con un estilo viejo y se le puso el nuevo.
    static PauseMenuView ArmarInicio(Transform canvas, out bool nueva, out bool restilada,
                                     out PauseMenuItem opcionPlay, out PauseMenuItem opcionQuit)
    {
        RectTransform panel = Estilo.Hijo(canvas, NombreInicio, 0, out nueva);

        // El mismo script que anima la pausa: fundido de entrada, opcion elegida, teclado, slider.
        PauseMenuView vista = Estilo.Componente<PauseMenuView>(panel.gameObject);
        restilada = !nueva && Estilo.HayQueRestilar(vista.styleVersion);
        bool forzar = restilada;   // lo que se cree recien toma el estilo igual, sin forzar

        if (nueva || forzar) Estilo.Estirar(panel);
        CanvasGroup grupo = Estilo.Grupo(panel.gameObject, true);

        // ---- Fondo: el pueblo de noche se sigue viendo atras. Solo un oscurecido MUY
        //      suave de toda la pantalla (y ataja los clics).
        Estilo.Oscurecido(panel, "GM_UI_Inicio_Fondo", 0, InicioOscurecido, forzar);

        // ---- Sombra de tinta del lado izquierdo, donde va el texto: se va perdiendo
        //      hacia la derecha, asi el titulo se lee aunque atras haya un farol.
        Estilo.SombraIzquierda(panel, "GM_UI_Inicio_Degradado", 1, InicioSombraHasta, InicioSombraAlpha, forzar);

        // ---- Columna: todo el texto, igual que en la pausa.
        RectTransform columna = Estilo.Columna(panel, "GM_UI_Inicio_Columna", 2, ColumnaDesdeIzquierda,
                                               new Vector2(900f, 464f), forzar);

        // Dentro de la columna todo se mide desde ARRIBA a la izquierda; "y" es cuanto baja.
        // El titulo va en dos renglones: uno chico en jade y el nombre grande en hueso.
        Estilo.Etiqueta(columna, "GM_UI_Inicio_Subtitulo", 0, "WHISPERS OF THE", 0f,
                        SubtituloTamano, SubtituloEspaciado, Estilo.AcentoClaro, forzar);

        // (El rectangulo de un texto es mas alto que sus letras: por eso este "y"
        // parece pisar al renglon de arriba, pero las letras no se tocan.)
        Estilo.Etiqueta(columna, "GM_UI_Inicio_Titulo", 1, "GREEN MIST", 30f,
                        TituloTamano, TituloEspaciado, Estilo.Hueso, forzar);

        Estilo.Raya(columna, "GM_UI_Inicio_Raya", 2, 178f, 56f, forzar);

        float y = 210f;
        opcionPlay = Estilo.Opcion(columna, "GM_UI_Inicio_Play", 3, "PLAY", y, forzar);
        y += Estilo.PasoOpcion;
        opcionQuit = Estilo.Opcion(columna, "GM_UI_Inicio_Quit", 4, "QUIT", y, forzar);
        y += Estilo.AltoOpcion + 56f;

        Estilo.Etiqueta(columna, "GM_UI_Inicio_MusicaTitulo", 5, "MUSIC", y, 16f, 40f,
                        Estilo.ConAlpha(Estilo.Hueso, 0.5f), forzar);
        y += 34f;

        // Arranca donde esta el volumen de la musica en la escena. Jugando,
        // PauseMenuView lo acomoda al volumen real cada vez que aparece el menu.
        Slider slider = Estilo.SliderFino(columna, "GM_UI_Inicio_Musica", 6, y, 260f,
                                          Estilo.VolumenMusicaDeLaEscena(), forzar);

        // ---- El script que lo anima ----
        Estilo.Registrar(vista);
        vista.group = grupo;
        vista.items = new[] { opcionPlay, opcionQuit };
        vista.musicSlider = slider;
        if (nueva) vista.fadeTime = 1.2f;   // el menu de inicio aparece mas despacio que la pausa
        if (nueva || restilada) vista.styleVersion = Estilo.VersionEstilo;
        return vista;
    }

    // ------------------------------------------------------------------
    // Pantalla de derrota
    // ------------------------------------------------------------------
    static DefeatScreenView ArmarDerrota(Transform canvas, PlayerDeath muerte, out bool nueva, out bool restilada,
                                         out PauseMenuItem opcionRetry, out PauseMenuItem opcionMenu)
    {
        RectTransform panel = Estilo.Hijo(canvas, NombreDerrota, 1, out nueva);

        // El script guarda con que version de estilo se armo la pantalla.
        DefeatScreenView vista = Estilo.Componente<DefeatScreenView>(panel.gameObject);
        restilada = !nueva && Estilo.HayQueRestilar(vista.styleVersion);
        bool forzar = restilada;

        if (nueva || forzar) Estilo.Estirar(panel);

        Vector2 centro = new Vector2(0.5f, 0.5f);
        bool n;

        // ---- Fondo: tinta TOTAL (alpha 1). Jugando arranca transparente y
        //      DefeatScreenView lo oscurece de a poco hasta este valor.
        Image fondo = Estilo.Oscurecido(panel, "GM_UI_Derrota_Fondo", 0, 1f, forzar);

        // ---- Titulo: un objeto con CanvasGroup (para fundirlo de una) que tiene
        //      la palabra y, debajo, la rayita. Centrado, un poco arriba del medio.
        RectTransform titulo = Estilo.Hijo(panel, "GM_UI_Derrota_Titulo", 1, out n);
        if (n || forzar) Estilo.Colocar(titulo, centro, centro, centro, new Vector2(0f, DerrotaAlturaTitulo), new Vector2(1400f, 180f));
        CanvasGroup grupoTitulo = Estilo.Grupo(titulo.gameObject, false);

        // Carmesi: el unico lugar de los menus donde se usa (ademas de la vida).
        RectTransform palabra = Estilo.Hijo(titulo, "GM_UI_Derrota_Palabra", 0, out n);
        if (n || forzar) Estilo.Estirar(palabra);
        Estilo.Texto(palabra, n || forzar, "DEFEAT", DerrotaTamano, DerrotaEspaciado, Estilo.Carmesi, TextAlignmentOptions.Capline);

        // La rayita: pegada al borde de abajo del titulo, centrada.
        RectTransform raya = Estilo.Hijo(titulo, "GM_UI_Derrota_Raya", 1, out n);
        if (n || forzar) Estilo.Colocar(raya, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), centro, Vector2.zero, new Vector2(56f, 2f));
        Estilo.Imagen(raya, n || forzar, Estilo.ConAlpha(Estilo.Hueso, 0.3f), null, false);

        // ---- Opciones: su propio objeto, con el mismo script que la pausa.
        //      DefeatScreenView lo tiene apagado hasta que les toca aparecer.
        bool opcionesNuevas;
        RectTransform opciones = Estilo.Hijo(panel, "GM_UI_Derrota_Opciones", 2, out opcionesNuevas);
        if (opcionesNuevas || forzar)
        {
            // Pivote arriba al centro: "y" de cada opcion se mide desde su borde de arriba.
            Estilo.Colocar(opciones, centro, centro, new Vector2(0.5f, 1f), new Vector2(0f, DerrotaAlturaOpciones),
                           new Vector2(700f, Estilo.PasoOpcion + Estilo.AltoOpcion));
        }
        CanvasGroup grupoOpciones = Estilo.Grupo(opciones.gameObject, true);

        opcionRetry = OpcionCentrada(opciones, "GM_UI_Derrota_Retry", 0, "RETRY", 0f, forzar);
        opcionMenu = OpcionCentrada(opciones, "GM_UI_Derrota_MainMenu", 1, "MAIN MENU", Estilo.PasoOpcion, forzar);

        PauseMenuView lista = Estilo.Componente<PauseMenuView>(opciones.gameObject);
        Estilo.Registrar(lista);
        lista.group = grupoOpciones;
        lista.items = new[] { opcionRetry, opcionMenu };
        lista.musicSlider = null;
        if (opcionesNuevas)
        {
            lista.fadeTime = 0.8f;
            lista.nudge = 0f;   // centradas: el texto elegido no se corre al costado
        }
        if (opcionesNuevas || restilada) lista.styleVersion = Estilo.VersionEstilo;

        // ---- El script de la secuencia ----
        Estilo.Registrar(vista);
        vista.dim = fondo;
        vista.title = grupoTitulo;
        vista.options = opciones.gameObject;
        if (nueva && muerte != null)
        {
            // Los tiempos de PlayerDeath: el oscurecido dura "Fundido Negro" y DEFEAT
            // empieza cuando PlayerDeath frena el juego ("Fundido Negro" + "Espera En Negro").
            vista.dimTime = Mathf.Clamp(muerte.fundidoNegro, 0f, 5f);
            vista.titleDelay = Mathf.Clamp(muerte.fundidoNegro + muerte.esperaEnNegro, 0f, 8f);
            vista.optionsDelay = Mathf.Clamp(vista.titleDelay + 1.5f, 0f, 12f);
        }
        if (nueva || restilada) vista.styleVersion = Estilo.VersionEstilo;
        return vista;
    }

    // Una opcion de menu CENTRADA. Estilo.Opcion la arma alineada a la izquierda
    // (texto + flecha a su izquierda). Para centrarla sin tocar el texto, el
    // rectangulo de la opcion se hace del ANCHO JUSTO de su texto y se centra en
    // el padre: el texto queda en el medio y la flecha, pegada a su izquierda.
    //   y  cuantas unidades baja desde el borde de arriba del padre.
    static PauseMenuItem OpcionCentrada(Transform padre, string nombre, int orden, string texto, float y, bool forzarEstilo)
    {
        bool nueva = padre.Find(nombre) == null;
        PauseMenuItem item = Estilo.Opcion(padre, nombre, orden, texto, y, forzarEstilo);

        if (nueva || forzarEstilo)
        {
            float ancho = AnchoDeTexto(item.label);
            Vector2 arribaCentro = new Vector2(0.5f, 1f);
            Estilo.Colocar((RectTransform)item.transform, arribaCentro, arribaCentro, arribaCentro,
                           new Vector2(0f, -y), new Vector2(ancho, Estilo.AltoOpcion));
        }
        return item;
    }

    // Cuanto mide de ancho un texto de una linea, en unidades del Canvas.
    // Suma lo que "avanza" cada letra segun la fuente, mas la separacion entre letras.
    // No necesita que el objeto este prendido ni que el texto ya se haya dibujado.
    static float AnchoDeTexto(TextMeshProUGUI etiqueta)
    {
        string texto = etiqueta.text;
        if (string.IsNullOrEmpty(texto)) return Estilo.AnchoOpcion;

        float tamano = etiqueta.fontSize;
        TMP_FontAsset fuente = etiqueta.font;

        // Cuantas unidades de Canvas mide una unidad de la fuente a este tamaño de letra.
        float escala = 0f;
        if (fuente != null && fuente.faceInfo.pointSize > 0)
        {
            // Fuente dinamica: las letras se agregan a la fuente recien cuando hacen falta.
            if (fuente.atlasPopulationMode == AtlasPopulationMode.Dynamic) fuente.TryAddCharacters(texto);
            escala = tamano / fuente.faceInfo.pointSize * fuente.faceInfo.scale;
        }

        float ancho = 0f;
        for (int i = 0; i < texto.Length; i++)
        {
            char letra = texto[i];
            TMP_Character dato;
            if (escala > 0f && fuente.characterLookupTable.TryGetValue(letra, out dato) && dato != null && dato.glyph != null)
                ancho += dato.glyph.metrics.horizontalAdvance * escala;
            else
                ancho += tamano * (letra == ' ' ? 0.35f : 0.72f);   // la fuente no tiene esa letra: un ancho tipico
        }

        // La separacion entre letras esta en centesimas del tamaño de la letra, y va ENTRE letras.
        ancho += (texto.Length - 1) * etiqueta.characterSpacing * tamano * 0.01f;
        return Mathf.Ceil(ancho);
    }

    // ------------------------------------------------------------------
    // Lo viejo
    // ------------------------------------------------------------------

    // Apaga todo lo que cuelga DIRECTO del panel viejo (botones, fondos, titulo)
    // y desactiva la imagen del propio panel, si tiene. El panel NO se toca: sigue
    // siendo el interruptor que miran los demas scripts. Lo que apaga queda
    // anotado en MenuScreenFollower para que "9b" lo vuelva a prender.
    // Devuelve cuantas cosas apago.
    static int ApagarLoViejo(GameObject panel, MenuScreenFollower seguidor)
    {
        if (panel == null) return 0;

        // Por las dudas: un panel de menu siempre cuelga de un Canvas. Si lo que
        // esta asignado es un objeto RAIZ (el Canvas entero), apagarle los hijos
        // se llevaria puesto al EventSystem y a medio juego.
        if (panel.transform.parent == null)
        {
            Debug.LogWarning("[Green Mist] '" + panel.name + "' es un objeto raíz de la escena, no un panel de menú: no le apagué nada adentro. "
                             + "Revisá qué panel tienen asignado MainMenuUI (Menu Panel) y PlayerDeath (Game Over Panel).");
            return 0;
        }

        // Lo que ya estaba anotado de antes (salteando huecos: cosas borradas a mano).
        List<GameObject> objetos = new List<GameObject>();
        if (seguidor.hiddenOldObjects != null)
        {
            foreach (GameObject anotado in seguidor.hiddenOldObjects)
                if (anotado != null) objetos.Add(anotado);
        }
        List<Behaviour> imagenes = new List<Behaviour>();
        if (seguidor.hiddenOldGraphics != null)
        {
            foreach (Behaviour anotada in seguidor.hiddenOldGraphics)
                if (anotada != null) imagenes.Add(anotada);
        }

        int apagados = 0;

        foreach (Transform hijo in panel.transform)
        {
            if (!hijo.gameObject.activeSelf) continue;   // ya estaba apagado: no es nuestro
            PonerActivo(hijo.gameObject, false);
            if (!objetos.Contains(hijo.gameObject)) objetos.Add(hijo.gameObject);
            apagados++;
        }

        foreach (Graphic imagen in panel.GetComponents<Graphic>())
        {
            if (!imagen.enabled) continue;
            Estilo.Registrar(imagen);
            imagen.enabled = false;
            if (!imagenes.Contains(imagen)) imagenes.Add(imagen);
            apagados++;
        }

        if (apagados > 0)
        {
            Estilo.Registrar(seguidor);
            seguidor.hiddenOldObjects = objetos.ToArray();
            seguidor.hiddenOldGraphics = imagenes.ToArray();
        }
        return apagados;
    }

    // Plan B de "9b": prende todo lo que cuelga directo del panel y su imagen. Devuelve cuantas cosas prendio.
    static int PrenderTodo(GameObject panel)
    {
        if (panel == null) return 0;

        int prendidos = 0;
        foreach (Transform hijo in panel.transform)
        {
            if (hijo.gameObject.activeSelf) continue;
            PonerActivo(hijo.gameObject, true);
            prendidos++;
        }
        foreach (Graphic imagen in panel.GetComponents<Graphic>())
        {
            if (imagen.enabled) continue;
            Estilo.Registrar(imagen);
            imagen.enabled = true;
            prendidos++;
        }
        return prendidos;
    }

    // ------------------------------------------------------------------
    // Buscar cosas de la escena
    // ------------------------------------------------------------------

    // El panel que MainMenuUI prende y apaga: ese es el interruptor de "estamos en el menu".
    static GameObject BuscarPanelMenu(MainMenuUI menu, PlayerDeath muerte)
    {
        if (menu != null)
        {
            // Su campo es privado, asi que lo leemos como lo lee el Inspector
            // (SerializedObject). Solo se LEE.
            SerializedProperty campo = new SerializedObject(menu).FindProperty("menuPanel");
            if (campo != null && campo.objectReferenceValue != null)
                return campo.objectReferenceValue as GameObject;
        }

        if (muerte != null && muerte.mainMenuPanel != null) return muerte.mainMenuPanel;
        return BuscarPorNombre(NombrePanelMenu);
    }

    // El panel que PlayerDeath prende al morir: el interruptor de "game over".
    static GameObject BuscarPanelDerrota(PlayerDeath muerte)
    {
        if (muerte != null && muerte.gameOverPanel != null) return muerte.gameOverPanel;
        return BuscarPorNombre(NombrePanelDerrota);
    }

    // Busca un objeto por nombre en toda la escena (tambien apagados), salteando el Canvas de los menus nuevos.
    static GameObject BuscarPorNombre(string nombre)
    {
        foreach (GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (raiz.name == NombreCanvas) continue;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t.gameObject;
        }
        return null;
    }

    // Prende un objeto si estaba apagado (con Undo).
    static void Prender(GameObject go)
    {
        PonerActivo(go, true);
    }

    // Prende o apaga un objeto, solo si hace falta (con Undo).
    static void PonerActivo(GameObject go, bool activo)
    {
        if (go == null || go.activeSelf == activo) return;
        Undo.RecordObject(go, TituloUndo);
        go.SetActive(activo);
    }
}
