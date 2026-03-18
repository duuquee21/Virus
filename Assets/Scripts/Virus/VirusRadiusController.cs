using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    [Header("Configuraci�n de Radio")]
    public float baseScale = 1f;
    [SerializeField] float radiusIncrement = 0f; // Lo que se suma al multiplicador por nivel

    private int currentLevel = 1;

    private float currentFinalRadius; // Variable para guardar el valor calculado
    public float CurrentFinalRadius => currentFinalRadius; // Propiedad de solo lectura

    private int bonusLevel = 0;

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
        // 1. Cargamos el nivel real que está guardado en PlayerPrefs
        if (Guardado.instance != null)
        {
            currentLevel = Guardado.instance.radiusLevel;
        }

        ApplyScale();
    }

    // Suma un nivel (y por tanto +0.25 al multiplicador)
    public void UpgradeRadius()
    {
        currentLevel++;
        if (Guardado.instance != null)
        {
            // Solo guardamos el nivel. El nodo de MultiplyRadius125 ya usa el AddRadiusMultiplier aparte.
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

    public void ApplyRoundBonus()
    {
        bonusLevel++;
        ApplyScale();
    }

    public void ApplyScale()
    {// 1. Calculamos el multiplicador basado estrictamente en el NIVEL de la tienda
     // Si nivel es 1 y increment es 0.25 -> mult = 1 + (0 * 0.25) = 1.0
        float shopMultiplier = 1f + ((currentLevel + bonusLevel - 1) * radiusIncrement);

        // 2. Obtenemos el multiplicador PERMANENTE del árbol de habilidades
        // Aseguramos que si Guardado no existe, sea 1 (no afecta)
        float skillTreeMultiplier = (Guardado.instance != null) ? Guardado.instance.radiusMultiplier : 1f;

        // 3. RESULTADO FINAL: Base * Multiplicador Tienda * Multiplicador Árbol
        currentFinalRadius = baseScale * shopMultiplier * skillTreeMultiplier;

        // --- Aplicación física ---
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null) collider.radius = currentFinalRadius;


        RadiusLineRenderer line = GetComponentInChildren<RadiusLineRenderer>();
        if (line != null)
        {
            line.transform.localScale = Vector3.one;
            line.DrawCircle(currentFinalRadius);
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
            float diametro = currentFinalRadius * 2f;
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