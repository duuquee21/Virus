using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class SelectorIdioma : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        var opciones = new List<string>();
        int indiceSeleccionado = 0;

        var idiomasDisponibles = LocalizationSettings.AvailableLocales.Locales;

        for (int i = 0; i < idiomasDisponibles.Count; i++)
        {
            var idioma = idiomasDisponibles[i];

            string nombreIdioma = idioma.Identifier.CultureInfo.NativeName;

            //la primera salga en mayuscula
            if(nombreIdioma.Length >0)
            {
                nombreIdioma = char.ToUpper(nombreIdioma[0]) + nombreIdioma.Substring(1) ;

            }

            opciones.Add(nombreIdioma);

            if(LocalizationSettings.SelectedLocale == idioma)
            {
                indiceSeleccionado = i;
            }
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(opciones);
        dropdown.value = indiceSeleccionado;
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(CambiarIdioma);
    }

    public void CambiarIdioma (int indice)
    {
        StartCoroutine(SetIdioma(indice));
    }

    IEnumerator SetIdioma(int indice)
    {
        yield return LocalizationSettings.InitializationOperation;

        // Cambiamos al idioma seleccionado
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[indice];
    }
}
