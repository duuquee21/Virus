using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum SkillEffectType
    {
        None,
        RandomInitialUpgrade,

        CoinsX2,
        CoinsX3,
        CoinsX4,
        CoinsX5,

        StartWith50Coins,
        StartWith100Coins,
        StartWith500Coins,
        StartWith2500Coins,
        StartWith25000Coins,
        StartWith50000Coins   // 🔥 NUEVO
    }

    [Header("Datos")]
    public string skillName;
    [TextArea] public string description;
    public int shinyCost = 1;

    [Header("Ramas")]
    public SkillNode[] nextNodes;

    [Header("Efecto")]
    public SkillEffectType effectType = SkillEffectType.None;

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
        ApplyEffect();

        SkillTreeLinesUI lines = FindObjectOfType<SkillTreeLinesUI>();
        RectTransform myRect = GetComponent<RectTransform>();

        if (transform.parent && lines)
        {
            RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
            if (parentRect)
                lines.Unlock(parentRect, myRect);
        }

        foreach (var node in nextNodes)
        {
            if (!node) continue;
            node.gameObject.SetActive(true);
            if (lines) lines.ShowFrom(myRect);
        }

        LevelManager.instance.UpdateUI();
    }

    void ApplyEffect()
    {
        switch (effectType)
        {
            case SkillEffectType.RandomInitialUpgrade:
                Guardado.instance.AssignRandomInitialUpgrade();
                break;

            case SkillEffectType.CoinsX2:
                Guardado.instance.SetCoinMultiplier(2);
                break;

            case SkillEffectType.CoinsX3:
                Guardado.instance.SetCoinMultiplier(3);
                break;

            case SkillEffectType.CoinsX4:
                Guardado.instance.SetCoinMultiplier(4);
                break;

            case SkillEffectType.CoinsX5:
                Guardado.instance.SetCoinMultiplier(5);
                break;

            case SkillEffectType.StartWith50Coins:
                Guardado.instance.SetStartingCoins(50);
                break;

            case SkillEffectType.StartWith100Coins:
                Guardado.instance.SetStartingCoins(100);
                break;

            case SkillEffectType.StartWith500Coins:
                Guardado.instance.SetStartingCoins(500);
                break;

            case SkillEffectType.StartWith2500Coins:
                Guardado.instance.SetStartingCoins(2500);
                break;

            case SkillEffectType.StartWith25000Coins:
                Guardado.instance.SetStartingCoins(25000);
                break;

            case SkillEffectType.StartWith50000Coins:
                Guardado.instance.SetStartingCoins(50000);
                break;
        }
    }

    void UnlockVisual()
    {
        button.interactable = false;
        button.image.color = Color.gray;
        if (lockIcon) lockIcon.SetActive(false);
    }

    void LockVisual()
    {
        button.interactable = true;
        button.image.color = Color.white;
        if (lockIcon) lockIcon.SetActive(false);

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
