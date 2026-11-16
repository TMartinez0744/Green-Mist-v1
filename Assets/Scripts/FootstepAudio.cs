using UnityEngine;

// Pasos de Onji.
//
// Hay DOS formas de decidir cuando suena un paso:
//
//   1) POR ANIMACION (la normal). Mira por donde va el ciclo de la animacion de
//      caminar y suena justo cuando un pie toca el suelo. Un ciclo completo son
//      dos pasos (pie izquierdo y pie derecho). Como el sonido sale de la misma
//      animacion que se ve, nunca quedan a destiempo: si la animacion va mas
//      lenta o mas rapida, los pasos la siguen solos.
//
//   2) POR DISTANCIA (plan B). Suena un paso cada vez que Onji RECORRE una
//      zancada. Se usa sola si no hay Animator, si ninguno de los estados de la
//      lista existe, o si destildas "Sync With Animation". Tambien entra un
//      rato cuando Onji se mueve con una animacion que NO es de caminar (por
//      ejemplo atacando mientras avanza): ahi no hay pies que seguir, pero si
//      no sonara nada pareceria que flota.
//
// En las dos formas el paso solo suena si Onji toca el piso y se mueve DE VERDAD.
// Eso sale de controller.velocity, que es lo que el CharacterController se movio:
// empujando contra una pared vale 0 y no suena nada (aunque la animacion camine).
// Al caer de un salto se fuerza un paso mas fuerte (el aterrizaje).
//
// Usa un AudioSource PROPIO (el hijo "GM_Audio_Pasos" de PlayerRoot). El
// AudioSource que ya tiene PlayerRoot es del whoosh del baston, que corta
// cualquier otro sonido que se le ponga.
//
// Mientras no haya clips cargados no hace nada (ni errores).
// A proposito NO es [ExecuteAlways]: en modo edicion no toca nada.
[DisallowMultipleComponent]
public class FootstepAudio : MonoBehaviour
{
    [Header("Referencias (las pone el menu Green Mist > 7. Sonido)")]
    [Tooltip("El CharacterController de Onji. De aca sale la velocidad real.")]
    public CharacterController controller;

    [Tooltip("El script de movimiento. Solo se le LEEN walkSpeed, runSpeed y la tecla de agacharse.")]
    public ThirdPersonMovement movement;

    [Tooltip("El Animator de Onji. Se le lee por donde va la animacion de caminar y el bool de agachado.")]
    public Animator animator;

    [Tooltip("AudioSource propio de los pasos (hijo 'GM_Audio_Pasos'). NO usar el de PlayerRoot: ese es del whoosh.")]
    public AudioSource source;

    [Tooltip("Panel del menu principal. Mientras esta a la vista no hay pasos.")]
    public GameObject mainMenuPanel;

    [Tooltip("Panel de game over. Mientras esta a la vista no hay pasos.")]
    public GameObject gameOverPanel;

    [Header("Clips")]
    [Tooltip("Sonidos del paso. Con 2 suenan alternados (uno por pie), que es lo que mejor queda: con muchos se nota un patron raro. Nunca se repite el mismo dos veces seguidas. Vacio = no suena nada. La lista la arma el menu Green Mist > 7. Sonido.")]
    public AudioClip[] clips;

    [Header("Sincronizar con la animacion")]
    [Tooltip("Tildado: el paso suena cuando el pie de la animacion toca el suelo. Destildado: suena por distancia recorrida (las zancadas de mas abajo).")]
    public bool syncWithAnimation = true;

    [Tooltip("AJUSTE A OIDO. Si el sonido llega ANTES de que el pie toque el suelo, subilo; si llega TARDE, bajalo. Se mide en partes del ciclo (un ciclo = dos pasos): proba de a 0.02. Caminando, 0.05 es mas o menos 0.06 segundos.")]
    [Range(-0.25f, 0.25f)] public float phaseOffset = 0f;

    [Tooltip("Nombres de los estados del Animator (capa 0) en los que Onji camina o corre. Tienen que ser animaciones en loop. Se escriben igual que en la ventana Animator.")]
    public string[] walkStates = { "Locomotion", "CrouchWalk" };

    [Tooltip("En que punto del ciclo (0 a 1) apoya el PRIMER pie. Medido en la animacion 'Standard Walk': 0.25.")]
    [Range(0f, 1f)] public float stepPhaseA = 0.25f;

