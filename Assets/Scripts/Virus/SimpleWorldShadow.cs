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

    // Referencia al script de infección
    private PersonaInfeccion scriptInfeccion;

    void Start()
    {
        parentSR = GetComponent<SpriteRenderer>();
        // Buscamos el script Infeccion en este mismo objeto
        scriptInfeccion = GetComponent<PersonaInfeccion>();

        if (parentSR != null)
        {
            shadowObj = new GameObject("GeneratedShadow");
            shadowObj.transform.parent = transform;

            shadowSR = shadowObj.AddComponent<SpriteRenderer>();
            shadowSR.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);

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

        shadowSR.sortingOrder = shadowOrder;

        // Posicionamiento
        shadowObj.transform.position = (Vector2)transform.position + worldOffset;

        // --- LÓGICA DE INFECCIÓN ---
        float currentScale = scaleMultiplier;

        // Si existe el script y IsInfected es true, reducimos la escala a la mitad
        if (scriptInfeccion != null && scriptInfeccion.alreadyInfected)
        {
            currentScale *= 0.75f;
        }

        // Aplicar escala y rotación
        shadowObj.transform.localScale = transform.localScale * currentScale;
        shadowObj.transform.rotation = transform.rotation;
    }

    public void CleanupShadow()
    {
        if (shadowObj != null)
        {
            DestroyImmediate(shadowObj);
            shadowObj = null;
        }
    }
}