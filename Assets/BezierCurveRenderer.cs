using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class BezierCurveRenderer : MonoBehaviour
{
    public enum Type { Quadratic, Cubic }
    public Type type = Type.Cubic;

    public Transform P0, P1, P2, P3;

    [Range(2,512)] public int resolution = 128;
    public Color curveColor = Color.cyan;
    public float width = 0.05f;
    [SerializeField] private Material materialAsset; // optionnel

    LineRenderer lr;

    void Reset() { lr = GetComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = false; lr.widthMultiplier = width; EnsureMaterial(); CreatePointsIfMissing(); UpdateCurve(); }
    void OnValidate() { lr = GetComponent<LineRenderer>(); if (lr) lr.widthMultiplier = width; EnsureMaterial(); UpdateCurve(); }
    void Update() { UpdateCurve(); }

    void EnsureMaterial()
    {
        if (!lr) return;
        var shader = Shader.Find("Sprites/Default");
        if (materialAsset)
        {
            if (Application.isPlaying) lr.material = materialAsset; else lr.sharedMaterial = materialAsset;
        }
        else
        {
            if (Application.isPlaying)
            {
                if (lr.material == null || lr.material.shader != shader) lr.material = new Material(shader);
            }
            else
            {
                if (lr.sharedMaterial == null || lr.sharedMaterial.shader != shader)
                {
                    var m = new Material(shader);
                    m.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                    lr.sharedMaterial = m;
                }
            }
        }
        var mat = Application.isPlaying ? lr.material : lr.sharedMaterial;
        if (mat) mat.color = curveColor;
    }

    public void UpdateCurve()
    {
        if (!lr) return;
        if (type == Type.Quadratic)
        {
            if (!(P0 && P1 && P2)) return;
            lr.positionCount = resolution;
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (resolution - 1f);
                lr.SetPosition(i, BezierQuad(P0.position, P1.position, P2.position, t));
            }
        }
        else
        {
            if (!(P0 && P1 && P2 && P3)) return;
            lr.positionCount = resolution;
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (resolution - 1f);
                lr.SetPosition(i, BezierCubic(P0.position, P1.position, P2.position, P3.position, t));
            }
        }
    }

    public static Vector3 BezierQuad(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t; return u*u*p0 + 2*u*t*p1 + t*t*p2;
    }
    public static Vector3 BezierCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t, uu = u*u, tt = t*t;
        return (uu*u)*p0 + (3*uu*t)*p1 + (3*u*tt)*p2 + (tt*t)*p3;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (P0 && P1) Gizmos.DrawLine(P0.position, P1.position);
        if (P1 && P2) Gizmos.DrawLine(P1.position, P2.position);
        if (type == Type.Cubic && P2 && P3) Gizmos.DrawLine(P2.position, P3.position);
        Gizmos.color = Color.magenta; float r = 0.2f;
        if (P0) Gizmos.DrawSphere(P0.position, r);
        if (P1) Gizmos.DrawSphere(P1.position, r);
        if (P2) Gizmos.DrawSphere(P2.position, r);
        if (type == Type.Cubic && P3) Gizmos.DrawSphere(P3.position, r);
    }

    void CreatePointsIfMissing()
    {
        if (!P0) P0 = Make("P0", new Vector3(0,  5, 0));
        if (!P1) P1 = Make("P1", new Vector3(60,15, 40));
        if (!P2) P2 = Make("P2", new Vector3(120,10, 60));
        if (type == Type.Cubic && !P3) P3 = Make("P3", new Vector3(180, 5, 0));
    }
    Transform Make(string n, Vector3 lp){ var go=new GameObject(n); go.transform.SetParent(transform); go.transform.position=lp; return go.transform; }
}
