using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Vitesses")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5.0f;
    public float sprintSpeed = 7.5f;
    public float acceleration = 22f;

    [Header("Saut / Sol")]
    public float jumpForce = 8.5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundMask;

    [Header("Caméra")]
    public Transform cameraTransform;

    [Header("Animation")]
    // Animator doit avoir: MoveX, MoveY, MoveAmount, SpeedTier, Grounded, Jump, Attack (Trigger)
    public Animator animator;

    [Header("Attaque")]
    public string attackTrigger = "Attack";  // nom du Trigger dans l'Animator
    public float attackCooldown = 0.6f;      // délai mini entre deux attaques
    float nextAttackTime = 0f;

    Rigidbody rb;
    Vector3 targetVel;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 1) Input (WASD)
        float ix = 0f, iy = 0f;
        if (kb.aKey.isPressed) ix -= 1f;
        if (kb.dKey.isPressed) ix += 1f;
        if (kb.sKey.isPressed) iy -= 1f;
        if (kb.wKey.isPressed) iy += 1f;

        Vector2 raw = new Vector2(ix, iy);
        if (raw.sqrMagnitude > 1f) raw.Normalize();
        float moveAmount = raw.magnitude;

        // 2) Direction relative caméra
        Vector3 camF = Vector3.forward, camR = Vector3.right;
        if (cameraTransform)
        {
            camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            camR = Vector3.ProjectOnPlane(cameraTransform.right,   Vector3.up).normalized;
        }
        Vector3 moveDir = (camF * raw.y + camR * raw.x);

        // 3) Tier de vitesse
        float tier = 1f; // Run par défaut
        float speed = runSpeed;
        if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) { tier = 2f; speed = sprintSpeed; }
        if (kb.leftCtrlKey.isPressed  || kb.rightCtrlKey.isPressed ) { tier = 0f; speed = walkSpeed;   }

        targetVel = moveDir.normalized * (speed * moveAmount);

        // 4) Sol + Saut
        if (groundCheck)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (kb.spaceKey.wasPressedThisFrame && isGrounded)
        {
            animator?.SetTrigger("Jump");
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // >>> 4bis) ATTAQUE sur E <<<
        if (kb.eKey.wasPressedThisFrame && animator && Time.time >= nextAttackTime)
        {
            animator.ResetTrigger("Jump");          // optionnel: évite de concurrencer le saut
            animator.SetTrigger(attackTrigger);     // déclenche l'animation d'attaque
            nextAttackTime = Time.time + attackCooldown;

            // Optionnel: figer le déplacement pendant l'attaque.
            // targetVel = Vector3.zero;
        }

        // 5) Paramètres Animator
        if (animator)
        {
            animator.SetFloat("MoveX", raw.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveY", raw.y, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveAmount", moveAmount, 0.1f, Time.deltaTime);
            animator.SetFloat("SpeedTier", tier, 0.05f, Time.deltaTime);
            animator.SetBool("Grounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
        Vector3 newHoriz = Vector3.MoveTowards(horiz, targetVel, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newHoriz.x, vel.y, newHoriz.z);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
