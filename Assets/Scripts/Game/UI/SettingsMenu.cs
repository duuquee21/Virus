using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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

    // Control de navegación con mando
    private int currentSelectedIndex = 0;
    private UIElement[] uiElements;
    private bool hasSelectedElement = false;
    private float lastInputTime = 0f;
    private const float inputCooldown = 0.25f;

    // Definimos los elementos UI en orden
    private enum UIElementType { Slider, Toggle, Dropdown }
    private struct UIElement
    {
        public UIElementType type;
        public Slider slider;
        public Toggle toggle;
        public TMP_Dropdown dropdown;
        public string name;

        public UIElement(Slider s, string n) { type = UIElementType.Slider; slider = s; toggle = null; dropdown = null; name = n; }
        public UIElement(Toggle t, string n) { type = UIElementType.Toggle; slider = null; toggle = t; dropdown = null; name = n; }
        public UIElement(TMP_Dropdown d, string n) { type = UIElementType.Dropdown; slider = null; toggle = null; dropdown = d; name = n; }
    }

    // Valores posibles para el dropdown de FPS
    private const string FpsPreferenceKey = "FPSLimit";
    private readonly int[] fpsOptions = new int[] { 30, 60, 120, 0 };
    private readonly string[] fpsOptionLabels = new string[] { "30 FPS", "60 FPS", "120 FPS", "Sin límite" };

    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        SetupUI();

        // Inicializar elementos UI para navegación con mando
        InitializeUIElements();

        // Enforce FPS limit and show counter (si está asignado).
        StartCoroutine(UpdateFpsCounter());
    }

    private void InitializeUIElements()
    {
        var elements = new List<UIElement>();
        if (masterVolumeSlider != null) elements.Add(new UIElement(masterVolumeSlider, "Master Volume"));
        if (musicVolumeSlider != null) elements.Add(new UIElement(musicVolumeSlider, "Music Volume"));
        if (sfxVolumeSlider != null) elements.Add(new UIElement(sfxVolumeSlider, "SFX Volume"));
        if (fpsDropdown != null) elements.Add(new UIElement(fpsDropdown, "FPS"));
        if (fullscreenToggle != null) elements.Add(new UIElement(fullscreenToggle, "Fullscreen"));
        if (languageDropdown != null) elements.Add(new UIElement(languageDropdown, "Language"));
        uiElements = elements.ToArray();
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

        // Asegura que el primer elemento quede seleccionado después del setup.
        yield return null; // espera un frame para que la UI esté lista
        if (uiElements != null && uiElements.Length > 0)
        {
            currentSelectedIndex = 0;
            hasSelectedElement = true;
            SelectCurrentElement();
        }

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

        hasSelectedElement = false; // reset para garantizar inicialización en coroutine
        SetupUI();
    }

    /// <summary>
    /// Oculta el panel de ajustes.
    /// </summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        // Deseleccionar
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
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

        // Forzar primer elemento seleccionado si no hay foco y el panel está abierto.
        if (panel != null && panel.activeSelf && !hasSelectedElement && uiElements != null && uiElements.Length > 0)
        {
            currentSelectedIndex = 0;
            hasSelectedElement = true;
            SelectCurrentElement();
        }

        // Manejar entrada de mando para navegación en menú
        if (panel != null && panel.activeSelf && uiElements != null && uiElements.Length > 0)
        {
            HandleControllerInput();
        }
    }

    private void HandleControllerInput()
    {
        if (panel == null || !panel.activeSelf || uiElements == null || uiElements.Length == 0) return;

        // Si no hay selección activa, forzamos seleccionar el elemento actual.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (!hasSelectedElement)
            {
                hasSelectedElement = true;
                currentSelectedIndex = Mathf.Clamp(currentSelectedIndex, 0, uiElements.Length - 1);
                SelectCurrentElement();
                return;
            }
        }

        if (Time.time - lastInputTime < inputCooldown) return;

        // Navegación vertical con stick izquierdo (estándar de industria)
        float vertical = Input.GetAxis("Vertical"); // Stick izquierdo vertical
        if (Mathf.Abs(vertical) > 0.5f)
        {
            if (vertical > 0) // Arriba
            {
                currentSelectedIndex = (currentSelectedIndex - 1 + uiElements.Length) % uiElements.Length;
                hasSelectedElement = true;
                SelectCurrentElement();
                lastInputTime = Time.time;
                return;
            }
            else // Abajo
            {
                currentSelectedIndex = (currentSelectedIndex + 1) % uiElements.Length;
                hasSelectedElement = true;
                SelectCurrentElement();
                lastInputTime = Time.time;
                return;
            }
        }

        // Ajuste con stick izquierdo horizontal
        float horizontal = Input.GetAxis("Horizontal");
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            AdjustCurrentElement(horizontal);
            lastInputTime = Time.time;
        }

        // Botón A (joystick button 0) para activar/select (estándar Xbox)
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            ActivateCurrentElement();
            lastInputTime = Time.time;
        }
    }

    private void SelectCurrentElement()
    {
        if (uiElements == null || uiElements.Length == 0 || EventSystem.current == null) return;

        var element = uiElements[currentSelectedIndex];
        GameObject obj = null;
        if (element.slider != null) obj = element.slider.gameObject;
        else if (element.toggle != null) obj = element.toggle.gameObject;
        else if (element.dropdown != null) obj = element.dropdown.gameObject;

        if (obj != null)
        {
            EventSystem.current.SetSelectedGameObject(obj);

            // Forzar resaltado de Unity
            var selectable = obj.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnSelect(new BaseEventData(EventSystem.current));
            }
        }
    }

    private void AdjustCurrentElement(float direction)
    {
        var element = uiElements[currentSelectedIndex];
        switch (element.type)
        {
            case UIElementType.Slider:
                if (element.slider != null)
                {
                    element.slider.value += direction * 0.01f; // Ajuste fino
                    element.slider.value = Mathf.Clamp01(element.slider.value);
                    // Trigger the change event
                    if (element.slider == masterVolumeSlider) OnMasterVolumeChanged(element.slider.value);
                    else if (element.slider == musicVolumeSlider) OnMusicVolumeChanged(element.slider.value);
                    else if (element.slider == sfxVolumeSlider) OnSfxVolumeChanged(element.slider.value);
                }
                break;
            case UIElementType.Toggle:
                if (direction > 0.5f) // Derecha para toggle
                {
                    if (element.toggle != null)
                    {
                        element.toggle.isOn = !element.toggle.isOn;
                        if (element.toggle == fullscreenToggle) OnFullscreenToggleChanged(element.toggle.isOn);
                    }
                }
                break;
            case UIElementType.Dropdown:
                if (direction > 0.5f) // Derecha para siguiente opción
                {
                    if (element.dropdown != null)
                    {
                        int newValue = (element.dropdown.value + 1) % element.dropdown.options.Count;
                        element.dropdown.value = newValue;
                        element.dropdown.RefreshShownValue();
                        if (element.dropdown == fpsDropdown) OnFpsDropdownChanged(newValue);
                        else if (element.dropdown == languageDropdown) OnLanguageDropdownChanged(newValue);
                    }
                }
                else if (direction < -0.5f) // Izquierda para anterior
                {
                    if (element.dropdown != null)
                    {
                        int newValue = (element.dropdown.value - 1 + element.dropdown.options.Count) % element.dropdown.options.Count;
                        element.dropdown.value = newValue;
                        element.dropdown.RefreshShownValue();
                        if (element.dropdown == fpsDropdown) OnFpsDropdownChanged(newValue);
                        else if (element.dropdown == languageDropdown) OnLanguageDropdownChanged(newValue);
                    }
                }
                break;
        }
    }

    private void ActivateCurrentElement()
    {
        var element = uiElements[currentSelectedIndex];
        if (element.type == UIElementType.Toggle && element.toggle != null)
        {
            element.toggle.isOn = !element.toggle.isOn;
            if (element.toggle == fullscreenToggle) OnFullscreenToggleChanged(element.toggle.isOn);
        }
        else if (element.type == UIElementType.Dropdown && element.dropdown != null)
        {
            // Abrir el dropdown
            element.dropdown.Show();
        }
        // Para sliders, no hay activación especial
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

