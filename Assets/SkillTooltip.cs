using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

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

    // 1. ¡Actualizado al nombre de tu nueva tabla!
    private string nombreTabla = "TextosUI";

    void Awake()
    {
        instance = this;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.blocksRaycasts = false;

        Hide();
    }

    // Función auxiliar para traducir solo el Coste y el ADN
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTabla, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si falla, devuelve la clave para que veas el error
        return op;
    }

    // 2. Modificado para recibir los textos ya traducidos desde SkillNode
    public void Show(string translatedTitle, string translatedDescription, int cost, RectTransform target)
    {
        // Como el título y descripción ya vienen en el idioma correcto, los ponemos directamente
        titleText.text = translatedTitle;
        descriptionText.text = translatedDescription;

        // Traducimos solo las palabras de la parte inferior (Coste, ADN) usando tu Excel
        string textoCoste = GetTexto("txt_coste");
        string textoAdn = GetTexto("txt_adn");
        costText.text = $"{textoCoste}: {cost} {textoAdn}";

        // Aplicamos tamaños
        titleText.fontSize = titleFontSize;
        descriptionText.fontSize = descriptionFontSize;
        costText.fontSize = costFontSize;

        // Posicionamiento
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