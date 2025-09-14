using UnityEngine;
using System.Collections.Generic;

public class BezierCameraFollower : MonoBehaviour
{
    public enum Type { Quadratic, Cubic }
    [Header("Courbe")]
    public Type type = Type.Cubic;
    public Transform P0, P1, P2, P3;      // points de contrôle (mêmes que le renderer)

    [Header("Caméra / Cible à déplacer")]
    public Transform target;              // si null → Camera.main
    public bool orientAlongTangent = true;
    public float lookAhead = 1.0f;        // mètres pour calculer la tangente

    [Header("Lecture")]
    public float speed = 20f;             // m/s
    public bool playOnStart = true;
    public bool loop = true;

    [Header("Précision")]
    [Range(16, 2048)] public int samples = 256;

    float totalLen;
    List<float> cumLen = new List<float>(); // cumulated lengths
    List<Vector3> pts = new List<Vector3>();
    float dist;  // distance parcourue le long de la courbe

    void Start()
    {
        if (!target) { var cam = Camera.main; if (cam) target = cam.transform; }
        BuildLengthTable();
        if (!playOnStart) enabled = false;
    }

    void OnValidate(){ BuildLengthTable(); }

    void Update()
    {
        if (totalLen <= 0f || !target) return;
        dist += speed * Time.deltaTime;
        if (loop) { dist %= totalLen; if (dist < 0) dist += totalLen; }
        else dist = Mathf.Clamp(dist, 0f, totalLen);

        Vector3 pos = PointAtDistance(dist);
        target.position = pos;

        if (orientAlongTangent)
        {
            Vector3 posAhead = PointAtDistance(Mathf.Min(dist + lookAhead, totalLen));
            Vector3 fwd = (posAhead - pos).sqrMagnitude > 1e-6f ? (posAhead - pos).normalized : target.forward;
            target.rotation = Quaternion.Slerp(target.rotation, Quaternion.LookRotation(fwd, Vector3.up), 0.5f);
        }
    }

    // ---- courbe ----
    Vector3 Eval(float t)
    {
        if (type == Type.Quadratic)
            return BezierCurveRenderer.BezierQuad(P0.position, P1.position, P2.position, t);
        else
            return BezierCurveRenderer.BezierCubic(P0.position, P1.position, P2.position, P3.position, t);
    }

    void BuildLengthTable()
    {
        if (!(P0 && P1 && P2 && (type==Type.Quadratic || P3))) { totalLen = 0; return; }
        pts.Clear(); cumLen.Clear();
        Vector3 prev = Eval(0f);
        pts.Add(prev); cumLen.Add(0f);
        totalLen = 0f;
        for (int i = 1; i < samples; i++)
        {
            float t = i / (samples - 1f);
            Vector3 p = Eval(t);
            totalLen += Vector3.Distance(prev, p);
            pts.Add(p);
            cumLen.Add(totalLen);
            prev = p;
        }
    }

    Vector3 PointAtDistance(float s)
    {
        if (s <= 0) return pts[0];
        if (s >= totalLen) return pts[pts.Count - 1];
        // recherche binaire dans cumLen
        int lo = 0, hi = cumLen.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (cumLen[mid] < s) lo = mid + 1; else hi = mid;
        }
        int i = Mathf.Max(1, lo);
        float segLen = cumLen[i] - cumLen[i - 1];
        float t = segLen > 1e-6f ? (s - cumLen[i - 1]) / segLen : 0f;
        return Vector3.Lerp(pts[i - 1], pts[i], t);
    }

    // Debug: touches +/- pour la vitesse
    void OnGUI()
    {
        GUI.Label(new Rect(10,10,260,20), $"Speed: {speed:0.0} m/s  ( +/- pour ajuster )");
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Equals || Event.current.keyCode == KeyCode.Plus) speed += 2f;
            if (Event.current.keyCode == KeyCode.Minus) speed = Mathf.Max(1f, speed - 2f);
        }
    }
}
