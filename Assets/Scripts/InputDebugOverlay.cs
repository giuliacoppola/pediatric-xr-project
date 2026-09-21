using UnityEngine;
using TMPro;

/// <summary>
/// Pannello di DEBUG temporaneo: mostra nel visore lo stato dei controller e dei
/// tasti, per capire se l'input arriva. Si crea da solo davanti alla camera.
/// Aggiungilo a un GameObject qualsiasi della scena (es. MissionController).
/// QUANDO IL PROBLEMA E RISOLTO: rimuovi il componente.
/// </summary>
public class InputDebugOverlay : MonoBehaviour
{
    [Tooltip("Attiva SOLO per debug: mostra il pannello giallo con lo stato dell'input.")]
    public bool showDebug = false;

    TMP_Text m_Text;
    int m_Frames;
    int m_APressCount;

    void Start()
    {
        if (!showDebug) { enabled = false; return; }
        var go = new GameObject("InputDebugText");
        m_Text = go.AddComponent<TextMeshPro>();
        m_Text.fontSize = 1.2f;
        m_Text.alignment = TextAlignmentOptions.Center;
        m_Text.color = Color.yellow;
        m_Text.rectTransform.sizeDelta = new Vector2(3.2f, 2f);
    }

    void Update()
    {
        m_Frames++;
        if (m_Text == null) return;

        // Tienilo davanti alla camera
        var cam = Camera.main;
        if (cam != null)
        {
            m_Text.transform.position = cam.transform.position + cam.transform.forward * 2.2f
                                        + cam.transform.up * 0.55f;
            m_Text.transform.rotation = Quaternion.LookRotation(m_Text.transform.position - cam.transform.position);
        }

        // Stato XR InputDevices
        var r = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        var l = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
        bool rValid = r.isValid, lValid = l.isValid;
        bool aXR = false, bXR = false, tXR = false;
        if (rValid)
        {
            r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out aXR);
            r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bXR);
            r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out tXR);
        }

        // Stato OVRInput
        bool aOVR = false, connOVR = false;
        try
        {
            aOVR = OVRInput.Get(OVRInput.RawButton.A);
            connOVR = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
        }
        catch { }

        // Stato INPUT SYSTEM (OpenXR) — il canale che usano i controller nella scena 2
        int isCtrls = 0; bool aIS = false; string isName = "-";
        foreach (var dev in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (!(dev is UnityEngine.InputSystem.XR.XRController)) continue;
            isCtrls++;
            isName = dev.name;
            var c = dev.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
            if (c != null && c.isPressed) aIS = true;
        }

        if (aXR || aOVR || aIS) m_APressCount++;

        m_Text.text =
            $"DEBUG INPUT  (frame {m_Frames})\n" +
            $"XR  Right valid: {rValid}   Left valid: {lValid}\n" +
            $"OVR RTouch conn: {connOVR}   A:{(aOVR ? "1" : "0")}\n" +
            $"IS  controllers: {isCtrls}   A:{(aIS ? "1" : "0")}\n" +
            $"IS  device: {isName}\n" +
            $"A premuto {m_APressCount} volte";
    }
}