    [Tooltip("En que punto del ciclo (0 a 1) apoya el OTRO pie. Normalmente es el primero + 0.5.")]
    [Range(0f, 1f)] public float stepPhaseB = 0.75f;

    [Tooltip("Solo para cuando CORRE: en la animacion de correr el pie apoya un poco mas tarde en el ciclo que caminando. Si corriendo el sonido llega antes que el pie, subilo; si llega tarde, bajalo. 0 = igual que caminando.")]
    [Range(-0.25f, 0.25f)] public float runPhaseShift = 0.09f;

    [Tooltip("Nombre del float de velocidad en el Animator (el que mezcla caminar y correr). Se usa para saber cuanto de 'correr' hay en la mezcla.")]
    public string speedParameter = "Speed";

    [Header("Zancada (plan B): metros entre un paso y el siguiente")]
    [Tooltip("Caminando. Solo se usa cuando el paso NO sale de la animacion (plan B, o mientras ataca moviendose). 1.46 m a 2.5 m/s da el mismo ritmo que la animacion (un paso cada 0.58 s).")]
    [Range(0.3f, 3f)] public float walkStride = 1.46f;

    [Tooltip("Corriendo (Shift). Solo plan B. 1.75 m a 5 m/s = un paso cada 0.35 s, como la animacion de correr.")]
    [Range(0.3f, 3f)] public float runStride = 1.75f;

    [Tooltip("Agachado. Solo plan B. 0.78 m a 1.5 m/s = un paso cada 0.52 s, como la animacion de caminar agachado.")]
    [Range(0.3f, 3f)] public float crouchStride = 0.78f;

    // Por que numeros tan chicos: Unity sube al maximo el volumen de los clips de
    // pasos al importarlos (son mono con "Normalize"), asi que con 0.5 ya sonaban
    // mas fuerte que la musica. Los pasos tienen que ser un detalle, no tapar nada.
    [Header("Volumen (bajito a proposito: es un detalle de fondo)")]
    [Tooltip("Volumen de cada paso caminando. Si los pasos quedan fuertes o no se oyen, este es el primero que hay que mover (de a 0.03).")]
    [Range(0f, 1f)] public float walkVolume = 0.18f;

    [Tooltip("Volumen de cada paso corriendo. Un poco mas fuerte que caminando: pisa con mas peso.")]
    [Range(0f, 1f)] public float runVolume = 0.26f;

    [Tooltip("Agachado suena mas bajo: va en sigilo.")]
    [Range(0f, 1f)] public float crouchVolume = 0.09f;

    [Tooltip("Volumen del paso al caer de un salto. Es el mas fuerte de los cuatro.")]
    [Range(0f, 1f)] public float landVolume = 0.32f;

