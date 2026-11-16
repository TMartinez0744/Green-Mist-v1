using UnityEngine;
using UnityEngine.UI;

// La barra de vida de Onji, abajo a la izquierda. Va en el objeto "GM_UI_Vida".
// (Se llama "Thin" porque la primera version era una linea finita; ahora es
// una barra de la familia Souls / Sekiro / Assassin's Creed.)
//
// Como esta armada, de atras para adelante:
//   borde    un marco fino color hueso, casi transparente
//   fondo    la pista oscura
//   estela   hueso palido: lo que se acaba de perder
//   relleno  carmesi: la vida que queda
//
// HUD minimo:
//   - Con la vida llena y sin que pase nada, NO se ve.
//   - Aparece cuando la vida cambia y se queda mientras falte vida.
//   - Si la vida vuelve a estar llena, se va sola a los pocos segundos.
//   - En el menu principal, en la pausa y en el game over no se ve.
//
// Al recibir un golpe el relleno baja de golpe y atras queda la "estela" clara
// con lo que se perdio, que espera un momento y despues se vacia hasta
// alcanzarlo (el efecto de los Souls). Con poca vida el relleno late lento y
// el borde se enciende en rojo al mismo ritmo.
//
// La vida se LEE en cada frame de Health.Current (es un numero, no cuesta
// nada). No hace falta engancharse a ningun evento ni importa el orden en
// que arrancan los scripts.
//
// Todos los tiempos son en tiempo REAL (unscaledDeltaTime): el juego frena el
// tiempo en los menus y un instante en cada golpe, y con el tiempo del juego
// la barra se trabaria.
[DisallowMultipleComponent]
public class ThinHealthBar : MonoBehaviour
{
    [Header("Referencias (las pone el menu Green Mist > 8. Interfaz)")]
    [Tooltip("La vida de Onji (el componente Health de PlayerRoot). Vacio = busca solo al objeto con tag Player.")]
    public Health target;

    [Tooltip("El CanvasGroup de este mismo objeto. Con el se funde toda la barra.")]
    public CanvasGroup group;

    [Tooltip("El relleno carmesi: la vida que queda.")]
    public Image fill;

    [Tooltip("La estela clara que queda un momento despues de un golpe.")]
    public Image trail;

    [Tooltip("El marco fino alrededor de la barra. Con poca vida se tiñe de rojo. Puede quedar vacio.")]
    public Image border;

    [Tooltip("Panel del menu principal. Mientras esta a la vista, la barra se oculta.")]
    public GameObject mainMenuPanel;

    [Tooltip("Panel de game over. Mientras esta a la vista, la barra se oculta.")]
    public GameObject gameOverPanel;

    [Tooltip("El menu de pausa. Mientras esta a la vista, la barra se oculta.")]
    public GameObject pausePanel;

    [Header("Aparecer y desaparecer")]
    [Tooltip("Segundos que la barra se queda a la vista con la vida LLENA antes de irse.")]
    [Range(0f, 10f)] public float hideDelay = 3f;

    [Tooltip("Segundos que tarda en aparecer.")]
    [Range(0.05f, 2f)] public float fadeInTime = 0.25f;

    [Tooltip("Segundos que tarda en desaparecer.")]
    [Range(0.05f, 3f)] public float fadeOutTime = 0.8f;

    [Header("Estela de daño")]
    [Tooltip("Segundos que la estela se queda quieta despues de un golpe.")]
    [Range(0f, 2f)] public float trailDelay = 0.45f;

    [Tooltip("Que tan rapido se vacia la estela. 0.5 = media barra por segundo.")]
    [Range(0.05f, 3f)] public float trailSpeed = 0.5f;

    [Header("Colores")]
    [Tooltip("Color del relleno con vida normal.")]
    public Color normalColor = new Color(0.58f, 0.14f, 0.13f, 1f);

    [Tooltip("Color del relleno con poca vida (un rojo un poco mas vivo).")]
    public Color lowColor = new Color(0.74f, 0.17f, 0.14f, 1f);

