using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("References")]
    public Transform target;           // Le Player suivi (point d’orbite)
    public Transform playerToRotate;   // Le Player qui doit s’orienter (RMB)
    public Transform pivot;            // Enfant de CameraRig
    public Camera cam;                 // Main Camera (enfant de Pivot)

    [Header("Follow")]
    [Tooltip("Décalage vertical du point d’orbite (ex: 1.6 ≈ hauteur d’épaules).")]
    public float targetHeight = 1.6f;
    [Tooltip("Suivi doux de la position du Player.")]
    public float followSmoothTime = 0.08f;

    [Header("Orbit")]
    [Tooltip("Vitesse de rotation horizontale (yaw).")]
    public float yawSpeed = 220f;
    [Tooltip("Vitesse de rotation verticale (pitch).")]
    public float pitchSpeed = 180f;
    [Tooltip("Limites verticales : empêche de passer au-dessus de la tête et sous le sol.")]
    public float minPitch = -10f;  // ne pas trop lever la caméra
    public float maxPitch = 70f;   // ne pas passer "au-dessus" (vue de bas en haut)

    [Header("Zoom")]
    [Tooltip("Distance orbitale actuelle.")]
    public float distance = 6f;
    public float minDistance = 2.5f;
    public float maxDistance = 12f;
    [Tooltip("Vitesse du zoom à la molette.")]
    public float zoomSpeed = 4f;

    [Header("Player Align (RMB)")]
    [Tooltip("Vitesse de rotation du Player pour s’aligner avec la caméra (RMB).")]
    public float playerTurnSpeed = 12f;

    private float yaw;    // rotation Y du rig
    private float pitch;  // rotation X du pivot
    private Vector3 followVelocity; // pour SmoothDamp

    void Start()
    {
        if (!target || !pivot || !cam)
        {
            Debug.LogError("[CameraOrbit] Références manquantes.");
            enabled = false;
            return;
        }

        // Init yaw/pitch depuis les rotations actuelles
        Vector3 rigEuler = transform.eulerAngles;
        yaw = rigEuler.y;

        Vector3 pivotEuler = pivot.localEulerAngles;
        // Convertit en -180..180 pour éviter les surprises
        pitch = NormalizeAngle(pivotEuler.x);

        // Clamp initial
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Place la caméra à la bonne distance
        SetCameraLocalZ(-distance);
    }

    void Update()
    {
        // --- Zoom molette ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // --- Rotation à la souris ---
        bool rmb = Input.GetMouseButton(1); // clic droit
        bool lmb = Input.GetMouseButton(0); // clic gauche

        if (rmb || lmb)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw   += mouseX * yawSpeed * Time.deltaTime;
            pitch -= mouseY * pitchSpeed * Time.deltaTime;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // --- Aligner le Player si clic droit ---
        if (rmb && playerToRotate != null)
        {
            // Le Player regarde la même direction horizontale que la caméra
            Quaternion targetRot = Quaternion.Euler(0f, yaw, 0f);
            playerToRotate.rotation = Quaternion.Slerp(playerToRotate.rotation, targetRot, playerTurnSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // Suivi de la position du Player (avec offset hauteur)
        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref followVelocity, followSmoothTime);

        // Appliquer la rotation yaw au rig et pitch au pivot
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Distance orbitale : caméra recule le long de -Z local du pivot
        SetCameraLocalZ(-distance);
    }

    private void SetCameraLocalZ(float z)
    {
        Vector3 lp = cam.transform.localPosition;
        lp.z = z;
        cam.transform.localPosition = lp;
    }

    private static float NormalizeAngle(float a)
    {
        a = Mathf.Repeat(a + 180f, 360f) - 180f;
        return a;
    }
}