    [Header("Variacion (para que no suene a metralleta)")]
    [Tooltip("Cuanto cambia el tono entre paso y paso. 0.07 = hasta un 7% mas agudo o mas grave.")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.07f;

    [Tooltip("Cuanto puede bajar el volumen de un paso al azar. 0.2 = entre 80% y 100%.")]
    [Range(0f, 0.5f)] public float volumeVariation = 0.2f;

    [Tooltip("Tono del aterrizaje. Menos de 1 = mas grave, suena mas pesado.")]
    [Range(0.5f, 1.2f)] public float landPitch = 0.88f;

    [Header("Ajustes finos")]
    [Tooltip("Por debajo de esta velocidad (m/s) se lo considera quieto y no suenan pasos.")]
    [Range(0f, 2f)] public float minSpeed = 0.4f;

    [Tooltip("Solo plan B (distancia). Al arrancar a caminar, que parte de la zancada ya viene 'hecha'. 0.6 = el primer paso suena enseguida.")]
    [Range(0f, 0.95f)] public float firstStepHeadStart = 0.6f;

    [Tooltip("Segundos minimos en el aire para que al tocar el piso suene el aterrizaje. Evita pasos falsos en desniveles.")]
    [Range(0.05f, 1f)] public float minAirTime = 0.2f;

    [Tooltip("Segundos de silencio al empezar a jugar (despues del menu). Evita un paso falso cuando el menu se cierra.")]
    [Range(0f, 2f)] public float silenceAfterMenu = 0.5f;

    [Tooltip("Nombre del bool de agachado en el Animator.")]
    public string crouchParameter = "IsCrouching";

    // NO se ve en el Inspector y el juego no lo usa. Es una marca que deja el menu
    // "Green Mist > 7. Sonido" para acordarse de que YA bajo los volumenes de los
    // pasos una vez. Sin esta marca, cada vez que corrieras el menu te pisaria los
    // volumenes que hayas ajustado a mano. (0 = todavia no los bajo.)
    [HideInInspector] public int tuningVersion = 0;

    // Si queda sin piso menos que esto (un escalon, una bajada), los pasos siguen como si nada.
    const float GraciaAire = 0.1f;

    // Nunca dos pasos mas pegados que esto (segundos). Es un seguro contra pasos
    // dobles: por ejemplo el aterrizaje y, enseguida, un paso de la animacion.
    const float SeparacionMinima = 0.12f;

    // El juego pone timeScale en 0 en la pausa y tambien 0.06 s en cada golpe.
    // Si estuvo congelado mas que esto, fue una pausa de verdad.
    const float PausaMinima = 0.25f;

    // Si el ciclo de la animacion retrocede menos que esto no se lo toma como "la
    // animacion volvio a empezar" (ver LeerCicloDeAnimacion). Es mas grande que
    // el maximo de los sliders de ajuste (0.25) a proposito.
    const float RetrocesoTolerado = 0.3f;

    // Si el ciclo avanza un ciclo entero o mas en UN frame, no es caminar: fue un salto de la animacion.
    const float SaltoMaximo = 1f;

    float distancia;                 // plan B: metros recorridos desde el ultimo paso
    float tiempoEnAire;              // segundos seguidos sin tocar el piso
    float tiempoJugando;             // segundos de juego desde que se cerro el menu
    float tiempoDesdePaso = 999f;    // segundos de juego desde el ultimo paso que sono
    float tiempoCongelado;           // segundos reales seguidos con el juego frenado
    int ultimoClip = -1;             // para no repetir el mismo clip dos veces seguidas

    // --- Seguimiento de la animacion ---
    bool siguiendoEstado;   // true si ya sabemos por donde iba el ciclo en el frame anterior
    int estadoSeguido;      // que estado del Animator estamos siguiendo (su "hash", un numero)
    float cicloAnterior;    // por donde iba el ciclo en el frame anterior

    // --- Datos del Animator que se averiguan UNA vez (ver PrepararAnimator) ---
    bool animPreparado;
    bool hayEstadosDeCaminar;       // al menos un nombre de walkStates existe en el Animator
    bool animatorTieneAgachado;
    bool animatorTieneVelocidad;
    bool yaAviseEstados;
    int hashAgachado;
    int hashVelocidad;
    int[] hashesCaminar;            // un numero por cada nombre de walkStates (0 = ese nombre no existe)

    void OnEnable()
    {
        // Al prender el componente no sabemos por donde iba la animacion: se vuelve a leer.
        siguiendoEstado = false;
        tiempoCongelado = 0f;
    }

    // Unity llama a esto cuando cambias un valor en el Inspector (tambien en Play).
    // Asi, si editas la lista de estados, se vuelve a revisar contra el Animator.
    void OnValidate()
    {
        animPreparado = false;
        siguiendoEstado = false;
        yaAviseEstados = false;
    }

    // LateUpdate y no Update: asi corre DESPUES de que ThirdPersonMovement movio
    // al personaje y de que el Animator avanzo la animacion en este frame.
    void LateUpdate()
    {
        if (controller == null || source == null) return;

        // Menu principal, game over, o Onji muerto (PlayerDeath apaga el controller):
        // nada de pasos, y se olvida lo acumulado.
        if (EnMenu() || !controller.enabled)
        {
            Reiniciar();
            return;
        }

        // Con el juego congelado (menu, pausa, hit stop) deltaTime vale 0. En esos
        // frames el "toca piso" del controller puede dar falso sin que Onji haya
        // saltado, asi que directamente no los miramos. Solo contamos cuanto dura.
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            tiempoCongelado += Time.unscaledDeltaTime;
            return;
        }

        // Volviendo de una pausa larga: la animacion se vuelve a leer desde cero,
        // sin sonar, por si se movio mientras tanto. (El hit stop es tan corto que
        // no hace falta: la animacion quedo quieta en el mismo lugar.)
        if (tiempoCongelado > PausaMinima) siguiendoEstado = false;
        tiempoCongelado = 0f;

        // Un respiro al empezar a jugar: Onji se esta acomodando en el piso.
        tiempoJugando += dt;
        if (tiempoJugando < silenceAfterMenu)
        {
            distancia = 0f;
            tiempoEnAire = 0f;
            siguiendoEstado = false;
            return;
        }

        tiempoDesdePaso += dt;

        if (!animPreparado) PrepararAnimator();

        // ---------- Velocidad real ----------
        // Solo XZ: subir o caer no cuenta como caminar.
        Vector3 v = controller.velocity;
        float velocidad = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        if (float.IsNaN(velocidad) || float.IsInfinity(velocidad)) velocidad = 0f;

        // ---------- Animacion ----------
        // Se lee en TODOS los frames (tambien quieto o en el aire) para saber
        // siempre por donde va el ciclo. Si solo la mirasemos al caminar, al
        // arrancar creeria que cruzo varios pasos juntos.
        bool porAnimacion = syncWithAnimation && AnimatorListo();
        bool pieApoyo = false;
        bool animacionCamina = false;   // true si la animacion de ahora es una de las de caminar
        if (porAnimacion) pieApoyo = LeerCicloDeAnimacion(velocidad, out animacionCamina);
        else siguiendoEstado = false;

        // ---------- Piso y aterrizaje ----------
        if (controller.isGrounded)
        {
            if (tiempoEnAire >= minAirTime)
            {
                // Venia en el aire de verdad y acaba de tocar: paso forzado.
                Sonar(landVolume, landPitch);
                distancia = 0f;
                pieApoyo = false; // en este frame ya sono el aterrizaje
            }
            tiempoEnAire = 0f;
        }
        else
        {
            tiempoEnAire += dt;
            if (tiempoEnAire > GraciaAire) return; // en el aire no hay pasos
        }

        // Elegimos zancada y volumen segun como se esta moviendo.
        float zancada = walkStride;
        float volumen = walkVolume;
        if (EstaAgachado())
        {
            zancada = crouchStride;
            volumen = crouchVolume;
        }
        else if (movement != null && velocidad > (movement.walkSpeed + movement.runSpeed) * 0.5f)
        {
            // "Corre" si va mas rapido que el punto medio entre caminar y correr.
            // Se mira la velocidad real, no la tecla Shift.
            zancada = runStride;
            volumen = runVolume;
        }

        if (velocidad < minSpeed)
        {
            // Quieto (o empujando una pared): no suena nada, aunque la animacion
            // siga moviendo los pies. Para el plan B dejamos la cuenta adelantada,
            // asi el primer paso suena enseguida al arrancar.
            distancia = zancada * firstStepHeadStart;
            return;
        }

        // ---------- Paso por animacion ----------
        if (porAnimacion && animacionCamina)
        {
            // Igual llevamos la cuenta de los metros desde el ultimo paso: si la
            // animacion deja de ser de caminar (un ataque), el plan B sigue desde
            // ahi con el mismo ritmo, sin un paso pegado ni un hueco largo.
            distancia += velocidad * dt;

            if (pieApoyo && tiempoDesdePaso >= SeparacionMinima)
            {
                Sonar(volumen, 1f);
                distancia = 0f;
            }
            return;
        }

        // ---------- Paso por distancia (plan B) ----------
        // Llega aca si no se sincroniza con la animacion, o si Onji se mueve con
        // una animacion que no es de caminar (atacando mientras avanza).
        distancia += velocidad * dt;
        if (distancia >= zancada)
        {
            distancia -= zancada;
            if (distancia > zancada) distancia = 0f; // por si hubo un tiron de frames
            Sonar(volumen, 1f);
        }
    }