    [Tooltip("Color al que se tiñe el borde con poca vida.")]
    public Color lowBorderColor = new Color(0.80f, 0.24f, 0.20f, 0.85f);

    [Header("Poca vida")]
    [Tooltip("Por debajo de esta parte de la vida (0.25 = un cuarto) el relleno late y el borde se pone rojo.")]
    [Range(0f, 1f)] public float lowThreshold = 0.25f;

    [Tooltip("Latidos por segundo con poca vida. 0.5 = uno cada dos segundos.")]
    [Range(0f, 3f)] public float breatheSpeed = 0.5f;

    [Tooltip("Cuanto se apagan el relleno y el borde en cada latido. 0 = no late.")]
    [Range(0f, 1f)] public float breatheAmount = 0.4f;

    [Header("Interno")]
    [Tooltip("Version del estilo (colores, tamaños, borde) que le puso el menu Green Mist a esta barra. "
           + "No hace falta tocarlo. Si lo pones en 0, la proxima vez que corras el menu vuelve todo a los valores de fabrica.")]
    public int styleVersion;

    // Al morir la barra vacia se queda este rato a la vista y despues se va.
    const float TiempoVisibleAlMorir = 1.2f;

    // Cuando la tapa otra pantalla (pausa, menu) se va rapido, no con fadeOutTime.
    const float FundidoRapido = 0.15f;

    bool listo;              // ya leimos la vida por primera vez
    int vidaAnterior;        // la vida del frame anterior, para detectar cambios
    float parteRelleno;      // 0..1: lo que muestra el relleno
    float parteEstela;       // 0..1: lo que muestra la estela
    float esperaEstela;      // segundos que le faltan a la estela para empezar a vaciarse
    float restoVisible;      // segundos que le quedan a la vista con la vida llena
    float tiempoMuerto;      // segundos desde que la vida llego a 0
    float alpha;             // transparencia actual de toda la barra
    float mezclaPocaVida;    // 0 = vida normal, 1 = poca vida
    float reloj;             // tiempo real acumulado, para el latido
    Color colorBorde;        // el color del borde tal como esta en la escena

    void Awake()
    {
        if (target == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) target = jugador.GetComponent<Health>();
        }

