using UnityEngine;
using System.Collections;

public class MissionController : MonoBehaviour
{
    [Header("Riferimenti")]
    public GameObject energiumSphere;
    public SpaceshipLauncher spaceshipLauncher;
    public TMPro.TextMeshPro countdownText;
    public TMPro.TextMeshPro missionCompleteText;

    [Header("Sfera - Impostazioni")]
    public float sphereAppearDuration = 1.5f;
    public float sphereMaxScale = 0.8f;
    public float breatheInDuration = 3f;
    public float breatheOutDuration = 3f;
    public float breatheMinScale = 0.8f;
    public float breatheMaxScale = 1.2f;

    [Header("Sfera - Scomparsa")]
    public float sphereDisappearDuration = 1f;

    // Stato interno
    private bool isBreathing = false;
    private Coroutine breathingCoroutine;
    private bool launchStarted = false;   // il razzo parte UNA sola volta
    private bool warnedLaunched = false;  // avviso vocale "gia partito" detto una volta sola

    void Start()
    {
        // Sfera invisibile all'inizio
        if (energiumSphere != null)
        {
            energiumSphere.transform.localScale = Vector3.zero;
            energiumSphere.SetActive(false);
        }
    }

    void Update()
    {
        // Tasti per testare in Editor
        if (Input.GetKeyDown(KeyCode.B)) StartBreathing();
        if (Input.GetKeyDown(KeyCode.F)) StartFueling();     // rifornimento (barra sale)
        if (Input.GetKeyDown(KeyCode.G)) CompleteFueling();  // serbatoio pieno
        if (Input.GetKeyDown(KeyCode.L)) StartRocketLaunch();

        // Comandi da CONTROLLER — doppio sistema per massima compatibilita:
        // 1) OVRInput (Meta)   2) UnityEngine.XR.InputDevices (funziona con qualsiasi loader)
        // A/X = rifornimento, B/Y = serbatoio pieno, grilletto = lancia, grip = respiro.
        bool aBtn = false, bBtn = false, trig = false, grip = false;

        // 1) OVRInput
        aBtn |= OVRInput.GetDown(OVRInput.RawButton.A) || OVRInput.GetDown(OVRInput.RawButton.X);
        bBtn |= OVRInput.GetDown(OVRInput.RawButton.B) || OVRInput.GetDown(OVRInput.RawButton.Y);
        trig |= OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) || OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);
        grip |= OVRInput.GetDown(OVRInput.RawButton.RHandTrigger) || OVRInput.GetDown(OVRInput.RawButton.LHandTrigger);

        // 2) XR InputDevices (lettura diretta dal driver, con edge detection)
        ReadXRButtons(ref aBtn, ref bBtn, ref trig, ref grip);

        // 3) INPUT SYSTEM (OpenXR) — la via che sicuramente funziona su questo progetto:
        //    e lo stesso canale usato dai controller nella scena di scelta pianeta.
        ReadInputSystemButtons(ref aBtn, ref bBtn, ref trig, ref grip);

        if (aBtn) StartFueling();
        if (bBtn) CompleteFueling();
        if (trig) StartRocketLaunch();
        // Respiro NON piu sul controller (il grip si preme per sbaglio):
        // si attiva solo dal pulsante 'Respiro' della web app operatore.
    }

    // Stato precedente per rilevare la PRESSIONE (non il tenere premuto)
    bool m_PrevA, m_PrevB, m_PrevTrig, m_PrevGrip;

    void ReadXRButtons(ref bool aBtn, ref bool bBtn, ref bool trig, ref bool grip)
    {
        bool a = false, b = false, t = false, g = false;

        foreach (var node in new[] { UnityEngine.XR.XRNode.RightHand, UnityEngine.XR.XRNode.LeftHand })
        {
            var dev = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
            if (!dev.isValid) continue;

            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,   out bool v1) && v1) a = true; // A / X
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool v2) && v2) b = true; // B / Y
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,   out bool v3) && v3) t = true;
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,      out bool v4) && v4) g = true;
        }

        // Scatta solo sul fronte di pressione
        if (a && !m_PrevA)    aBtn = true;
        if (b && !m_PrevB)    bBtn = true;
        if (t && !m_PrevTrig) trig = true;
        if (g && !m_PrevGrip) grip = true;

        m_PrevA = a; m_PrevB = b; m_PrevTrig = t; m_PrevGrip = g;
    }

    // Legge i tasti dai controller XR tramite il NUOVO Input System (OpenXR).
    void ReadInputSystemButtons(ref bool aBtn, ref bool bBtn, ref bool trig, ref bool grip)
    {
        foreach (var dev in UnityEngine.InputSystem.InputSystem.devices)
        {
            // Considera solo i controller XR (nome tipico: OculusTouchController / XRController)
            if (!(dev is UnityEngine.InputSystem.XR.XRController)) continue;

            if (WasPressed(dev, "primaryButton"))                    aBtn = true; // A / X
            if (WasPressed(dev, "secondaryButton"))                  bBtn = true; // B / Y
            if (WasPressed(dev, "triggerPressed", "triggerButton"))  trig = true;
            if (WasPressed(dev, "gripPressed", "gripButton"))        grip = true;
        }
    }

    static bool WasPressed(UnityEngine.InputSystem.InputDevice dev, params string[] names)
    {
        foreach (var n in names)
        {
            var c = dev.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>(n);
            if (c != null && c.wasPressedThisFrame) return true;
        }
        return false;
    }

    [Header("Guida respiro")]
    [Tooltip("Se spento, il respiro guidato e disattivato del tutto (anche dalla web app).")]
    public bool breathingEnabled = true;

    // ── Chiamato dal tablet ─────────────────────────────
    public void StartBreathing()
    {
        if (!breathingEnabled) return;
        VoiceGuide.Speak("Respira con me. Inspira piano... e adesso butta fuori l'aria.");
        StartCoroutine(AppearAndBreathe());
    }

    public void StartRocketLaunch()
    {
        // Il razzo parte UNA sola volta: niente sequenze ripetute a razzo gia partito
        if (launchStarted)
        {
            if (!warnedLaunched)
            {
                warnedLaunched = true;
                VoiceGuide.Speak("Il razzo e gia partito! Missione compiuta.");
            }
            return;
        }
        // Il razzo parte SOLO se il serbatoio e stato riempito
        var gauge = FindAnyObjectByType<FuelGauge>();
        if (gauge != null && !gauge.IsFull)
        {
            VoiceGuide.Speak("Prima dobbiamo fare il pieno di energia! Riempiamo il serbatoio.");
            return;
        }

        launchStarted = true;

        VoiceGuide.Speak("Tieni fermo il braccio. Si parte per lo spazio!");
        FindAnyObjectByType<StillnessGuide>()?.BeginSequence();
        StartCoroutine(DisappearAndLaunch());
    }

    // ── Rifornimento (durante il prelievo) ──────────────
    public void StartFueling()
    {
        if (launchStarted) return; // a razzo partito il rifornimento non ha senso
        VoiceGuide.Speak("Facciamo il pieno di energia! Guarda la barra salire, e tieni fermo il braccio.");
        FindAnyObjectByType<FuelGauge>()?.StartFilling();
    }

    public void CompleteFueling()
    {
        if (launchStarted) return; // a razzo partito il rifornimento non ha senso
        VoiceGuide.Speak("Serbatoio pieno! Bravissimo, siamo pronti a partire.");
        FindAnyObjectByType<FuelGauge>()?.CompleteFilling();
    }

    // ── Sfera appare e inizia a respirare ───────────────
    IEnumerator AppearAndBreathe()
    {
        // Attiva e scala da 0 a dimensione corretta
        energiumSphere.SetActive(true);

        float elapsed = 0f;
        while (elapsed < sphereAppearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sphereAppearDuration;
            float scale = Mathf.Lerp(0f, sphereMaxScale, Mathf.SmoothStep(0, 1, t));
            energiumSphere.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        energiumSphere.transform.localScale = Vector3.one * breatheMinScale;

        // Inizia respirazione
        isBreathing = true;
        breathingCoroutine = StartCoroutine(BreathingLoop());
    }

    IEnumerator BreathingLoop()
    {
        while (isBreathing)
        {
            // Espandi (inspira)
            float elapsed = 0f;
            while (elapsed < breatheInDuration && isBreathing)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / breatheInDuration;
                float scale = Mathf.Lerp(breatheMinScale, breatheMaxScale, Mathf.SmoothStep(0, 1, t));
                energiumSphere.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            // Contrai (espira)
            elapsed = 0f;
            while (elapsed < breatheOutDuration && isBreathing)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / breatheOutDuration;
                float scale = Mathf.Lerp(breatheMaxScale, breatheMinScale, Mathf.SmoothStep(0, 1, t));
                energiumSphere.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }

    // ── Sfera sparisce e razzo parte ────────────────────
    IEnumerator DisappearAndLaunch()
    {
        // Ferma respirazione
        isBreathing = false;
        if (breathingCoroutine != null)
            StopCoroutine(breathingCoroutine);

        // Posizione e scala iniziali
        Vector3 startScale = energiumSphere.transform.localScale;
        Vector3 startPosition = energiumSphere.transform.position;
        Vector3 targetPosition = new Vector3(0f, 1.5f, 3f); // dentro il razzo
        Vector3 targetScale = new Vector3(0.1f, 0.1f, 0.1f);

        float elapsed = 0f;

        while (elapsed < sphereDisappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sphereDisappearDuration;
            float smooth = Mathf.SmoothStep(0, 1, t);

            // Si rimpicciolisce e si sposta verso il razzo
            energiumSphere.transform.localScale = Vector3.Lerp(startScale, targetScale, smooth);
            energiumSphere.transform.position = Vector3.Lerp(startPosition, targetPosition, smooth);

            yield return null;
        }

        energiumSphere.SetActive(false);

        yield return StartCoroutine(CountdownSequence());

        // Attraversa l'atmosfera: cielo azzurro -> nero spazio + stelle
        FindAnyObjectByType<AtmosphereLaunch>()?.BeginAscent();

        // Lancia il razzo
        if (spaceshipLauncher != null)
            spaceshipLauncher.TriggerLaunch();

        // Aspetta che il razzo finisca il volo (warmup 2 + volo 7 + margine)
        yield return new WaitForSeconds(10f);
        yield return StartCoroutine(ShowMissionComplete());
    }

    IEnumerator CountdownSequence()
    {
        countdownText.gameObject.SetActive(true);

        for (int i = 5; i >= 1; i--)
        {
            countdownText.text = i.ToString();
        
            // Animazione scala - pulse
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1f;
                float scale = Mathf.Lerp(1.2f, 0.8f, t);
                countdownText.transform.localScale = Vector3.one * scale * 0.3f;
                yield return null;
            }
        }

        countdownText.gameObject.SetActive(false);
    }

    IEnumerator ShowMissionComplete()
    {
        // Ferma i messaggi "stai fermo": la procedura e finita
        FindAnyObjectByType<StillnessGuide>()?.StopSequence();
        VoiceGuide.Speak("Ce l'hai fatta! Sei un vero eroe dello spazio. Bravissimo!");

        missionCompleteText.gameObject.SetActive(true);
    
        float elapsed = 0f;
        float duration = 1.5f;
        Color startColor = missionCompleteText.color;
        startColor.a = 0f;
        missionCompleteText.color = startColor;
    
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            Color c = missionCompleteText.color;
            c.a = t;
            missionCompleteText.color = c;
            yield return null;
        }
    
        Color finalColor = missionCompleteText.color;
        finalColor.a = 1f;
        missionCompleteText.color = finalColor;
    }
}