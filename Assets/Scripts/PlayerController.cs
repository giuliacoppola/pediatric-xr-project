using UnityEngine;

// Muove il personaggio con WASD o frecce direzionali sul piano XZ.
// Richiede: Rigidbody (IsKinematic=true, UseGravity=false) + Collider sul GameObject.
public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 4f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(h, 0f, v).normalized;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
}
