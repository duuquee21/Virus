using UnityEngine;
using UnityEngine.Localization.Settings; 
using System.Collections;

public class IdiomasPro : MonoBehaviour
{
    public void Spanish()
    {
        StartCoroutine(CambiarRutina("es"));
    }

    public void English()
    {
        StartCoroutine(CambiarRutina("en"));
    }

    IEnumerator CambiarRutina(string codigoIdioma)
    {
        yield return LocalizationSettings.InitializationOperation;

        //buscar idioma en la lista
        var idiomaDeseado = LocalizationSettings.AvailableLocales.GetLocale(codigoIdioma);

        if (idiomaDeseado != null)
        {
            LocalizationSettings.SelectedLocale = idiomaDeseado;
        }
    }
}
