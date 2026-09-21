using UnityEngine;

/// <summary>
/// Lucine/bollicine morbide che fluttuano lentamente nell'aria: effetto visivo
/// dolce e rilassante, pensato per pazienti pediatrici. Sale piano con leggero
/// ondeggiamento, dissolvenze morbide, colori tenui.
///
/// Tutto autonomo: crea da solo il Particle System e una texture rotonda sfumata.
/// Uso: crea un GameObject vuoto (es. "Particelle") posizionato piu o meno all'altezza
/// del bambino e aggiungi questo componente. Non serve assegnare nulla.
/// </summary>
[DisallowMultipleComponent]
public class FloatingParticles : MonoBehaviour
{
    [Header("Aspetto")]
    [Tooltip("Due colori tenui tra cui variano le particelle.")]
    public Color colorA = new Color(0.60f, 0.90f, 1.00f, 0.55f); // azzurro tenue
    public Color colorB = new Color(0.85f, 0.80f, 1.00f, 0.55f); // lilla tenue
    public float sizeMin = 0.06f;
    public float sizeMax = 0.22f;

    [Header("Movimento (lento = rilassante)")]
    public float riseSpeed = 0.10f;   // velocita di salita (m/s)
    public float swayStrength = 0.08f; // ondeggiamento laterale
    public float lifetime = 13f;

    [Header("Quantita e area")]
    [Tooltip("Particelle nuove al secondo (poche = calmo).")]
    public float emissionRate = 6f;
    public int   maxParticles = 60;
    [Tooltip("Dimensioni della zona (metri) in cui compaiono, intorno all'oggetto.")]
    public Vector3 areaSize = new Vector3(5f, 3f, 5f);

    ParticleSystem m_PS;

    void Awake()
    {
        m_PS = GetComponent<ParticleSystem>();
        if (m_PS == null) m_PS = gameObject.AddComponent<ParticleSystem>();

        Configure();
        SetupRenderer();
    }

    void Configure()
    {
        var main = m_PS.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;                 // il movimento lo diamo con Velocity over Lifetime
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // restano nell'ambiente
        main.gravityModifier = 0f;

        var emission = m_PS.emission;
        emission.rateOverTime = emissionRate;

        // Compaiono in un volume attorno all'oggetto
        var shape = m_PS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;

        // Salita lenta. I tre assi devono avere la STESSA modalita: valori costanti.
        var vel = m_PS.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = 0f;
        vel.y = riseSpeed;
        vel.z = 0f;

        // Ondeggiamento organico (da qui viene la deriva laterale morbida)
        var noise = m_PS.noise;
        noise.enabled = true;
        noise.strength = Mathf.Max(0.15f, swayStrength * 2f);
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.1f;

        // Dissolvenza morbida in entrata e in uscita
        var col = m_PS.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Leggero "respiro" della dimensione
        var size = m_PS.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.7f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.85f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void SetupRenderer()
    {
        var r = m_PS.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;

        // Shader compatibile URP e non; "Sprites/Default" rispetta trasparenza e colore
        var sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var mat = new Material(sh);
        mat.mainTexture = BuildSoftCircle(64);
        r.material = mat;
    }

    // Texture rotonda sfumata (bokeh morbido) generata a runtime
    Texture2D BuildSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - c) / c;
            float dy = (y - c) / c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            // 1 al centro, sfuma dolcemente a 0 sul bordo
            float a = Mathf.Clamp01(1f - d);
            a = a * a; // bordo piu morbido
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }
}
