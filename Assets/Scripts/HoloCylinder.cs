using UnityEngine;
using System.Collections.Generic;

// Cilindro olografico che avvolge il personaggio selezionato.
// Aggiungilo a un Empty GameObject posizionato ai piedi del personaggio.
// Disattiva il GameObject di default — viene attivato da CharacterPickable.
public class HoloCylinder : MonoBehaviour
{
    [Header("Dimensioni")]
    [SerializeField] float cylRadius = 0.55f;
    [SerializeField] float cylHeight = 1.90f;

    [Header("Colori")]
    [SerializeField] Color mainColor = new Color(0f, 0.88f, 0.82f); // cyan-teal
    [SerializeField] Color scanColor = new Color(0.80f, 1.00f, 1.00f); // bianco-ciano

    [Header("Animazione")]
    [SerializeField] float scanSpeed = 0.6f;

    // interni
    List<Material> pulseMats = new List<Material>();
    Material       scanMat;
    Transform      scanRing;
    float          scanY;
    float          pulseT;
    bool           isSelected;

    void Start()  => Build();

    void Update() { }

    public void SetSelected(bool value)
    {
        isSelected = value;
        scanSpeed  = value ? 1.2f : 0.6f;
    }

    void Build()
    {
        MakeShell();
        MakeRing("RingBottom", 0f,       mainColor, 0.020f);
        MakeRing("RingTop",    cylHeight, mainColor, 0.020f);
        MakeParticles();
    }

    // cilindro semitrasparente — solo volume morbido
    void MakeShell()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Shell";
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, cylHeight * 0.5f, 0f);
        go.transform.localScale    = new Vector3(cylRadius * 2f, cylHeight * 0.5f, cylRadius * 2f);
        Destroy(go.GetComponent<CapsuleCollider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", new Color(mainColor.r, mainColor.g, mainColor.b, 0.05f));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        go.GetComponent<Renderer>().material = mat;
    }

    // anello orizzontale fisso
    void MakeRing(string id, float y, Color color, float width)
    {
        int pts = 72;
        var go  = new GameObject(id);
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, y, 0f);

        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true; lr.positionCount = pts;
        lr.startWidth = width; lr.endWidth = width;
        lr.useWorldSpace = false;

        for (int i = 0; i < pts; i++)
        {
            float a = i * Mathf.PI * 2f / pts;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * cylRadius, 0f, Mathf.Sin(a) * cylRadius));
        }

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.35f));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3001;
        lr.material = mat;
    }

    // anello che scorre verso l'alto
    void MakeScanRing()
    {
        int pts = 72;
        var go  = new GameObject("ScanRing");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        scanRing = go.transform;

        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true; lr.positionCount = pts;
        lr.startWidth = 0.018f; lr.endWidth = 0.018f;
        lr.useWorldSpace = false;

        for (int i = 0; i < pts; i++)
        {
            float a = i * Mathf.PI * 2f / pts;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * cylRadius, 0f, Mathf.Sin(a) * cylRadius));
        }

        scanMat = AdditiveMat(scanColor);
        lr.material = scanMat;
    }

    // linee verticali sottili distribuite sul cilindro
    void MakeVertLines(int count)
    {
        var parent = new GameObject("VertLines");
        parent.transform.SetParent(transform);
        parent.transform.localPosition = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            float x = Mathf.Cos(a) * cylRadius;
            float z = Mathf.Sin(a) * cylRadius;

            var go = new GameObject($"VLine_{i}");
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.startWidth    = 0.005f;
            lr.endWidth      = 0.005f;
            lr.SetPosition(0, new Vector3(x, 0f,          z));
            lr.SetPosition(1, new Vector3(x, cylHeight,   z));

            // sfumatura: invisibile agli estremi, visibile al centro
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(mainColor, 0f),
                    new GradientColorKey(mainColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.35f, 0.3f),
                    new GradientAlphaKey(0.35f, 0.7f),
                    new GradientAlphaKey(0f,    1f)
                }
            );
            lr.colorGradient = grad;

            var mat = AdditiveMat(mainColor);
            lr.material = mat;
            pulseMats.Add(mat);
        }
    }

    void MakeParticles()
    {
        var go = new GameObject("Particles");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, cylHeight * 0.1f, 0f);

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = true;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.05f, 0.20f);
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.008f, 0.020f);
        main.startColor      = new Color(1f, 1f, 1f, 0.7f);
        main.maxParticles    = 25;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;

        var em = ps.emission;
        em.rateOverTime = 5;

        var sh = ps.shape;
        sh.shapeType      = ParticleSystemShapeType.Circle;
        sh.radius         = cylRadius * 0.8f;
        sh.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        // Tutti e tre gli assi nella STESSA modalita (intervallo), altrimenti Unity
        // spamma "Particle velocity curves must all be in the same mode"
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(mainColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        ps.GetComponent<ParticleSystemRenderer>().material = ParticleMat();
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

    Material ParticleMat()
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }
}
