using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings; // <--- NECESARIO

public class SkillTooltip : MonoBehaviour
{
    public static SkillTooltip instance;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    // Asegúrate de que este sea el nombre EXACTO de tu archivo en Unity
    private string nombreTabla = "MisTextos";

    void Awake()
    {
        instance = this;
        Hide();
    }

    // Función auxiliar para traducir rápido
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTabla, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si falla, devuelve la clave para que veas el error
        return op;
    }

    // AHORA: titleKey y descriptionKey son las CLAVES del Excel (ej: "skill_fuego_titulo")
    public void Show(string titleKey, string descriptionKey, int cost)
    {
        // 1. Traducimos el Título y la Descripción
        titleText.text = GetTexto(titleKey);
        descriptionText.text = GetTexto(descriptionKey);

        // 2. Traducimos las partes fijas del coste
        // Necesitarás crear las claves "txt_coste" y "txt_adn" en el Excel
        string textoCoste = GetTexto("txt_coste");
        string textoAdn = GetTexto("txt_adn");

        costText.text = $"{textoCoste}: {cost} {textoAdn}";

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}