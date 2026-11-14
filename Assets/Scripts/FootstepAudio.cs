using UnityEngine;

// Pasos de Onji.
//
// Suena un paso cada vez que Onji RECORRE una zancada (no por tiempo ni por tecla).
// La distancia sale de controller.velocity, que es lo que el CharacterController
// se movio de verdad: empujando contra una pared vale 0 y no suena nada.
// La zancada cambia si camina, corre o va agachado, y al caer de un salto
// se fuerza un paso mas fuerte (el aterrizaje).
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

    [Tooltip("El Animator de Onji. Se le lee el bool de agachado. Si queda vacio se mira la tecla.")]
    public Animator animator;

    [Tooltip("AudioSource propio de los pasos (hijo 'GM_Audio_Pasos'). NO usar el de PlayerRoot: ese es del whoosh.")]
    public AudioSource source;

    [Tooltip("Panel del menu principal. Mientras esta a la vista no hay pasos.")]
    public GameObject mainMenuPanel;

    [Tooltip("Panel de game over. Mientras esta a la vista no hay pasos.")]
    public GameObject gameOverPanel;

    [Header("Clips")]
    [Tooltip("Variantes del paso (5 o 6 queda bien). Se elige una al azar sin repetir la anterior. Vacio = no suena nada.")]
    public AudioClip[] clips;

    [Header("Zancada: metros entre un paso y el siguiente")]
    [Tooltip("Caminando. Si los pasos suenan muy seguidos, subilo.")]
    [Range(0.3f, 3f)] public float walkStride = 0.95f;

    [Tooltip("Corriendo (Shift). Mas largo que caminando: corriendo se dan pasos mas largos.")]
    [Range(0.3f, 3f)] public float runStride = 1.5f;

    [Tooltip("Agachado. Pasos cortitos.")]
    [Range(0.3f, 3f)] public float crouchStride = 0.8f;

    [Header("Volumen")]
    [Tooltip("Volumen de cada paso caminando.")]
    [Range(0f, 1f)] public float walkVolume = 0.5f;

    [Tooltip("Volumen de cada paso corriendo. Mas fuerte que caminando: pisa con mas peso.")]
    [Range(0f, 1f)] public float runVolume = 0.75f;

    [Tooltip("Agachado suena mas bajo: va en sigilo.")]
    [Range(0f, 1f)] public float crouchVolume = 0.25f;

    [Tooltip("Volumen del paso al caer de un salto.")]
    [Range(0f, 1f)] public float landVolume = 0.9f;

    [Header("Variacion (para que no suene a metralleta)")]
    [Tooltip("Cuanto cambia el tono entre paso y paso. 0.07 = hasta un 7% mas agudo o mas grave.")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.07f;

    [Tooltip("Cuanto puede bajar el volumen de un paso al azar. 0.2 = entre 80% y 100%.")]
    [Range(0f, 0.5f)] public float volumeVariation = 0.2f;

    [Tooltip("Tono del aterrizaje. Menos de 1 = mas grave, suena mas pesado.")]
    [Range(0.5f, 1.2f)] public float landPitch = 0.88f;

    [Header("Ajustes finos")]
    [Tooltip("Por debajo de esta velocidad (m/s) se lo considera quieto y no se cuenta distancia.")]
    [Range(0f, 2f)] public float minSpeed = 0.4f;

    [Tooltip("Al arrancar a caminar, que parte de la zancada ya viene 'hecha'. 0.6 = el primer paso suena enseguida.")]
    [Range(0f, 0.95f)] public float firstStepHeadStart = 0.6f;

    [Tooltip("Segundos minimos en el aire para que al tocar el piso suene el aterrizaje. Evita pasos falsos en desniveles.")]
    [Range(0.05f, 1f)] public float minAirTime = 0.2f;

    [Tooltip("Segundos de silencio al empezar a jugar (despues del menu). Evita un paso falso cuando el menu se cierra.")]
    [Range(0f, 2f)] public float silenceAfterMenu = 0.5f;

    [Tooltip("Nombre del bool de agachado en el Animator.")]
    public string crouchParameter = "IsCrouching";

    // Si queda sin piso menos que esto (un escalon, una bajada), los pasos siguen como si nada.
    const float GraciaAire = 0.1f;

    float distancia;        // metros recorridos desde el ultimo paso
    float tiempoEnAire;     // segundos seguidos sin tocar el piso
    float tiempoJugando;    // segundos de juego desde que se cerro el menu
    int ultimoClip = -1;    // para no repetir el mismo clip dos veces seguidas

    int hashAgachado;
    bool animatorTieneAgachado;

    // Start y no Awake: para entonces el Animator ya cargo su lista de parametros.
    void Start()
    {
        // Averiguamos UNA vez si el Animator tiene el bool de agachado. Preguntar
        // por un parametro que no existe tira un warning en cada frame.
        hashAgachado = Animator.StringToHash(crouchParameter);
        animatorTieneAgachado = false;
        if (animator != null)
        {
            AnimatorControllerParameter[] parametros = animator.parameters;
            for (int i = 0; i < parametros.Length; i++)
            {
                if (parametros[i].name == crouchParameter
                    && parametros[i].type == AnimatorControllerParameterType.Bool)
                    animatorTieneAgachado = true;
            }
        }
    }

    // LateUpdate y no Update: asi corre DESPUES de que ThirdPersonMovement movio
    // al personaje en este frame, y la velocidad y el "toca piso" estan al dia.
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
        // saltado, asi que directamente no los miramos.
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Un respiro al empezar a jugar: Onji se esta acomodando en el piso.
        tiempoJugando += dt;
        if (tiempoJugando < silenceAfterMenu)
        {
            distancia = 0f;
            tiempoEnAire = 0f;
            return;
        }

        // ---------- Piso y aterrizaje ----------
        if (controller.isGrounded)
        {
            if (tiempoEnAire >= minAirTime)
            {
                // Venia en el aire de verdad y acaba de tocar: paso forzado.
                Sonar(landVolume, landPitch);
                distancia = 0f;
            }
            tiempoEnAire = 0f;
        }
        else
        {
            tiempoEnAire += dt;
            if (tiempoEnAire > GraciaAire) return; // en el aire no hay pasos
        }

        // ---------- Distancia recorrida ----------
        // Solo XZ: subir o caer no cuenta como caminar.
        Vector3 v = controller.velocity;
        float velocidad = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        if (float.IsNaN(velocidad) || float.IsInfinity(velocidad)) return;

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
            // Quieto (o empujando una pared). Dejamos la cuenta adelantada para
            // que el primer paso suene enseguida al arrancar.
            distancia = zancada * firstStepHeadStart;
            return;
        }

        distancia += velocidad * dt;
        if (distancia >= zancada)
        {
            distancia -= zancada;
            if (distancia > zancada) distancia = 0f; // por si hubo un tiron de frames
            Sonar(volumen, 1f);
        }
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
        if (animatorTieneAgachado && animator != null) return animator.GetBool(hashAgachado);

        // Plan B: la misma tecla que usa el movimiento.
        if (movement != null) return Input.GetKey(movement.crouchKey);
        return false;
    }

    void Reiniciar()
    {
        distancia = 0f;
        tiempoEnAire = 0f;
        tiempoJugando = 0f;
    }

    void Sonar(float volumen, float tonoBase)
    {
        if (clips == null || clips.Length == 0) return;
        if (!source.isActiveAndEnabled) return;

        // Elegir un clip al azar que no sea el mismo de recien.
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
    }
}
