using UnityEngine;

/// <summary>
/// Disegna un raggio-laser visibile dal controller, per mostrare dove si sta puntando.
/// Utile nella WaitingRoom (rig OVR) dove il laser XR non c'e.
///
/// Uso: aggiungi questo componente all'ancora del controller (es. RightHandAnchor e
/// LeftHandAnchor dell'OVRCameraRig, o all'oggetto del modello del controller).
/// Il raggio parte in avanti nella direzione dell'oggetto e si ferma su cio che colpisce.
/// </summary>
[DisallowMultipleComponent]
public class ControllerLaser : MonoBehaviour
{
    [Header("Aspetto")]
    public Color color   = new Color(0f, 0.88f, 0.82f); // teal
    public float maxLength = 5f;
    public float width     = 0.006f;

    LineRenderer m_Line;

    void Start()
    {
        m_Line = gameObject.GetComponent<LineRenderer>();
        if (m_Line == null) m_Line = gameObject.AddComponent<LineRenderer>();

        m_Line.useWorldSpace  = true;
        m_Line.positionCount  = 2;
        m_Line.startWidth     = width;
        m_Line.endWidth       = width;
        m_Line.numCapVertices = 2;
        m_Line.textureMode    = LineTextureMode.Stretch;

        var sh = Shader.Find("Sprites/Default");
        m_Line.material   = new Material(sh) { color = color };
        m_Line.startColor = color;
        m_Line.endColor   = new Color(color.r, color.g, color.b, 0.15f);
    }

    void Update()
    {
        Vector3 origin = transform.position;
        Vector3 dir    = transform.forward;

        float len = maxLength;
        if (Physics.Raycast(origin, dir, out var hit, maxLength))
            len = hit.distance;

        m_Line.SetPosition(0, origin);
        m_Line.SetPosition(1, origin + dir * len);
    }
}
