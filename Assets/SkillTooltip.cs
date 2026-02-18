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

    void Awake()
    {
        instance = this;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.blocksRaycasts = false;

        Hide();
    }

    public void Show(string title, string description, int cost, RectTransform target)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTabla, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si falla, devuelve la clave para que veas el error
        return op;
    }

        // Aplicar tamaños
        titleText.fontSize = titleFontSize;
        descriptionText.fontSize = descriptionFontSize;
        costText.fontSize = costFontSize;

        rect.position = target.position;
        rect.anchoredPosition += offset;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}