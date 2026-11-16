using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// La barra de vida de un ENEMIGO, sobre su cabeza.
// Va en un objeto hijo del enemigo (el "Canvas" que ya tenian, o el
// "GM_UI_VidaEnemigo" que crea el menu Green Mist > 10).
//
// Tiene el mismo dibujo que la barra de Onji (familia Souls / Sekiro):
//   borde    un marco fino color hueso, casi transparente
//   fondo    la pista oscura
//   estela   hueso palido: lo que se acaba de perder
//   relleno  carmesi: la vida que queda
//
// COMO SE COMPORTA
//   - Con el enemigo sano y sin tocar, NO se ve.
//   - Aparece cuando recibe un golpe y se queda mientras le sigas pegando.
//   - Unos segundos despues del ultimo golpe se va sola.
//   - El relleno baja de golpe y atras queda la estela clara, que espera un
//     momento y despues se vacia hasta alcanzarlo.
//   - Sigue a la cabeza del enemigo y se ve SIEMPRE DEL MISMO TAMAÑO en
//     pantalla, este cerca o lejos (recien se achica pasada Shrink Distance).
//   - Si la cabeza del enemigo se sale por arriba de la pantalla (pasa con el
//     jefe de cerca), la barra se queda pegada al borde de arriba.
//   - En la pausa, el menu principal y el game over se esconde, igual que la
//     barra de Onji. Al volver al juego reaparece si todavia le tocaba verse.
//
// ES PARTE DEL HUD, NO DEL MUNDO. La barra se dibuja en la PANTALLA (como la
// de Onji) y no flotando en el mundo 3D. En cada frame le preguntamos a la
// camara en que pixel cae la cabeza del enemigo y ponemos la barra ahi.
// Por eso:
//   - nada la tapa: ni Onji, ni su baston, ni la niebla (asi es en los Souls);
//   - los efectos de la camara (el color de la noche, la viñeta) no le cambian
//     el color: el carmesi es exactamente el mismo que el de la barra de Onji;
//   - cae siempre en pixeles enteros, asi el marco fino no titila al moverse.
//
// LA BARRA SE ARMA SOLA al jugar, con los tamaños y colores de este
// componente. Por eso no hace falta tocar nada en la escena para que cambie
// de aspecto, y un enemigo nuevo (o duplicado) la tiene igual que los demas.
// Las piezas se crean la primera vez que hacen falta, en un objeto SUELTO en
// la raiz de la Hierarchy llamado "GM_UI_VidaEnemigo (nombre del enemigo)".
// Cuando el enemigo se destruye, esa barra se destruye con el.
//
// La barra vieja (el Slider verde) no se borra: se apaga sola al jugar. Para
// volver a verla, destilda New Style (o usa Green Mist > 10b).
//
// La vida se LEE en cada frame de Health.Current (es un numero, no cuesta
// nada). No se engancha a ningun evento, asi que no queda nada colgado
// cuando el enemigo muere.
//
// Los tiempos son en tiempo REAL (unscaledDeltaTime): cada golpe frena el
// juego un instante (HitStop) y con el tiempo del juego la barra se
// trabaria justo cuando tiene que aparecer.
//
// DefaultExecutionOrder(100): este script corre DESPUES que los demas, o sea
// despues de que la camara se movio en este frame. Asi la barra se acomoda
// segun donde esta la camara AHORA y no donde estaba un frame atras.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    // ---------- Tamaños de fabrica ----------
    // Estan aca (y no solo en los campos de abajo) para que el menu
    // Green Mist > 10 use exactamente los mismos numeros.
    // Son pixeles en una pantalla de 1080 de alto, igual que la barra de Onji (340 x 12).
    public const float AnchoNormal = 110f;
    public const float AltoNormal = 8f;
    public const float AnchoJefe = 200f;
    public const float AltoJefe = 10f;
    public const float BordeDeFabrica = 2f;

    // A que version del estilo de la interfaz (GreenMistUIEstilo.VersionEstilo)
    // corresponden los colores y tamaños escritos en este archivo.
    public const int VersionDeFabrica = 3;

    [Header("Referencias")]
    [Tooltip("La vida del enemigo (su componente Health). Vacio = usa el Health del objeto padre.")]
    public Health target;

    [Tooltip("La barra VIEJA (el Slider verde). Con el estilo nuevo se apaga sola al jugar. Solo se usa si destildas New Style.")]
    public Slider slider;

    [Header("Estilo")]
    [Tooltip("Tildado: la barra nueva (oscura, carmesi, con estela). Destildado: el Slider verde de antes.")]
    public bool newStyle = true;

    [Header("Lugar")]
    [Tooltip("Tildado: la barra se pone sola un poco arriba de la cabeza, midiendo el collider del enemigo. "
           + "Destildado (o si el enemigo no tiene collider): usa World Offset.")]
    public bool autoHeight = true;

    [Tooltip("Con Auto Height: cuantos metros por encima de la cabeza queda la barra. Si la barra le pisa la cabeza a un enemigo, subi este numero.")]
    [Range(0f, 1.5f)] public float headMargin = 0.25f;

    [Tooltip("Sin Auto Height (y siempre en la barra vieja): donde va la barra, en metros desde los pies del enemigo.")]
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    [Header("Tamaño en pantalla (pixeles a 1080p, como la barra de Onji)")]
    [Tooltip("Largo de la barra. La de Onji mide 340.")]
    [Range(30f, 600f)] public float width = AnchoNormal;

    [Tooltip("Alto de la parte que se llena. La de Onji mide 12.")]
    [Range(2f, 40f)] public float height = AltoNormal;

    [Tooltip("Grosor del marco. 0 = sin marco.")]
    [Range(0f, 6f)] public float borderThickness = BordeDeFabrica;

    [Tooltip("Hasta esta distancia de la camara (en metros) la barra se ve siempre del mismo tamaño. Mas lejos se va achicando.")]
    [Range(5f, 80f)] public float shrinkDistance = 20f;

    [Header("Aparecer y desaparecer")]
    [Tooltip("Tildado: la barra arranca oculta y aparece recien cuando el enemigo recibe un golpe. Destildado: se ve siempre.")]
    [FormerlySerializedAs("ocultarHastaDanar")]
    public bool hideUntilDamaged = true;

    [Tooltip("Segundos que se queda a la vista despues del ultimo golpe.")]
    [FormerlySerializedAs("tiempoVisible")]
    [Range(0.5f, 20f)] public float visibleTime = 4f;

    [Tooltip("Segundos que tarda en aparecer. Corto: el golpe se tiene que leer al instante.")]
    [Range(0.02f, 1f)] public float fadeInTime = 0.12f;

    [Tooltip("Segundos que tarda en desaparecer.")]
    [Range(0.05f, 3f)] public float fadeOutTime = 0.7f;

    [Header("Estela de daño")]
    [Tooltip("Segundos que la estela clara se queda quieta despues de un golpe.")]
    [Range(0f, 2f)] public float trailDelay = 0.45f;

    [Tooltip("Que tan rapido se vacia la estela. 0.5 = media barra por segundo.")]
    [Range(0.05f, 3f)] public float trailSpeed = 0.5f;

    [Header("Colores (los mismos que la barra de Onji)")]
    [Tooltip("El relleno: la vida que queda.")]
    public Color fillColor = new Color(0.58f, 0.14f, 0.13f, 1f);

    [Tooltip("La estela: lo que se acaba de perder.")]
    public Color trailColor = new Color(0.87f, 0.82f, 0.70f, 0.90f);

    [Tooltip("La pista oscura: la parte vacia.")]
    public Color trackColor = new Color(0.025f, 0.03f, 0.035f, 0.92f);

    [Tooltip("El marco fino.")]
    public Color borderColor = new Color(0.92f, 0.90f, 0.84f, 0.32f);

    [Header("Interno")]
    [Tooltip("Version del estilo (colores y tamaños) que tiene esta barra. No hace falta tocarlo. "
           + "Si lo pones en 0, la proxima vez que corras el menu Green Mist > 10 vuelve a los colores y tamaños de fabrica.")]
    public int styleVersion = VersionDeFabrica;

    // Los tamaños estan pensados para una pantalla de este alto.
    const float AltoDePantalla = 1080f;

    // Un HitStop dura 0.06 s. Si el tiempo lleva frenado mas que esto, es una
    // pausa de verdad (menu de pausa, menu principal, game over).
    const float PausaLarga = 0.25f;

    // Cuando la tapa un menu se va rapido, no con Fade Out Time.
    const float FundidoRapido = 0.15f;

    // Si la cabeza del enemigo se sale por arriba de la pantalla, la barra se
    // queda a esta distancia del borde (pixeles a 1080p).
    const float MargenArriba = 24f;

    // "Sort Order" del Canvas de cada barra. Un Canvas con numero mas alto tapa
    // a los de numero mas bajo. Este queda POR DEBAJO de toda la interfaz
    // (GM_UI_Canvas esta en -1, el Canvas viejo en 0, los menus en 10 y el
    // fundido a negro en 999): un menu abierto siempre tapa a las barras.
    const int OrdenDeDibujo = -20;

    Camera camara;

    bool listo;              // ya leimos la vida por primera vez
    int vidaAnterior;        // la vida del frame anterior, para detectar golpes
    float parteRelleno;      // 0..1: lo que muestra el relleno
    float parteEstela;       // 0..1: lo que muestra la estela
    float esperaEstela;      // segundos que le faltan a la estela para empezar a vaciarse
    float restoVisible;      // segundos que le quedan a la vista
    float alpha;             // transparencia actual de toda la barra
    float tiempoFrenado;     // segundos reales que lleva el juego con el tiempo en 0

    // La altura de la cabeza se mide una sola vez (el enemigo no cambia de tamaño).
    Collider cuerpo;
    bool cuerpoBuscado;
    bool alturaMedida;
    float alturaCabeza;      // metros desde los pies (el origen del enemigo) hasta arriba del collider

    // Las piezas de la barra nueva (se crean al jugar).
    GameObject raizNueva;          // el objeto suelto, con el Canvas
    Canvas lienzoNuevo;
    CanvasGroup grupoNuevo;
    RectTransform rectBarra;       // el rectangulo de la barra, que se mueve por la pantalla
    Image imagenBorde, imagenFondo, imagenEstela, imagenRelleno;
    bool estiloPendiente = true;   // hay que volver a escribir tamaños y colores en las piezas
    int altoPantallaUsado;         // alto de pantalla (en pixeles) con el que se calcularon los tamaños

    // Para desvanecer la barra vieja.
    CanvasGroup grupoViejo;

    void Awake()
    {
        if (target == null) target = GetComponentInParent<Health>();
    }

    void OnEnable()
    {
        // Arranca siempre invisible y vuelve a leer la vida desde cero.
        listo = false;
        alpha = 0f;
    }

    void OnDisable()
    {
        // Si apagan al enemigo (o a esta barra), que no quede el dibujo en pantalla.
        listo = false;
        alpha = 0f;
        if (lienzoNuevo != null) lienzoNuevo.enabled = false;
        if (grupoViejo != null) grupoViejo.alpha = 0f;
    }

    void OnDestroy()
    {
        // El dibujo esta suelto en la escena (no es hijo del enemigo): hay que
        // llevarselo a mano cuando el enemigo muere.
        if (raizNueva != null) Destroy(raizNueva);
    }

    // Unity llama a esto (solo en el editor) cuando cambias un valor en el
    // Inspector. Asi podes ajustar tamaños y colores MIENTRAS jugas.
    void OnValidate()
    {
        estiloPendiente = true;
    }

    void LateUpdate()
    {
        // Tiempo real, con tope: despues de un tiron no pega un salto.
        float real = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        // HitStop frena el tiempo 0.06 s: ahi la barra sigue andando como si nada.
        // Si el tiempo lleva frenado mas que eso, hay un menu abierto: la barra
        // se esconde y sus relojes (estela, tiempo a la vista) se quedan quietos
        // para seguir donde estaban cuando se vuelva al juego.
        if (Time.timeScale > 0f) tiempoFrenado = 0f;
        else tiempoFrenado += real;
        bool enPausa = tiempoFrenado > PausaLarga;
        float dt = enPausa ? 0f : real;

        // Con el estilo nuevo, el Slider verde viejo se apaga (no se borra).
        if (newStyle && slider != null && slider.gameObject.activeSelf) slider.gameObject.SetActive(false);

        // Sin enemigo (no asignado, destruido o apagado) no hay nada que mostrar.
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            listo = false;
            alpha = 0f;
            if (lienzoNuevo != null && lienzoNuevo.enabled) lienzoNuevo.enabled = false;
            if (grupoViejo != null) grupoViejo.alpha = 0f;
            return;
        }

        int maximo = Mathf.Max(target.maxHp, 1);
        int vida = Mathf.Clamp(target.Current, 0, maximo);
        float parte = vida / (float)maximo;

        // Primera lectura: tomamos la vida como esta, sin animar ni mostrar nada.
        if (!listo)
        {
            listo = true;
            vidaAnterior = vida;
            parteRelleno = parte;
            parteEstela = parte;
            esperaEstela = 0f;
            restoVisible = 0f;
        }

        // ---------- Le pegaron? ----------
        if (vida != vidaAnterior)
        {
            if (vida < vidaAnterior)
            {
                esperaEstela = trailDelay;    // la estela espera antes de vaciarse
                restoVisible = visibleTime;   // y la barra se queda a la vista un rato mas
            }
            vidaAnterior = vida;
        }

        // ---------- Relleno ----------
        // Al perder vida baja de golpe (el golpe se tiene que leer al instante).
        // Si algun dia un enemigo se cura, sube suave.
        if (parte < parteRelleno) parteRelleno = parte;
        else parteRelleno = Mathf.MoveTowards(parteRelleno, parte, dt * 1.5f);

        // ---------- Estela ----------
        if (parteEstela < parteRelleno)
        {
            parteEstela = parteRelleno;   // nunca queda por detras del relleno
        }
        else if (esperaEstela > 0f)
        {
            esperaEstela -= dt;
        }
        else
        {
            parteEstela = Mathf.MoveTowards(parteEstela, parteRelleno, dt * trailSpeed);
        }

        // ---------- Se tiene que ver? ----------
        if (restoVisible > 0f) restoVisible -= dt;
        bool mostrar = !enPausa && (restoVisible > 0f || (!hideUntilDamaged && vida > 0));

        // El fundido usa el tiempo real aunque haya un menu abierto: justamente
        // ahi es cuando se tiene que ir.
        float tiempo = enPausa ? FundidoRapido : (mostrar ? fadeInTime : fadeOutTime);
        alpha = Mathf.MoveTowards(alpha, mostrar ? 1f : 0f, real / Mathf.Max(tiempo, 0.01f));

        if (newStyle) PintarNueva();
        else PintarVieja(vida, maximo);
    }

    // ==================================================================
    // LA BARRA NUEVA
    // ==================================================================

    void PintarNueva()
    {
        // Invisible del todo: no hace falta dibujar ni acomodar nada.
        if (alpha <= 0f)
        {
            if (lienzoNuevo != null && lienzoNuevo.enabled) lienzoNuevo.enabled = false;
            return;
        }

        // Las piezas se crean la primera vez que la barra tiene que verse.
        if (raizNueva == null) ConstruirNueva();

        // Si no se puede acomodar (no hay camara, o el enemigo quedo detras de
        // la camara o fuera de la pantalla) no se dibuja.
        bool acomodada = Acomodar();
        if (lienzoNuevo.enabled != acomodada) lienzoNuevo.enabled = acomodada;
        if (!acomodada) return;

        grupoNuevo.alpha = alpha;
        PonerLargo(imagenRelleno.rectTransform, parteRelleno);

        // La estela ocupa SOLO el tramo perdido: empieza donde termina el relleno.
        // No va por debajo del relleno a proposito: mientras la barra aparece o
        // se desvanece el relleno queda medio transparente, y atras tiene que
        // asomar la pista oscura. Con la estela clara atras se veria rosada.
        // (Se usa el ancla del relleno, el numero exacto, para que no quede ni una rendija.)
        PonerTramo(imagenEstela.rectTransform, imagenRelleno.rectTransform.anchorMax.x, parteEstela);
    }

    // Crea el objeto de la barra: un Canvas de pantalla y, adentro, el
    // rectangulo de la barra con sus cuatro piezas.
    void ConstruirNueva()
    {
        raizNueva = new GameObject("GM_UI_VidaEnemigo (" + target.name + ")", typeof(RectTransform));

        int capaUI = LayerMask.NameToLayer("UI");
        raizNueva.layer = capaUI < 0 ? 0 : capaUI;

        // Screen Space - Overlay: un Canvas pegado a la pantalla, igual que el de
        // la barra de Onji. Sin CanvasScaler, una unidad del Canvas es un pixel.
        lienzoNuevo = raizNueva.AddComponent<Canvas>();
        lienzoNuevo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzoNuevo.sortingOrder = OrdenDeDibujo;

        // Con el CanvasGroup se funde toda la barra de una. Nunca ataja el mouse
        // (ademas el Canvas no tiene GraphicRaycaster: el mouse ni lo ve).
        grupoNuevo = raizNueva.AddComponent<CanvasGroup>();
        grupoNuevo.alpha = 0f;
        grupoNuevo.interactable = false;
        grupoNuevo.blocksRaycasts = false;

        // El rectangulo de la barra. Anclado a la esquina de ABAJO A LA IZQUIERDA
        // de la pantalla: asi su posicion se escribe directamente en pixeles.
        // El pivote al centro: la posicion es la del centro de la barra.
        GameObject barra = new GameObject("Barra", typeof(RectTransform));
        barra.layer = raizNueva.layer;
        rectBarra = (RectTransform)barra.transform;
        rectBarra.SetParent(raizNueva.transform, false);
        rectBarra.anchorMin = Vector2.zero;
        rectBarra.anchorMax = Vector2.zero;
        rectBarra.pivot = new Vector2(0.5f, 0.5f);

        // En la UI lo que se crea despues se dibuja ENCIMA.
        imagenBorde = CrearPieza("Borde");
        imagenFondo = CrearPieza("Fondo");
        imagenEstela = CrearPieza("Estela");
        imagenRelleno = CrearPieza("Relleno");

        estiloPendiente = true;
    }

    // Un rectangulo liso (un Image sin dibujo) que ocupa toda la barra.
    Image CrearPieza(string nombre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = raizNueva.layer;

        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(rectBarra, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        Image imagen = go.AddComponent<Image>();
        imagen.raycastTarget = false;
        return imagen;
    }

    // Escribe en las piezas los tamaños y colores de este componente.
    //   altoPantalla  alto de la pantalla en pixeles reales.
    void AplicarEstilo(int altoPantalla)
    {
        estiloPendiente = false;
        altoPantallaUsado = altoPantalla;

        // Width y Height estan pensados para 1080 de alto. En otra pantalla (o
        // en una ventana Game mas chica) se achican en proporcion, igual que la
        // barra de Onji, y se redondean a pixeles enteros para que se vea nitida.
        float proporcion = altoPantalla / AltoDePantalla;

        // El tamaño del rectangulo es el de la parte que se llena.
        rectBarra.sizeDelta = new Vector2(PixelesPares(width * proporcion), PixelesPares(height * proporcion));

        // El marco no es un dibujo hueco: es un rectangulo hueso un poco mas
        // grande que la pista, puesto DETRAS. Lo unico que asoma es el borde.
        // Si hay marco, mide como minimo un pixel (si no, en una ventana chica desapareceria).
        int borde = borderThickness > 0f ? Mathf.Max(1, Mathf.RoundToInt(borderThickness * proporcion)) : 0;

        // (Con las anclas estiradas, "sizeDelta" es cuanto MAS grande que el padre es.)
        imagenBorde.rectTransform.sizeDelta = new Vector2(borde * 2f, borde * 2f);
        imagenBorde.enabled = borde > 0;

        imagenBorde.color = borderColor;
        imagenFondo.color = trackColor;
        imagenEstela.color = trailColor;
        imagenRelleno.color = fillColor;
    }

    // Redondea a una cantidad PAR de pixeles (minimo 2). Par a proposito: como
    // el centro de la barra se pone en un pixel entero, con un tamaño par los
    // dos bordes caen justo entre pixeles y no salen borrosos.
    static float PixelesPares(float pixeles)
    {
        return Mathf.Max(2, Mathf.RoundToInt(pixeles * 0.5f) * 2);
    }

    // El largo del relleno se maneja con el ancla derecha: 0 = vacia, 1 = llena.
    static void PonerLargo(RectTransform barra, float parte)
    {
        Vector2 ancla = barra.anchorMax;
        if (Mathf.Approximately(ancla.x, parte)) return;   // no cambio: no tocamos la UI
        ancla.x = parte;
        barra.anchorMax = ancla;
    }

    // Hace que la pieza ocupe solo el tramo que va de "desde" a "hasta"
    // (los dos de 0 a 1: 0 = borde izquierdo de la barra, 1 = borde derecho).
    static void PonerTramo(RectTransform barra, float desde, float hasta)
    {
        if (hasta < desde) hasta = desde;   // tramo vacio: no se ve nada

        Vector2 izquierda = barra.anchorMin;
        Vector2 derecha = barra.anchorMax;
        if (izquierda.x == desde && derecha.x == hasta) return;   // no cambio: no tocamos la UI

        izquierda.x = desde;
        derecha.x = hasta;
        barra.anchorMin = izquierda;
        barra.anchorMax = derecha;
    }

    // Pone la barra en el pixel de la pantalla donde se ve la cabeza del enemigo.
    // Devuelve false si no hay que dibujarla: no hay camara, o el enemigo esta
    // detras de la camara o fuera de la pantalla.
    // (Como la barra esta en la pantalla y no en el mundo, siempre queda de
    // frente y derecha: no hace falta girarla hacia la camara.)
    bool Acomodar()
    {
        if (camara == null || !camara.isActiveAndEnabled) camara = Camera.main;
        if (camara == null) return false;

        // WorldToScreenPoint: "este punto del mundo, en que pixel se ve?"
        // x, y son pixeles desde la esquina de abajo a la izquierda;
        // z son los metros hacia adelante de la camara.
        Vector3 enPantalla = camara.WorldToScreenPoint(PuntoSobreLaCabeza());

        // Detras de la camara (o pegado a ella): no se dibuja.
        if (enPantalla.z <= camara.nearClipPlane) return false;

        // Los tamaños dependen del alto de la pantalla: se recalculan si cambio
        // (por ejemplo al agrandar la ventana Game) o si tocaste algo en el Inspector.
        int altoPantalla = camara.pixelHeight;
        if (estiloPendiente || altoPantalla != altoPantallaUsado) AplicarEstilo(altoPantalla);

        // Fuera de la pantalla por los costados o por abajo (con un poco de
        // margen): tampoco se dibuja.
        Vector2 tamano = rectBarra.sizeDelta;
        float margenX = tamano.x * 0.5f + 8f;
        float margenY = tamano.y * 0.5f + 8f;
        if (enPantalla.x < -margenX || enPantalla.x > camara.pixelWidth + margenX) return false;
        if (enPantalla.y < -margenY) return false;

        // Por ARRIBA es distinto. El jefe es alto: de cerca, con la camara
        // mirando un poco hacia abajo, su cabeza se sale de la pantalla y la
        // barra se iria con ella justo en la pelea. En ese caso la barra se
        // queda pegada al borde de arriba, encima de el.
        // Solo mientras el enemigo se vea: si hasta los pies quedaron fuera, no.
        float tope = altoPantalla - MargenArriba * (altoPantalla / AltoDePantalla) - tamano.y * 0.5f;
        if (enPantalla.y > tope)
        {
            Vector3 pies = camara.WorldToScreenPoint(target.transform.position);
            if (pies.z <= camara.nearClipPlane || pies.y > altoPantalla) return false;
            enPantalla.y = tope;
        }

        // Hasta Shrink Distance mide siempre lo mismo. Mas lejos se achica en la
        // misma proporcion que el enemigo (al doble de distancia, la mitad).
        float escala = 1f;
        float limite = Mathf.Max(shrinkDistance, 0.1f);
        if (!camara.orthographic && enPantalla.z > limite) escala = limite / enPantalla.z;

        // Pixeles enteros: si la barra cayera "entre dos pixeles", el marco de
        // un pixel se veria mas grueso o mas fino segun el frame (titilaria).
        Vector2 posicion = new Vector2(Mathf.Round(enPantalla.x), Mathf.Round(enPantalla.y));
        if (rectBarra.anchoredPosition != posicion) rectBarra.anchoredPosition = posicion;

        Vector3 escalaNueva = new Vector3(escala, escala, 1f);
        if (rectBarra.localScale != escalaNueva) rectBarra.localScale = escalaNueva;
        return true;
    }

    // El punto del mundo donde va el centro de la barra.
    // Se mide desde el enemigo (no desde el suelo): si el enemigo esta mas
    // alto o mas bajo que el piso, la barra lo sigue igual.
    Vector3 PuntoSobreLaCabeza()
    {
        Vector3 pies = target.transform.position;

        if (autoHeight)
        {
            if (!alturaMedida) MedirAltura();
            if (alturaMedida) return pies + Vector3.up * (alturaCabeza + headMargin);
        }
        return pies + worldOffset;
    }

    // Mide hasta donde llega el collider del enemigo por encima de su origen.
    void MedirAltura()
    {
        if (!cuerpoBuscado)
        {
            cuerpoBuscado = true;
            cuerpo = target.GetComponent<Collider>();
        }
        if (cuerpo == null || !cuerpo.enabled) return;

        // "bounds" es la caja que envuelve al collider, en metros del mundo
        // (ya tiene en cuenta la escala del enemigo).
        Bounds caja = cuerpo.bounds;
        if (caja.size.y < 0.01f) return;   // todavia no esta lista: se prueba de nuevo el proximo frame

        alturaCabeza = caja.max.y - target.transform.position.y;
        alturaMedida = true;
    }

    // ==================================================================
    // LA BARRA VIEJA (el Slider verde). Solo con New Style destildado.
    // ==================================================================

    void PintarVieja(int vida, int maximo)
    {
        // La nueva se esconde.
        if (lienzoNuevo != null && lienzoNuevo.enabled) lienzoNuevo.enabled = false;

        if (slider == null) return;

        // Un CanvasGroup nos deja desvanecer todo el Slider de una. Si no hay, lo agregamos.
        if (grupoViejo == null)
        {
            grupoViejo = slider.GetComponent<CanvasGroup>();
            if (grupoViejo == null) grupoViejo = slider.gameObject.AddComponent<CanvasGroup>();
        }

        bool conVida = vida > 0;
        if (slider.gameObject.activeSelf != conVida) slider.gameObject.SetActive(conVida);

        slider.minValue = 0f;
        slider.maxValue = maximo;
        slider.value = vida;
        grupoViejo.alpha = alpha;

        // Se para sobre el enemigo y mira a la camara (este objeto es el Canvas viejo,
        // que si esta en el mundo 3D).
        transform.position = target.transform.position + worldOffset;

        if (camara == null || !camara.isActiveAndEnabled) camara = Camera.main;
        if (camara != null)
        {
            Vector3 haciaAdelante = transform.position - camara.transform.position;
            if (haciaAdelante.sqrMagnitude > 0.0001f) transform.forward = haciaAdelante.normalized;
        }
    }
}
