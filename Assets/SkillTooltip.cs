using UnityEngine;
using TMPro;

public class SkillTooltip : MonoBehaviour
{
    public static SkillTooltip instance;

    public RectTransform tooltipRect;   // ← NUEVO
    public Vector2 offset = new Vector2(0, 100f); // Altura sobre el botón

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    void Awake()
    {
        instance = this;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        Hide();
    }


    public void Show(string title, string description, int cost, RectTransform target)
    {
        titleText.text = title;
        descriptionText.text = description;
        costText.text = "Coste: " + cost + " ADN Shiny";

        PositionAbove(target);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    void PositionAbove(RectTransform target)
    {
        tooltipRect.position = target.position;
        tooltipRect.anchoredPosition += offset;
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
