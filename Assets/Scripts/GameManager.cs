using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public string SelectedPlanet { get; set; } = "Marte";
    public string SelectedRole   { get; set; } = "Comandante";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        // Cambio scena con dissolvenza in nero (se il fader e presente)
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeAndLoad(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
