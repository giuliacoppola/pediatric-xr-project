using System.Collections;
using UnityEngine;

public class RocketLauncher : MonoBehaviour
{
    [Header("Impostazioni")]
    public ParticleSystem thrusterParticles;
    public float speed = 1.5f;
    public float duration = 3f;

    public void Launch()
    {
        StartCoroutine(LaunchRoutine());
    }

    IEnumerator LaunchRoutine()
    {
        // Attiva particelle
        if (thrusterParticles != null)
        {
            thrusterParticles.gameObject.SetActive(true);
            thrusterParticles.Play();
        }

        yield return new WaitForSeconds(0.3f);

        // Vola su e rimpicciolisce
        float elapsed = 0f;
        Vector3 startPos   = transform.position;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float eased = t * t; // accelerazione

            transform.position   = startPos + Vector3.up * (speed * elapsed * (1 + eased * 4));
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.02f, t);

            yield return null;
        }

        gameObject.SetActive(false);
    }
}