using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings; // Necesario para el sistema de idiomas

public class ControlIdioma : MonoBehaviour
{
    [Header("Referencia al nuevo Selector")]
    public SelectorHorizontalUI selectorIdioma;

    private bool cambiandoIdioma = false;

    IEnumerator Start()
    {
        // 1. Esperamos a que Unity cargue los diccionarios de idiomas al arrancar
        yield return LocalizationSettings.InitializationOperation;

        // 2. Averiguamos qué idioma está puesto ahora mismo (0 = Inglés, 1 = Español, etc...)
        var idiomaActual = LocalizationSettings.SelectedLocale;
        int indiceGuardado = LocalizationSettings.AvailableLocales.Locales.IndexOf(idiomaActual);

        // Si por algún motivo da error, por defecto ponemos el 0
        if (indiceGuardado < 0) indiceGuardado = 0;

        // 3. Sincronizamos la ruleta visual con el idioma real
        if (selectorIdioma != null)
        {
            selectorIdioma.EstablecerIndice(indiceGuardado);
        }
    }

    // Esta función la llamará la ruleta cuando le des a Izquierda/Derecha
    public void CambiarIdioma(int indice)
    {
        if (cambiandoIdioma) return; // Evita que el jugador cambie 5 veces por segundo y cuelgue el juego

        StartCoroutine(RutinaCambiarIdioma(indice));
    }

    IEnumerator RutinaCambiarIdioma(int indice)
    {
        cambiandoIdioma = true;

        // Esperamos por si acaso el sistema está ocupado
        yield return LocalizationSettings.InitializationOperation;

        // Cambiamos el idioma internamente
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[indice];

        Debug.Log($"<color=cyan>[Ajustes]</color> Idioma cambiado a: {LocalizationSettings.SelectedLocale.Identifier.Code}");

        cambiandoIdioma = false;
    }
}