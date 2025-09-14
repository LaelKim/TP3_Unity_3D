using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.6f, 0);

    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 2f;
    public float maxDistance = 8f;
    public float zoomSpeed = 5f;

    [Header("Rotation")]
    public float sensitivity = 150f;
    public float minPitch = 10f;  // évite de passer au-dessus de la tête (vision contre-plongée)
    public float maxPitch = 80f;  // évite de regarder sous le sol

    [Header("Player Sync")]
    public Transform playerToRotate; // assigne le Player pour tourner avec clic droit

    float yaw;
    float pitch = 30f;

    void LateUpdate()
    {
        if (!target) return;

        // Zoom molette
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        bool right = Input.GetMouseButton(1);
        bool left = Input.GetMouseButton(0);
        if (right || left)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * sensitivity * Time.deltaTime;
            pitch -= mouseY * sensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // si clic droit : le Player s'aligne avec la direction de la caméra
            if (right && playerToRotate)
            {
                Vector3 fwd = GetForwardOnPlane();
                if (fwd.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(fwd, Vector3.up);
                    playerToRotate.rotation = Quaternion.Slerp(playerToRotate.rotation, look, 15f * Time.deltaTime);
                }
            }
        }

        // Positionner la caméra
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + targetOffset - (rot * Vector3.forward * distance);
        transform.SetPositionAndRotation(desiredPos, rot);
    }

    Vector3 GetForwardOnPlane()
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        return rot * Vector3.forward;
    }
}
