using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    public float baseScale = 1f;
    public float scaleStep = 0.5f;

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
        ApplyScale();
    }

    public void UpgradeRadius()
    {
        currentLevel++;
        ApplyScale();
    }

    void ApplyScale()
    {
        float newRadius = baseScale + (currentLevel - 1) * scaleStep;

        // Collider real de contagio
        CircleCollider2D collider = GetComponentInChildren<CircleCollider2D>();
        if (collider != null)
            collider.radius = newRadius;

        // Aro visual
        RadiusLineRenderer line = GetComponentInChildren<RadiusLineRenderer>();
        if (line != null)
            line.DrawCircle(newRadius);

        // (opcional) sprite rojo si aún lo usas
        Transform redSprite = transform.Find("InfectionRadiusVisual");
        if (redSprite != null)
            redSprite.localScale = new Vector3(newRadius * 2f, newRadius * 2f, 1f);
    }



    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplyScale();
    }
}
