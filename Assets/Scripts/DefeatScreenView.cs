using UnityEngine;
using UnityEngine.UI;

// La pantalla de DERROTA. Va en el objeto "GM_UI_Derrota".
//
// Cada vez que su objeto se PRENDE, hace una secuencia lenta, al estilo de los
// Souls:
//   1) la pantalla se oscurece de a poco hasta quedar color tinta;
//   2) un respiro a oscuras;
//   3) aparece la palabra DEFEAT, despacio y creciendo apenas;
//   4) recien despues aparecen las opciones (RETRY, MAIN MENU).
//
// Las opciones son otro objeto, con su propio PauseMenuView (el mismo script
// del menu de pausa: flecha de pincel, mouse, W/S y Enter). Este script lo
// tiene APAGADO hasta el paso 4, asi no se puede apretar Enter sobre una
// opcion que todavia no se ve.
//
// Quien prende este objeto es MenuScreenFollower, cuando PlayerDeath prende el
// panel "DeathMenu". Este script no pausa el juego ni recarga la escena: solo
// dibuja.
//
// OJO: PlayerDeath frena el tiempo (Time.timeScale = 0) en medio de la
// secuencia. Por eso todo aca usa Time.unscaledDeltaTime (tiempo real).
[DisallowMultipleComponent]
public class DefeatScreenView : MonoBehaviour
{
    [Header("Referencias (las pone el menu Green Mist > 9. Menus)")]
    [Tooltip("El oscurecido de pantalla completa. Jugando arranca transparente y llega hasta la transparencia que tiene en la escena.")]
    public Image dim;

    [Tooltip("El CanvasGroup del objeto que tiene la palabra DEFEAT (y la rayita de abajo).")]
    public CanvasGroup title;

    [Tooltip("El objeto con las opciones (RETRY, MAIN MENU). Se prende solo cuando les toca aparecer.")]
    public GameObject options;

    [Header("1) Oscurecido")]
    [Tooltip("Segundos que tarda la pantalla en oscurecerse. Conviene que sea igual a 'Fundido Negro' de PlayerDeath.")]
    [Range(0f, 5f)] public float dimTime = 1.5f;

    [Header("2) y 3) La palabra DEFEAT")]
    [Tooltip("Segundos desde que se prende la pantalla hasta que EMPIEZA a aparecer DEFEAT. "
           + "Conviene que sea 'Fundido Negro' + 'Espera En Negro' de PlayerDeath: ahi es cuando el juego se frena.")]
    [Range(0f, 8f)] public float titleDelay = 2.1f;

    [Tooltip("Segundos que tarda DEFEAT en aparecer del todo.")]
    [Range(0.05f, 6f)] public float titleTime = 2f;

    [Tooltip("Tamaño con el que arranca DEFEAT (despues crece hasta su tamaño normal). 1 = no crece.")]
    [Range(0.8f, 1f)] public float titleStartScale = 0.94f;

    [Header("4) Opciones")]
    [Tooltip("Segundos desde que se prende la pantalla hasta que aparecen RETRY y MAIN MENU. Tiene que ser mas que 'Title Delay'. "
           + "El mouse tambien vuelve recien en ese momento.")]
    [Range(0f, 12f)] public float optionsDelay = 3.6f;

    [Header("Interno")]
    [Tooltip("Version del estilo (colores, tamaños) que le puso el menu Green Mist a esta pantalla. "
           + "No hace falta tocarlo. Si lo pones en 0, la proxima vez que corras el menu vuelve todo a los valores de fabrica.")]
    public int styleVersion;

    bool preparado;
    Color colorFondo;          // el color del oscurecido tal como esta en la escena
    Vector3 escalaTitulo;      // la escala del titulo tal como esta en la escena

    float reloj;               // segundos reales desde que se prendio la pantalla
    bool opcionesMostradas;
    float fondoAplicado = -1f; // lo ultimo que se le escribio a la UI, para no reescribir lo mismo
    float tituloAplicado = -1f;
    bool cursorEscondido;      // este script escondio el mouse y todavia no lo devolvio

