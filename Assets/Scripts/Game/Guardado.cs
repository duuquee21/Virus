using UnityEngine;
using TMPro;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    public int totalInfected = 0;
    public int shinyDNA = 0;

    private void Awake()
    {
        // Singleton: Si ya existe uno, me destruyo
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return; // Importante salir para no ejecutar lo de abajo
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadData();
    }

    public void AddShinyDNA(int amountShiny)
    {
        shinyDNA += amountShiny;
        SaveData();
        Debug.Log("Nuevo Shiny Total: " + shinyDNA); // Chivato para confirmar
    }

    public void AddTotalData(int amount)
    {
        totalInfected += amount;
        SaveData();
    }

    void SaveData()
    {
        // CORREGIDO: Ahora guardamos las VARIABLES, no un 0
        PlayerPrefs.SetInt("TotalInfected", totalInfected);
        PlayerPrefs.SetInt("TotalShinyDNA", shinyDNA);
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
        shinyDNA = PlayerPrefs.GetInt("TotalShinyDNA", 0);
    }
}