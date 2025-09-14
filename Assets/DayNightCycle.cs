using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Range(0f,1f)] public float timeOfDay = 0f; // 0..1
    public float dayLengthSeconds = 120f;      // 2 minutes pour 24h
    public float timeScale = 1f;               // accélération du temps
    public Light sun;                          // Directional Light
    public Gradient lightColorOverDay;
    public AnimationCurve intensityOverDay = AnimationCurve.Linear(0, 0, 1, 1.2f);

    void Reset()
    {
        lightColorOverDay = new Gradient();
    }

    void Update()
    {
        if (!sun) return;

        timeOfDay += (Time.deltaTime / dayLengthSeconds) * timeScale;
        if (timeOfDay > 1f) timeOfDay -= 1f;

        // Rotation du soleil (–90° = lever, +270° = cycle complet)
        float sunAngle = timeOfDay * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Couleur + intensité
        if (lightColorOverDay != null && lightColorOverDay.colorKeys.Length > 0)
            sun.color = lightColorOverDay.Evaluate(timeOfDay);

        float intensity = intensityOverDay.Evaluate(timeOfDay);
        // atténuer la nuit selon l'axe
        float dot = Mathf.Clamp01(Vector3.Dot(sun.transform.forward, Vector3.down));
        sun.intensity = intensity * dot;

        // (Optionnel) Ambient
        RenderSettings.ambientLight = Color.Lerp(new Color(0.05f,0.05f,0.1f), Color.white, dot);
    }
}
