using UnityEngine;

/// <summary>
/// Mostra la Terra (pianeta di partenza) e il pianeta scelto (destinazione) nella
/// scena del razzo. Legge la scelta salvata in GameManager.SelectedPlanet.
///
/// La Terra viene messa come un GRANDE pianeta proprio SOTTO il razzo, e si posiziona
/// da sola: calcola il proprio raggio e appoggia la superficie all'altezza del razzo
/// (nessun numero da indovinare). Regoli solo quanto grande con "Earth Ground Scale".
///
/// Uso: GameObject vuoto -> Add Component -> Mission Planets -> assegna i 4 prefab
/// (Earth + Mars/Saturn/Moon). Poi disattiva il vecchio "Plane" grigio.
/// </summary>
[DisallowMultipleComponent]
public class MissionPlanets : MonoBehaviour
{
    [Header("Prefab pianeti (da 'Planets of the Solar System 3D/Prefabs')")]
    public GameObject earthPrefab;
    public GameObject marsPrefab;
    public GameObject saturnPrefab;
    public GameObject moonPrefab;

    [Header("Terra come suolo di partenza (sotto il razzo)")]
    public bool  earthAsGround = true;
    [Tooltip("Quanto grande appare la Terra. Piu grande = piu piatta (piu 'suolo'); piu piccola = 'pianetino' curvo.")]
    public float earthGroundScale = 1.2f;
    [Tooltip("Altezza del suolo dove poggia il razzo (di solito 0).")]
    public float groundLevelY = 0f;
    [Tooltip("Nome dell'oggetto razzo, per centrarci la Terra sotto.")]
    public string rocketName = "Spaceship_root";

    [Header("Destinazione - lontana, in alto (dove va il razzo)")]
    public Vector3 destinationPosition = new Vector3(3f, 6f, 22f);
    public float   destinationScale = 2.5f;

    [Header("Rotazione dolce")]
    public float spinSpeed = 6f;

    void Start()
    {
        // TERRA (partenza)
        if (earthAsGround) SpawnEarthAsGround();
        else Spawn(earthPrefab, new Vector3(0f, -1f, 5f), earthGroundScale);

        // DESTINAZIONE in base alla scelta
        string planet = GameManager.Instance != null ? GameManager.Instance.SelectedPlanet : "Marte";
        GameObject destPrefab = planet switch
        {
            "Saturno" => saturnPrefab,
            "Luna"    => moonPrefab,
            _          => marsPrefab,
        };
        Spawn(destPrefab, destinationPosition, destinationScale);
    }

    // Terra grande sotto il razzo, con la superficie all'altezza del suolo
    void SpawnEarthAsGround()
    {
        var go = SpawnRaw(earthPrefab, earthGroundScale);
        if (go == null) return;

        // Dove sta il razzo (x,z), per centrarci sotto la Terra
        var rocket = GameObject.Find(rocketName);
        float rx = rocket != null ? rocket.transform.position.x : 0f;
        float rz = rocket != null ? rocket.transform.position.z : 3f;

        // Calcola il raggio reale e alza/abbassa la Terra cosi la sua CIMA
        // e all'altezza del suolo (il razzo ci poggia sopra).
        Bounds b = ComputeBounds(go);
        float offsetY = groundLevelY - b.max.y;
        go.transform.position = new Vector3(rx, go.transform.position.y + offsetY, rz);
    }

    void Spawn(GameObject prefab, Vector3 pos, float scale)
    {
        var go = SpawnRaw(prefab, scale);
        if (go != null) go.transform.position = pos;
    }

    // Instanzia + risolve il LOD + accende i renderer + rotazione
    GameObject SpawnRaw(GameObject prefab, float scale)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        go.transform.localScale = Vector3.one * scale;

        var lod = go.GetComponentInChildren<LODGroup>();
        if (lod != null) Destroy(lod);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        if (go.GetComponent<PlanetSpin>() == null)
            go.AddComponent<PlanetSpin>().speed = spinSpeed;

        return go;
    }

    Bounds ComputeBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }
}
