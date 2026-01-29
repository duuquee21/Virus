using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos")]
    public string skillName;
    [TextArea] public string description;
    public int shinyCost = 1;

    [Header("Ramas")]
    public SkillNode[] nextNodes;

    [Header("UI")]
    public Button button;
    public GameObject lockIcon;

    bool unlocked;

    void Start()
    {
        unlocked = false;
        LockVisual();
    }
    public void TryUnlock()
    {
        if (unlocked) return;
        if (Guardado.instance.shinyDNA < shinyCost) return;

        Guardado.instance.shinyDNA -= shinyCost;
        unlocked = true;

        UnlockVisual();

        foreach (var node in nextNodes)
            if (node) node.gameObject.SetActive(true);

        SkillTreeLinesUI lines = FindObjectOfType<SkillTreeLinesUI>();
        if (lines)
        {
            lines.DrawConnections();
            lines.UnlockLine(transform as RectTransform);
        }

        LevelManager.instance.UpdateUI();
    }

    void UnlockVisual()
    {
        button.interactable = false;
        button.image.color = Color.gray;

        if (lockIcon)
            lockIcon.SetActive(false);
    }

    void LockVisual()
    {
        button.interactable = true;
        button.image.color = Color.white;

        if (lockIcon)
            lockIcon.SetActive(false);

        foreach (var node in nextNodes)
            if (node) node.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillTooltip.instance.Show(skillName, description, shinyCost);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillTooltip.instance.Hide();
    }
}
