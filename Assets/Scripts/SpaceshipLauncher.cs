using UnityEngine;
using System.Collections;

public class SpaceshipLauncher : MonoBehaviour
{

    [Header("Durata")]
    public float warmupDuration = 1.2f;
    public float flightDuration = 3.5f;

    [Header("Traiettoria")]
    public float verticalPeak = 3f;
    public float forwardDistance = 15f;
    public float horizontalDrift = 0.5f;

    [Header("Rotazione")]
    public float maxTilt = 30f;

    [Header("Effetti")]
    public ParticleSystem smokeTrail;
    public ParticleSystem groundSmoke;
    public ParticleSystem starFlare;

    [Header("Audio")]
    public AudioSource rocketAudio;

    // Stato interno
    private Vector3 startPosition;
    private bool isLaunching = false;

    void Start()
    {
        startPosition = transform.position;

        if (smokeTrail != null)
            smokeTrail.Stop();
        if (groundSmoke != null)
            groundSmoke.Stop();
    }

    void Update()
    {
#if UNITY_EDITOR
        // Premi Spazio per testare (solo Editor, nuovo Input System)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            TriggerLaunch();
#endif
    }

    public void TriggerLaunch()
    {
        // Evita l'errore "coroutine su oggetto inattivo": il razzo dopo il volo si
        // disattiva, quindi non si puo rilanciare finche la scena non viene ricaricata.
        if (isLaunching || !isActiveAndEnabled) return;
        StartCoroutine(LaunchSequence());
    }

    IEnumerator StopGroundSmokeDelayed()
    {
        // Aspetta che il razzo sia abbastanza in alto (25% del volo)
        yield return new WaitForSeconds(flightDuration * 0.25f);
    
        if (groundSmoke != null)
            groundSmoke.Stop();
    }

    IEnumerator LaunchSequence()
    {
        isLaunching = true;

        // Prima parte il fumo a terra
        if (groundSmoke != null)
            groundSmoke.Play();

        if (rocketAudio != null)
            rocketAudio.Play();

        // Aspetta il warmup poi accendi il trail
        yield return new WaitForSeconds(warmupDuration);

        if (smokeTrail != null)
            smokeTrail.Play();

        // FASE 2 inizia - spegni groundSmoke quando il razzo sale abbastanza
        StartCoroutine(StopGroundSmokeDelayed());

        // FASE 2 - Volo
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration; // va da 0 a 1

            // Posizione
            // Fase verticale pura (primo 25%), poi traiettoria curva
            float tCurve = Mathf.Clamp01((t - 0.25f) / 0.75f);

            float x = startPosition.x + (horizontalDrift * tCurve);
            float y = startPosition.y + (verticalPeak * Mathf.Sin(t * Mathf.PI * 0.6f));
            float z = startPosition.z + (forwardDistance * tCurve * tCurve);

            transform.position = new Vector3(x, y, z);

            // Inclinazione progressiva
            // Inclinazione che segue la traiettoria reale
            Vector3 currentPos = transform.position;
            Vector3 nextPos = new Vector3(
                startPosition.x + (horizontalDrift * (t + 0.05f)),
                startPosition.y + (verticalPeak * Mathf.Sin((t + 0.05f) * Mathf.PI * 0.6f)),
                startPosition.z + (forwardDistance * (t + 0.05f) * (t + 0.05f))
            );

            Vector3 direction = (nextPos - currentPos).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion correction = Quaternion.Euler(90f, 0f, 0f);

            // Inizia a ruotare solo dopo il 20% del volo
            float rotationBlend = Mathf.Clamp01((t - 0.2f) / 0.3f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * correction, Time.deltaTime * 5f * rotationBlend);

            // Rimpicciolisce
            float scale = Mathf.Lerp(1f, 0.25f, t);
            transform.localScale = new Vector3(scale, scale, scale);
            // Trigger stellina quando il razzo e quasi arrivato in alto
            if (t >= 0.92f && starFlare != null && !starFlare.isPlaying)
            {
                starFlare.transform.position = transform.position;
                starFlare.Play();
            }

            yield return null;
        }

        if (rocketAudio != null)
            StartCoroutine(FadeOutAudio(rocketAudio, 1.5f));

        // Fine
        gameObject.SetActive(false);
        isLaunching = false;
    }

    IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }
}