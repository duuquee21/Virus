using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI - Vol�menes (Sliders de 0.0001 a 1)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("UI - Pantalla y Juego")]
    public Toggle fullscreenToggle;
    public Toggle shakeToggle;
    public TMP_Dropdown fpsDropdown; // Opciones: 30, 60, 120, Ilimitado

    [Header("UI - Idioma")]
    public TMP_Dropdown languageDropdown;

    private bool isReady = false;

    IEnumerator Start()
    {
        // 1. Esperamos a que el sistema de idiomas cargue primero (Obligatorio)
        // Wait for the language system to load first (Mandatory)
        yield return LocalizationSettings.InitializationOperation;

        // 2. Sincronizar la UI con los datos guardados en GameSettings
        // Sync the UI with the saved data in GameSettings
        if (GameSettings.instance != null)
        {
            if (masterSlider != null) masterSlider.value = GameSettings.instance.masterVol;
            if (musicSlider != null) musicSlider.value = GameSettings.instance.musicVol;
            if (sfxSlider != null) sfxSlider.value = GameSettings.instance.sfxVol;

            if (fullscreenToggle != null) fullscreenToggle.isOn = GameSettings.instance.isFullscreen;
            if (shakeToggle != null) shakeToggle.isOn = GameSettings.instance.shakeEnabled;

            if (fpsDropdown != null) fpsDropdown.value = GetFPSDropdownIndex(GameSettings.instance.targetFPS);
        }

        // 2.5. Asegurar que los Dropdowns TMP tengan su Template asignado (si se ha perdido en el prefab/escena)
        FixTMPDropdownTemplate(fpsDropdown);
        FixTMPDropdownTemplate(languageDropdown);

        // 3. Configurar el Dropdown de Idiomas autom�ticamente
        // Set up the Language Dropdown automatically
        ConfigurarDropdownIdiomas();

        // 4. Conectar la UI con el Cerebro (Listeners)
        // Connect the UI to the Brain (Listeners)
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(val => GameSettings.instance.SetMasterVolume(val));
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(val => GameSettings.instance.SetMusicVolume(val));
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(val => GameSettings.instance.SetSFXVolume(val));

        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(val => GameSettings.instance.SetFullscreen(val));
        if (shakeToggle != null) shakeToggle.onValueChanged.AddListener(val => GameSettings.instance.SetShake(val));
        if (fpsDropdown != null) fpsDropdown.onValueChanged.AddListener(SetFPSFromDropdown);

        isReady = true;
    }

    // --- L�GICA DE FPS ---
    int GetFPSDropdownIndex(int fps)
    {
        if (fps == 30) return 0;
        if (fps == 60) return 1;
        if (fps == 120) return 2;
        return 3; // Ilimitado / Unlimited
    }

    void SetFPSFromDropdown(int index)
    {
        int fps = 60;
        if (index == 0) fps = 30;
        else if (index == 1) fps = 60;
        else if (index == 2) fps = 120;
        else if (index == 3) fps = -1; // -1 en Unity significa ilimitado

        GameSettings.instance.SetFPS(fps);
    }

    // --- L�GICA DE IDIOMA (LOCALIZATION) ---
    void ConfigurarDropdownIdiomas()
    {
        if (languageDropdown == null) return;

        languageDropdown.ClearOptions();
        List<string> opciones = new List<string>();
        int indiceActual = 0;

        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[i];
            
            // Obtener el nombre del idioma
            string nombreIdioma = locale.Identifier.CultureInfo != null ? locale.Identifier.CultureInfo.NativeName : locale.name;
            
            // Capitalizar la primera letra
            if (!string.IsNullOrEmpty(nombreIdioma))
            {
                nombreIdioma = char.ToUpper(nombreIdioma[0]) + nombreIdioma.Substring(1);
            }
            
            opciones.Add(nombreIdioma);

            if (LocalizationSettings.SelectedLocale == locale)
                indiceActual = i;
        }

        languageDropdown.AddOptions(opciones);
        languageDropdown.value = indiceActual;
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.AddListener(CambiarIdioma);
    }

    void CambiarIdioma(int indice)
    {
        if (!isReady) return;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[indice];
    }

    void FixTMPDropdownTemplate(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        if (dropdown.template != null) return;

        // Busca un child llamado "Template" (es lo que crea el prefab por defecto)
        var template = dropdown.transform.Find("Template") as RectTransform;
        if (template == null)
        {
            foreach (var child in dropdown.GetComponentsInChildren<RectTransform>(true))
            {
                if (child.name.ToLower().Contains("template"))
                {
                    template = child;
                    break;
                }
            }
        }

        if (template != null)
        {
            dropdown.template = template;
            Debug.Log($"TMP Dropdown template asignado automáticamente para {dropdown.gameObject.name}", dropdown);
        }
    }
}