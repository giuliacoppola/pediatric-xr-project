using UnityEngine;

/// <summary>
/// Fa ruotare lentamente un modello di pianeta in anteprima, per dargli vita.
/// Aggiungilo al modello del pianeta (Marte, Saturno, Luna).
/// Di default ruota attorno all'asse Y (verticale), come la rotazione di un pianeta.
/// </summary>
public class PlanetSpin : MonoBehaviour
{
    [Tooltip("Gradi al secondo.")]
    public float speed = 12f;

    [Tooltip("Asse di rotazione (Y = verticale, come un pianeta).")]
    public Vector3 axis = Vector3.up;

    [Tooltip("Ruota nello spazio locale del modello.")]
    public bool localSpace = true;

    void Update()
    {
        transform.Rotate(axis.normalized * speed * Time.deltaTime,
                         localSpace ? Space.Self : Space.World);
    }
}
