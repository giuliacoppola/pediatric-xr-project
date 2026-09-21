using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float hoverScale = 1.08f;
    [SerializeField] float duration   = 0.12f;

    Vector3    originalScale;
    Coroutine  anim;

    void Awake() => originalScale = transform.localScale;

    public void OnPointerEnter(PointerEventData e) => Animate(originalScale * hoverScale);
    public void OnPointerExit(PointerEventData e)  => Animate(originalScale);

    void Animate(Vector3 target)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(ScaleRoutine(target));
    }

    IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        transform.localScale = target;
    }
}
