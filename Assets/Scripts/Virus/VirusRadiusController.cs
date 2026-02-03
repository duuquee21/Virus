using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    [Header("Configuración Base")]
    public float baseScale = 1f;

    // Multiplicadores según el nivel de la tienda de monedas
    private float[] radiusMultipliers = {
        1f,     // Nivel 1 (100%)
        1.2f,   // Nivel 2 (120%)
        1.5f,   // Nivel 3 (150%)
        2f,     // Nivel 4 (200%)
        2.5f,   // Nivel 5 (250%)
        3f      // Nivel 6 (300% FULL)
    };

    private int currentLevelIndex = 0;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

    public void ApplyScale()
    {
        // 1. Cálculo del radio final (Tienda + Habilidades)
        float shopRadius = baseScale * radiusMultipliers[currentLevelIndex];
        float skillMultiplier = (Guardado.instance != null) ? Guardado.instance.radiusMultiplier : 1f;
        float finalRadius = shopRadius * skillMultiplier;

        // 2. SINCRONIZAR FÍSICA (El círculo verde de la imagen c523a1)
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = finalRadius; // El collider ahora mide lo mismo que el radio visual
        }

        // 3. SINCRONIZAR BORDE (LineRenderer)
        RadiusLineRenderer line = GetComponentInChildren<RadiusLineRenderer>();
        if (line != null)
        {
            // Forzamos que el objeto de la línea no tenga escala propia para evitar que se vea más grande
            line.transform.localScale = Vector3.one;
            line.DrawCircle(finalRadius);
        }

        // 4. SINCRONIZAR RELLENO (Círculo rosa)
        Transform visualArea = null;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "InfectionRadiusVisual")
            {
                visualArea = t;
                break;
            }
        }

        if (visualArea != null)
        {
            visualArea.gameObject.SetActive(true);
            visualArea.localPosition = new Vector3(0, 0, -0.05f);

            // Diámetro = radio * 2. Con escala 1 en el padre, esto encaja perfecto con el collider.
            float diametro = finalRadius * 2f;
            visualArea.localScale = new Vector3(diametro, diametro, 1f);
        }
    }
    public int GetCurrentLevel() => currentLevelIndex + 1;
    public bool IsMaxLevel() => currentLevelIndex >= radiusMultipliers.Length - 1;

    public void ResetUpgrade()
    {
        currentLevelIndex = 0;
        ApplyScale();
    }
}