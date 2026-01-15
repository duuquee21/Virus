using UnityEngine;
using TMPro;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    public int totalInfected = 0;

    private void Awake()
    {
        //sigleton para que solo haya uno

        if (instance == null) instance = this;
        else Destroy(gameObject);

        //cargar datos al iniciar
        LoadData();
    }

    public void AddTotalData(int amount)
    {
        totalInfected += amount;
        SaveData();
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("TotalInfected", 0);
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
    }
}

