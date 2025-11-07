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

    [Header("Jump")]
    public float jumpHeight = 1.4f;
    public float gravity    = -9.81f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;

    Animator anim;

    float turnSmoothVelocity;
    Vector3 velocity;
    float lastGroundedTime;
    float lastJumpPressed;

    // velocidad XZ suavizada
    float currentSpeed;
    float speedVel; // ref para SmoothDamp

    void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>(); // o GetComponent<Animator>()
        if (anim) anim.applyRootMotion = false;
    }

    void Update()
    {
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

        // --- ROTACIÓN SUAVIZADA HACIA desiredDir ---
        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(desiredDir.x, desiredDir.z) * Mathf.Rad2Deg;
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
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, usedAccel);

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
            anim.SetBool("IsGrounded", controller.isGrounded);
        }
    }
}