    // Guarda como estan las cosas en la escena, para animar a partir de ahi.
    void Preparar()
    {
        if (preparado) return;
        preparado = true;

        if (dim != null) colorFondo = dim.color;
        if (title != null) escalaTitulo = title.transform.localScale;
    }

    void OnEnable()
    {
        Preparar();

        // Todo arranca invisible.
        reloj = 0f;
        opcionesMostradas = false;
        fondoAplicado = -1f;
        tituloAplicado = -1f;
        AplicarFondo(0f);
        AplicarTitulo(0f);

        // Las opciones esperan apagadas. Si la espera es 0 no hay nada que esperar.
        if (optionsDelay > 0f)
        {
            if (options != null && options.activeSelf) options.SetActive(false);
        }
        else
        {
            MostrarOpciones();
        }
    }

    void Update()
    {
        // Tiempo real, con tope: si el juego pego un tiron no se saltea la animacion.
        reloj += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        // ---------- 1) Oscurecido ----------
        float fondo = dimTime > 0f ? Mathf.Clamp01(reloj / dimTime) : 1f;
        AplicarFondo(fondo);

        // ---------- 3) DEFEAT ----------
        float titulo = Mathf.Clamp01((reloj - titleDelay) / Mathf.Max(titleTime, 0.05f));
        AplicarTitulo(titulo);

        // ---------- 4) Opciones ----------
        if (!opcionesMostradas && reloj >= optionsDelay) MostrarOpciones();
    }

    // El mouse: cuando PlayerDeath frena el juego, CursorLock suelta el mouse y
    // lo muestra en el medio de la pantalla. Eso pasa ANTES de que aparezcan las
    // opciones, y quedaba una flechita sola sobre la pantalla negra, justo
    // mientras aparece DEFEAT. Hasta que haya algo para elegir, lo escondemos.
    // (Solo se esconde: CursorLock sigue decidiendo si esta encerrado o suelto.)
    // LateUpdate porque corre despues del Update de CursorLock, en el mismo frame.
    void LateUpdate()
    {
        if (opcionesMostradas) return;

        if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
        {
            Cursor.visible = false;
            cursorEscondido = true;
        }
    }

    void OnDisable()
    {
        // Por las dudas: si la pantalla se apaga antes de mostrar las opciones,
        // que el mouse no quede escondido.
        DevolverCursor();
    }

    // Vuelve a mostrar el mouse si lo habia escondido este script.
    void DevolverCursor()
    {
        if (!cursorEscondido) return;
        cursorEscondido = false;

        // Si el mouse esta encerrado (el juego corre) no lo tocamos: lo maneja CursorLock.
        if (Cursor.lockState == CursorLockMode.None) Cursor.visible = true;
    }

    //   avance  0 = transparente, 1 = oscuro del todo
    void AplicarFondo(float avance)
    {
        if (dim == null) return;
        if (Mathf.Approximately(avance, fondoAplicado)) return;   // no cambio: no tocamos la UI
        fondoAplicado = avance;

        // Curva suave: arranca y frena despacio en vez de oscurecer parejo.
        float suave = avance * avance * (3f - 2f * avance);

        Color c = colorFondo;
        c.a = colorFondo.a * suave;
        dim.color = c;
    }

    //   avance  0 = invisible, 1 = a la vista
    void AplicarTitulo(float avance)
    {
        if (title == null) return;
        if (Mathf.Approximately(avance, tituloAplicado)) return;
        tituloAplicado = avance;

        float suave = avance * avance * (3f - 2f * avance);
        title.alpha = suave;

        // Crece apenas mientras aparece, y va frenando al llegar (el "YOU DIED" de los Souls).
        float frenando = 1f - (1f - avance) * (1f - avance);
        title.transform.localScale = escalaTitulo * Mathf.Lerp(titleStartScale, 1f, frenando);
    }

    void MostrarOpciones()
    {
        opcionesMostradas = true;

        // Ya hay algo para elegir: vuelve el mouse.
        DevolverCursor();

        // Al prenderse, el PauseMenuView de las opciones hace su propio fundido.
        if (options != null && !options.activeSelf) options.SetActive(true);
    }
}
