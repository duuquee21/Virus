using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el panel de ajustes (audio, FPS, pantalla completa e idioma).
/// Conecta los sliders/toggles/dropdowns desde el Inspector.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Audio")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video")]
    public TMP_Dropdown fpsDropdown;
    public Toggle fullscreenToggle;

    [Header("Feedback")]
    public TMP_Text fpsCounterText;

    [Header("Idioma")]
    public TMP_Dropdown languageDropdown;

    // Valores posibles para el dropdown de FPS
    private const string FpsPreferenceKey = "FPSLimit";
    private readonly int[] fpsOptions = new int[] { 30, 60, 120, 0 };
    private readonly string[] fpsOptionLabels = new string[] { "30 FPS", "60 FPS", "120 FPS", "Sin límite" };

    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        SetupUI();

        // Enforce FPS limit and show counter (si está asignado).
        StartCoroutine(UpdateFpsCounter());
    }

    private void SetupUI()
    {
        StartCoroutine(SetupUICoroutine());
    }

    private IEnumerator SetupUICoroutine()
    {
        // En algunos casos la inicialización de Localization puede tardar, así que garantizamos que esté lista.
        yield return LocalizationSettings.InitializationOperation;

        // Audio
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (musicVolumeSlider != null) musicVolumeSlider.value = music;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;

        ApplyMasterVolume(master);
        ApplyMusicVolume(music);
        ApplySfxVolume(sfx);

        // FPS
        if (fpsDropdown != null)
        {
            fpsDropdown.ClearOptions();
            fpsDropdown.AddOptions(new List<string>(fpsOptionLabels));

            int savedFps = PlayerPrefs.GetInt(FpsPreferenceKey, 60);
            int idx = System.Array.IndexOf(fpsOptions, savedFps);
            if (idx < 0) idx = 1; // 60 FPS por defecto
            fpsDropdown.value = idx;
            fpsDropdown.RefreshShownValue();

            // Aseguramos que el dropdown siempre llame al método correcto cuando se cambia.
            fpsDropdown.onValueChanged.RemoveAllListeners();
            fpsDropdown.onValueChanged.AddListener(OnFpsDropdownChanged);

            ApplyTargetFrameRate(savedFps);
        }

        // Pantalla completa
        bool savedFullscreen = PlayerPrefs.GetInt("FullScreen", Screen.fullScreen ? 1 : 0) == 1;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = savedFullscreen;
            ApplyFullscreen(savedFullscreen);
        }

        // Idioma
        if (languageDropdown != null)
        {
            PopulateLanguageDropdown();
        }
    }

    /// <summary>
    /// Muestra el panel de ajustes y refresca los valores.
    /// </summary>
    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        SetupUI();
    }

    /// <summary>
    /// Oculta el panel de ajustes.
    /// </summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ======== EVENTOS UI ========

    public void OnMasterVolumeChanged(float value) => ApplyMasterVolume(value);
    public void OnMusicVolumeChanged(float value) => ApplyMusicVolume(value);
    public void OnSfxVolumeChanged(float value) => ApplySfxVolume(value);

    public void OnFpsDropdownChanged(int index)
    {
        if (index < 0 || index >= fpsOptions.Length) return;
        ApplyTargetFrameRate(fpsOptions[index]);
    }

    public void OnFullscreenToggleChanged(bool isFull) => ApplyFullscreen(isFull);

    public void OnLanguageDropdownChanged(int index)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (index < 0 || index >= locales.Count) return;

        var locale = locales[index];
        if (locale == null) return;

        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString("SelectedLocale", locale.Identifier.Code);
        PlayerPrefs.Save();

        // Actualiza inmediatamente los textos que usan LocalizedStringEvent.
        RefreshLocalizedText();
    }

    // ======== APLICAR VALORES ========

    private void ApplyMasterVolume(float value)
    {
        AudioListener.volume = value;
        if (AudioManager.instance != null)
            AudioManager.instance.UpdateMixerVolume("Master", value);

        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float value)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.UpdateMixerVolume("MusicVol", value);

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplySfxVolume(float value)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.UpdateMixerVolume("SFXVol", value);

        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplyTargetFrameRate(int fps)
    {
        // Desactivamos VSync para que el targetFrameRate tenga efecto.
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = fps;
        PlayerPrefs.SetInt(FpsPreferenceKey, fps);
        PlayerPrefs.Save();
    }

    private IEnumerator UpdateFpsCounter()
    {
        const float updateInterval = 0.25f;
        float timer = 0f;
        int frames = 0;

        while (true)
        {
            yield return null;
            frames++;
            timer += Time.unscaledDeltaTime;

            if (timer >= updateInterval)
            {
                // Force the FPS limit to match the saved setting (evita que otro script lo sobreescriba).
                EnforceTargetFrameRate();

                float fps = frames / timer;
                string targetLabel = Application.targetFrameRate <= 0 ? "unlimited" : Application.targetFrameRate.ToString();
                if (fpsCounterText != null)
                {
                    fpsCounterText.text = $"FPS: {fps:0} (Tgt: {targetLabel})";
                }
                frames = 0;
                timer = 0f;
            }
        }
    }

    private void EnforceTargetFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        int savedFps = PlayerPrefs.GetInt(FpsPreferenceKey, 60);
        if (Application.targetFrameRate != savedFps)
        {
            Application.targetFrameRate = savedFps;
        }
    }

    private void LateUpdate()
    {
        // Aseguramos que ningún otro script cambie el límite de FPS en Update.
        EnforceTargetFrameRate();
    }

    private void ApplyFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("FullScreen", isFull ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void PopulateLanguageDropdown()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        languageDropdown.ClearOptions();

        var options = new List<string>(locales.Count);
        int selectedIndex = 0;

        string savedCode = PlayerPrefs.GetString("SelectedLocale", "");
        for (int i = 0; i < locales.Count; i++)
        {
            var locale = locales[i];
            options.Add(locale.LocaleName);
            if (!string.IsNullOrEmpty(savedCode) && locale.Identifier.Code == savedCode)
                selectedIndex = i;
        }

        languageDropdown.AddOptions(options);
        languageDropdown.value = selectedIndex;
        languageDropdown.RefreshShownValue();

        // Aseguramos que el dropdown siempre llame al método correcto cuando se cambia.
        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);

        if (locales.Count > 0)
        {
            LocalizationSettings.SelectedLocale = locales[selectedIndex];

            // Si ya hay un idioma guardado, forzamos una actualización rápida.
            RefreshLocalizedText();
        }
    }

    private void RefreshLocalizedText()
    {
        // Forzamos actualización de textos que usan LocalizedStringEvent (si existen)
        var allComponents = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var comp in allComponents)
        {
            var type = comp.GetType();
            if (type.Name != "LocalizedStringEvent") continue;

            var method = type.GetMethod("RefreshString", BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(comp, null);
        }
    }
}

