using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Mostra messaggi dolci e rassicuranti per invitare il bambino a restare fermo
/// (soprattutto il braccio) durante il lancio = durante il prelievo.
/// I messaggi si alternano con dissolvenze morbide dal conto alla rovescia fino
/// alla fine del volo.
///
/// Uso: crea un testo TextMeshPro (dove vuoi che appaia il messaggio) e aggiungi
/// questo componente ALLO STESSO oggetto (trova il testo da solo), oppure assegna
/// il campo Message Text. La sequenza parte/ferma da sola col lancio (MissionController).
/// </summary>
[DisallowMultipleComponent]
public class StillnessGuide : MonoBehaviour
{
    [Header("Testo")]
    [Tooltip("Se vuoto, usa il TMP su questo oggetto o su un figlio.")]
    public TMP_Text messageText;
    public Color textColor = new Color(0f, 0.88f, 0.82f); // teal, coerente col resto

    [Header("Messaggio unico (consigliato)")]
    [Tooltip("Se attivo: mostra UN solo messaggio fisso, dall'inizio della scena fino a missione completata.")]
    public bool useSingleMessage = true;
    [TextArea] public string singleMessage = "Non muoverti, stai andando benissimo!";
    [Tooltip("Mostra il messaggio subito all'avvio della scena (non solo al lancio).")]
    public bool showFromStart = true;

    [Header("Messaggi alternati (usati solo se Messaggio unico e spento)")]
    [TextArea]
    public string[] messages =
    {
        "Tieni fermo il braccio",
        "Resta immobile come un vero astronauta",
        "Sei bravissimo, non muoverti",
        "Respira piano e stai fermo",
        "Ci siamo quasi... resta fermo"
    };

    [Header("Tempi")]
    public float secondsPerMessage = 3.5f;
    public float fadeTime = 0.6f;

    Coroutine m_Loop;

    void Start()
    {
        if (showFromStart) BeginSequence();
    }

    void Awake()
    {
        if (messageText == null) messageText = GetComponent<TMP_Text>();
        if (messageText == null) messageText = GetComponentInChildren<TMP_Text>(true);
        if (messageText != null)
        {
            messageText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            messageText.text = "";
        }
    }

    /// <summary>Inizia a mostrare i messaggi.</summary>
    public void BeginSequence()
    {
        if (messageText == null) return;
        if (m_Loop != null) StopCoroutine(m_Loop);

        if (useSingleMessage)
        {
            // Un solo messaggio, fisso e sempre visibile
            messageText.text = singleMessage;
            m_Loop = StartCoroutine(FadeTo(1f));
            return;
        }

        if (messages == null || messages.Length == 0) return;
        m_Loop = StartCoroutine(Loop());
    }

    /// <summary>Ferma i messaggi con una dissolvenza (a missione completata).</summary>
    public void StopSequence()
    {
        if (m_Loop != null) StopCoroutine(m_Loop);
        m_Loop = null;
        if (messageText != null) StartCoroutine(FadeTo(0f));
    }

    IEnumerator Loop()
    {
        int i = 0;
        while (true)
        {
            messageText.text = messages[i % messages.Length];
            yield return FadeTo(1f);
            yield return new WaitForSeconds(secondsPerMessage);
            yield return FadeTo(0f);
            i++;
        }
    }

    IEnumerator FadeTo(float targetA)
    {
        if (messageText == null) yield break;
        float startA = messageText.color.a;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, targetA, t / fadeTime);
            messageText.color = new Color(textColor.r, textColor.g, textColor.b, a);
            yield return null;
        }
        messageText.color = new Color(textColor.r, textColor.g, textColor.b, targetA);
    }
}
