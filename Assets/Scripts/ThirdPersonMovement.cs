using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;

    [Header("Move")]
    public float walkSpeed = 4.5f;
    public float runSpeed  = 7.5f;
    public float turnSmoothTime = 0.10f;     // giro normal
    public float turnSmoothTimeRun = 0.07f;  // giro más reactivo al correr
    public float accelTime = 0.08f;          // acel/freno

    [Header("Crouch")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchSpeed = 2.2f;
    public float crouchAccelTime = 0.05f;        // acelera más rápido al agacharse
    public float turnSmoothTimeCrouch = 0.14f;   // giro un toque más pesado al agacharse
    public float standHeight  = 2.0f;
    public float crouchHeight = 1.2f;
    bool isCrouching;

    [Header("Sincronizacion de animacion")]
    [Tooltip("Velocidad a la que el ciclo de pasos se ve natural. Si Onji patina, bajalo. Si corre como dibujito, subilo.")]
    public float velocidadAnimBase = 1.2f;

    [Tooltip("Limites del multiplicador, para que nunca quede congelado ni frenetico.")]
    public float animSpeedMin = 0.6f;
    public float animSpeedMax = 2.2f;

    [Tooltip("Nombre del estado de locomocion en el Animator. Solo ahi se acelera el ciclo.")]
    public string estadoLocomocion = "Locomotion";

    [Header("Jump")]
    public float jumpHeight = 1.4f;
    public float gravity    = -9.81f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;

    [Tooltip("Cuanto tiene que llevar sin piso para que la animacion pase a 'en el aire'. Evita que parpadee al aparecer o al pisar un desnivel.")]
    public float graciaAire = 0.08f;

    [Header("Giro asistido al atacar")]
    [Tooltip("Al atacar, Onji gira solo hacia el enemigo vivo mas cercano. Apagalo para que todo quede como antes.")]
    public bool giroAsistido = true;

    [Tooltip("Distancia maxima (en metros) a la que busca un enemigo para encararlo.")]
    [Range(0.5f, 10f)] public float giroAsistidoRango = 4f;

    [Tooltip("Mitad del cono frente a Onji, en grados. 70 = toma enemigos hasta 70 grados a cada lado. 180 = tambien gira hacia los que estan a su espalda.")]
    [Range(5f, 180f)] public float giroAsistidoMedioAngulo = 70f;

    [Tooltip("Suavizado del giro hacia el enemigo (mismo tipo de valor que Turn Smooth Time). Mas chico = giro mas seco.")]
    [Range(0.01f, 0.3f)] public float giroAsistidoTiempo = 0.08f;

    [Tooltip("Segundos que el giro asistido manda sobre las teclas de direccion. Conviene unas 3 veces el tiempo de giro, asi llega a completar la vuelta. Con ~0.65 Onji sigue mirando al enemigo hasta que cae el primer golpe ligero.")]
    [Range(0.05f, 1.5f)] public float giroAsistidoDuracion = 0.25f;

    [Tooltip("Tag de los enemigos. Tiene que ser el mismo que usa WeaponHitbox.")]
    public string giroAsistidoTag = "Enemy";

    Animator anim;

    // --- Giro asistido: estado interno ---
    float giroAsistidoRestante;      // segundos que le quedan (0 = apagado)
    float giroAsistidoAngulo;        // hacia donde mirar, en grados (ya con los +180)
    Transform giroAsistidoObjetivo;  // el enemigo elegido

    // Si el enemigo esta practicamente encima no hay una direccion clara
    // hacia el: en ese caso no recalculamos el angulo.
    const float giroAsistidoDistMin = 0.2f;

    float turnSmoothVelocity;
    Vector3 velocity;
    float lastGroundedTime;

    // Arranca muy negativo a proposito. Si quedara en 0, en el primer
    // frame Time.time tambien vale casi 0 y el buffer de salto cree que
    // acabas de apretar el boton: Onji saltaba solo al aparecer.
    float lastJumpPressed = -999f;

    // velocidad XZ suavizada
    float currentSpeed;
    float speedVel; // ref para SmoothDamp

    // Para que la camara pueda abrir el FOV segun lo rapido que vamos.
    // Va contra la velocidad real y no contra la tecla Shift, asi el
    // efecto acompania la aceleracion en vez de saltar de golpe.
    public float VelocidadActual => currentSpeed;

    void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>(); // o GetComponent<Animator>()
        if (anim) anim.applyRootMotion = false;

        // Arrancamos considerandolo apoyado, asi el primer frame no
        // cuenta como tiempo en el aire.
        lastGroundedTime = Time.time;
    }

    // --- GIRO ASISTIDO AL ATACAR ---
    // Lo llama PlayerAttackSimple justo antes de arrancar la animacion de
    // ataque. Busca el enemigo VIVO mas cercano dentro del rango y del cono
    // que Onji tiene adelante, y solo ANOTA el pedido: la rotacion la aplica
    // Update() de este mismo script. Asi hay un unico lugar que escribe la
    // rotacion y no se pelean dos scripts.
    // Si no hay ningun enemigo que cumpla, no hace nada.
    public void GirarHaciaEnemigoCercano()
    {
        if (!giroAsistido || !enabled) return;
        if (!controller || !controller.enabled) return; // Onji muerto: no gira
        if (string.IsNullOrEmpty(giroAsistidoTag)) return;

        // Esto recorre la escena y crea un array, pero solo pasa al apretar
        // el boton de ataque, no en cada frame.
        GameObject[] enemigos;
        try { enemigos = GameObject.FindGameObjectsWithTag(giroAsistidoTag); }
        catch (UnityException) { return; } // el tag no existe en el proyecto

        // El modelo esta espejado: el frente que SE VE es -forward.
        Vector3 frente = -transform.forward;
        frente.y = 0f;

        Transform mejor = null;
        float mejorAngulo = 0f;
        float mejorDistSqr = giroAsistidoRango * giroAsistidoRango;

        for (int i = 0; i < enemigos.Length; i++)
        {
            GameObject enemigo = enemigos[i];

            // Solo enemigos vivos. Uno recien muerto sigue existiendo hasta
            // el final del frame, por eso miramos la vida y no solo que exista.
            Health salud = enemigo.GetComponent<Health>();
            if (salud == null) salud = enemigo.GetComponentInChildren<Health>();
            if (salud == null || salud.Current <= 0) continue;

            // Distancia en el plano del piso (ignoramos la altura).
            Vector3 hacia = enemigo.transform.position - transform.position;
            hacia.y = 0f;
            float distSqr = hacia.sqrMagnitude;

            if (distSqr > mejorDistSqr) continue;                               // lejos, o ya hay uno mas cerca
            if (distSqr < giroAsistidoDistMin * giroAsistidoDistMin) continue;  // encima nuestro
            if (Vector3.Angle(frente, hacia) > giroAsistidoMedioAngulo) continue; // fuera del cono

            mejor = enemigo.transform;
            mejorDistSqr = distSqr;
            // Mismo calculo que el giro normal de Update: +180 por el espejado.
            mejorAngulo = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg + 180f;
        }

        if (mejor == null) return;

        // turnSmoothVelocity guarda el envion del ultimo giro con las teclas
        // (queda viejo cuando soltas la direccion). Lo ponemos en 0 para que
        // el giro asistido no arranque con un tiron hacia otro lado.
        // Si ya habia un giro asistido en curso (ataques seguidos) NO lo
        // tocamos: ese envion es del propio giro y cortarlo daria un saltito.
        if (giroAsistidoRestante <= 0f) turnSmoothVelocity = 0f;

        giroAsistidoObjetivo = mejor;
        giroAsistidoAngulo = mejorAngulo;
        giroAsistidoRestante = giroAsistidoDuracion;
    }

    // Termina el giro asistido y devuelve la rotacion a las teclas.
    void TerminarGiroAsistido()
    {
        giroAsistidoRestante = 0f;
        giroAsistidoObjetivo = null;
        turnSmoothVelocity = 0f; // el giro normal arranca limpio, sin envion heredado
    }

    void OnDisable()
    {
        // Si apagan el script en medio de un giro asistido, que no quede
        // un pedido viejo esperando para cuando lo vuelvan a prender.
        TerminarGiroAsistido();
    }

    void Update()
    {
        if (!enabled) return;
        if (!controller || !controller.enabled) return;

        // --- INPUT ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool run = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetButtonDown("Jump")) lastJumpPressed = Time.time;

        // mantener para agacharse (HOLD)
        isCrouching = Input.GetKey(crouchKey);

        // direcciones de la cámara, aplastadas al plano XZ
        Vector3 camFwd   = Vector3.Scale(cam.forward, new Vector3(1f, 0f, 1f)).normalized;
        Vector3 camRight = Vector3.Scale(cam.right,  new Vector3(1f, 0f, 1f)).normalized;

        // dirección deseada (relativa a cámara)
        Vector3 desiredDir = (camFwd * v + camRight * h);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        // Con el tiempo congelado (menu, pausa, hit stop) deltaTime vale 0 y
        // no suavizamos nada (ni el giro ni la velocidad de abajo). No es
        // solo prolijidad: si el valor ya llego a su destino, SmoothDamp
        // hace 0/0 por dentro y su velocidad interna queda en NaN ("no es
        // un numero"), que despues rompe el giro o mete un frame invalido.
        bool hayTiempo = Time.deltaTime > 0f;

        // --- GIRO ASISTIDO (lo pide GirarHaciaEnemigoCercano al atacar) ---
        // Mientras dura, manda sobre las teclas: reemplaza al giro normal de
        // abajo. Onji se sigue DESPLAZANDO hacia donde apretas (eso depende
        // de la camara, no de hacia donde mira), asi que no se siente que te
        // sacan el control: solo mira al enemigo un instante.
        // Va fuera del "hay input" a proposito, para que funcione parado.
        if (hayTiempo && giroAsistidoRestante > 0f)
        {
            giroAsistidoRestante -= Time.deltaTime;

            // Seguimos al enemigo mientras dura: los enemigos caminan y salen
            // empujados por el golpe, y un angulo fijo quedaria apuntando al
            // aire. Si lo destruyeron, nos quedamos con el ultimo angulo.
            if (giroAsistidoObjetivo != null)
            {
                Vector3 hacia = giroAsistidoObjetivo.position - transform.position;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > giroAsistidoDistMin * giroAsistidoDistMin)
                    giroAsistidoAngulo = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg + 180f;
            }

            float anguloAsistido = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, giroAsistidoAngulo,
                ref turnSmoothVelocity, giroAsistidoTiempo);

            transform.rotation = Quaternion.Euler(0f, anguloAsistido, 0f);

            if (giroAsistidoRestante <= 0f) TerminarGiroAsistido();
        }
        // --- ROTACIÓN SUAVIZADA HACIA desiredDir ---
