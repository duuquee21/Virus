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
        float newScale = baseScale + (currentLevel - 1) * scaleStep;
        transform.localScale = new Vector3(newScale, newScale, 1f);
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
