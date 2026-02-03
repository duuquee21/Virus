using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    public float baseScale = 1f;

    float[] radiusMultipliers = {
        1f,     // 100%
        1.2f,   // 120%
        1.5f,   // 150%
        2f,     // 200%
        2.5f,   // 250%
        3f      // 300% (FULL)
    };

    private int currentLevelIndex = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplyScale();
    }

    public void UpgradeRadius()
    {
        if (IsMaxLevel()) return;

        currentLevelIndex++;
        ApplyScale();
    }

    public void SetLevel(int level)
    {
        currentLevelIndex = Mathf.Clamp(level - 1, 0, radiusMultipliers.Length - 1);
        ApplyScale();
    }

    // Esta es la función que hace la magia
    public void ApplyScale()
    {
        // 1. Calculamos el radio base según el nivel de la tienda
        float shopRadius = baseScale * radiusMultipliers[currentLevelIndex];

        // 2. Aplicamos el multiplicador del Árbol de Habilidades (1.25, 1.5, etc.)
        // Si no tienes ninguna habilidad, el valor en Guardado debe ser 1f
        float skillMultiplier = 1f;
        if (Guardado.instance != null)
        {
            skillMultiplier = Guardado.instance.radiusMultiplier;
        }

        float finalRadius = shopRadius * skillMultiplier;

        // 3. Aplicamos el radio final a los componentes
        CircleCollider2D collider = GetComponentInChildren<CircleCollider2D>();
        if (collider != null)
            collider.radius = finalRadius;

        RadiusLineRenderer line = GetComponentInChildren<RadiusLineRenderer>();
        if (line != null)
            line.DrawCircle(finalRadius);

        Transform redSprite = transform.Find("InfectionRadiusVisual");
        if (redSprite != null)
            redSprite.localScale = new Vector3(finalRadius * 2f, finalRadius * 2f, 1f);

        Debug.Log($"Radio Actualizado: Base({shopRadius}) x Multiplicador({skillMultiplier}) = {finalRadius}");
    }

    public int GetCurrentLevel() => currentLevelIndex + 1;

    public bool IsMaxLevel() => currentLevelIndex >= radiusMultipliers.Length - 1;

    public void ResetUpgrade()
    {
        currentLevelIndex = 0;
        ApplyScale();
    }
}