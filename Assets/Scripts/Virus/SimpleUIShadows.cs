using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GlobalUIShadow : MonoBehaviour
{
    [Header("Offset Global (Pixeles)")]
    [Tooltip("La sombra siempre irá en esta dirección, sin importar la rotación del objeto.")]
    public Vector2 globalOffset = new Vector2(10f, -10f);

    [Header("Ajustes Visuales")]
    public float scaleMultiplier = 1f;
    [Range(0f, 1f)] public float alpha = 0.5f;
    public Color shadowColor = Color.black;

    private Image parentImage;
    private Image shadowImage;
    private RectTransform shadowRect;
    private RectTransform parentRect;
    private GameObject shadowObj;

    void Start()
    {
        parentImage = GetComponent<Image>();
        parentRect = GetComponent<RectTransform>();

        if (parentImage != null)
        {
            shadowObj = new GameObject("GeneratedGlobalShadow");
            shadowObj.transform.SetParent(transform);
            shadowObj.transform.SetAsFirstSibling();

            shadowRect = shadowObj.AddComponent<RectTransform>();
            shadowImage = shadowObj.AddComponent<Image>();
            shadowImage.raycastTarget = false;
        }
    }

    void LateUpdate()
    {
        if (parentImage == null || shadowImage == null) return;

        // 1. Sincronizar visuales
        shadowImage.sprite = parentImage.sprite;
        shadowImage.type = parentImage.type;
        shadowImage.preserveAspect = parentImage.preserveAspect;
        shadowImage.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);

        // 2. Sincronizar dimensiones y pivote
        shadowRect.pivot = parentRect.pivot;
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = parentRect.sizeDelta;

        // 3. CALCULAR OFFSET GLOBAL
        // Convertimos el offset deseado a espacio local del padre para "anular" su rotación
        // Esto hace que si rotas el objeto, la sombra parezca quedarse quieta en el mundo
        Vector3 worldOffset = new Vector3(globalOffset.x, globalOffset.y, 0);

        // Invertimos la rotación del padre para que el desplazamiento siempre sea hacia la misma dirección visual
        shadowRect.anchoredPosition = Quaternion.Inverse(transform.rotation) * worldOffset;

        // 4. Sincronizar Rotación y Escala
        // La rotación local es identity para que herede la del padre exactamente
        shadowRect.localRotation = Quaternion.identity;
        shadowRect.localScale = Vector3.one * scaleMultiplier;
    }
}