using UnityEngine;
using TMPro;
using DG.Tweening;

public class TooltipManager : MonoBehaviour
{
    // Singleton para acceder desde cualquier botón sin arrastrar referencias
    public static TooltipManager Instance;

    [Header("Referencias UI")]
    [SerializeField] private CanvasGroup tooltipCanvasGroup; // Para hacer fade in/out
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Configuración")]
    [SerializeField] private Vector3 offset = new Vector3(0, 50, 0); // Altura sobre el botón

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ocultar al inicio
        tooltipCanvasGroup.alpha = 0;
        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        tooltipText.text = text;

        // Mover el tooltip encima del botón
        tooltipRect.position = position + offset;

        // Animación de aparición suave
        tooltipCanvasGroup.DOKill();
        tooltipCanvasGroup.DOFade(1, 0.2f);
    }

    public void HideTooltip()
    {
        tooltipCanvasGroup.DOKill();
        tooltipCanvasGroup.DOFade(0, 0.1f);
    }
}