using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// El ESTILO de toda la interfaz nueva, en un solo lugar.
//
// No tiene menu propio. Lo usan los menus "Green Mist" que arman pantallas
// (pausa, vida, menu principal, derrota, barras de enemigos) para que todas
// se vean como un mismo juego sin copiar y pegar: los colores, la letra, los
// dibujitos generados y las "piezas" que se repiten.
//
// ---------------------------------------------------------------------------
// COMO SE USA (lo mismo en todas las funciones de este archivo)
// ---------------------------------------------------------------------------
// 1) Todo es "buscar o crear": cada pieza se busca por NOMBRE entre los hijos
//    directos del padre. Si ya esta, se reusa; si no, se crea. Por eso correr
//    un menu dos veces no duplica nada.
//
// 2) El parametro "forzarEstilo" (o "aplicar" en las funciones basicas):
//      false = si la pieza YA EXISTE, no se le tocan tamaños, posiciones ni
//              colores (se respeta lo que hayas ajustado a mano). Si se crea
//              recien, se le pone el estilo de fabrica igual.
//      true  = se le escribe el estilo de fabrica aunque ya exista.
//    Cada menu guarda en su componente (PauseMenuView.styleVersion,
//    ThinHealthBar.styleVersion) con que VERSION de estilo armo las cosas, y
//    pasa forzarEstilo = HayQueRestilar(esa version). Asi el estilo nuevo se
//    aplica UNA sola vez cuando cambia VersionEstilo, y despues se vuelve a
//    respetar lo que toques a mano.
//
// 3) "orden" es el lugar entre los hermanos y solo se usa al crear. En la UI
//    lo que esta mas abajo en la Hierarchy se dibuja ENCIMA.
//
// 4) Las medidas son unidades de Canvas. En el Canvas de la interfaz
//    (1920 x 1080 de referencia) una unidad es un pixel a 1080p.
//
// 5) Todo queda anotado en el Undo (Ctrl+Z). Unica excepcion: si estas
//    armando cosas DENTRO de un prefab abierto por codigo
//    (PrefabUtility.LoadPrefabContents), pone UsarUndo = false antes y volvelo
//    a true al terminar (en un try/finally): ahi el Undo no tiene sentido.
public static class GreenMistUIEstilo
{
    // Subi este numero cuando cambie el estilo de fabrica (colores, tamaños,
    // forma de las piezas). Los menus comparan contra el y restilan lo viejo.
    //   1 = primera interfaz (acento verde niebla, rayita, barra finita de 240 x 5)
    //   2 = acento indigo, flecha de pincel, barra de vida estilo Souls
    //   3 = acento verde jade azulado (el azul indigo no pegaba con el juego)
    public const int VersionEstilo = 3;

    // El Canvas de la interfaz nueva (objeto raiz de la escena).
    public const string NombreCanvas = "GM_UI_Canvas";

    // Nombres de los hijos de una opcion de menu (ver Opcion).
    public const string NombreMarca = "GM_UI_Marca";
    public const string NombreTextoOpcion = "GM_UI_Texto";

    public const string CarpetaTexturas = GreenMistEditorUtil.CarpetaGenerados + "/Textures";
    public const string RutaFuente = "Assets/Fonts/Cinzel-VariableFont_wght SDF.asset";
    public const string TituloUndo = "Green Mist: interfaz";

    // Ver el punto 5 de arriba. Normalmente NO se toca.
    public static bool UsarUndo = true;

    // ------------------------------------------------------------------
    // Paleta (la misma en toda la interfaz)
    // ------------------------------------------------------------------
    // Tinta para oscurecer, hueso para el texto, UN solo acento (un verde jade
    // azulado y apagado, como el celadon japones) para lo elegido y los titulos
    // chicos, y carmesi SOLO para la vida y la palabra DEFEAT.

    // Casi negro. Para los oscurecidos de pantalla completa.
    public static readonly Color Tinta = new Color(0.03f, 0.045f, 0.045f, 1f);

    // Blanco calido. Para todo el texto.
    public static readonly Color Hueso = new Color(0.92f, 0.90f, 0.84f, 1f);

    // El acento: verde jade azulado, opaco y oscuro. VERDE, no azul.
    // Para la flecha de la opcion elegida y formas grandes.
    // Para cambiarlo: toca estos dos colores, subi VersionEstilo en 1 y corre los menus 8 y 9.
    public static readonly Color Acento = new Color(0.27f, 0.50f, 0.45f, 1f);

    // El mismo jade un poco mas claro. Para letras CHICAS (PAUSED, subtitulos):
    // con el jade puro las letras finitas se pierden sobre el fondo oscuro.
    public static readonly Color AcentoClaro = new Color(0.40f, 0.63f, 0.57f, 1f);

    // Rojo apagado. Relleno de las barras de vida y la palabra DEFEAT. Nada mas.
    public static readonly Color Carmesi = new Color(0.58f, 0.14f, 0.13f, 1f);

    // Un carmesi mas vivo: el relleno de la barra con poca vida.
    public static readonly Color CarmesiVivo = new Color(0.74f, 0.17f, 0.14f, 1f);

    // El rojo al que se enciende el borde de la barra con poca vida.
    public static readonly Color BordeAlerta = new Color(0.80f, 0.24f, 0.20f, 0.85f);

    // Hueso palido: la "estela" de daño de las barras (lo que se acaba de perder).
    public static readonly Color EstelaHueso = new Color(0.87f, 0.82f, 0.70f, 0.90f);

