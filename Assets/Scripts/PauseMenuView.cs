using UnityEngine;
using UnityEngine.UI;

// La parte VISUAL de un menu de opciones de texto. Nacio para la pausa (por eso
// el nombre) y va en "GM_UI_Pausa", pero no sabe nada de pausar: sirve igual
// para el menu principal o la pantalla de derrota. Alcanza con ponerlo en el
// objeto que se prende y se apaga, y cargarle la lista de opciones.
//
// Cada vez que su objeto se PRENDE, este script:
//   - hace aparecer todo de a poco (fundido);
//   - marca la primera opcion y anima la que este elegida (la flecha de pincel);
//   - deja moverse con W/S o las flechas y aceptar con Enter;
//   - si tiene un slider de musica, lo pone en el volumen que tiene la musica de verdad.
//
// Quien prende y apaga el objeto es otro script (PauseMenu con Esc, MainMenuUI,
// PlayerDeath...). Este no pausa, no reanuda y no lee la tecla Esc.
//
// OJO: en los menus Time.timeScale vale 0 y Time.deltaTime tambien.
// Por eso todo aca usa Time.unscaledDeltaTime (tiempo real).
[DisallowMultipleComponent]
public class PauseMenuView : MonoBehaviour
{
    [Header("Referencias (las ponen los menus Green Mist)")]
    [Tooltip("El CanvasGroup de este mismo objeto. Con el se funde todo el menu de una.")]
    public CanvasGroup group;

    [Tooltip("Las opciones, de arriba hacia abajo.")]
    public PauseMenuItem[] items;

    [Tooltip("El slider de musica (puede quedar vacio). Al abrirse el menu se acomoda al volumen real de la musica.")]
    public Slider musicSlider;

    [Header("Aparicion")]
    [Tooltip("Segundos que tarda en aparecer el menu. 0 = aparece de golpe.")]
    [Range(0f, 3f)] public float fadeTime = 0.25f;

    [Header("Opcion elegida")]
    [Tooltip("Transparencia del texto de las opciones que NO estan elegidas. 1 = igual de blancas que la elegida.")]
    [Range(0.1f, 1f)] public float idleAlpha = 0.55f;

    [Tooltip("Segundos que tarda una opcion en marcarse o desmarcarse.")]
    [Range(0.01f, 0.5f)] public float highlightTime = 0.12f;

    [Tooltip("Cuanto se corre a la derecha el texto de la opcion elegida.")]
    [Range(0f, 40f)] public float nudge = 10f;

    [Tooltip("Desde cuantas unidades a la izquierda entra la flecha de pincel al elegir una opcion. 0 = aparece en su lugar.")]
    [Range(0f, 40f)] public float indicatorSlide = 14f;

    [Header("Teclado")]
    [Tooltip("Tildado: W/S o flechas para moverse y Enter para aceptar. Destildado: solo mouse.")]
    public bool keyboard = true;

    [Header("Interno")]
    [Tooltip("Version del estilo (colores, tamaños, flecha) que le puso el menu Green Mist a este panel. "
           + "No hace falta tocarlo. Si lo pones en 0, la proxima vez que corras el menu vuelve todo a los valores de fabrica.")]
    public int styleVersion;

    int elegida;        // indice de la opcion marcada
    float aparicion;    // 0 = invisible, 1 = visible del todo

    void OnEnable()
    {
        // Arranca invisible y aparece sola en Update.
        aparicion = fadeTime > 0f ? 0f : 1f;
        if (group != null) group.alpha = aparicion;

        // Siempre se abre con la primera opcion marcada.
        elegida = 0;
        if (items != null)
        {
            bool yaHayPrimera = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                if (!yaHayPrimera)
                {
                    elegida = i;        // la primera que exista (por si quedo un hueco vacio en la lista)
                    yaHayPrimera = true;
                }
                items[i].alApuntar = Elegir;   // para enterarnos cuando el mouse le pasa por encima
                items[i].Reiniciar(idleAlpha);
            }
        }

        SincronizarVolumen();
    }

    void Start()
    {
        // Si este menu ya esta prendido cuando arranca la escena (el menu principal),
        // OnEnable puede correr ANTES de que la musica exista. Aca ya existe seguro.
        SincronizarVolumen();
    }

    void Update()
    {
        // Tiempo real, con tope: si el juego pego un tiron no se saltea la animacion.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        // ---------- Fundido de entrada ----------
        if (aparicion < 1f)
        {
            aparicion = fadeTime > 0f ? Mathf.MoveTowards(aparicion, 1f, dt / fadeTime) : 1f;
            if (group != null) group.alpha = aparicion * aparicion * (3f - 2f * aparicion);
        }

        if (items == null || items.Length == 0) return;

        // ---------- Teclado ----------
        if (keyboard)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Mover(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Mover(1);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (elegida >= 0 && elegida < items.Length && items[elegida] != null)
                {
                    items[elegida].Activar();
                    // La opcion pudo haber cerrado el menu o recargado la escena:
                    // cortamos aca y no tocamos nada mas en este frame.
                    return;
                }
            }
        }

        // ---------- Animar las opciones ----------
        float paso = dt / Mathf.Max(highlightTime, 0.01f);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            items[i].Animar(i == elegida, paso, idleAlpha, nudge, indicatorSlide);
        }
    }

    // Mueve la marca una opcion para arriba (-1) o para abajo (+1), dando la vuelta en las puntas.
    void Mover(int direccion)
    {
        int cantidad = items.Length;
        // Probamos como mucho una vuelta entera, salteando huecos vacios de la lista.
        for (int intento = 0; intento < cantidad; intento++)
        {
            elegida = (elegida + direccion + cantidad) % cantidad;
            if (items[elegida] != null) return;
        }
    }

    // La llama cada opcion cuando el mouse le pasa por encima.
    void Elegir(PauseMenuItem item)
    {
        if (items == null) return;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == item)
            {
                elegida = i;
                return;
            }
        }
    }

    // ----- Musica -----

    // La llama el slider de musica (esta conectada en su "On Value Changed").
    // Usa MusicManager.Instance, que es el MusicManager que sigue vivo despues
    // de recargar la escena (Retry o Main Menu): una referencia arrastrada a
    // mano al MusicManager de la escena queda muerta despues de la recarga.
    public void SetMusicVolume(float volumen)
    {
        if (MusicManager.Instance != null) MusicManager.Instance.SetVolume(volumen);
    }

    // Pone el slider donde esta el volumen real, SIN disparar SetMusicVolume.
    void SincronizarVolumen()
    {
        if (musicSlider == null || MusicManager.Instance == null) return;

        // La misma busqueda que hace MusicManager para encontrar su AudioSource.
        AudioSource fuente = MusicManager.Instance.GetComponentInChildren<AudioSource>();
        if (fuente != null) musicSlider.SetValueWithoutNotify(fuente.volume);
    }
}
