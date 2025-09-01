using UnityEngine;
// Pour AmbientMode
using UnityEngine.Rendering;

[ExecuteAlways]
public class DayNightCycle : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("Durée IRL d’un cycle complet de 24 h virtuelles (en secondes). Ex: 120 = 2 min.")]
    public float cycleDurationSeconds = 120f;

    [Tooltip("Heure de départ (0..24). 12 = midi.")]
    [Range(0f, 24f)] public float startHour = 9f;

    [Tooltip("Multiplier temps (x1 par défaut).")]
    public float timeScale = 1f;

    [Tooltip("Temps courant en heures virtuelles (0..24).")]
    [Range(0f, 24f)] public float currentHour = 0f;

    [Header("Sun (Directional Light)")]
    public Light sunLight;

    [Tooltip("Azimut (orientation Est-Ouest). 0 = axe Z+, 90 = axe X+.")]
    [Range(0f, 360f)] public float azimuth = 0f;

    [Tooltip("Intensité max du soleil à midi.")]
    public float maxSunIntensity = 1.2f;

    [Tooltip("Courbe d’intensité en fonction de la hauteur du soleil (0 = nuit, 1 = zénith).")]
    public AnimationCurve sunIntensityByHeight = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Couleur du soleil selon l’heure/hauteur.")]
    public Gradient sunColorOverDay;

    [Header("Ambient / Sky")]
    [Tooltip("Mode d’éclairage ambiant. Flat = couleur pilotée par le script.")]
    public AmbientMode ambientMode = AmbientMode.Flat;

    [Tooltip("Couleur ambiante selon l’heure/hauteur.")]
    public Gradient ambientColorOverDay;

    [Tooltip("Intensité ambiante globale (multiplie la couleur).")]
    [Range(0f, 2f)] public float ambientIntensity = 1f;

    // Interne : 0..1
    [SerializeField, Range(0f, 1f)] private float tDay = 0f;

    void Reset()
    {
        sunLight = GetComponent<Light>();
        if (sunColorOverDay == null || sunColorOverDay.colorKeys.Length == 0)
        {
            // Dégradé par défaut : nuit bleu sombre -> aube orangée -> blanc midi -> couchant orange -> nuit
            sunColorOverDay = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.15f,0.2f,0.35f), 0.00f), // nuit
                    new GradientColorKey(new Color(1.00f,0.55f,0.25f), 0.23f), // aube
                    new GradientColorKey(new Color(1.00f,0.95f,0.90f), 0.50f), // midi
                    new GradientColorKey(new Color(1.00f,0.55f,0.25f), 0.77f), // couchant
                    new GradientColorKey(new Color(0.15f,0.2f,0.35f), 1.00f), // nuit
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            };
        }

        if (ambientColorOverDay == null || ambientColorOverDay.colorKeys.Length == 0)
        {
            // Ambiance plus désaturée
            ambientColorOverDay = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.05f,0.07f,0.12f), 0.00f), // nuit
                    new GradientColorKey(new Color(0.40f,0.35f,0.30f), 0.23f), // aube
                    new GradientColorKey(new Color(0.75f,0.80f,0.85f), 0.50f), // midi
                    new GradientColorKey(new Color(0.40f,0.35f,0.30f), 0.77f), // couchant
                    new GradientColorKey(new Color(0.05f,0.07f,0.12f), 1.00f), // nuit
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            };
        }

        if (sunIntensityByHeight == null || sunIntensityByHeight.length == 0)
        {
            // 0 à l’horizon, pic au zénith, doux aux transitions
            sunIntensityByHeight = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    void OnEnable()
    {
        // Initialisation heure
        if (!Application.isPlaying)
            currentHour = Mathf.Clamp(startHour, 0f, 24f);

        ApplyAmbientMode();
    }

    void Update()
    {
        if (sunLight == null) return;

        // 1) Avancement du temps (boucle 24h)
        if (Application.isPlaying)
        {
            float secondsPerCycle = Mathf.Max(1f, cycleDurationSeconds);
            float dt = Time.deltaTime * timeScale;
            tDay = Mathf.Repeat(tDay + dt / secondsPerCycle, 1f);
            currentHour = tDay * 24f;
        }
        else
        {
            // En mode Éditeur (pas play), tDay suit directement currentHour pour prévisualiser
            tDay = Mathf.Repeat(currentHour / 24f, 1f);
        }

        // 2) Mise à jour soleil + ambiance
        UpdateSunRotation();
        UpdateSunLight();
        UpdateAmbient();
    }

    private void UpdateSunRotation()
    {
        // Mapping : t=0.00 -> minuit ; 0.25 -> 6h ; 0.50 -> midi ; 0.75 -> 18h
        // Altitude = t * 360 - 90  ( -90 : sous l’horizon ; 90 : zénith )
        float altitude = tDay * 360f - 90f;
        transform.rotation = Quaternion.Euler(altitude, azimuth, 0f);
    }

    private void UpdateSunLight()
    {
        // Hauteur du soleil (0..1). 0 = sous l’horizon ; 1 = zénith.
        // La Directional Light éclaire selon transform.forward.
        // Au zénith (altitude=90), forward ≈ (0,-1,0) -> -forward ≈ up -> dot=1
        float height01 = Mathf.Clamp01(Vector3.Dot(-transform.forward, Vector3.up));

        // Intensité lissée via courbe + cap nuit à 0
        float inten = sunIntensityByHeight.Evaluate(height01) * maxSunIntensity;
        inten = Mathf.Max(0f, inten);
        sunLight.intensity = inten;

        // Couleur évolutive
        Color c = sunColorOverDay.Evaluate(tDay);
        sunLight.color = c;

        // (Optionnel) éteindre vraiment la light la nuit (légère opti)
        sunLight.enabled = inten > 0.01f;
    }

    private void UpdateAmbient()
    {
        RenderSettings.ambientMode = ambientMode;
        Color amb = ambientColorOverDay.Evaluate(tDay) * ambientIntensity;

        switch (ambientMode)
        {
            case AmbientMode.Flat:
                RenderSettings.ambientLight = amb;
                break;
            case AmbientMode.Trilight:
                // Si tu veux Trilight, tu peux dériver 3 couleurs à partir de amb/tDay
                RenderSettings.ambientSkyColor = amb;
                RenderSettings.ambientEquatorColor = Color.Lerp(amb * 0.8f, amb, 0.5f);
                RenderSettings.ambientGroundColor = amb * 0.6f;
                break;
            case AmbientMode.Skybox:
                // Laisse Unity gérer depuis le skybox ; on peut tout de même toucher l’intensité globale
                RenderSettings.ambientIntensity = Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(Vector3.Dot(-transform.forward, Vector3.up)));
                break;
        }
    }

    private void ApplyAmbientMode()
    {
        RenderSettings.ambientMode = ambientMode;
    }

    // Utilitaire (debug): forcer une heure
    public void SetHour(float hour)
    {
        currentHour = Mathf.Repeat(Mathf.Max(0f, hour), 24f);
        tDay = currentHour / 24f;
        UpdateSunRotation();
        UpdateSunLight();
        UpdateAmbient();
    }
}