    // La pista oscura de las barras (lo que queda vacio).
    public static readonly Color Pista = new Color(0.025f, 0.03f, 0.035f, 0.92f);

    // El marco fino de las barras: hueso casi transparente.
    public static readonly Color BordeBarra = new Color(0.92f, 0.90f, 0.84f, 0.32f);

    // ------------------------------------------------------------------
    // Medidas que se repiten (unidades de un Canvas de 1920 x 1080)
    // ------------------------------------------------------------------
    public const float OscurecidoAlpha = 0.78f;   // cuanto tapa el juego un oscurecido de pantalla completa

    public const float TamanoOpcion = 42f;        // tamaño de letra de RESUME / PLAY / RETRY...
    public const float EspaciadoOpcion = 14f;     // separacion entre letras de una opcion
    public const float AltoOpcion = 68f;          // alto de la zona que recibe el mouse
    public const float PasoOpcion = 72f;          // distancia entre una opcion y la siguiente
    // Ancho de la zona que recibe el mouse: apenas mas que el texto mas largo
    // ("MAIN MENU"). Si fuera mucho mas ancha llegaria al centro de la pantalla,
    // que es donde reaparece el cursor, y el menu se abriria con una opcion ya
    // marcada sin haber movido el mouse.
    public const float AnchoOpcion = 380f;
    public const float AlphaOpcionReposo = 0.55f; // transparencia del texto de una opcion sin elegir

    public const float TamanoTitulo = 20f;        // titulos chicos: PAUSED
    public const float EspaciadoTitulo = 40f;

    public static readonly Vector2 TamanoFlecha = new Vector2(80f, 25f);   // la flecha de pincel de la opcion elegida
    public const float SeparacionFlecha = 8f;     // hueco entre la punta de la flecha y el texto (sin elegir)

    public const float GrosorBordeBarra = 2f;     // marco de la barra de vida de Onji

    // true si algo armado con la version "versionGuardada" necesita el estilo nuevo.
    // Lo recien creado tiene version 0 pero ya nace con el estilo actual: para eso
    // cada menu mira primero si el objeto es nuevo.
    public static bool HayQueRestilar(int versionGuardada)
    {
        return versionGuardada < VersionEstilo;
    }

    // ==================================================================
    // FUENTE
    // ==================================================================

    static double ultimoAvisoFuente = -100.0;

