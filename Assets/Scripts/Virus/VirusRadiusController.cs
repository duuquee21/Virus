using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    [Header("Configuraci�n de Radio")]
    public float baseScale = 1f;
    [SerializeField] float radiusIncrement = 0f; // Lo que se suma al multiplicador por nivel

    private int currentLevel = 1;

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

    // Suma un nivel (y por tanto +0.25 al multiplicador)
    public void UpgradeRadius()
    {
        currentLevel++;

        // sincronizamos con Guardado
        if (Guardado.instance != null)
        {
            Guardado.instance.AddRadiusMultiplier(radiusIncrement);
            Guardado.instance.radiusLevel = currentLevel;
            Guardado.instance.SaveData();
        }

        ApplyScale();
    }


    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplyScale();
    }

    public void ApplyScale()
    {
        // F�RMULA: Multiplicador base (1.0) + (niveles extra * incremento)
        float currentMultiplier = 1f + ((currentLevel - 1) * radiusIncrement);

        float shopRadius = baseScale * currentMultiplier;
        float skillMultiplier = (Guardado.instance != null) ? Guardado.instance.radiusMultiplier : 1f;
        float finalRadius = shopRadius * skillMultiplier;

        // --- Sincronizaci�n de Componentes ---
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null) collider.radius = finalRadius;

        RadiusLineRenderer line = GetComponentInChildren<RadiusLineRenderer>();
        if (line != null)
        {
            line.transform.localScale = Vector3.one;
            line.DrawCircle(finalRadius);
        }

        Transform visualArea = null;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "InfectionRadiusVisual") { visualArea = t; break; }
        }

        if (visualArea != null)
        {
            visualArea.gameObject.SetActive(true);
            visualArea.localPosition = new Vector3(0, 0, -0.05f);
            float diametro = finalRadius * 2f;
            visualArea.localScale = new Vector3(diametro, diametro, 1f);
        }
    }

    public int GetCurrentLevel() => currentLevel;

    // Ya no hay un "M�ximo" t�cnico, pero puedes poner uno si quieres
    public bool IsMaxLevel() => false;

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplyScale();
    }
}