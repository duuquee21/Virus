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

        // 2. Averiguamos qu� idioma est� puesto ahora mismo (0 = Ingl�s, 1 = Espa�ol, etc...)
        var idiomaActual = LocalizationSettings.SelectedLocale;
        int indiceGuardado = LocalizationSettings.AvailableLocales.Locales.IndexOf(idiomaActual);

        // Si por alg�n motivo da error, por defecto ponemos el 0
        if (indiceGuardado < 0) indiceGuardado = 0;

        // 3. Sincronizamos la ruleta visual con el idioma real
        if (selectorIdioma != null)
        {
            selectorIdioma.EstablecerIndice(indiceGuardado);
        }
    }

    // Esta funci�n la llamar� la ruleta cuando le des a Izquierda/Derecha
    public void CambiarIdioma(int indice)
    {
        if (cambiandoIdioma) return; // Evita que el jugador cambie 5 veces por segundo y cuelgue el juego

        StartCoroutine(RutinaCambiarIdioma(indice));
    }

    IEnumerator RutinaCambiarIdioma(int indice)
    {
        cambiandoIdioma = true;

        // Esperamos por si acaso el sistema est� ocupado
        yield return LocalizationSettings.InitializationOperation;

        // Cambiamos el idioma internamente
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[indice];

        Debug.Log($"<color=cyan>[Ajustes]</color> Idioma cambiado a: {LocalizationSettings.SelectedLocale.Identifier.Code}");

        // Refuerza la sincronización visual tras el cambio
        if (selectorIdioma != null)
        {
            var idiomaActual = LocalizationSettings.SelectedLocale;
            int indiceReal = LocalizationSettings.AvailableLocales.Locales.IndexOf(idiomaActual);
            if (indiceReal < 0) indiceReal = 0;
            selectorIdioma.EstablecerIndice(indiceReal);
        }

        cambiandoIdioma = false;
    }
}