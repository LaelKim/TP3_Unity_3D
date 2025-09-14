using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    public Health target;
    public Image fill;
    public Transform follow;      // tête (empty au-dessus), sinon le root
    public Vector3 offset = new Vector3(0, 2.0f, 0);

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (target != null)
            target.OnHealthChanged += OnHpChanged;
    }

    void Start()
    {
        if (target != null)
            OnHpChanged(target.currentHp, target.maxHp);
    }

    void LateUpdate()
    {
        if (follow)
            transform.position = follow.position + offset;
        else if (target)
            transform.position = target.transform.position + offset;

        if (cam)
            transform.forward = cam.transform.forward; // billboard
    }

    void OnHpChanged(float cur, float max)
    {
        if (fill) fill.fillAmount = max > 0f ? cur / max : 0f;
    }
}
