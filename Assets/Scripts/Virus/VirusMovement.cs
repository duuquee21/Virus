using UnityEngine;

public class VirusMovement : MonoBehaviour
{
    public static VirusMovement instance;

    [Header("Configuración")]
    public float baseMoveSpeed = 80f; // La velocidad que te da la tienda de monedas
    private float currentFinalSpeed; // La velocidad real aplicada (Base * Multiplicador Árbol)

    private Rigidbody2D rb;
    private Vector2 movementInput;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplySpeedMultiplier(); // Calcular al empezar
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        if (movementInput.magnitude > 0)
        {
            // Usamos currentFinalSpeed en lugar de moveSpeed directamente
            rb.AddForce(movementInput * currentFinalSpeed);
        }
    }

    // --- FUNCIÓN CLAVE ---
    public void ApplySpeedMultiplier()
    {
        float skillMultiplier = 1f;

        // Leemos el multiplicador del árbol (1.25, 1.5, etc.)
        if (Guardado.instance != null)
        {
            skillMultiplier = Guardado.instance.speedMultiplier;
        }

        currentFinalSpeed = baseMoveSpeed * skillMultiplier;

        Debug.Log($"Velocidad Actualizada: Base({baseMoveSpeed}) x Árbol({skillMultiplier}) = {currentFinalSpeed}");
    }

    // 👉 Usado por UpgradeManager (Tienda normal de monedas)
    public void SetSpeed(float newSpeed)
    {
        baseMoveSpeed = newSpeed;
        ApplySpeedMultiplier(); // Re-calculamos con el multiplicador del árbol aplicado
    }
}