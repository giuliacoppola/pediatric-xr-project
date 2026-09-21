using UnityEngine;
using System.Collections.Generic;

// Pedana olografica 3D per selezione personaggio.
// Aggiungilo al GameObject cilindro sotto il personaggio.
// Il cilindro originale viene nascosto — la geometria e' generata via codice.
public class HoloPlatform : MonoBehaviour
{
    [Header("Colori")]
    [SerializeField] Color primary = new Color(0.00f, 0.88f, 0.82f); // cyan-teal
    [SerializeField] Color accent  = new Color(0.70f, 0.97f, 1.00f); // azzurro chiaro

    [Header("Dimensioni")]
    [SerializeField] float radius     = 0.65f; // raggio pedana
    [SerializeField] float baseHeight = 0.10f; // altezza base fisica

    // interni
    Transform  ringA, ringB;
    List<Material> pulseMats = new List<Material>();
    float pulseT;
    bool  selected;

    // ---- ciclo Unity ----

    void Start()
    {
        GetComponent<Renderer>().enabled = false; // nasconde cilindro originale
        Build();
    }

    void Update()
    {
        pulseT += Time.deltaTime;
        float glow = selected
            ? 2.2f + Mathf.Sin(pulseT * 3.0f) * 0.6f
            : 0.8f + Mathf.Sin(pulseT * 1.1f) * 0.18f;

        foreach (var m in pulseMats)
            m.SetColor("_EmissionColor", primary * glow);

        if (ringA) ringA.Rotate(0f,  12f * Time.deltaTime, 0f);
        if (ringB) ringB.Rotate(0f, -20f * Time.deltaTime, 0f);
    }

    public void SetSelected(bool value) => selected = value;

    // ---- costruzione geometria ----

    void Build()
    {
        float top = baseHeight; // Y dove finisce la base fisica

        MakePhysicalBase(top);
        MakeTopCap(top);
        ringA = MakeRing("Ring_A", radius * 1.05f, 0.020f, top + 0.04f, primary).transform;
        ringB = MakeRing("Ring_B", radius * 0.72f, 0.014f, top + 0.09f, accent ).transform;
                MakeRing("Ring_C", radius * 0.42f, 0.009f, top + 0.13f, primary);
        MakeBeams(top);
        MakeParticles();
    }

    // base cilindrica fisica — scura e metallica
    void MakePhysicalBase(float top)
    {
        // base esterna
        Spawn("Base_Outer", Vector3.up * (baseHeight * 0.5f),
              new Vector3(radius * 2f, baseHeight * 0.5f, radius * 2f),
              BaseMat(new Color(0.05f, 0.07f, 0.09f, 1f), primary * 0.12f));

        // bordo superiore leggermente piu chiaro
        float rimH = 0.018f;
        Spawn("Base_Rim", Vector3.up * (top + rimH * 0.5f),
              new Vector3(radius * 2.04f, rimH * 0.5f, radius * 2.04f),
              BaseMat(new Color(0.08f, 0.14f, 0.16f, 1f), primary * 0.30f));
    }

    // disco emissivo sulla sommita' della base
    void MakeTopCap(float top)
    {
        float capH = 0.010f;
        var mat = BaseMat(new Color(0f, 0.12f, 0.14f, 0.75f), primary * 1.0f);
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        Spawn("TopCap", Vector3.up * (top + 0.018f + capH * 0.5f),
              new Vector3(radius * 1.90f, capH * 0.5f, radius * 1.90f), mat);
        pulseMats.Add(mat);
    }

    // anello luminoso (LineRenderer)
    GameObject MakeRing(string id, float r, float width, float y, Color color)
    {
        int pts = 90;
        var go = new GameObject(id);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * y;

        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true; lr.positionCount = pts;
        lr.startWidth = width; lr.endWidth = width;
        lr.useWorldSpace = false;
        for (int i = 0; i < pts; i++)
        {
            float a = i * Mathf.PI * 2f / pts;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
        lr.material = AdditiveMat(color);
        return go;
    }

    // fasci verticali sottili sul bordo superiore
    void MakeBeams(float baseTop)
    {
        int n = 18;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            bool  tall = (i % 3 != 0);
            float h    = tall ? Random.Range(0.18f, 0.28f) : Random.Range(0.06f, 0.10f);

            var go = new GameObject($"Beam_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.startWidth    = tall ? 0.009f : 0.005f;
            lr.endWidth      = 0f;
            lr.SetPosition(0, new Vector3(x, baseTop + 0.018f, z));
            lr.SetPosition(1, new Vector3(x, baseTop + h,      z));

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(accent, 0f), new GradientColorKey(accent, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.70f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            lr.colorGradient = g;
            lr.material = AdditiveMat(accent);
        }
    }

    // particelle sparkle e aura morbida
    void MakeParticles()
    {
        float above = baseHeight + 0.05f;

        // sparkle bianchi sottili
        var ps1 = NewPS("Sparkles");
        ps1.transform.localPosition = Vector3.up * above;
        ConfigPS(ps1, rate: 6, minSize: 0.008f, maxSize: 0.022f,
                 color: Color.white, alpha: 0.85f, radius: radius * 0.85f, speedY: 0.12f, lifetime: 2.5f);

        // aura cyan diffusa
        var ps2 = NewPS("Aura");
        ps2.transform.localPosition = Vector3.up * above;
        ConfigPS(ps2, rate: 3, minSize: 0.04f, maxSize: 0.10f,
                 color: primary, alpha: 0.25f, radius: radius * 0.55f, speedY: 0.06f, lifetime: 3.5f);
    }

    // ---- helpers ----

    void Spawn(string id, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = id;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        Destroy(go.GetComponent<CapsuleCollider>());
        go.GetComponent<Renderer>().material = mat;
    }

    Material BaseMat(Color baseColor, Color emission)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic",   0.92f);
        mat.SetFloat("_Smoothness", 0.88f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        pulseMats.Add(mat);
        return mat;
    }

    Material AdditiveMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite",   0);
        mat.renderQueue = 3001;
        return mat;
    }

    Material ParticleMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetColor("_BaseColor", color);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }

    ParticleSystem NewPS(string id)
    {
        var go = new GameObject(id);
        go.transform.SetParent(transform);
        return go.AddComponent<ParticleSystem>();
    }

    void ConfigPS(ParticleSystem ps, int rate, float minSize, float maxSize,
                  Color color, float alpha, float radius, float speedY, float lifetime)
    {
        var main = ps.main;
        main.loop            = true;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(speedY * 0.3f, speedY);
        main.startLifetime   = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSize       = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor      = new Color(color.r, color.g, color.b, alpha);
        main.maxParticles    = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;

        var em = ps.emission;
        em.rateOverTime = rate;

        var sh = ps.shape;
        sh.shapeType      = ParticleSystemShapeType.Circle;
        sh.radius         = radius;
        sh.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(speedY * 0.4f, speedY);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.25f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        ps.GetComponent<ParticleSystemRenderer>().material = ParticleMat(color);
    }
}
