using UnityEngine;
using System.Collections.Generic;
using System;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager instance;
    private Dictionary<string, string> traducciones;
    public string idiomaActual = "es";

    public event Action OnLanguageChanged;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CargarIdioma(idiomaActual);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CargarIdioma(string codigoiIdioma)
    {
        idiomaActual = codigoiIdioma;
        traducciones = new Dictionary<string, string>();
        
        //cargar archivo desde la carpeta resources
        
        TextAsset archivoJson = Resources.Load<TextAsset>("textos_" + codigoiIdioma);

        if (archivoJson != null)
        {
            LocalizationData data =JsonUtility.FromJson<LocalizationData>(archivoJson.text);

            foreach (var item in data.items)
            {
                if (!traducciones.ContainsKey(item.key))
                {
                    traducciones.Add(item.key, item.value);
                }
            }
            Debug.Log ("Idioma cargado" + codigoiIdioma);
            OnLanguageChanged?.Invoke(); 

        }
        else
        {
            Debug.LogError("no encontrado el archiivo : extos_" + codigoiIdioma);
        }
    }

    public string GetTexto(string clave)
    {
        if (traducciones != null && traducciones.ContainsKey(clave))
        {
            return traducciones[clave];
        }

        return clave;
    }
}

//clases auxiliares de json 
[System.Serializable]
public class LocalizationData
{
    public LocalizationItem[] items;
}

[System.Serializable]
public class LocalizationItem
{
    public string key;
    public string value;
}