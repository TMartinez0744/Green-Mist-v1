using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Una opcion de TEXTO de un menu (RESUME, MAIN MENU, QUIT, PLAY, RETRY...).
// Se llama "PauseMenuItem" porque nacio en la pausa, pero sirve igual para
// cualquier menu: no sabe nada de pausar.
//
// Es texto solo, sin caja de boton. Cuando esta elegida (el mouse le paso por
// encima o llegaste con W/S) el texto se aclara, se corre un poquito a la
// derecha y a su izquierda aparece la flecha de pincel: entra deslizandose
// hacia el texto mientras se "pinta" de la cola a la punta.
//
// Este script no decide nada: PauseMenuView le dice en cada frame si esta
// elegida o no, y el solo se encarga de animarse y de avisar los clics.
// Lo que hace cada opcion al hacerle clic esta en "On Click" (lo conectan los
// menus Green Mist, igual que si lo arrastraras a mano).
[DisallowMultipleComponent]
public class PauseMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Referencias (las ponen los menus Green Mist)")]
    [Tooltip("El texto de la opcion.")]
    public TextMeshProUGUI label;

    // FormerlySerializedAs: este campo antes se llamaba "line" (era una rayita).
    // Con esto Unity no pierde la referencia que ya estaba guardada en la escena.
    [Tooltip("La flecha de pincel que aparece a la izquierda del texto cuando la opcion esta elegida. "
           + "En la escena se ve entera; jugando la esconde y la muestra este script.")]
    [FormerlySerializedAs("line")]
    public Image indicator;

    [Tooltip("Que pasa al hacer clic (o Enter) en esta opcion.")]
    public UnityEvent onClick = new UnityEvent();

    // PauseMenuView se anota aca para enterarse cuando el mouse entra a esta opcion.
    [System.NonSerialized] public System.Action<PauseMenuItem> alApuntar;

    bool preparado;
    Vector2 posicionTexto;     // donde esta el texto cuando NO esta elegida
    Vector2 posicionFlecha;    // donde queda la flecha cuando SI esta elegida (como esta en la escena)
    Color colorFlecha;         // el color de la flecha tal como esta en la escena
    bool flechaSePinta;        // la imagen es de tipo Filled: se puede mostrar "de a poco"

    float mezcla;              // 0 = sin elegir, 1 = elegida. Es lo que se anima.
    float mezclaAplicada = -1f;

    // Guarda como estan el texto y la flecha en la escena, para animar a partir de ahi.
    // Se llama sola la primera vez que hace falta.
    void Preparar()
    {
        if (preparado) return;
        preparado = true;

        if (label != null) posicionTexto = label.rectTransform.anchoredPosition;
        if (indicator != null)
        {
            posicionFlecha = indicator.rectTransform.anchoredPosition;
            colorFlecha = indicator.color;
            // Filled solo funciona si la imagen tiene un dibujo (sprite).
            flechaSePinta = indicator.type == Image.Type.Filled && indicator.sprite != null;
        }
    }

    // La deja de golpe en "sin elegir" (cuando se abre el menu).
    public void Reiniciar(float alphaReposo)
    {
        Preparar();
        mezcla = 0f;
        mezclaAplicada = -1f;   // obliga a aplicar aunque ya estuviera en 0
        Aplicar(alphaReposo, 0f, 0f);
    }

    // La llama PauseMenuView en cada frame.
    //   elegida        si es la opcion marcada en este momento
    //   paso           cuanto avanza la animacion en este frame (0 a 1)
    //   alphaReposo    transparencia del texto cuando NO esta elegida
    //   empujon        cuantas unidades se corre el texto a la derecha al elegirla
    //   deslizamiento  desde cuantas unidades a la izquierda entra la flecha
    public void Animar(bool elegida, float paso, float alphaReposo, float empujon, float deslizamiento)
    {
        Preparar();
        mezcla = Mathf.MoveTowards(mezcla, elegida ? 1f : 0f, paso);
        Aplicar(alphaReposo, empujon, deslizamiento);
    }

    void Aplicar(float alphaReposo, float empujon, float deslizamiento)
    {
        // Si no cambio nada desde el frame anterior no tocamos la UI:
        // cada cambio obliga a Unity a rearmar el texto.
        if (Mathf.Approximately(mezcla, mezclaAplicada)) return;
        mezclaAplicada = mezcla;

        // Curva suave: arranca y frena despacio en vez de moverse parejo.
        float suave = mezcla * mezcla * (3f - 2f * mezcla);

        if (label != null)
        {
            label.alpha = Mathf.Lerp(alphaReposo, 1f, suave);
            label.rectTransform.anchoredPosition = posicionTexto + new Vector2(empujon * suave, 0f);
        }

        if (indicator != null)
        {
            // Entra desde la izquierda hasta su lugar...
            indicator.rectTransform.anchoredPosition = posicionFlecha - new Vector2(deslizamiento * (1f - suave), 0f);

            // ...mientras se pinta de la cola a la punta, como una pincelada...
            if (flechaSePinta) indicator.fillAmount = suave;

            // ...y aparece de a poco.
            Color c = colorFlecha;
            c.a = colorFlecha.a * suave;
            indicator.color = c;
        }
    }

    // Hace lo que tenga conectado en "On Click".
    public void Activar()
    {
        onClick.Invoke();
    }

    // ----- Mouse (lo llama el EventSystem de Unity) -----

    public void OnPointerEnter(PointerEventData datos)
    {
        if (alApuntar != null) alApuntar(this);
    }

    public void OnPointerClick(PointerEventData datos)
    {
        if (datos.button != PointerEventData.InputButton.Left) return;
        Activar();
    }
}
