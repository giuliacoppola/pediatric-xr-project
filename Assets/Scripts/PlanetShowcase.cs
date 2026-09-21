using UnityEngine;

public class PlanetShowcase : MonoBehaviour
{
    public GameObject marteModel;
    public GameObject saturnoModel;
    public GameObject lunaModel;

    void Start()
    {
        NascondiTutti();
    }

    public void MostraPianeta(string nomePianeta)
    {
        NascondiTutti(); 

        if (nomePianeta == "Marte" && marteModel != null)
            marteModel.SetActive(true);
        else if (nomePianeta == "Saturno" && saturnoModel != null)
            saturnoModel.SetActive(true);
        else if (nomePianeta == "Luna" && lunaModel != null)
            lunaModel.SetActive(true);
    }

    private void NascondiTutti()
    {
        if (marteModel != null) marteModel.SetActive(false);
        if (saturnoModel != null) saturnoModel.SetActive(false);
        if (lunaModel != null) lunaModel.SetActive(false);
    }
}