else if (hayTiempo && desiredDir.sqrMagnitude > 0.0001f)
{
    float targetAngle = Mathf.Atan2(desiredDir.x, desiredDir.z) * Mathf.Rad2Deg;

    //SUMO 180° PARA QUE DEJE DE HACER MOONWALK
    targetAngle += 180f;

    float turnTime = isCrouching ? turnSmoothTimeCrouch : (run ? turnSmoothTimeRun : turnSmoothTime);

    float angle = Mathf.SmoothDampAngle(
        transform.eulerAngles.y, targetAngle,
        ref turnSmoothVelocity, turnTime);

    transform.rotation = Quaternion.Euler(0f, angle, 0f);
}


        // --- ACELERACIÓN SUAVIZADA (con crouch) ---
        float targetSpeed = 0f;
        if (desiredDir.sqrMagnitude > 0f)
        {
            if (isCrouching) targetSpeed = crouchSpeed;
            else             targetSpeed = run ? runSpeed : walkSpeed;
        }
        float usedAccel = isCrouching ? crouchAccelTime : accelTime;
        // Con deltaTime 0 SmoothDamp devolveria el mismo valor igual, asi que
        // saltearlo no cambia nada... salvo que evita el NaN de arriba (el
        // juego arranca en el menu con Onji quieto: 0 == 0 y tiempo en 0).
        if (hayTiempo) currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, usedAccel);

        Vector3 horizontal = desiredDir * currentSpeed;

        // --- GROUNDED / COYOTE ---
        if (controller.isGrounded) lastGroundedTime = Time.time;
        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool bufferedJump = (Time.time - lastJumpPressed) <= jumpBuffer;

        // --- JUMP ---
        if (bufferedJump && canCoyote)
        {
            lastJumpPressed = -999f; // consumir buffer
            velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);

            if (anim) anim.SetTrigger("Jump");
        }

        // --- GRAVEDAD + SNAP SUAVE ---
        if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        // --- MOVER ---
        Vector3 motion = new Vector3(horizontal.x, velocity.y, horizontal.z);
        controller.Move(motion * Time.deltaTime);

        // --- AJUSTE DE ALTURA DEL CONTROLLER (para que no quede diagonal) ---
        float targetH = isCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetH, 12f * Time.deltaTime);
        Vector3 c = controller.center; 
        c.y = controller.height * 0.5f;
        controller.center = c;

        // --- Animator params ---
        if (anim)
        {
            anim.SetFloat("Speed", currentSpeed, 0.12f, Time.deltaTime);
            anim.SetBool("IsCrouching", isCrouching);
            anim.SetFloat("YVel", velocity.y);

            // CharacterController.isGrounded da false en el primer frame
            // siempre, porque Unity recien lo calcula despues del primer
            // Move(). Sin este filtro el Animator se iba a Jump al aparecer.
            // Tambien evita que parpadee al pisar un desnivel chico.
            // El salto intencional no se retrasa: ese entra por el trigger.
            bool enAire = !controller.isGrounded
                          && (Time.time - lastGroundedTime) > graciaAire;

            anim.SetBool("IsGrounded", !enAire);

            // --- CICLO DE PASOS AL RITMO DEL CUERPO ---
            // Las animaciones de Mixamo son in-place: el ciclo corre siempre
            // a la misma cadencia, no importa cuanto avance el personaje.
            // Como Onji esta escalado a 0.65 su zancada cubre ~1 m/s, pero
            // el controller lo mueve a 4. Por eso el pie patina.
            // Aca aceleramos la reproduccion en proporcion a la velocidad real.
            float mult = 1f;

            var estado = anim.GetCurrentAnimatorStateInfo(0);
            bool enLocomocion = estado.IsName(estadoLocomocion);

            // Solo en locomocion: si tocaramos anim.speed siempre, los ataques
            // y el salto se reproducirian acelerados tambien.
            if (enLocomocion && controller.isGrounded && currentSpeed > 0.1f)
                mult = Mathf.Clamp(currentSpeed / velocidadAnimBase, animSpeedMin, animSpeedMax);

            anim.speed = Mathf.Lerp(anim.speed, mult, 12f * Time.deltaTime);
        }
    }
}
