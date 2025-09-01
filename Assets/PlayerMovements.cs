using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public System.Action OnJump; // event simple (public delegate)
public bool IsGrounded => isGrounded; // getter public utile à l’anim


public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Vitesse cible à pleine inclinaison des axes.")]
    public float moveSpeed = 6f;

    [Tooltip("Accélération horizontale (plus grand = plus réactif).")]
    public float acceleration = 20f;

    [Tooltip("Contrôle en l'air (0 = aucun, 1 = comme au sol).")]
    [Range(0f, 1f)] public float airControl = 0.5f;

    [Header("Jump")]
    [Tooltip("Vitesse verticale initiale du saut (en m/s). 7 ~ saut standard.")]
    public float jumpVelocity = 7f;

    [Tooltip("Tolérance après avoir quitté le sol (sec).")]
    public float coyoteTime = 0.1f;

    [Tooltip("Tolérance si SPACE est pressé juste avant d'atterrir (sec).")]
    public float jumpBuffer = 0.1f;

    [Header("Camera Relative Movement")]
    [SerializeField] private Transform cameraRef; 


    [Header("Ground Check")]
    public Transform groundCheck;           
    public float groundRadius = 0.25f;     
    public LayerMask groundMask;           

    private Rigidbody rb;
    private Vector3 targetHorizontalVel;
    private bool isGrounded;
    private float lastGroundedTime = -999f;
    private float lastJumpPressedTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        if (cameraRef == null && Camera.main != null)
        cameraRef = Camera.main.transform; 


    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float z = Input.GetAxisRaw("Vertical");   // W/S

        // Repère caméra (horizontal uniquement)
        Vector3 fwd = cameraRef ? cameraRef.forward : Vector3.forward;
        Vector3 right = cameraRef ? cameraRef.right : Vector3.right;
        fwd.y = 0f; right.y = 0f;
        fwd.Normalize(); right.Normalize();

        Vector3 move = (fwd * z + right * x);
        if (move.sqrMagnitude > 1f) move.Normalize();


        // 3) Vitesse horizontale cible
        targetHorizontalVel = move * moveSpeed;

        // 4) Buffer de saut
        if (Input.GetKeyDown(KeyCode.Space))
            lastJumpPressedTime = Time.time;
    }

    void FixedUpdate()
    {
        // 1) Check sol avec une petite sphère sous la capsule
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
        else
            isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded)
            lastGroundedTime = Time.time;

        // 2) Accélération horizontale progressive
        Vector3 vel = rb.linearVelocity;
        Vector3 horiz = new Vector3(vel.x, 0f, vel.z);

        float accel = isGrounded ? acceleration : acceleration * airControl;
        horiz = Vector3.MoveTowards(horiz, targetHorizontalVel, accel * Time.fixedDeltaTime);

        vel.x = horiz.x;
        vel.z = horiz.z;

        // 3) Saut (coyote + jump buffer), uniquement si "au sol" récent
        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        bool pressedRecently = (Time.time - lastJumpPressedTime) <= jumpBuffer;

        if (canJump && pressedRecently)
        {
            vel.y = jumpVelocity;              // on impose une vitesse verticale
            lastJumpPressedTime = -999f;       // on "consomme" le buffer
        }

        // 4) Appliquer la vélocité finale (la gravité est gérée par le Rigidbody)
        rb.linearVelocity = vel;
    }

    // Aide visuelle dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}
