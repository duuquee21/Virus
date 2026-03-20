using UnityEngine;

public class VirusRadiusController : MonoBehaviour
{
    public static VirusRadiusController instance;

    [Header("Configuraci�n de Radio")]
    public float baseScale = 1f;
  

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

    public void ApplyScale()
    {// 1. Calculamos el multiplicador basado estrictamente en el NIVEL de la tienda


        // 3. RESULTADO FINAL: Base * Multiplicador Tienda * Multiplicador Árbol
        currentFinalRadius = baseScale+Guardado.instance.radiusLevel*2f;

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

   
}