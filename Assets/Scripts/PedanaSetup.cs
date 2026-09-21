using UnityEngine;

public class PedanaSetup : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        mat.SetColor("_BaseColor", new Color(0.12f, 0.16f, 0.24f));
        mat.SetFloat("_Metallic", 0.8f);
        mat.SetFloat("_Smoothness", 0.7f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 0.08f, 0.20f));

        renderer.material = mat;
    }
}
