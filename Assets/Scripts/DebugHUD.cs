using UnityEngine;

// Overlay di debug visivo — mostra messaggi ARIA e SFX direttamente a schermo.
// Funziona senza Canvas, senza setup. Da rimuovere nella build finale.
// Chiamare DebugHUD.ShowARIA("testo") e DebugHUD.ShowSFX("nome suono") da qualsiasi script.
public class DebugHUD : MonoBehaviour
{
    public static DebugHUD Instance { get; private set; }

    string ariaMessage = "";
    string sfxMessage  = "";
    float  sfxTimer    = 0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (sfxTimer > 0f) sfxTimer -= Time.deltaTime;
        else sfxMessage = "";
    }

    public static void ShowARIA(string text)
    {
        if (Instance != null) Instance.ariaMessage = text;
        Debug.Log("[ARIA] " + text);
    }

    public static void ShowSFX(string name)
    {
        if (Instance != null) { Instance.sfxMessage = "♪ " + name; Instance.sfxTimer = 2f; }
        Debug.Log("[SFX] " + name);
    }

    void OnGUI()
    {
        // Pannello ARIA in basso
        if (!string.IsNullOrEmpty(ariaMessage))
        {
            var ariaStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = true
            };
            ariaStyle.normal.textColor = new Color(0f, 0.88f, 0.82f);

            float w = Screen.width * 0.6f;
            float h = 100f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - h - 20f;
            GUI.Box(new Rect(x, y, w, h), "ARIA: " + ariaMessage, ariaStyle);
        }

        // SFX in alto a destra
        if (!string.IsNullOrEmpty(sfxMessage))
        {
            var sfxStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                alignment = TextAnchor.MiddleRight
            };
            sfxStyle.normal.textColor = Color.yellow;

            GUI.Label(new Rect(Screen.width - 300f, 10f, 280f, 30f), sfxMessage, sfxStyle);
        }
    }
}