    // Devuelve true si en ESTE frame la animacion de caminar paso por el punto
    // donde un pie toca el suelo.
    //
    // El Animator da "normalizedTime": cuantas vueltas lleva dada la animacion
    // (0.25 = un cuarto de la primera vuelta, 3.75 = tres vueltas y tres cuartos).
    // Comparamos el valor de este frame con el del frame anterior: si entre los
    // dos quedo una de las dos fases de paso, un pie acaba de apoyar.
    // En el estado "Locomotion" (mezcla de quieto, caminar y correr) ese numero
    // ya sigue el ritmo de la mezcla, y tambien la velocidad de la animacion.
    //
    // 'esDeCaminar' avisa si la animacion de ahora es una de las de caminar. Si
    // no lo es, aca no hay pies que seguir y LateUpdate usa el plan B.
    bool LeerCicloDeAnimacion(float velocidad, out bool esDeCaminar)
    {
        // Durante una transicion miramos el estado al que se esta YENDO: es el
        // que va ganando. Si va hacia algo que no es caminar (ataque, salto,
        // agachado quieto), la animacion no marca pasos.
        AnimatorStateInfo info;
        if (animator.IsInTransition(0)) info = animator.GetNextAnimatorStateInfo(0);
        else info = animator.GetCurrentAnimatorStateInfo(0);

        esDeCaminar = EsEstadoDeCaminar(info.shortNameHash);
        if (!esDeCaminar)
        {
            siguiendoEstado = false;
            return false;
        }

        // Cuanto de "correr" hay en la mezcla (0 = caminando, 1 = corriendo).
        float corrida = 0f;
        if (movement != null && runPhaseShift != 0f)
        {
            float velocidadAnim = animatorTieneVelocidad ? animator.GetFloat(hashVelocidad) : velocidad;
            corrida = Mathf.InverseLerp(movement.walkSpeed, movement.runSpeed, velocidadAnim);
        }

        // El ciclo ya corrido por los ajustes: subir el ajuste = el paso suena mas tarde.
        float ciclo = info.normalizedTime - phaseOffset - runPhaseShift * corrida;
        if (float.IsNaN(ciclo) || float.IsInfinity(ciclo))
        {
            siguiendoEstado = false;
            return false;
        }

        // Estado nuevo (o primera lectura): solo anotamos por donde va. NO suena
        // nada, asi no hay una rafaga de pasos al entrar a caminar.
        if (!siguiendoEstado || info.shortNameHash != estadoSeguido)
        {
            siguiendoEstado = true;
            estadoSeguido = info.shortNameHash;
            cicloAnterior = ciclo;
            return false;
        }

        float avance = ciclo - cicloAnterior;

        if (avance <= 0f)
        {
            // No avanzo. Si retrocedio MUCHO es que la animacion volvio a empezar:
            // anotamos el punto nuevo, sin sonar. Si retrocedio poco (por ejemplo
            // porque moviste un slider de ajuste, o el ajuste de correr cambio mas
            // rapido que el ciclo), dejamos la marca donde estaba: hasta que el
            // ciclo no la pase de nuevo no cuenta nada, y asi un mismo paso nunca
            // suena dos veces.
            if (avance < -RetrocesoTolerado) cicloAnterior = ciclo;
            return false;
        }

        if (avance >= SaltoMaximo)
        {
            // Salto enorme en un solo frame: no es caminar. Anotamos y seguimos.
            cicloAnterior = ciclo;
            return false;
        }

        // Floor redondea hacia abajo. "ciclo - fase" pasa por un numero entero
        // justo cuando el ciclo cruza esa fase, y ahi el redondeo sube en 1.
        // Aunque en un frame largo se crucen las dos fases, suena UN solo paso.
        bool cruzo = Mathf.Floor(ciclo - stepPhaseA) > Mathf.Floor(cicloAnterior - stepPhaseA)
                  || Mathf.Floor(ciclo - stepPhaseB) > Mathf.Floor(cicloAnterior - stepPhaseB);

        cicloAnterior = ciclo;
        return cruzo;
    }

