using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    [Header("Visual Radius (this defines capture zone)")]
    public Transform radiusVisual;

    // El radio REAL se calcula desde el tamaño del círculo visible
    public float GetRadius()
    {
        if (radiusVisual == null)
            return 0f;

        return radiusVisual.localScale.x / 2f;
    }
}
