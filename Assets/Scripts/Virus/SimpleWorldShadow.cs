using UnityEngine;

public class SimpleWorldShadow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;   // Personaje

    [Header("Offset global (mundo)")]
    public Vector2 worldOffset = new Vector2(0.3f, -0.3f);

    [Header("Ajustes visuales")]
    public float scaleMultiplier = 1f;
    [Range(0f, 1f)] public float alpha = 0.5f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Posición global fija abajo-derecha
        transform.position = (Vector2)target.position + worldOffset;

        // Misma escala que el target (opcional)
        transform.localScale = target.localScale * scaleMultiplier;

        // Forzar rotación cero
        transform.rotation = Quaternion.identity;
    }

    void Start()
    {
        // Aplicar transparencia
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