    // True si el Animator esta en condiciones de leerle la animacion de caminar.
    bool AnimatorListo()
    {
        if (animator == null || !animator.isActiveAndEnabled) return false;
        return animPreparado && hayEstadosDeCaminar;
    }

    // Averigua UNA vez lo que hace falta saber del Animator: si tiene los
    // parametros y si existen los estados de la lista. Preguntar por un
    // parametro que no existe tira un warning en cada frame, por eso se revisa antes.
    void PrepararAnimator()
    {
        hayEstadosDeCaminar = false;
        animatorTieneAgachado = false;
        animatorTieneVelocidad = false;

        if (animator == null)
        {
            animPreparado = true; // sin Animator no hay nada mas que averiguar
            return;
        }

        // Todavia no arranco (o esta apagado): probamos de nuevo en el proximo frame.
        if (!animator.isActiveAndEnabled || !animator.isInitialized) return;
        if (animator.runtimeAnimatorController == null || animator.layerCount == 0)
        {
            animPreparado = true; // Animator sin controller: plan B
            return;
        }

        animPreparado = true;

        hashAgachado = Animator.StringToHash(crouchParameter);
        hashVelocidad = Animator.StringToHash(speedParameter);
        AnimatorControllerParameter[] parametros = animator.parameters;
        for (int i = 0; i < parametros.Length; i++)
        {
            if (parametros[i].name == crouchParameter && parametros[i].type == AnimatorControllerParameterType.Bool)
                animatorTieneAgachado = true;
            if (parametros[i].name == speedParameter && parametros[i].type == AnimatorControllerParameterType.Float)
                animatorTieneVelocidad = true;
        }

        // Pasamos cada nombre de la lista a su "hash" (el numero con el que el
        // Animator identifica un estado) y nos quedamos solo con los que existen.
        int cantidad = walkStates != null ? walkStates.Length : 0;
        if (hashesCaminar == null || hashesCaminar.Length != cantidad) hashesCaminar = new int[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            hashesCaminar[i] = 0;
            if (string.IsNullOrEmpty(walkStates[i])) continue;

            int hash = Animator.StringToHash(walkStates[i]);
            if (animator.HasState(0, hash))
            {
                hashesCaminar[i] = hash;
                hayEstadosDeCaminar = true;
            }
        }

        if (syncWithAnimation && !hayEstadosDeCaminar && !yaAviseEstados)
        {
            yaAviseEstados = true;
            Debug.LogWarning("[Green Mist] FootstepAudio: ninguno de los estados de 'Walk States' existe en la capa 0 del Animator. "
                             + "Los pasos van a sonar por distancia (plan B). Revisá los nombres en el Inspector.", this);
        }
    }

