using UnityEngine;
using TMPro;

public class SkillTooltip : MonoBehaviour
{
    public static SkillTooltip instance;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    void Awake()
    {
        instance = this;
        Hide();   // apagado inicial seguro
    }

    public void Show(string title, string description, int cost)
    {
        titleText.text = title;
        descriptionText.text = description;
        costText.text = "Coste: " + cost + " ADN Shiny";

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
