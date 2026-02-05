using UnityEngine;

public class SimpleWorldShadow : MonoBehaviour
{
    [Header("Offset global (mundo)")]
    public Vector2 worldOffset = new Vector2(0.2f, -0.2f);

    [Header("Ajustes visuales")]
    public float scaleMultiplier = 1f;
    [Range(0f, 1f)] public float alpha = 0.5f;
    public Color shadowColor = Color.black;
    public string sortingLayerName = ""; // Opcional: para poner la sombra en una capa específica

    private SpriteRenderer parentSR;   // El render del personaje
    private SpriteRenderer shadowSR;   // El render de la sombra (creado por código)
    private GameObject shadowObj;

    void Start()
    {
        parentSR = GetComponent<SpriteRenderer>();

        if (parentSR != null)
        {
            // 1. Crear el objeto de la sombra como hijo
            shadowObj = new GameObject("GeneratedShadow");
            shadowObj.transform.parent = transform;

            // 2. Configurar el SpriteRenderer de la sombra
            shadowSR = shadowObj.AddComponent<SpriteRenderer>();
            shadowSR.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);

            // 3. Orden de dibujado (siempre uno por debajo del padre)
            if (!string.IsNullOrEmpty(sortingLayerName))
                shadowSR.sortingLayerName = sortingLayerName;

            shadowSR.sortingOrder = parentSR.sortingOrder - 1;
        }
    }

    void LateUpdate()
    {
        if (parentSR == null || shadowSR == null) return;

        // Sincronizar el sprite actual de la animación
        shadowSR.sprite = parentSR.sprite;
        shadowSR.flipX = parentSR.flipX;
        shadowSR.flipY = parentSR.flipY;

        // Mantener la sombra en posición global (ignorando rotación/escala relativa del padre si fuera necesario)
        // Aquí calculamos la posición sumando el offset a la posición actual del padre
        shadowObj.transform.position = (Vector2)transform.position + worldOffset;

        // Sincronizar escala y rotación
        shadowObj.transform.localScale = transform.localScale * scaleMultiplier;
        shadowObj.transform.rotation = transform.rotation;
    }
}