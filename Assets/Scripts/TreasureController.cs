using UnityEngine;

// Rileva quando il giocatore tocca il tesoro.
// Richiede: BoxCollider con Is Trigger = true sul GameObject del tesoro.
// Il giocatore deve avere il Tag "Player" e un Rigidbody (IsKinematic=true).
public class TreasureController : MonoBehaviour
{
    [SerializeField] Color glowColor = new Color(1f, 0.85f, 0f);
    [SerializeField] GameObject missionCompletePanel;

    Renderer treasureRenderer;
    bool collected;

    void Start()
    {
        treasureRenderer = GetComponent<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;
        treasureRenderer.material.color = glowColor;
        missionCompletePanel.SetActive(true);
    }
}