    // La fuente Cinzel del proyecto. Si no aparece avisa por Console (una sola
    // vez por clic de menu) y devuelve la fuente por defecto de TextMeshPro.
    public static TMP_FontAsset Fuente()
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuente);
        if (fuente != null) return fuente;

        // Por si la moviste o le cambiaste el nombre: cualquier fuente TMP que se llame "Cinzel...".
        foreach (string guid in AssetDatabase.FindAssets("Cinzel t:TMP_FontAsset"))
        {
            fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (fuente != null) return fuente;
        }

        // Esta funcion se llama una vez por cada texto: sin este freno el aviso saldria diez veces.
        if (EditorApplication.timeSinceStartup - ultimoAvisoFuente > 5.0)
        {
            ultimoAvisoFuente = EditorApplication.timeSinceStartup;
            Debug.LogWarning("[Green Mist] No encontré la fuente Cinzel de TextMeshPro (" + RutaFuente + "). "
                             + "Usé la fuente por defecto de TextMeshPro: los textos nuevos se van a ver con otra letra. "
                             + "Podés cambiarla a mano en el campo Font Asset de cada texto.");
        }
        return TMP_Settings.defaultFontAsset;
    }

    // ==================================================================
    // UNDO Y COMPONENTES
    // ==================================================================

    // Llamala ANTES de cambiarle algo a un objeto o componente que ya existia:
    // lo anota en el Undo y lo marca como modificado para que se guarde.
    public static void Registrar(Object objeto)
    {
        if (objeto == null) return;
        if (UsarUndo) Undo.RecordObject(objeto, TituloUndo);
        EditorUtility.SetDirty(objeto);
    }

    // Devuelve el componente T del objeto; si no lo tiene, lo agrega.
    public static T Componente<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = UsarUndo ? Undo.AddComponent<T>(go) : go.AddComponent<T>();
        return c;
    }

    // La capa "UI" de Unity (0 si el proyecto no la tuviera).
    public static int CapaUI()
    {
        int capa = LayerMask.NameToLayer("UI");
        return capa < 0 ? 0 : capa;
    }

    // ==================================================================
    // BASICOS: crear y acomodar rectangulos
    // ==================================================================

    // Busca o crea, en la RAIZ de la escena, un Canvas que escala con la pantalla
    // (referencia 1920 x 1080, segun el ALTO: en 16:10 o en pantalla ancha el
    // texto queda del mismo tamaño relativo).
    //   ordenDeDibujo  el "Sort Order": un Canvas con numero mas alto tapa a los de numero mas bajo.
    //                  (El "Canvas" viejo de la escena esta en 0 y el fundido a negro, FadeCanvas, en 999.)
    //   nuevo          sale en true si el Canvas se creo recien.
    // La configuracion se escribe solo al crearlo.
    public static GameObject CanvasEscalado(string nombre, int ordenDeDibujo, out bool nuevo)
    {
        GameObject raiz = GreenMistEditorUtil.BuscarRaiz(nombre);
        if (raiz == null)
        {
            raiz = new GameObject(nombre);
            if (UsarUndo) Undo.RegisterCreatedObjectUndo(raiz, TituloUndo);
        }

        nuevo = raiz.GetComponent<Canvas>() == null;

        Canvas canvas = Componente<Canvas>(raiz);
        CanvasScaler escala = Componente<CanvasScaler>(raiz);
        Componente<GraphicRaycaster>(raiz);   // sin esto el mouse no "toca" nada de este Canvas

        if (nuevo)
        {
            raiz.layer = CapaUI();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordenDeDibujo;

            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 1f;
        }
        return raiz;
    }

    // Busca un hijo DIRECTO por nombre (tambien si esta apagado); si no existe lo
    // crea con RectTransform. "nuevo" sale en true si lo creo recien.
    public static RectTransform Hijo(Transform padre, string nombre, int orden, out bool nuevo)
    {
        Transform t = padre.Find(nombre);
        nuevo = t == null;
        if (!nuevo)
        {
            RectTransform ya = t as RectTransform;
            if (ya != null) return ya;
            // Existia sin RectTransform: se lo ponemos.
            return UsarUndo ? Undo.AddComponent<RectTransform>(t.gameObject) : t.gameObject.AddComponent<RectTransform>();
        }

        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = CapaUI();
        if (UsarUndo) Undo.RegisterCreatedObjectUndo(go, TituloUndo);

        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.SetSiblingIndex(Mathf.Clamp(orden, 0, padre.childCount - 1));
        return rt;
    }

    // Escribe anclas, pivote, posicion y tamaño de un rectangulo (y le saca rotacion y escala).
    public static void Colocar(RectTransform rt, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 posicion, Vector2 tamano)
    {
        Registrar(rt);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    // Hace que ocupe todo el rectangulo del padre.
    public static void Estirar(RectTransform rt)
    {
        Colocar(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    // Ocupa todo el padre y ademas sobresale "margen" unidades por cada lado
    // (con margen negativo queda metido hacia adentro).
    public static void EstirarConMargen(RectTransform rt, float margen)
    {
        // Con las anclas estiradas, "tamaño" es cuanto MAS grande que el padre es.
        Colocar(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(margen * 2f, margen * 2f));
    }

    // Dentro de una columna: pegado arriba a la izquierda, "y" unidades mas abajo del borde de arriba.
    public static void ColocarEnColumna(RectTransform rt, float y, float ancho, float alto)
    {
        Colocar(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -y), new Vector2(ancho, alto));
    }

    // El mismo color con otra transparencia.
    public static Color ConAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // Devuelve el CanvasGroup del objeto (lo agrega si falta). Con el se funde
    // de una todo lo que cuelga del objeto.
    //   recibeMouse  false para cosas que nunca deben tapar un clic (una barra de vida).
    // Se configura solo al agregarlo.
    public static CanvasGroup Grupo(GameObject go, bool recibeMouse)
    {
        bool faltaba = go.GetComponent<CanvasGroup>() == null;
        CanvasGroup grupo = Componente<CanvasGroup>(go);
        if (faltaba)
        {
            grupo.alpha = 1f;
            grupo.interactable = recibeMouse;
            grupo.blocksRaycasts = recibeMouse;
        }
        return grupo;
    }

    // Devuelve el Image del objeto (lo agrega si falta).
    //   aplicar      true = escribirle color, sprite y demas (objeto nuevo o cambio de estilo).
    //                Si el Image faltaba se le escribe igual.
    //   sprite       null = rectangulo liso.
    //   recibeClics  true si esta imagen tiene que "atajar" el mouse.
    public static Image Imagen(RectTransform rt, bool aplicar, Color color, Sprite sprite, bool recibeClics)
    {
        bool faltaba = rt.GetComponent<Image>() == null;
        Image imagen = Componente<Image>(rt.gameObject);
        if (aplicar || faltaba)
        {
            Registrar(imagen);
            imagen.sprite = sprite;
            imagen.type = Image.Type.Simple;
            imagen.color = color;
            imagen.raycastTarget = recibeClics;
        }
        return imagen;
    }

    // Convierte el rectangulo en una zona INVISIBLE que recibe el mouse
    // (para las opciones de texto y el slider, que son demasiado finitos para apuntarles).
    public static void ZonaDeClic(RectTransform rt, bool aplicar)
    {
        bool faltaba = rt.GetComponent<Image>() == null;
        Image zona = Imagen(rt, aplicar, new Color(1f, 1f, 1f, 0f), null, true);
        if (aplicar || faltaba)
        {
            // Unity saltea las imagenes 100% transparentes. Esta la queremos igual,
            // porque es la que "toca" el mouse.
            CanvasRenderer render = zona.GetComponent<CanvasRenderer>();
            if (render != null)
            {
                Registrar(render);
                render.cullTransparentMesh = false;
            }
        }
    }

    // Devuelve el texto (TextMeshPro) del objeto; lo agrega si falta. Letra Cinzel,
    // una sola linea, alineado a la izquierda y centrado en alto por las MAYUSCULAS.
    //   aplicar    true = escribirle letra, texto, tamaño, color... (objeto nuevo o cambio de estilo).
    //   espaciado  separacion extra entre letras, en centesimas del tamaño de la letra.
    public static TextMeshProUGUI Texto(RectTransform rt, bool aplicar, string texto, float tamano, float espaciado, Color color)
    {
        return Texto(rt, aplicar, texto, tamano, espaciado, color, TextAlignmentOptions.CaplineLeft);
    }

    // Igual que la anterior pero eligiendo la alineacion (por ejemplo
    // TextAlignmentOptions.Capline para un titulo centrado).
    public static TextMeshProUGUI Texto(RectTransform rt, bool aplicar, string texto, float tamano, float espaciado, Color color,
                                        TextAlignmentOptions alineacion)
    {
        bool faltaba = rt.GetComponent<TextMeshProUGUI>() == null;
        TextMeshProUGUI t = Componente<TextMeshProUGUI>(rt.gameObject);
        if (aplicar || faltaba)
        {
            Registrar(t);
            TMP_FontAsset fuente = Fuente();
            if (fuente != null) t.font = fuente;
            t.text = texto;
            t.enableAutoSizing = false;
            t.fontSize = tamano;
            t.fontStyle = FontStyles.Normal;
            t.characterSpacing = espaciado;
            t.color = color;
            // Capline: centra en alto las MAYUSCULAS (no la linea entera), asi la
            // flecha de pincel queda justo a la mitad de las letras.
            t.alignment = alineacion;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = false;
            t.raycastTarget = false;   // el mouse lo recibe la zona de clic, no las letras
        }
        return t;
    }

    // ==================================================================
    // PIEZAS DE PANTALLA
    // ==================================================================

    // Un oscurecido de PANTALLA COMPLETA color tinta. Ataja los clics, asi no
    // pasan "a traves" del menu hacia lo que haya atras.
    //   alpha  cuanto tapa (OscurecidoAlpha = 0.78 es el de la pausa; 1 = negro total).
    public static Image Oscurecido(Transform padre, string nombre, int orden, float alpha, bool forzarEstilo)
    {
        bool n;
        RectTransform rt = Hijo(padre, nombre, orden, out n);
        bool aplicar = n || forzarEstilo;
        if (aplicar) Estirar(rt);
        return Imagen(rt, aplicar, ConAlpha(Tinta, alpha), null, true);
    }

    // Una sombra que oscurece un poco mas el lado IZQUIERDO de la pantalla y se
    // va perdiendo hacia la derecha: para que el texto se lea aunque atras haya
    // un farol prendido.
    //   hastaDonde  hasta que parte del ancho llega (0.62 = 62% de la pantalla).
    public static Image SombraIzquierda(Transform padre, string nombre, int orden, float hastaDonde, float alpha, bool forzarEstilo)
    {
        bool n;
        RectTransform rt = Hijo(padre, nombre, orden, out n);
        bool aplicar = n || forzarEstilo;
        if (aplicar) Colocar(rt, new Vector2(0f, 0f), new Vector2(hastaDonde, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Sprite degradado = SpriteDegradado();
        bool faltaba = rt.GetComponent<Image>() == null;
        Image imagen = Imagen(rt, aplicar, ConAlpha(Tinta, alpha), degradado, false);
        if (aplicar || faltaba) imagen.enabled = degradado != null;   // sin el dibujo seria un rectangulo duro: mejor nada
        return imagen;
    }

    // Una COLUMNA: un rectangulo vacio donde se apilan titulo, opciones, slider...
    // Empieza en "desdeIzquierda" (0.12 = al 12% del ancho de la pantalla) y queda
    // centrada en alto. Lo que se pone adentro se mide desde su esquina de ARRIBA
    // a la izquierda (ver ColocarEnColumna y el parametro "y" de las piezas).
    public static RectTransform Columna(Transform padre, string nombre, int orden, float desdeIzquierda, Vector2 tamano, bool forzarEstilo)
    {
        bool n;
        RectTransform rt = Hijo(padre, nombre, orden, out n);
        if (n || forzarEstilo)
        {
            Colocar(rt, new Vector2(desdeIzquierda, 0.5f), new Vector2(desdeIzquierda, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, tamano);
        }
        return rt;
    }

    // Un texto suelto dentro de una columna (un titulo, una ayuda).
    //   y  cuantas unidades baja desde el borde de arriba de la columna.
    // Ejemplo, el titulo de la pausa:
    //   Etiqueta(columna, "GM_UI_Pausa_Titulo", 0, "PAUSED", 0f, TamanoTitulo, EspaciadoTitulo, AcentoClaro, forzar);
    public static TextMeshProUGUI Etiqueta(Transform columna, string nombre, int orden, string texto, float y,
                                           float tamano, float espaciado, Color color, bool forzarEstilo)
    {
        bool n;
        RectTransform rt = Hijo(columna, nombre, orden, out n);
        bool aplicar = n || forzarEstilo;
        // El alto es una vez y media la letra, redondeado a numero par.
        if (aplicar) ColocarEnColumna(rt, y, 600f, Mathf.Ceil(tamano * 0.75f) * 2f);
        return Texto(rt, aplicar, texto, tamano, espaciado, color);
    }

    // Una raya fina color hueso, de 2 de alto (el subrayado corto de un titulo).
    public static Image Raya(Transform columna, string nombre, int orden, float y, float ancho, bool forzarEstilo)
    {
        bool n;
        RectTransform rt = Hijo(columna, nombre, orden, out n);
        bool aplicar = n || forzarEstilo;
        if (aplicar) ColocarEnColumna(rt, y, ancho, 2f);
        return Imagen(rt, aplicar, ConAlpha(Hueso, 0.3f), null, false);
    }

    // Una OPCION de menu: texto solo, con la flecha de pincel a su izquierda.
    // Arma tres cosas:
    //   <nombre>          la zona invisible que recibe el mouse (AnchoOpcion x AltoOpcion) + PauseMenuItem
    //     GM_UI_Marca     la flecha de pincel, color Acento (jugando la muestra y esconde PauseMenuItem)
    //     GM_UI_Texto     el texto, color hueso
    // Devuelve el PauseMenuItem: pasaselo a ConectarClic para decirle que hace,
    // y ponelo en la lista "items" del PauseMenuView del panel.
    //   y  cuantas unidades baja desde el borde de arriba de la columna.
    //      Entre una opcion y la siguiente suma PasoOpcion.
    // El texto va alineado a la IZQUIERDA. Para una pantalla centrada, centra la
    // columna (el padre), no el texto: la flecha se acomoda a la izquierda del texto.
    public static PauseMenuItem Opcion(Transform columna, string nombre, int orden, string texto, float y, bool forzarEstilo)
    {
        bool nuevo;
        RectTransform opcion = Hijo(columna, nombre, orden, out nuevo);
        bool aplicar = nuevo || forzarEstilo;
        if (aplicar) ColocarEnColumna(opcion, y, AnchoOpcion, AltoOpcion);
        ZonaDeClic(opcion, aplicar);

        // ---- La flecha: cuelga a la IZQUIERDA del texto, con la punta hacia el.
        // En la escena se ve entera (asi la podes acomodar); jugando, PauseMenuItem
        // la esconde y la hace entrar cuando la opcion esta elegida.
        bool n;
        RectTransform marca = Hijo(opcion, NombreMarca, 0, out n);
        bool aplicarMarca = n || forzarEstilo;
        Sprite flecha = SpriteFlecha();
        if (aplicarMarca)
        {
            // Pivote a la derecha: la posicion es donde queda la PUNTA.
            // Si el dibujo no se pudo generar queda una rayita, como antes.
            Vector2 tamano = flecha != null ? TamanoFlecha : new Vector2(26f, 2f);
            Colocar(marca, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-SeparacionFlecha, 0f), tamano);
        }
        Image imagenMarca = Imagen(marca, aplicarMarca, Acento, flecha, false);
        if (aplicarMarca && flecha != null)
        {
            // Filled + Horizontal: la imagen se puede mostrar "de a poco" de izquierda
            // a derecha. PauseMenuItem lo usa para que la flecha se pinte como una pincelada.
            imagenMarca.type = Image.Type.Filled;
            imagenMarca.fillMethod = Image.FillMethod.Horizontal;
            imagenMarca.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenMarca.fillAmount = 1f;
        }

        // ---- El texto. Con AlphaOpcionReposo se ve en el editor igual que jugando sin elegir.
        RectTransform rectTexto = Hijo(opcion, NombreTextoOpcion, 1, out n);
        bool aplicarTexto = n || forzarEstilo;
        if (aplicarTexto) Estirar(rectTexto);
        TextMeshProUGUI etiqueta = Texto(rectTexto, aplicarTexto, texto, TamanoOpcion, EspaciadoOpcion, ConAlpha(Hueso, AlphaOpcionReposo));

        // ---- El script de la opcion (las referencias se rehacen siempre).
        PauseMenuItem item = Componente<PauseMenuItem>(opcion.gameObject);
        Registrar(item);
        item.label = etiqueta;
        item.indicator = imagenMarca;
        return item;
    }

    // Un slider de linea fina con una perilla redonda chica (el de MUSIC), de 0 a 1.
    //   valorInicial  donde arranca. Se usa SOLO al crear el slider; despues no se pisa.
    // Devuelve el Slider: pasaselo a ConectarSlider para decirle que controla.
    public static Slider SliderFino(Transform columna, string nombre, int orden, float y, float ancho, float valorInicial, bool forzarEstilo)
    {
        bool nuevo;
        RectTransform raiz = Hijo(columna, nombre, orden, out nuevo);
        bool aplicar = nuevo || forzarEstilo;
        if (aplicar) ColocarEnColumna(raiz, y, ancho, 24f);
        ZonaDeClic(raiz, aplicar);   // la linea mide 2 de alto: sin esta zona seria imposible agarrarla

        bool n;

        // El riel: la linea de fondo, centrada en alto.
        RectTransform riel = Hijo(raiz, "GM_UI_Riel", 0, out n);
        if (n || forzarEstilo) Colocar(riel, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 2f));
        Imagen(riel, n || forzarEstilo, ConAlpha(Hueso, 0.22f), null, false);

        // El relleno va DENTRO del riel. OJO: su rectangulo lo maneja el Slider (le
        // mueve el ancla derecha segun el valor), por eso solo se acomoda al crearlo.
        RectTransform relleno = Hijo(riel, "GM_UI_Relleno", 0, out n);
        if (n) Estirar(relleno);
        Imagen(relleno, n || forzarEstilo, ConAlpha(Hueso, 0.8f), null, false);

        // La perilla se mueve dentro de esta zona (mismo ancho que el riel, 12 de alto).
        RectTransform zonaPerilla = Hijo(raiz, "GM_UI_ZonaPerilla", 1, out n);
        if (n || forzarEstilo) Colocar(zonaPerilla, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 12f));

        // La perilla tambien la mueve el Slider: solo se acomoda al crearla.
        RectTransform perilla = Hijo(zonaPerilla, "GM_UI_Perilla", 0, out n);
        if (n) Colocar(perilla, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 0f));
        Image imagenPerilla = Imagen(perilla, n || forzarEstilo, Hueso, SpriteCirculo(), false);

        bool sliderNuevo = raiz.GetComponent<Slider>() == null;
        Slider slider = Componente<Slider>(raiz.gameObject);
        Registrar(slider);
        slider.fillRect = relleno;
        slider.handleRect = perilla;
        slider.targetGraphic = imagenPerilla;

        if (sliderNuevo)
        {
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(valorInicial));
        }

        if (sliderNuevo || forzarEstilo)
        {
            // Sin navegacion automatica: las flechas del teclado no saltan a otros controles.
            Navigation navegacion = slider.navigation;
            navegacion.mode = Navigation.Mode.None;
            slider.navigation = navegacion;

            // La perilla se aclara al pasar el mouse y se pone jade al arrastrarla.
            // Estos colores MULTIPLICAN al de la perilla (hueso): el de "pressed"
            // es AcentoClaro dividido por Hueso, asi el resultado da AcentoClaro.
            slider.transition = Selectable.Transition.ColorTint;
            ColorBlock colores = slider.colors;
            colores.normalColor = new Color(1f, 1f, 1f, 0.8f);
            colores.highlightedColor = Color.white;
            colores.selectedColor = Color.white;
            colores.pressedColor = new Color(AcentoClaro.r / Hueso.r, AcentoClaro.g / Hueso.g, AcentoClaro.b / Hueso.b, 1f);
            colores.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.1f;
            slider.colors = colores;
        }
        return slider;
    }

    // ==================================================================
    // BARRA DE VIDA (familia Souls / Sekiro / Assassin's Creed)
    // ==================================================================

    // Lo que devuelve Barra: las cuatro imagenes, para conectarlas al script que la anima.
    public class PiezasBarra
    {
        public RectTransform raiz;   // el rectangulo de la barra (el que le pasaste)
        public Image borde;          // el marco fino color hueso
        public Image fondo;          // la pista oscura
        public Image estela;         // hueso palido: lo que se acaba de perder
        public Image relleno;        // carmesi: la vida que queda
    }

    // Arma las piezas de una barra de vida DENTRO del rectangulo "raiz", que ya
    // tiene que existir con su tamaño y su lugar (crealo con Hijo + Colocar).
    // Sirve igual en el Canvas de pantalla (la de Onji) que en un Canvas chico
    // en el mundo (las de los enemigos): solo cambia el tamaño de "raiz".
    //
    // Crea cuatro hijos, de atras para adelante (los nombres salen del de la raiz):
    //   <raiz>_Borde    sobresale "grosorBorde" por cada lado: eso es el marco que se ve
    //   <raiz>_Fondo    la pista oscura, del tamaño exacto de la raiz
    //   <raiz>_Estela   la estela de daño
    //   <raiz>_Relleno  la vida
    // El tamaño de la raiz es el de la parte que SE LLENA; el marco queda por fuera.
    //
    // COMO SE ANIMA: en la escena estela y relleno ocupan toda la raiz. Jugando,
    // el script los acorta con la X de las anclas (0 = borde izquierdo, 1 = derecho):
    //   relleno  anchorMax.x = la vida que queda (0.5 = media vida);
    //   estela   SOLO el tramo perdido: anchorMin.x = donde termina el relleno,
    //            anchorMax.x = hasta donde llega la estela.
    // La estela no va por debajo del relleno: si el relleno se pone medio
    // transparente (latido de poca vida, fundidos) atras tiene que asomar la
    // pista oscura y no la estela clara. Asi lo hacen ThinHealthBar y HealthBarUI.
    //
    //   grosorBorde  grosor del marco, en unidades del Canvas (GrosorBordeBarra = 2 en la de Onji).
    //                Conviene un numero entero: con medios pixeles un lado del marco sale mas grueso que el otro.
    public static PiezasBarra Barra(RectTransform raiz, float grosorBorde, bool forzarEstilo)
    {
        PiezasBarra piezas = new PiezasBarra();
        piezas.raiz = raiz;

        string baseNombre = raiz.name;
        bool n;

        // El marco no es un dibujo hueco: es un rectangulo hueso un poco mas grande
        // que la pista, puesto DETRAS. Lo unico que asoma es el borde.
        RectTransform borde = Hijo(raiz, baseNombre + "_Borde", 0, out n);
        if (n || forzarEstilo) EstirarConMargen(borde, grosorBorde);
        piezas.borde = Imagen(borde, n || forzarEstilo, BordeBarra, null, false);

        RectTransform fondo = Hijo(raiz, baseNombre + "_Fondo", 1, out n);
        if (n || forzarEstilo) Estirar(fondo);
        piezas.fondo = Imagen(fondo, n || forzarEstilo, Pista, null, false);

        RectTransform estela = Hijo(raiz, baseNombre + "_Estela", 2, out n);
        if (n || forzarEstilo) Estirar(estela);
        piezas.estela = Imagen(estela, n || forzarEstilo, EstelaHueso, null, false);

        RectTransform relleno = Hijo(raiz, baseNombre + "_Relleno", 3, out n);
        if (n || forzarEstilo) Estirar(relleno);
        piezas.relleno = Imagen(relleno, n || forzarEstilo, Carmesi, null, false);

        return piezas;
    }

    // ==================================================================
    // CONEXIONES DE EVENTOS (quedan guardadas, igual que arrastrarlas a mano)
    // ==================================================================

    // Saca de un evento (el "On Click" de una opcion, el "On Value Changed" de un
    // slider) las conexiones rotas y las que apuntan a un componente del tipo
    // "tipoDestino". Las demas no se tocan.
    //   dueno  el componente que tiene el evento (para el Undo).
    public static void QuitarConexiones(Object dueno, UnityEventBase evento, System.Type tipoDestino)
    {
        Registrar(dueno);
        for (int i = evento.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            Object destino = evento.GetPersistentTarget(i);
            if (destino == null || (tipoDestino != null && tipoDestino.IsInstanceOfType(destino)))
                UnityEventTools.RemovePersistentListener(evento, i);
        }
    }

    // Conecta el "On Click" de una opcion con un metodo "public void X()" de un
    // componente de la escena. Ejemplo:  ConectarClic(opcionResume, pausa.Resume);
    // Antes saca las conexiones rotas y las que ya apuntaban a un componente de
    // ese MISMO tipo, asi correr el menu dos veces no las duplica. Las que hayas
    // agregado vos hacia otros objetos no se tocan.
    // (Si una opcion pasa a llamar a OTRO script, saca primero la vieja con
    // QuitarConexiones(item, item.onClick, typeof(ScriptViejo)).)
    public static void ConectarClic(PauseMenuItem item, UnityAction accion)
    {
        Object destino = accion != null ? accion.Target as Object : null;
        if (item == null || destino == null)
        {
            Debug.LogWarning("[Green Mist] No pude conectar una opción de menú: falta la opción o el script al que tiene que llamar.");
            return;
        }

        QuitarConexiones(item, item.onClick, destino.GetType());
        UnityEventTools.AddVoidPersistentListener(item.onClick, accion);
        EditorUtility.SetDirty(item);
    }

    // Conecta el "On Value Changed" de un slider con un metodo "public void X(float)".
    // Ejemplo:  ConectarSlider(slider, vista.SetMusicVolume);
    // Misma regla que ConectarClic para no duplicar.
    public static void ConectarSlider(Slider slider, UnityAction<float> accion)
    {
        Object destino = accion != null ? accion.Target as Object : null;
        if (slider == null || destino == null)
        {
            Debug.LogWarning("[Green Mist] No pude conectar un slider: falta el slider o el script al que tiene que llamar.");
            return;
        }

        QuitarConexiones(slider, slider.onValueChanged, destino.GetType());
        UnityEventTools.AddPersistentListener(slider.onValueChanged, accion);
        EditorUtility.SetDirty(slider);
    }

    // El volumen que tiene la musica en la escena (el AudioSource que usa MusicManager).
    // Sirve como "valorInicial" de un slider de musica.
    public static float VolumenMusicaDeLaEscena()
    {
        MusicManager musica = Object.FindFirstObjectByType<MusicManager>(FindObjectsInactive.Include);
        if (musica != null)
        {
            AudioSource fuente = musica.GetComponentInChildren<AudioSource>();
            if (fuente != null) return Mathf.Clamp01(fuente.volume);
        }
        return 0.35f;
    }

    // ==================================================================
    // DIBUJITOS GENERADOS POR CODIGO (se guardan en Assets/GreenMist/Textures)
    // ==================================================================
    // Cada funcion devuelve el sprite si ya existe en el proyecto; si no, lo
    // dibuja pixel por pixel, lo guarda como PNG y lo importa. Todos son
    // BLANCOS: el color se lo da el Image que los usa.
    // Pueden devolver null si no se pudo crear el archivo (avisan por Console).

    // Un circulo de borde suave: la perilla del slider.
    public static Sprite SpriteCirculo()
    {
        const string nombre = "GM_UI_Circulo";
        Sprite ya = CargarSprite(nombre);
        if (ya != null) return ya;

        // Chico a proposito: la perilla se dibuja de unos 12 pixeles. Con una textura
        // mucho mas grande que eso el borde suave se pierde al achicarla.
        const int n = 32;
        const float radio = n * 0.5f - 1.5f;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - n * 0.5f;
                float dy = y + 0.5f - n * 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);

                // 1 adentro, 0 afuera, con dos pixeles de borde difuminado.
                float cobertura = Mathf.Clamp01((radio - r) / 2f + 0.5f);
                px[y * n + x] = new Color(1f, 1f, 1f, cobertura);
            }
        }
        return GuardarSprite(nombre, n, n, px, true);
    }

    // Una franja que va de opaca (izquierda) a transparente (derecha).
    public static Sprite SpriteDegradado()
    {
        const string nombre = "GM_UI_Degradado";
        Sprite ya = CargarSprite(nombre);
        if (ya != null) return ya;

        const int ancho = 256;
        const int alto = 4;
        Color[] px = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float u = x / (float)(ancho - 1);                 // 0 a la izquierda, 1 a la derecha
                float a = 1f - Mathf.SmoothStep(0f, 1f, u);       // caida suave, sin borde marcado
                px[y * ancho + x] = new Color(1f, 1f, 1f, a);
            }
        }
        return GuardarSprite(nombre, ancho, alto, px, false);   // se dibuja estirado, nunca achicado: sin mipmaps
    }

    // La FLECHA DE PINCEL (sumi-e): una sola pincelada que apunta a la derecha.
    // Arranca gruesa, con la cabeza cortada en diagonal (asi apoya el pincel),
    // los bordes un poco desparejos, y se afina hasta terminar en punta.
    // Proporcion 128 x 40: mostrala de 80 x 25 (TamanoFlecha) o parecido.
    public static Sprite SpriteFlecha()
    {
        const string nombre = "GM_UI_Flecha";
        Sprite ya = CargarSprite(nombre);
        if (ya != null) return ya;

        const int ancho = 128;
        const int alto = 40;
        // Cada pixel se calcula como el promedio de 4 x 4 puntos adentro suyo.
        // Eso es lo que deja los bordes suaves en vez de con escalones.
        const int muestras = 4;

        Color[] px = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float suma = 0f;
                for (int j = 0; j < muestras; j++)
                {
                    for (int i = 0; i < muestras; i++)
                    {
                        float puntoX = x + (i + 0.5f) / muestras;
                        float puntoY = y + (j + 0.5f) / muestras;

                        // u: 0 en la cola (izquierda), 1 en la punta (derecha). 3 pixeles de margen.
                        // v: -1 abajo, 0 en el medio, 1 arriba.
                        float u = (puntoX - 3f) / (ancho - 6f);
                        float v = (puntoY - alto * 0.5f) / (alto * 0.5f - 3f);
                        suma += TintaDeLaFlecha(u, v);
                    }
                }
                px[y * ancho + x] = new Color(1f, 1f, 1f, suma / (muestras * muestras));
            }
        }
        // Con mipmaps: se muestra a menos de la mitad de su tamaño.
        return GuardarSprite(nombre, ancho, alto, px, true);
    }

    // Cuanta tinta hay en un punto de la pincelada: 0 = nada, 1 = tinta llena.
    // Los senos con numeros "raros" son solo para que los bordes queden
    // desparejos, como hechos a mano, pero iguales cada vez que se genera.
    static float TintaDeLaFlecha(float u, float v)
    {
        if (u <= 0f || u >= 1f) return 0f;

        float arco = 0.10f * Mathf.Sin(u * 2.9f);   // el trazo se comba apenas
        float irregular = 1f - u;                    // lo desparejo se apaga hacia la punta

        // Borde de arriba casi recto, borde de abajo curvo: los dos se juntan en la punta.
        float arriba = arco + 0.56f * Mathf.Pow(1f - u, 0.80f)
                       * (1f + (0.09f * Mathf.Sin(u * 17f + 1.0f) + 0.04f * Mathf.Sin(u * 41f + 2.3f)) * irregular);
        float abajo = arco - 0.82f * Mathf.Pow(1f - u, 1.25f)
                      * (1f + (0.10f * Mathf.Sin(u * 13f + 4.0f) + 0.05f * Mathf.Sin(u * 37f + 0.6f)) * irregular);
        if (v > arriba || v < abajo) return 0f;

        // La cabeza: un corte en diagonal, con el filo un poco desparejo.
        float corte = 0.02f + 0.11f * (0.5f + 0.5f * v) + 0.012f * Mathf.Sin(v * 19f + 0.7f);
        if (u < corte) return 0f;

        // Pincel seco: un par de vetas mas claras a lo largo, desde la mitad.
        float onda = Mathf.Sin(v * 15f + 0.9f + u * 3.0f);
        float veta = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.70f, 0.98f, onda))
                     * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 0.50f, u));
        return 1f - 0.45f * veta;
    }

    // "GM_UI_Flecha" -> "Assets/GreenMist/Textures/GM_UI_Flecha.png"
    public static string RutaSprite(string nombre)
    {
        return CarpetaTexturas + "/" + nombre + ".png";
    }

    // Devuelve el sprite generado con ese nombre si ya existe en el proyecto; si no, null.
    public static Sprite CargarSprite(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(nombre));
    }

    // Guarda los pixeles como PNG en Assets/GreenMist/Textures, lo importa como
    // Sprite de UI y lo devuelve (null si fallo).
    //   pixeles     ancho * alto colores, fila por fila empezando por la de ABAJO.
    //   conMipmaps  true para dibujos que se muestran mas CHICOS que la textura.
    public static Sprite GuardarSprite(string nombre, int ancho, int alto, Color[] pixeles, bool conMipmaps)
    {
        GreenMistEditorUtil.AsegurarCarpeta(CarpetaTexturas);
        string ruta = RutaSprite(nombre);

        Texture2D temporal = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        temporal.SetPixels(pixeles);
        temporal.Apply();
        byte[] png = temporal.EncodeToPNG();
        Object.DestroyImmediate(temporal);

        // Application.dataPath termina en ".../Assets"; le sacamos ese "Assets" repetido.
        File.WriteAllBytes(Application.dataPath + ruta.Substring("Assets".Length), png);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador != null)
        {
            importador.textureType = TextureImporterType.Sprite;       // para poder usarla en un Image de UI
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.sRGBTexture = true;
            importador.alphaSource = TextureImporterAlphaSource.FromInput;
            importador.alphaIsTransparency = true;                     // evita bordes oscuros
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.filterMode = FilterMode.Bilinear;
            importador.mipmapEnabled = conMipmaps;                     // solo hacen falta si se dibuja achicado
            importador.textureCompression = TextureImporterCompression.Uncompressed; // son chicas: sin bandas

            // Full Rect: el sprite usa el rectangulo entero. El modo por defecto (Tight)
            // recorta lo transparente y deformaria el dibujo.
            TextureImporterSettings ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            importador.SetTextureSettings(ajustes);

            importador.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (sprite == null) Debug.LogWarning("[Green Mist] No pude crear el sprite " + ruta + ". La interfaz funciona igual, pero sin ese dibujo.");
        return sprite;
    }
}
