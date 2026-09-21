using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Mostra il conto alla rovescia 5-4-3-2-1-PARTENZA!
// Chiamato da MissionController quando entra nello stato Countdown.
public class CountdownUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] int startFrom = 5;

    public void StartCountdown(Action onComplete)
    {
        StartCoroutine(RunCountdown(onComplete));
    }

    IEnumerator RunCountdown(Action onComplete)
    {
        for (int i = startFrom; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownText.text = "PARTENZA!";
        yield return new WaitForSeconds(1f);
        onComplete?.Invoke();
    }
}
