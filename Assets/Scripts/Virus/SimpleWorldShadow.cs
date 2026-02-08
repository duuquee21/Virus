using UnityEngine;

public class SimpleWorldShadow : MonoBehaviour
{
    [Header("Offset global (mundo)")]
    public Vector2 worldOffset = new Vector2(0.2f, -0.2f);

    [Header("Ajustes visuales")]
    public float scaleMultiplier = 1f;
    [Range(0f, 1f)] public float alpha = 0.5f;
    public Color shadowColor = Color.black;

    [Header("Capa de la Sombra")]
    [Tooltip("El valor exacto de Order in Layer que tendrá la sombra.")]
    public int shadowOrder = -1;

    private SpriteRenderer parentSR;
    private SpriteRenderer shadowSR;
    private GameObject shadowObj;

    void Start()
    {
        parentSR = GetComponent<SpriteRenderer>();

        if (parentSR != null)
        {
            shadowObj = new GameObject("GeneratedShadow");
            shadowObj.transform.parent = transform;

            shadowSR = shadowObj.AddComponent<SpriteRenderer>();
            shadowSR.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);

            // Asignamos el orden directamente al inicio
            shadowSR.sortingOrder = shadowOrder;
        }
    }

    void LateUpdate()
    {
        if (parentSR == null || shadowSR == null) return;

        // Sincronizar el sprite
        shadowSR.sprite = parentSR.sprite;
        shadowSR.flipX = parentSR.flipX;
        shadowSR.flipY = parentSR.flipY;

        // Forzar el orden manual en cada frame (por si lo cambias en el inspector en tiempo real)
        shadowSR.sortingOrder = shadowOrder;

        // Posicionamiento
        shadowObj.transform.position = (Vector2)transform.position + worldOffset;

        // Escala y rotación
        shadowObj.transform.localScale = transform.localScale * scaleMultiplier;
        shadowObj.transform.rotation = transform.rotation;
    }

    // Añade esto dentro de la clase SimpleWorldShadow
    public void CleanupShadow()
    {
        if (shadowObj != null)
        {
            DestroyImmediate(shadowObj);
            shadowObj = null;
        }
    }
}