    bool EsEstadoDeCaminar(int hash)
    {
        if (hashesCaminar == null) return false;
        for (int i = 0; i < hashesCaminar.Length; i++)
        {
            if (hashesCaminar[i] != 0 && hashesCaminar[i] == hash) return true;
        }
        return false;
    }

    bool EnMenu()
    {
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy) return true;
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return true;
        return false;
    }

    bool EstaAgachado()
    {
        // Lo mas confiable es el Animator: ThirdPersonMovement le escribe el bool cada frame.
        if (animatorTieneAgachado && animator != null && animator.isActiveAndEnabled)
            return animator.GetBool(hashAgachado);

        // Plan B: la misma tecla que usa el movimiento.
        if (movement != null) return Input.GetKey(movement.crouchKey);
        return false;
    }

    void Reiniciar()
    {
        distancia = 0f;
        tiempoEnAire = 0f;
        tiempoJugando = 0f;
        tiempoCongelado = 0f;
        siguiendoEstado = false;
    }

    void Sonar(float volumen, float tonoBase)
    {
        if (clips == null || clips.Length == 0) return;
        if (!source.isActiveAndEnabled) return;

        // Elegir un clip al azar que no sea el mismo de recien.
        // Con 2 clips eso da siempre "el otro": suenan alternados, uno por pie.
        int cantidad = clips.Length;
        int elegido;
        if (cantidad == 1)
        {
            elegido = 0;
        }
        else if (ultimoClip < 0 || ultimoClip >= cantidad)
        {
            elegido = Random.Range(0, cantidad);
        }
        else
        {
            // Sorteamos entre los demas: uno menos, y salteamos el anterior.
            elegido = Random.Range(0, cantidad - 1);
            if (elegido >= ultimoClip) elegido++;
        }
        ultimoClip = elegido;

        AudioClip clip = clips[elegido];
        if (clip == null) return;

        source.pitch = tonoBase + Random.Range(-pitchVariation, pitchVariation);

        // PlayOneShot no corta el paso anterior: se pisan como en la vida real.
        source.PlayOneShot(clip, volumen * Random.Range(1f - volumeVariation, 1f));
        tiempoDesdePaso = 0f;
    }
}
