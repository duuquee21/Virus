using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    // Tu base sigue existiendo (100%)
    public float baseScale = 1f;

    // Progresión real de la tabla
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
        ApplyScale();
    }

    public void UpgradeRadius()
    {
        if (IsMaxLevel()) return;

        currentLevelIndex++;
        ApplyScale();
    }

    // BONUS PERMANENTE / SET DIRECTO
    public void SetLevel(int level)
    {
        currentLevelIndex = Mathf.Clamp(level - 1, 0, radiusMultipliers.Length - 1);
        ApplyScale();
    }

    void ApplyScale()
    {
        float newRadius = baseScale * radiusMultipliers[currentLevelIndex];

        CircleCollider2D collider = GetComponentInChildren<CircleCollider2D>();
        if (collider != null)
            collider.radius = newRadius;

        RadiusLineRenderer line = GetComponentInChildren<RadiusLineRenderer>();
        if (line != null)
            line.DrawCircle(newRadius);

        Transform redSprite = transform.Find("InfectionRadiusVisual");
        if (redSprite != null)
            redSprite.localScale = new Vector3(newRadius * 2f, newRadius * 2f, 1f);
    }

    public int GetCurrentLevel()
    {
        return currentLevelIndex + 1;
    }

    public bool IsMaxLevel()
    {
        return currentLevelIndex >= radiusMultipliers.Length - 1;
    }

    public void ResetUpgrade()
    {
        currentLevelIndex = 0;
        ApplyScale();
    }
}
