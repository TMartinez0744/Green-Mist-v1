using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;                    
    public float distance = 7f;                 
    public Vector3 targetOffset = new Vector3(0f, 2f, 0f); 
    public float sensitivity = 3f;            
    public Vector2 pitchLimits = new Vector2(-10f, 65f);

    public float rotateSmooth = 0.07f;          
    public float moveSmooth = 0.07f;            

    public float fieldOfView = 70f;

    [Header("Initial view")]
    public float startPitch = 18f;              // menos cenital al inicio
    public float shoulderOffset = 0.35f;        // 0 centrado, >0 derecha, <0 izquierda

    float yaw, pitch;
    Vector3 currentPos;
    Quaternion currentRot;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (cam != null) cam.fieldOfView = fieldOfView;

        // vista inicial
        pitch = startPitch;                      
        yaw   = target.eulerAngles.y;     

        Vector3 lookAt = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // posición detrás + offset lateral
        Vector3 back = rot * Vector3.forward * distance;
        Vector3 side = rot * Vector3.right * shoulderOffset;

        transform.position = lookAt - back + side;
        transform.rotation = rot;

        currentPos = transform.position;
        currentRot = transform.rotation;
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Vector3 lookAt = target.position + targetOffset;

        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredBack = desiredRot * Vector3.forward * distance;
        Vector3 desiredSide = desiredRot * Vector3.right * shoulderOffset;
        Vector3 desiredPos  = lookAt - desiredBack + desiredSide;

        currentRot = Quaternion.Slerp(currentRot, desiredRot, 1f - Mathf.Exp(-Time.deltaTime / rotateSmooth));
        currentPos = Vector3.Lerp(currentPos, desiredPos, 1f - Mathf.Exp(-Time.deltaTime / moveSmooth));

        transform.rotation = currentRot;
        transform.position = currentPos;

        transform.LookAt(lookAt);
    }
}
