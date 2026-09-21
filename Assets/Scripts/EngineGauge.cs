using UnityEngine;
using UnityEngine.UI;

// Barra del motore: fillAmount e colore si aggiornano in base al valore 0-1.
// Rosso = basso, giallo = medio, verde = stabile.
public class EngineGauge : MonoBehaviour
{
    [SerializeField] Image fillImage;

    public void SetPower(float value)
    {
        fillImage.fillAmount = value;
        fillImage.color = Color.Lerp(Color.red, Color.green, value);
    }
}
