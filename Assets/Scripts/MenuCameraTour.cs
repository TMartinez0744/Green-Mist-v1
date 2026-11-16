using UnityEngine;
using UnityEngine.UI;

// El RECORRIDO DE CAMARA del menu de inicio.
//
// Mientras el menu principal esta a la vista, este script lleva la camara del
// juego por una lista de "tomas" (el pueblo desde arriba, la calle, el
// estanque...). Cada toma es un movimiento lento entre dos puntos, INICIO y
// FIN. Al terminar, la imagen se va a negro un segundo, la camara salta a la
// toma siguiente y la imagen vuelve. Cuando se acaban, empieza otra vez.
//
// Va en el objeto "GM_MenuCamara", que arma el menu Green Mist > 11. Los puntos
// de cada toma son hijos de ese objeto: se mueven como cualquier otro objeto.
//
// COMO SABE QUE ESTAMOS EN EL MENU: mira si "MainMenuPanel" esta prendido, lo
// mismo que hacen la pausa, la barra de vida y el sonido.
//
// LA CAMARA ES LA DEL JUEGO, no una segunda: otros scripts usan la camara
// principal y ella lleva el "oido" del juego (AudioListener). Por eso, mientras
// dura el recorrido, este script APAGA ThirdPersonCamera (asi no se pelean por
// mover la camara y tampoco se acumula el movimiento del mouse) y al terminar
// la vuelve a prender y deja la camara exactamente detras de Onji.
//
// CUANDO TERMINA: MainMenuUI, al apretar PLAY, funde a negro y recien con la
// pantalla negra apaga "MainMenuPanel". En ese mismo frame este script devuelve
// la camara, asi que el salto no se ve.
//
// TODO VA EN TIEMPO REAL (unscaled): en el menu Time.timeScale vale 0 y con el
// tiempo del juego la camara no se moveria nunca.
//
// EL NEGRO ENTRE TOMAS lo crea este script al jugar (un objeto suelto llamado
// "GM_MenuCamara_Cortina"). Se dibuja DEBAJO del menu, asi el titulo y las
// opciones no parpadean, y no puede atajar clics (no tiene con que).
//
// DefaultExecutionOrder(50): corre despues que los scripts comunes y antes que
// las barras de vida de los enemigos (100), que leen donde quedo la camara.
[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public class MenuCameraTour : MonoBehaviour
{
    // Una toma: de donde a donde va la camara y cuanto tarda.
    [System.Serializable]
    public class Shot
    {
        [Tooltip("Nombre de la toma. Es solo para reconocerla en esta lista: no cambia nada.")]
        public string name = "";

        [Tooltip("Donde EMPIEZA la camara: su posicion y hacia donde mira. Es un objeto vacio; para acomodarlo, "
               + "elegilo, lleva la Scene view a donde te guste y usa GameObject > Align With View (Ctrl+Shift+F).")]
        public Transform start;

        [Tooltip("Donde TERMINA la camara. Se acomoda igual que el inicio.")]
        public Transform end;

        [Tooltip("Segundos que dura la toma entera, contando el fundido de entrada y el de salida.")]
        [Range(4f, 90f)] public float duration = 20f;

        [Tooltip("Campo de vision de esta toma, en grados. 0 = el mismo que usa la camara jugando. Mas chico = mas zoom.")]
        [Range(0f, 100f)] public float fieldOfView = 0f;
    }

    [Header("Referencias (las pone el menu Green Mist > 11)")]
    [Tooltip("El interruptor: 'MainMenuPanel'. Mientras esta prendido, la camara hace el recorrido. Si queda vacio, este script no hace nada.")]
    public GameObject mainMenuPanel;

    [Tooltip("La camara que se mueve. Vacio = la camara principal (la que tiene el tag MainCamera).")]
    public Camera targetCamera;

    [Header("Tomas (se recorren de arriba hacia abajo y vuelven a empezar)")]
    [Tooltip("La lista de tomas. Arrastra un elemento para cambiar el orden. Para saltear una toma sin borrarla, "
           + "apaga su objeto 'Toma N' en la Hierarchy. Con la lista vacia la camara queda quieta detras de Onji, como antes.")]
    public Shot[] shots = new Shot[0];

    [Tooltip("Tildado: cada vez que aparece el menu arranca en una toma al azar. Destildado: siempre por la primera.")]
    public bool startOnRandomShot = false;

    [Tooltip("PARA ENSAYAR. 0 = recorrido normal. 1, 2, 3... = repite solo esa toma (segun el orden de la lista), "
           + "asi la miras en Play sin esperar a las demas. Acordate de volverlo a 0.")]
    [Range(0, 12)] public int onlyShot = 0;

    [Header("Movimiento")]
    [Tooltip("Que tan suave arranca y frena la camara en cada toma. 0 = velocidad pareja de punta a punta. "
           + "1 = arranca y termina quieta del todo.")]
    [Range(0f, 1f)] public float easing = 0.6f;

    [Header("Fundido a negro entre tomas")]
    [Tooltip("Segundos que tarda la imagen en irse a negro al final de una toma, y lo mismo en volver al empezar la siguiente. 0 = corte seco.")]
    [Range(0f, 4f)] public float transitionTime = 1f;

    [Tooltip("Color del fundido. De fabrica es el 'tinta' de los menus (casi negro, apenas verdoso).")]
    public Color dipColor = new Color(0.03f, 0.045f, 0.045f, 1f);

    [Header("Dibujo en la Scene view")]
    [Tooltip("Tildado: dibuja en la Scene view el camino de cada toma, una camarita en cada punta y el numero de la toma.")]
    public bool showGizmos = true;

    // "Sort Order" del Canvas del negro entre tomas. Un Canvas con numero mas
    // alto tapa a los de numero mas bajo. Este queda DEBAJO de todos los menus
    // (GM_UI_Canvas esta en -1, el Canvas viejo en 0, los menus nuevos en 10 y
    // el fundido de PLAY en 999) y encima de las barras de los enemigos (-20).
    // Por eso sirve igual con el menu nuevo y con el viejo (Green Mist > 9b).
    const int OrdenDeLaCortina = -10;

    // Si un frame tarda mucho (al cargar la escena, por ejemplo), que la toma
    // no pegue un salto: como mucho avanza esto por frame.
    const float PasoMaximo = 0.1f;

    // ---------- Estado ----------
    bool recorriendo;        // la camara la estamos moviendo nosotros
    int tomaActual = -1;     // indice en "shots"
    float tiempoToma;        // segundos que lleva la toma actual

    Camera cam;                          // la camara que estamos moviendo
    ThirdPersonCamera camaraDeJuego;     // el script que la mueve jugando
    bool apagamosLaCamaraDeJuego;        // la apagamos nosotros: hay que volver a prenderla

    // Como hay que dejar la camara al terminar: detras de Onji, como la espera el juego.
    Vector3 posicionDeJuego;
    Quaternion rotacionDeJuego;
    float campoDeJuego;

    // El negro entre tomas (se crea al jugar, la primera vez que hace falta).
    GameObject cortinaObjeto;
    Canvas cortinaLienzo;
    Image cortinaImagen;

    // Para el Inspector y otros scripts: true mientras la camara esta en el recorrido.
    public bool IsTouring => recorriendo;

    // ------------------------------------------------------------------
    // Arrancar y terminar
    // ------------------------------------------------------------------

    void Update()
    {
        // El recorrido ARRANCA aca, en Update, y no en LateUpdate. En el primer
        // frame del juego todos los Start ya corrieron (MainMenuUI ya prendio el
        // menu y ThirdPersonCamera ya puso la camara detras de Onji), pero
        // ThirdPersonCamera todavia no hizo su primer LateUpdate, que es donde
        // lee el mouse. La apagamos justo antes: no llega a acumular nada.
        if (!recorriendo && MenuALaVista()) Empezar();
    }

    void LateUpdate()
    {
        if (!recorriendo) return;

        // El recorrido TERMINA aca, en LateUpdate. MainMenuUI apaga el menu
        // desde una corrutina, y las corrutinas corren entre Update y
        // LateUpdate: si miraramos en Update nos enterariamos un frame tarde.
        if (!MenuALaVista() || cam == null)
        {
            Terminar();
            return;
        }

        // La toma actual dejo de servir (la borraron o la apagaron mientras se
        // jugaba): se pasa a la siguiente. Si no queda ninguna, se termina.
        if (!EsUsable(tomaActual) && !PasarALaSiguiente())
        {
            Terminar();
            return;
        }

        tiempoToma += Mathf.Min(Time.unscaledDeltaTime, PasoMaximo);

        // Se acabo la toma: corte a la siguiente. En este momento la pantalla
        // esta negra (el fundido de salida ya llego al final).
        if (tiempoToma >= Duracion(shots[tomaActual])) PasarALaSiguiente();

        Shot toma = shots[tomaActual];
        float duracion = Duracion(toma);

        // ---------- La camara ----------
        Vector3 posicion;
        Quaternion rotacion;
        CalcularPose(toma, tiempoToma / duracion, out posicion, out rotacion);
        cam.transform.SetPositionAndRotation(posicion, rotacion);
        cam.fieldOfView = toma.fieldOfView > 0f ? Mathf.Clamp(toma.fieldOfView, 20f, 100f) : campoDeJuego;

        // ---------- El negro: baja al empezar la toma y sube al terminar ----------
        float fundido = Mathf.Min(transitionTime, duracion * 0.5f);
        float negro = 0f;
        if (fundido > 0.0001f)
        {
            if (tiempoToma < fundido) negro = 1f - tiempoToma / fundido;
            else if (tiempoToma > duracion - fundido) negro = (tiempoToma - (duracion - fundido)) / fundido;

            // SmoothStep: entra y sale suave en vez de en linea recta.
            negro = Mathf.SmoothStep(0f, 1f, negro);
        }
        PintarCortina(negro);
    }

    void OnDisable()
    {
        // Si apagan este componente (o su objeto) en medio del recorrido, la
        // camara vuelve a ser del juego.
        Terminar();
    }

    void OnDestroy()
    {
        // La cortina es un objeto suelto en la escena: hay que llevarsela a mano.
        if (cortinaObjeto != null) Destroy(cortinaObjeto);
    }

    bool MenuALaVista()
    {
        return mainMenuPanel != null && mainMenuPanel.activeInHierarchy;
    }

    void Empezar()
    {
        // Sin ninguna toma que sirva no tocamos nada: la camara queda como siempre.
        int primera = PrimeraToma();
        if (primera < 0) return;

        Camera camara = targetCamera != null ? targetCamera : Camera.main;
        if (camara == null) return;
        cam = camara;

        // Anotamos como esta la camara ahora, para dejarla igual al terminar.
        posicionDeJuego = cam.transform.position;
        rotacionDeJuego = cam.transform.rotation;
        campoDeJuego = cam.fieldOfView;

        camaraDeJuego = cam.GetComponent<ThirdPersonCamera>();
        apagamosLaCamaraDeJuego = false;
        if (camaraDeJuego != null && camaraDeJuego.enabled)
        {
            // ThirdPersonCamera termina cada frame mirando al punto
            // "target + targetOffset" (su transform.LookAt). Hacemos esa misma
            // cuenta para anotar la rotacion tal cual ella la va a dejar: asi,
            // al devolverle la camara, no hay ni un frame de diferencia.
            if (camaraDeJuego.target != null)
            {
                Vector3 hacia = camaraDeJuego.target.position + camaraDeJuego.targetOffset - posicionDeJuego;
                if (hacia.sqrMagnitude > 0.0001f) rotacionDeJuego = Quaternion.LookRotation(hacia);
            }

            // Apagada no corre su LateUpdate: no mueve la camara ni lee el mouse.
            // Sus datos internos (donde cree que esta la camara) quedan intactos,
            // y Onji no se mueve mientras dura el menu, asi que al prenderla
            // sigue exactamente donde estaba.
            camaraDeJuego.enabled = false;
            apagamosLaCamaraDeJuego = true;
        }

        tomaActual = primera;
        tiempoToma = 0f;
        recorriendo = true;
        // De mover la camara se encarga LateUpdate, en este mismo frame.
    }

    void Terminar()
    {
        if (!recorriendo) return;
        recorriendo = false;
        tomaActual = -1;

        PintarCortina(0f);

        // La camara vuelve detras de Onji. Cuando esto pasa por PLAY la pantalla
        // esta negra (el fundido de MainMenuUI), asi que el salto no se ve.
        if (cam != null)
        {
            cam.transform.SetPositionAndRotation(posicionDeJuego, rotacionDeJuego);
            cam.fieldOfView = campoDeJuego;
        }

        if (apagamosLaCamaraDeJuego && camaraDeJuego != null) camaraDeJuego.enabled = true;
        apagamosLaCamaraDeJuego = false;
    }

    // ------------------------------------------------------------------
    // Las tomas
    // ------------------------------------------------------------------

    // Una toma sirve si tiene sus dos puntos y estan prendidos.
    // (Apagar el objeto "Toma N" en la Hierarchy es la forma de saltearla.)
    public static bool IsUsable(Shot toma)
    {
        if (toma == null || toma.start == null || toma.end == null) return false;
        return toma.start.gameObject.activeInHierarchy && toma.end.gameObject.activeInHierarchy;
    }

    bool EsUsable(int indice)
    {
        return shots != null && indice >= 0 && indice < shots.Length && IsUsable(shots[indice]);
    }

    static float Duracion(Shot toma)
    {
        return Mathf.Max(toma.duration, 1f);
    }

    // Con que toma se arranca. Devuelve -1 si no hay ninguna que sirva.
    int PrimeraToma()
    {
        if (shots == null) return -1;

        // Modo ensayo: solo esa (si sirve).
        if (EsUsable(onlyShot - 1)) return onlyShot - 1;

        if (!startOnRandomShot) return SiguienteUsable(-1);

        // Al azar entre las que sirven: se cuentan y se elige la numero "elegida".
        int cuantas = 0;
        for (int i = 0; i < shots.Length; i++)
            if (IsUsable(shots[i])) cuantas++;
        if (cuantas == 0) return -1;

        int elegida = Random.Range(0, cuantas);
        for (int i = 0; i < shots.Length; i++)
        {
            if (!IsUsable(shots[i])) continue;
            if (elegida == 0) return i;
            elegida--;
        }
        return -1;
    }

    // La proxima toma que sirve despues de "desde", dando la vuelta a la lista.
    // Si la unica que sirve es la misma, devuelve esa. -1 si no hay ninguna.
    int SiguienteUsable(int desde)
    {
        if (shots == null || shots.Length == 0) return -1;

        for (int paso = 1; paso <= shots.Length; paso++)
        {
            int i = (desde + paso) % shots.Length;
            if (IsUsable(shots[i])) return i;
        }
        return -1;
    }

    // Pasa a la toma siguiente y la arranca de cero. Devuelve false si no hay ninguna.
    bool PasarALaSiguiente()
    {
        int siguiente = EsUsable(onlyShot - 1) ? onlyShot - 1 : SiguienteUsable(tomaActual);
        if (siguiente < 0) return false;

        tomaActual = siguiente;
        tiempoToma = 0f;
        return true;
    }

    // Donde esta la camara cuando la toma lleva recorrida la parte "avance" (0 = inicio, 1 = fin).
    void CalcularPose(Shot toma, float avance, out Vector3 posicion, out Quaternion rotacion)
    {
        avance = Mathf.Clamp01(avance);

        // Mezcla entre ir a velocidad pareja (avance) y arrancar/frenar suave
        // (SmoothStep). Con "easing" en el medio la camara nunca queda quieta
        // del todo, pero tampoco arranca de golpe.
        float t = Mathf.Lerp(avance, Mathf.SmoothStep(0f, 1f, avance), easing);

        posicion = Vector3.Lerp(toma.start.position, toma.end.position, t);
        rotacion = Quaternion.Slerp(toma.start.rotation, toma.end.rotation, t);
    }

    // Lo mismo, para el boton "Reproducir" del Inspector (asi la prueba en la
    // Scene view usa exactamente la misma cuenta que el juego).
    // Devuelve false si esa toma no existe o no sirve.
    public bool GetPose(int index, float progress, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (!EsUsable(index)) return false;

        CalcularPose(shots[index], progress, out position, out rotation);
        return true;
    }

    // ------------------------------------------------------------------
    // El negro entre tomas
    // ------------------------------------------------------------------

    void PintarCortina(float cuanto)
    {
        // Transparente del todo: se apaga el Canvas y no se dibuja nada.
        if (cuanto <= 0.001f)
        {
            if (cortinaLienzo != null && cortinaLienzo.enabled) cortinaLienzo.enabled = false;
            return;
        }

        if (cortinaObjeto == null) CrearCortina();
        if (!cortinaLienzo.enabled) cortinaLienzo.enabled = true;

        Color color = dipColor;
        color.a = dipColor.a * Mathf.Clamp01(cuanto);
        cortinaImagen.color = color;
    }

    // Un Canvas de pantalla con una sola imagen lisa que ocupa todo.
    // A proposito NO tiene GraphicRaycaster y la imagen no es "raycast target":
    // el mouse lo atraviesa, nunca puede tapar un clic del menu.
    void CrearCortina()
    {
        cortinaObjeto = new GameObject("GM_MenuCamara_Cortina", typeof(RectTransform));

        cortinaLienzo = cortinaObjeto.AddComponent<Canvas>();
        cortinaLienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        cortinaLienzo.sortingOrder = OrdenDeLaCortina;

        GameObject negro = new GameObject("Negro", typeof(RectTransform));
        negro.transform.SetParent(cortinaObjeto.transform, false);

        // Estirada de punta a punta de la pantalla.
        RectTransform rect = (RectTransform)negro.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        cortinaImagen = negro.AddComponent<Image>();
        cortinaImagen.raycastTarget = false;
    }

    // ------------------------------------------------------------------
    // Dibujo en la Scene view (solo en el editor, no existe en el juego compilado)
    // ------------------------------------------------------------------

    static readonly Color ColorCamino = new Color(0.40f, 0.63f, 0.57f, 0.9f);   // jade
    static readonly Color ColorInicio = new Color(0.45f, 0.85f, 0.55f, 1f);     // verde: de aca sale
    static readonly Color ColorFin = new Color(0.95f, 0.65f, 0.30f, 1f);        // naranja: aca llega
    static readonly Color ColorApagada = new Color(0.5f, 0.5f, 0.5f, 0.5f);     // toma salteada

    void OnDrawGizmos()
    {
        if (!showGizmos || shots == null) return;

        float campo = targetCamera != null ? targetCamera.fieldOfView : 60f;

        for (int i = 0; i < shots.Length; i++)
        {
            Shot toma = shots[i];
            if (toma == null || toma.start == null || toma.end == null) continue;

            bool sirve = IsUsable(toma);
            float campoToma = toma.fieldOfView > 0f ? Mathf.Clamp(toma.fieldOfView, 20f, 100f) : campo;

            // El camino.
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = sirve ? ColorCamino : ColorApagada;
            Gizmos.DrawLine(toma.start.position, toma.end.position);

            // Una camarita en cada punta, mirando hacia donde mira la toma.
            DibujarCamarita(toma.start, campoToma, sirve ? ColorInicio : ColorApagada);
            DibujarCamarita(toma.end, campoToma, sirve ? ColorFin : ColorApagada);

#if UNITY_EDITOR
            // El numero de la toma, a mitad de camino.
            Vector3 medio = (toma.start.position + toma.end.position) * 0.5f;
            UnityEditor.Handles.Label(medio, "Toma " + (i + 1));
#endif
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    static void DibujarCamarita(Transform punto, float campo, Color color)
    {
        Gizmos.color = color;

        // Con esta matriz, lo que se dibuje en (0,0,0) mirando hacia +Z queda
        // en el lugar del punto y mirando hacia donde mira el punto.
        Gizmos.matrix = Matrix4x4.TRS(punto.position, punto.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, campo, 2.5f, 0.05f, 16f / 9f);
        Gizmos.DrawSphere(Vector3.zero, 0.12f);
    }
}