        if (border != null) colorBorde = border.color;
    }

    void OnEnable()
    {
        // Arranca siempre invisible: la escena abre en el menu principal.
        listo = false;
        alpha = 0f;
        if (group != null) group.alpha = 0f;
    }

    void Update()
    {
        // Tiempo real, con tope: despues de un tiron (cargar la escena) no pega un salto.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        reloj += dt;

        if (target == null)
        {
            Fundir(0f, dt);
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
            tiempoMuerto = 0f;
            mezclaPocaVida = 0f;
            AplicarBarras();
        }

        // ---------- La vida cambio? ----------
        if (vida != vidaAnterior)
        {
            // Si bajo, la estela espera un momento antes de vaciarse.
            if (vida < vidaAnterior) esperaEstela = trailDelay;

            restoVisible = hideDelay;
            vidaAnterior = vida;
        }

        // ---------- Relleno ----------
        // Al perder vida baja de golpe (el golpe se tiene que leer al instante).
        // Si algun dia hay curacion, sube suave.
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
        float objetivo;
        bool tapada = TapadaPorOtraPantalla();
        if (tapada)
        {
            objetivo = 0f;
        }
        else if (vida <= 0)
        {
            // Muerto: se ve vaciarse y despues se va, antes del fundido a negro.
            tiempoMuerto += dt;
            objetivo = tiempoMuerto < TiempoVisibleAlMorir ? 1f : 0f;
        }
        else if (vida < maximo)
        {
            objetivo = 1f;
        }
        else
        {
            // Vida llena: solo se queda un rato despues del ultimo cambio.
            if (restoVisible > 0f) restoVisible -= dt;
            objetivo = restoVisible > 0f ? 1f : 0f;
        }

        if (tapada && objetivo < alpha) alpha = Mathf.MoveTowards(alpha, 0f, dt / FundidoRapido);
        else Fundir(objetivo, dt);
        if (group != null) group.alpha = alpha;

        // Invisible del todo: no hace falta seguir pintando.
        if (alpha <= 0f) return;

        AplicarBarras();
        AplicarColor(parte, vida > 0, dt);
    }

    // Lleva la transparencia de la barra hacia "objetivo" (0 o 1) de a poco.
    void Fundir(float objetivo, float dt)
    {
        float tiempo = objetivo > alpha ? fadeInTime : fadeOutTime;
        alpha = Mathf.MoveTowards(alpha, objetivo, dt / Mathf.Max(tiempo, 0.01f));
        if (group != null) group.alpha = alpha;
    }

    bool TapadaPorOtraPantalla()
    {
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy) return true;
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return true;
        if (pausePanel != null && pausePanel.activeInHierarchy) return true;
        return false;
    }

    // El largo de cada barra se maneja con las anclas: 0 = borde izquierdo, 1 = borde derecho.
    // (Es lo mismo que hace por dentro el Slider de Unity.)
    //   relleno  va de 0 hasta la vida que queda;
    //   estela   ocupa SOLO el tramo perdido: empieza donde termina el relleno.
    // La estela NO va por debajo del relleno a proposito: cuando el relleno se
    // pone un poco transparente (el latido de poca vida, o toda la barra
    // apareciendo y desapareciendo) lo que tiene que asomar atras es la pista
    // oscura. Si atras estuviera la estela clara, la vida se veria rosada.
    void AplicarBarras()
    {
        float finRelleno = parteRelleno;
        if (fill != null)
        {
            PonerLargo(fill.rectTransform, parteRelleno);
            finRelleno = fill.rectTransform.anchorMax.x;   // el numero exacto: asi no queda ni una rendija entre los dos
        }
        if (trail != null) PonerTramo(trail.rectTransform, finRelleno, parteEstela);
    }

    static void PonerLargo(RectTransform barra, float parte)
    {
        Vector2 ancla = barra.anchorMax;
        if (Mathf.Approximately(ancla.x, parte)) return;   // no cambio: no tocamos la UI
        ancla.x = parte;
        barra.anchorMax = ancla;
    }

    // Hace que la barra ocupe solo el tramo que va de "desde" a "hasta" (los dos de 0 a 1).
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

    // Colores del relleno y del borde. Con vida normal no cambian nunca.
    // Con poca vida: como el relleno YA es rojo, "ponerse rojo" no avisaria nada.
    // El aviso es otro: el relleno late (se apaga y se prende) y el marco, que
    // normalmente es hueso casi invisible, se enciende en rojo al mismo ritmo.
    void AplicarColor(float parte, bool vivo, float dt)
    {
        bool pocaVida = vivo && parte <= lowThreshold;
        mezclaPocaVida = Mathf.MoveTowards(mezclaPocaVida, pocaVida ? 1f : 0f, dt * 3f);

        // El latido: 1 = prendido del todo, baja hasta (1 - breatheAmount) y vuelve.
        float latido = 1f;
        if (mezclaPocaVida > 0f && breatheSpeed > 0f)
        {
            float onda = 0.5f - 0.5f * Mathf.Cos(reloj * breatheSpeed * 2f * Mathf.PI);   // va de 0 a 1 y vuelve
            latido = 1f - breatheAmount * onda * mezclaPocaVida;
        }

        if (fill != null)
        {
            Color c = Color.Lerp(normalColor, lowColor, mezclaPocaVida);
            c.a *= latido;
            if (fill.color != c) fill.color = c;
        }

        if (border != null)
        {
            Color b = Color.Lerp(colorBorde, lowBorderColor, mezclaPocaVida * latido);
            if (border.color != b) border.color = b;
        }
    }
}
