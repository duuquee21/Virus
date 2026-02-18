using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings; // <--- NECESARIO

public class SkillTooltip : MonoBehaviour
{
    public static SkillTooltip instance;

    public RectTransform rect;
    public Vector2 offset = new Vector2(0, 120f);

    [Header("Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    [Header("Font Sizes")]
    public float titleFontSize = 28f;
    public float descriptionFontSize = 22f;
    public float costFontSize = 24f;

    // Nombre de tu tabla en Unity
    private string nombreTabla = "MisTextos";

    void Awake()
    {
        instance = this;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.blocksRaycasts = false;

        Hide();
    }

    // --- FUNCIÓN AUXILIAR PARA TRADUCIR ---
    // (Esta función estaba mezclada dentro de Show, la sacamos fuera para que funcione bien)
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTabla, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si falla, devuelve la clave
        return op;
    }

    // --- FUNCIÓN SHOW CORREGIDA ---
    // Acepta las Keys y el RectTransform del botón
    public void Show(string titleKey, string descriptionKey, int cost, RectTransform target)
    {
        // 1. TRADUCCIÓN (Usamos la función auxiliar)
        titleText.text = GetTexto(titleKey);
        descriptionText.text = GetTexto(descriptionKey);

        // Traducimos el coste (Asegúrate de tener "txt_coste" y "txt_adn" en el Excel)
        string textoCoste = GetTexto("txt_coste");
        string textoAdn = GetTexto("txt_adn");
        costText.text = $"{textoCoste}: {cost} {textoAdn}";

        // 2. APLICAR TAMAÑOS (Tu código original)
        titleText.fontSize = titleFontSize;
        descriptionText.fontSize = descriptionFontSize;
        costText.fontSize = costFontSize;

        // 3. POSICIONAMIENTO (Tu código original)
        if (rect != null && target != null)
        {
            rect.position = target.position;
            rect.anchoredPosition += offset;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}