using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

// Gestisce l'interazione con i personaggi 3D nella WaitingRoom.
// Doppio sistema di input:
//   OnMouseX    → funziona su PC/Editor con la Main Camera standard
//   IPointerX   → funziona su Quest 3 con XRUIInputModule + PhysicsRaycaster
// Un flag evita che i due sistemi triggerino la stessa azione due volte.
public class CharacterPickable : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] string characterRole;
    [SerializeField] float  rotationSpeed = 180f;

    [SerializeField] HoloCylinder holoCylinder;
    [SerializeField] TextMeshPro  nameLabel;

    Animator   animator;
    Color      normalLabelColor   = new Color(0f, 0.88f, 0.82f);
    Color      selectedLabelColor = new Color(1f, 1f, 1f);
    bool       isRotating;
    bool       isSelected;
    bool       isHovered;

    Vector3    originalWorldPos;
    Vector3    originalScale;
    Quaternion originalRot;

    // Il modello è figlio dello Slot insieme a etichetta e cilindro: per spostarli
    // tutti insieme durante la showcase/reset, animiamo il genitore, non "transform".
    Transform Group => transform.parent;

    void Start()
    {
        animator         = GetComponentInChildren<Animator>();
        originalWorldPos = Group.position;
        originalScale    = Group.localScale;
        originalRot      = transform.rotation;
        if (nameLabel != null) nameLabel.color = normalLabelColor;

        // Root motion sposta il personaggio seguendo l'animazione — va disabilitato
        if (animator != null)
            animator.applyRootMotion = false;

        // Se c'è un Rigidbody, lo rendiamo kinematic per evitare fisica indesiderata
        var rb = GetComponentInChildren<Rigidbody>();
        if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }
    }

    void Update()
    {
        if (isRotating)
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    // --- IPointerX: Quest 3 (XRUIInputModule + PhysicsRaycaster) ---
    public void OnPointerEnter(PointerEventData _) => HandleHoverEnter();
    public void OnPointerExit(PointerEventData _)  => HandleHoverExit();
    public void OnPointerClick(PointerEventData _) => HandleSelect();

    // --- OnMouseX: PC/Editor (Main Camera standard) ---
    void OnMouseEnter() => HandleHoverEnter();
    void OnMouseExit()  => HandleHoverExit();
    void OnMouseDown()  => HandleSelect();

    /// <summary>Ruolo del personaggio (per selezione remota dall'operatore).</summary>
    public string Role => characterRole;

    /// <summary>Seleziona questo personaggio da remoto (web app operatore).</summary>
    public void SelectRemotely() => HandleSelect();

    // --- Logica condivisa con guard anti-doppio trigger ---
    void HandleHoverEnter()
    {
        if (isSelected || isHovered) return;
        isHovered  = true;
        isRotating = true;
        ShowCylinder(true);
        AudioManager.Instance?.PlayCharacterHover();
    }

    void HandleHoverExit()
    {
        isHovered  = false;
        isRotating = false;
        if (!isSelected) ShowCylinder(false);
    }

    void HandleSelect()
    {
        if (isSelected) return;
        isSelected = true;
        isHovered  = false;
        isRotating = false;
        // Reazione dolce: niente scatto di rotazione ne salto. La rotazione avviene
        // in modo fluido in ShowcaseRoutine quando il personaggio va in mostra.
        ShowCylinder(false);
        if (nameLabel != null) { nameLabel.color = selectedLabelColor; nameLabel.fontStyle = FontStyles.Bold; }
        WaitingRoomManager.Instance?.SelectCharacter(characterRole, this);
    }

    public void PlayConfirm()
    {
        animator?.SetTrigger("Confirm");
    }

    public void Deselect()
    {
        isSelected = false;
        isHovered  = false;
        ShowCylinder(false);
        animator?.ResetTrigger("Jump");
        animator?.CrossFade("Breathing Idle", 0.2f);
        if (nameLabel != null) { nameLabel.color = normalLabelColor; nameLabel.fontStyle = FontStyles.Normal; }
    }

    public void AnimateToShowcase(Vector3 targetWorldPos, float targetScale, float duration = 0.55f)
    {
        ShowCylinder(false);
        StartCoroutine(ShowcaseRoutine(targetWorldPos, targetScale, duration));
    }

    public void ResetToOriginal(float duration = 0.45f)
    {
        isSelected = false;
        isHovered  = false;
        ShowCylinder(false);
        animator?.ResetTrigger("Jump");
        animator?.CrossFade("Breathing Idle", 0.2f);
        if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(true);
            nameLabel.color     = normalLabelColor;
            nameLabel.fontStyle = FontStyles.Normal;
        }
        StartCoroutine(ResetRoutine(duration));
    }

    IEnumerator ShowcaseRoutine(Vector3 targetPos, float targetScale, float duration)
    {
        Vector3    startPos      = Group.position;
        Vector3    startScale    = Group.localScale;
        Quaternion startModelRot = transform.rotation;
        Quaternion endModelRot   = Quaternion.Euler(0f, 180f, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Group.position   = Vector3.Lerp(startPos, targetPos, s);
            Group.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, s);
            transform.rotation = Quaternion.Slerp(startModelRot, endModelRot, s);
            yield return null;
        }
        Group.position     = targetPos;
        Group.localScale   = Vector3.one * targetScale;
        transform.rotation = endModelRot;
    }

    IEnumerator ResetRoutine(float duration)
    {
        Vector3    startPos      = Group.position;
        Vector3    startScale    = Group.localScale;
        Quaternion startModelRot = transform.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Group.position     = Vector3.Lerp(startPos, originalWorldPos, s);
            Group.localScale   = Vector3.Lerp(startScale, originalScale, s);
            transform.rotation = Quaternion.Slerp(startModelRot, originalRot, s);
            yield return null;
        }
        Group.position      = originalWorldPos;
        Group.localScale    = originalScale;
        transform.rotation  = originalRot;
    }

    void ShowCylinder(bool show)
    {
        if (holoCylinder != null) holoCylinder.gameObject.SetActive(show);
    }

    public void SetHighlight(bool active)
    {
        Group.localScale = active ? originalScale * 1.1f : originalScale;
    }
}
